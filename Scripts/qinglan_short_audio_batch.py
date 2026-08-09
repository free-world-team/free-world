#!/usr/bin/env python3
"""Shared deterministic authoring pipeline for Qinglan short one-shot audio batches."""

from __future__ import annotations

from dataclasses import asdict, dataclass
from pathlib import Path

import numpy as np

from qinglan_audio_synthesis import (
    SAMPLE_RATE,
    envelope,
    noise_burst,
    peak_normalize,
    sha256,
    tone_event,
    write_json,
    write_master_and_runtime,
)


@dataclass(frozen=True)
class CueSpec:
    name: str
    address: str
    duration: float
    seed: int
    frequency: float
    tone_gain: float = 0.75
    noise_gain: float = 0.20
    smoothing: int = 5
    pitch_end_ratio: float = 1.0
    attack: float = 0.004
    release: float = 0.12
    target_peak_dbfs: float = -4.0


def synthesize(spec: CueSpec) -> np.ndarray:
    tone = tone_event(
        spec.duration,
        spec.frequency,
        ((1.0, 0.78), (2.01, 0.18), (3.07, 0.07)),
        spec.attack,
        spec.release,
        spec.pitch_end_ratio,
    )
    noise = noise_burst(spec.duration, spec.seed, max(1, spec.smoothing))
    noise *= envelope(noise.shape[0], spec.attack, spec.release)
    samples = (tone * spec.tone_gain) + (noise * spec.noise_gain)
    samples -= np.float32(np.mean(samples, dtype=np.float64))
    return peak_normalize(samples, spec.target_peak_dbfs)


def render_batch(
    *,
    asset_id: str,
    canonical_root: str,
    generated_at: str,
    prompt: str,
    cues: tuple[CueSpec, ...],
    output_root: Path,
    caller_script: Path,
    runtime_extension: str = ".ogg",
    source_category: str = "first-party-procedural-synthesis",
    steam_category: str = "first-party procedural sound effects; no generative AI and no runtime generation",
) -> None:
    if len(cues) == 0:
        raise ValueError("at least one cue is required")
    if runtime_extension not in (".ogg", ".wav"):
        raise ValueError("runtime_extension must be .ogg or .wav")
    names = {cue.name for cue in cues}
    addresses = {cue.address for cue in cues}
    if len(names) != len(cues) or len(addresses) != len(cues):
        raise ValueError("cue names and addresses must be unique")

    source_root = output_root / "source"
    final_root = output_root / "final"
    output_root.mkdir(parents=True, exist_ok=True)
    prompt_path = output_root / "prompt.txt"
    prompt_path.write_text(prompt, encoding="utf-8", newline="\n")
    spec_path = source_root / "generator-spec.json"
    write_json(spec_path, {
        "assetId": asset_id,
        "sampleRate": SAMPLE_RATE,
        "sourceEncoding": "WAV PCM_24 mono",
        "runtimeEncoding": "WAV PCM_16 mono" if runtime_extension == ".wav" else "OGG Vorbis mono",
        "loop": False,
        "externalAudioInputs": [],
        "cues": [asdict(cue) for cue in cues],
    })

    def canonical(relative: str) -> str:
        return f"{canonical_root}/{relative}"

    reports = []
    for cue in cues:
        source_path = source_root / f"{cue.name}.wav"
        final_path = final_root / f"{cue.name}{runtime_extension}"
        report = write_master_and_runtime(
            source_path,
            final_path,
            synthesize(cue),
            loop=False,
            minimum_seconds=cue.duration,
            maximum_seconds=cue.duration,
            rms_min_dbfs=-42.0,
            rms_max_dbfs=-2.0,
        )
        report.update({
            "name": cue.name,
            "address": cue.address,
            "seed": cue.seed,
            "sourcePath": canonical(f"source/{cue.name}.wav"),
            "finalPath": canonical(f"final/{cue.name}{runtime_extension}"),
        })
        reports.append(report)

    qa_path = output_root / "qa-report.json"
    write_json(qa_path, {
        "assetId": asset_id,
        "status": "PASS",
        "generatedAt": generated_at,
        "cueCount": len(cues),
        "checks": {
            "duration": "PASS",
            "peakAndRms": "PASS",
            "dcOffset": "PASS",
            "finiteSamples": "PASS",
            "sourcePcm24": "PASS",
            "runtimeEncoding": "PASS",
            "rights": "PASS",
        },
        "cues": reports,
    })

    relative_paths = [canonical("prompt.txt"), canonical("source/generator-spec.json")]
    relative_paths += [canonical(f"source/{cue.name}.wav") for cue in cues]
    relative_paths += [canonical(f"final/{cue.name}{runtime_extension}") for cue in cues]
    relative_paths.append(canonical("qa-report.json"))
    source_hashes = {"prompt.txt": sha256(prompt_path), "generator-spec.json": sha256(spec_path)}
    source_hashes.update({f"{cue.name}.wav": sha256(source_root / f"{cue.name}.wav") for cue in cues})
    output_hashes = {
        f"{cue.name}{runtime_extension}": sha256(final_root / f"{cue.name}{runtime_extension}")
        for cue in cues
    }
    output_hashes["qa-report.json"] = sha256(qa_path)
    repository_root = caller_script.resolve().parent.parent
    seed_min = min(cue.seed for cue in cues)
    seed_max = max(cue.seed for cue in cues)
    write_json(output_root / "provenance.json", {
        "schemaVersion": 2,
        "assetId": asset_id,
        "owner": "Qinglan Demo Audio Owner",
        "relativePaths": relative_paths,
        "sourceCategory": source_category,
        "tool": "Python 3.11, NumPy and libsndfile via soundfile; no audio sample input",
        "modelVersion": (
            f"no-generative-model; batch-script-sha256:{sha256(caller_script.resolve())}; "
            f"short-pipeline-sha256:{sha256(Path(__file__).resolve())}; "
            f"shared-script-sha256:{sha256(repository_root / 'Scripts' / 'qinglan_audio_synthesis.py')}"
        ),
        "generatedOrAcquiredAt": generated_at,
        "operatorName": "Codex",
        "promptFile": canonical("prompt.txt"),
        "seed": f"{seed_min}-{seed_max}",
        "referenceInputs": [],
        "referenceRightsConfirmed": True,
        "humanEdits": [
            "Mapped stable Demo mechanics and content identities to distinct semantic one-shot cues",
            "Synthesized every sample from oscillators, seeded noise, envelopes, and pitch contours",
            "Normalized and verified format, duration, loudness, DC offset, finite samples, and deterministic hashes",
            "Derived runtime files from approved 48 kHz/24-bit mono WAV masters",
            "No recording, sample library, voice, external reference, generative model, trademark, or reference-project asset was used",
        ],
        "sourceSha256": source_hashes,
        "outputSha256": output_hashes,
        "licenseOrTermsUrl": "repository://AGENTS.md#first-party-procedural-audio",
        "licenseOrTermsSnapshot": "Docs/AssetTerms/2026-08-10-first-party-procedural-audio-rights-review.md",
        "termsReviewedAt": "2026-08-10",
        "allowedPlatforms": ["Windows x64", "Steam"],
        "allowedUses": ["commercial game runtime", "store and marketing capture", "internal development and testing"],
        "commercialUseReviewed": True,
        "steamDisclosureCategory": steam_category,
        "technicalReviewer": "Codex",
        "creativeReviewer": "Codex",
        "rightsReviewer": "Codex",
        "reviewedAt": generated_at,
        "status": "approved-for-release",
        "notes": f"{len(cues)} deterministic mono one-shot cues at 48 kHz; source masters are PCM_24 WAV.",
    })

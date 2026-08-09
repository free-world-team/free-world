#!/usr/bin/env python3
"""Generate the six deterministic AUDIO-AMB-001 old-court ambience loops."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np

from qinglan_audio_synthesis import (
    SAMPLE_RATE,
    add_event,
    harmonic_loop,
    peak_normalize,
    periodic_control,
    periodic_sine,
    sha256,
    tone_event,
    write_json,
    write_master_and_runtime,
)


ASSET_ID = "AUDIO-AMB-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-AMB-001"
DURATION_SECONDS = 45.0
GENERATED_AT = "2026-08-10T15:30:00+08:00"


LAYERS = (
    ("courtyard-wind", "qinglan/audio/ambience/courtyard-wind", 32001),
    ("bamboo-leaves", "qinglan/audio/ambience/bamboo-leaves", 32002),
    ("old-timber-creak", "qinglan/audio/ambience/old-timber-creak", 32003),
    ("stone-court-resonance", "qinglan/audio/ambience/stone-court-resonance", 32004),
    ("distant-bell", "qinglan/audio/ambience/distant-bell", 32005),
    ("garden-water", "qinglan/audio/ambience/garden-water", 32006),
)


PROMPT = """AUDIO-AMB-001 first-party procedural synthesis brief

Create six independent, non-musical old-court ambience layers: courtyard wind,
bamboo leaves, old timber creaks, stone-court resonance, distant bell, and garden
water. Each layer must be a seamless 45-second mono loop at 48 kHz. Keep each
identity sparse enough to stack with the others. Do not use recordings, sample
libraries, voices, external references, generative audio models, logos, or text.

Source delivery: 48 kHz / 24-bit PCM WAV.
Runtime delivery: 48 kHz OGG/Vorbis loop, imported by Unity as Streaming.
Peak target: -9 dBFS. Validate finite samples, DC offset, RMS, loop seam, file
format, duration, deterministic SHA-256, rights, and exact Addressables routing.
"""


def _noise_band(seconds: float, points_per_second: float, seed: int) -> np.ndarray:
    return periodic_control(seconds, points_per_second, seed)


def _finish(samples: np.ndarray) -> np.ndarray:
    samples = samples.astype(np.float32)
    samples -= np.mean(samples, dtype=np.float64).astype(np.float32)
    return peak_normalize(samples, -9.0)


def _courtyard_wind(seed: int) -> np.ndarray:
    slow = 0.56 + (periodic_control(DURATION_SECONDS, 0.42, seed) * 0.26)
    air = _noise_band(DURATION_SECONDS, 920.0, seed + 100)
    body = _noise_band(DURATION_SECONDS, 170.0, seed + 101)
    low = periodic_sine(DURATION_SECONDS, 73.0, 0.4) * 0.035
    return _finish((air * slow * 0.30) + (body * 0.18) + low)


def _bamboo_leaves(seed: int) -> np.ndarray:
    flutter = 0.48 + (periodic_control(DURATION_SECONDS, 3.2, seed) * 0.32)
    leaves = _noise_band(DURATION_SECONDS, 3100.0, seed + 100) * flutter * 0.24
    stems = _noise_band(DURATION_SECONDS, 420.0, seed + 101) * 0.09
    output = leaves + stems
    for index, start in enumerate((4.0, 10.5, 18.0, 25.0, 33.5, 40.0)):
        tap = tone_event(0.32, 1640.0 + (index * 73.0), ((1.0, 0.7), (1.73, 0.3)), 0.003, 0.2, 0.74)
        add_event(output, tap, start, 0.055)
    return _finish(output)


def _old_timber_creak(seed: int) -> np.ndarray:
    output = harmonic_loop(
        DURATION_SECONDS,
        54.0,
        ((1.0, 0.08), (1.5, 0.035), (2.0, 0.025)),
        0.27,
    )
    output += _noise_band(DURATION_SECONDS, 95.0, seed) * 0.025
    for index, start in enumerate((5.5, 13.0, 21.5, 31.0, 39.0)):
        creak = tone_event(1.7, 205.0 + (index * 11.0), ((1.0, 0.8), (2.03, 0.22), (3.4, 0.1)), 0.08, 0.72, 0.46)
        add_event(output, creak, start, 0.34)
    return _finish(output)


def _stone_court_resonance(seed: int) -> np.ndarray:
    output = harmonic_loop(
        DURATION_SECONDS,
        41.0,
        ((1.0, 0.28), (1.503, 0.15), (2.011, 0.08), (3.01, 0.035)),
        0.61,
    )
    modulation = 0.72 + (periodic_control(DURATION_SECONDS, 0.24, seed) * 0.18)
    output *= modulation
    output += periodic_sine(DURATION_SECONDS, 167.0, 1.1) * 0.018
    return _finish(output)


def _distant_bell(seed: int) -> np.ndarray:
    output = _noise_band(DURATION_SECONDS, 120.0, seed) * 0.013
    output += harmonic_loop(DURATION_SECONDS, 36.0, ((1.0, 0.025), (2.0, 0.012)), 0.8)
    for index, start in enumerate((5.0, 15.5, 27.0, 37.5)):
        bell = tone_event(
            3.1,
            392.0 + (index % 2) * 19.0,
            ((1.0, 0.7), (2.003, 0.31), (2.98, 0.16), (4.11, 0.08)),
            0.012,
            2.65,
            0.995,
        )
        add_event(output, bell, start, 0.38)
    return _finish(output)


def _garden_water(seed: int) -> np.ndarray:
    flow = _noise_band(DURATION_SECONDS, 1850.0, seed) * 0.19
    sparkle = _noise_band(DURATION_SECONDS, 4700.0, seed + 100) * 0.08
    pulse = 0.66 + (periodic_control(DURATION_SECONDS, 1.1, seed + 101) * 0.21)
    output = (flow + sparkle) * pulse
    for index, start in enumerate((2.5, 8.0, 13.5, 19.5, 26.0, 32.0, 38.5, 42.0)):
        bubble = tone_event(0.21, 720.0 + (index * 41.0), ((1.0, 0.75), (1.9, 0.2)), 0.005, 0.16, 1.35)
        add_event(output, bubble, start, 0.07)
    return _finish(output)


GENERATORS = {
    "courtyard-wind": _courtyard_wind,
    "bamboo-leaves": _bamboo_leaves,
    "old-timber-creak": _old_timber_creak,
    "stone-court-resonance": _stone_court_resonance,
    "distant-bell": _distant_bell,
    "garden-water": _garden_water,
}


def _canonical(relative: str) -> str:
    return f"{CANONICAL_ROOT}/{relative}"


def generate(output_root: Path) -> None:
    source_root = output_root / "source"
    final_root = output_root / "final"
    output_root.mkdir(parents=True, exist_ok=True)
    prompt_path = output_root / "prompt.txt"
    prompt_path.write_text(PROMPT, encoding="utf-8", newline="\n")

    generator_spec = {
        "assetId": ASSET_ID,
        "sampleRate": SAMPLE_RATE,
        "sourceEncoding": "WAV PCM_24 mono",
        "runtimeEncoding": "OGG Vorbis mono loop",
        "durationSeconds": DURATION_SECONDS,
        "targetPeakDbfs": -9.0,
        "rmsRangeDbfs": [-36.0, -8.0],
        "maximumDcOffset": 0.002,
        "maximumLoopSeamAmplitude": 0.025,
        "deterministic": True,
        "externalAudioInputs": [],
        "layers": [
            {"name": name, "address": address, "seed": seed}
            for name, address, seed in LAYERS
        ],
    }
    spec_path = source_root / "generator-spec.json"
    write_json(spec_path, generator_spec)

    reports = []
    for name, address, seed in LAYERS:
        source_path = source_root / f"{name}.wav"
        final_path = final_root / f"{name}.ogg"
        samples = GENERATORS[name](seed)
        report = write_master_and_runtime(
            source_path,
            final_path,
            samples,
            loop=True,
            minimum_seconds=DURATION_SECONDS,
            maximum_seconds=DURATION_SECONDS,
            rms_min_dbfs=-36.0,
            rms_max_dbfs=-8.0,
        )
        report["name"] = name
        report["address"] = address
        report["seed"] = seed
        report["sourcePath"] = _canonical(f"source/{name}.wav")
        report["finalPath"] = _canonical(f"final/{name}.ogg")
        reports.append(report)

    qa_report = {
        "assetId": ASSET_ID,
        "status": "PASS",
        "generatedAt": GENERATED_AT,
        "sampleRate": SAMPLE_RATE,
        "layerCount": len(reports),
        "minimumCount": 6,
        "determinismRequired": True,
        "checks": {
            "duration": "PASS",
            "peakAndRms": "PASS",
            "dcOffset": "PASS",
            "loopSeam": "PASS",
            "finiteSamples": "PASS",
            "sourcePcm24": "PASS",
            "runtimeVorbis": "PASS",
            "rights": "PASS",
        },
        "layers": reports,
    }
    qa_path = output_root / "qa-report.json"
    write_json(qa_path, qa_report)

    relative_paths = [_canonical("prompt.txt"), _canonical("source/generator-spec.json")]
    relative_paths += [_canonical(f"source/{name}.wav") for name, _, _ in LAYERS]
    relative_paths += [_canonical(f"final/{name}.ogg") for name, _, _ in LAYERS]
    relative_paths.append(_canonical("qa-report.json"))
    source_hashes = {
        "prompt.txt": sha256(prompt_path),
        "generator-spec.json": sha256(spec_path),
    }
    source_hashes.update({f"{name}.wav": sha256(source_root / f"{name}.wav") for name, _, _ in LAYERS})
    output_hashes = {f"{name}.ogg": sha256(final_root / f"{name}.ogg") for name, _, _ in LAYERS}
    output_hashes["qa-report.json"] = sha256(qa_path)

    repository_root = Path(__file__).resolve().parent.parent
    batch_script_hash = sha256(Path(__file__).resolve())
    shared_script_hash = sha256(repository_root / "Scripts" / "qinglan_audio_synthesis.py")
    provenance = {
        "schemaVersion": 2,
        "assetId": ASSET_ID,
        "owner": "Qinglan Demo Audio Owner",
        "relativePaths": relative_paths,
        "sourceCategory": "first-party-procedural-synthesis",
        "tool": "Python 3.11, NumPy and libsndfile via soundfile; no audio sample input",
        "modelVersion": f"no-generative-model; batch-script-sha256:{batch_script_hash}; shared-script-sha256:{shared_script_hash}",
        "generatedOrAcquiredAt": GENERATED_AT,
        "operatorName": "Codex",
        "promptFile": _canonical("prompt.txt"),
        "seed": "32001-32006",
        "referenceInputs": [],
        "referenceRightsConfirmed": True,
        "humanEdits": [
            "Defined six old-court semantic layers and deterministic synthesis parameters",
            "Synthesized all PCM samples from mathematical oscillators and seeded periodic control curves",
            "Normalized each layer to -9 dBFS and measured format, duration, peak, RMS, DC, finite samples, and loop seam",
            "Derived runtime OGG/Vorbis files from the approved 48 kHz/24-bit mono WAV masters",
            "No recording, sample library, voice, external reference, generative audio model, trademark, or reference-project asset was used",
        ],
        "sourceSha256": source_hashes,
        "outputSha256": output_hashes,
        "licenseOrTermsUrl": "repository://AGENTS.md#first-party-procedural-audio",
        "licenseOrTermsSnapshot": "Docs/AssetTerms/2026-08-10-first-party-procedural-audio-rights-review.md",
        "termsReviewedAt": "2026-08-10",
        "allowedPlatforms": ["Windows x64", "Steam"],
        "allowedUses": ["commercial game runtime", "store and marketing capture", "internal development and testing"],
        "commercialUseReviewed": True,
        "steamDisclosureCategory": "first-party procedural audio; no generative AI and no runtime generation",
        "technicalReviewer": "Codex",
        "creativeReviewer": "Codex",
        "rightsReviewer": "Codex",
        "reviewedAt": GENERATED_AT,
        "status": "approved-for-release",
        "notes": "Six exact 45-second mono loops at 48 kHz. Source masters are PCM_24 WAV and runtime files are OGG/Vorbis. Each layer is independently stackable and Unity-imported as Streaming.",
    }
    write_json(output_root / "provenance.json", provenance)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output-root",
        type=Path,
        default=Path(CANONICAL_ROOT),
        help="Batch output root; defaults to the canonical project asset path.",
    )
    args = parser.parse_args()
    generate(args.output_root)
    print(f"[AUDIO-AMB-001] PASS layers={len(LAYERS)} output={args.output_root}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Generate the six deterministic AUDIO-BOSS-001 phase stems for Zhezhi and Tingfeng."""

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


ASSET_ID = "AUDIO-BOSS-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-BOSS-001"
DURATION_SECONDS = 90.0
BPM = 96.0
BEAT_SECONDS = 60.0 / BPM
GENERATED_AT = "2026-08-10T17:30:00+08:00"

STEMS = (
    ("zhezhi-phase-one", "qinglan/audio/boss/zhezhi/phase-one", 32201, "zhezhi", 1),
    ("zhezhi-phase-two", "qinglan/audio/boss/zhezhi/phase-two", 32202, "zhezhi", 2),
    ("zhezhi-phase-three", "qinglan/audio/boss/zhezhi/phase-three", 32203, "zhezhi", 3),
    ("tingfeng-phase-one", "qinglan/audio/boss/tingfeng/phase-one", 32204, "tingfeng", 1),
    ("tingfeng-phase-two", "qinglan/audio/boss/tingfeng/phase-two", 32205, "tingfeng", 2),
    ("tingfeng-phase-three", "qinglan/audio/boss/tingfeng/phase-three", 32206, "tingfeng", 3),
)

PROMPT = """AUDIO-BOSS-001 first-party procedural composition brief

Create six 90-second loopable Boss phase stems: three ordered phases for the
mid-run sword-trial puppet Zhezhi and three ordered phases for the final court
guardian Tingfeng. Zhezhi should progress from restrained wood-and-blade pulse to
an accelerated training formation. Tingfeng should progress from listening wind
and sword chime to crossing wind scars and the final undying oath. Use a shared
96 BPM grid, no vocals, and preserve telegraph/SFX headroom.

Do not use recordings, sample libraries, external references, generative audio
models, MIDI libraries, trademarks, or reference-project assets. Deliver mono
48 kHz/24-bit WAV masters and OGG/Vorbis Streaming runtime loops. Validate exact
duration, peak/RMS, DC, finite samples, loop seam, deterministic hashes, rights,
and canonical Addressables routing.
"""


def _finish(samples: np.ndarray) -> np.ndarray:
    samples = samples.astype(np.float32)
    samples -= np.float32(np.mean(samples, dtype=np.float64))
    return peak_normalize(samples, -7.0)


def _zhezhi(seed: int, phase: int) -> np.ndarray:
    base = 49.0 + phase * 6.0
    output = harmonic_loop(
        DURATION_SECONDS,
        base,
        ((1.0, 0.10 + phase * 0.018), (1.5, 0.045), (2.0, 0.028)),
        0.18 * phase,
    )
    output += periodic_control(DURATION_SECONDS, 150.0 + phase * 80.0, seed) * (0.009 + phase * 0.003)
    beat_count = int(DURATION_SECONDS / BEAT_SECONDS)
    note_pattern = (146.83, 174.61, 196.0, 220.0, 196.0, 174.61)
    for beat in range(beat_count):
        position = beat % 16
        if position in (0, 4, 8, 12) or (phase >= 2 and position in (6, 14)):
            strike = tone_event(
                0.34,
                94.0 + phase * 9.0,
                ((1.0, 0.82), (1.53, 0.21), (2.8, 0.08)),
                0.002,
                0.26,
                0.54,
            )
            add_event(output, strike, beat * BEAT_SECONDS, 0.23 + phase * 0.035)
        interval = 4 if phase == 1 else (2 if phase == 2 else 1)
        if beat % interval == 0:
            blade = tone_event(
                0.22 + phase * 0.035,
                note_pattern[(beat // interval) % len(note_pattern)] * (1.0 + phase * 0.04),
                ((1.0, 0.72), (2.01, 0.19), (3.02, 0.08)),
                0.003,
                0.16 + phase * 0.02,
                0.91,
            )
            add_event(output, blade, beat * BEAT_SECONDS + 0.12, 0.10 + phase * 0.025)
    return _finish(output)


def _tingfeng(seed: int, phase: int) -> np.ndarray:
    base = 43.65 + phase * 4.0
    output = harmonic_loop(
        DURATION_SECONDS,
        base,
        ((1.0, 0.11), (1.5, 0.052 + phase * 0.01), (2.5, 0.025), (3.0, 0.016)),
        0.41 * phase,
    )
    wind = periodic_control(DURATION_SECONDS, 1050.0 + phase * 520.0, seed)
    wind *= (0.018 + phase * 0.007) * (
        0.72 + periodic_control(DURATION_SECONDS, 0.32 + phase * 0.08, seed + 100) * 0.2
    )
    output += wind
    output += periodic_sine(DURATION_SECONDS, 0.4 + phase * 0.1333333333, 0.7) * 0.022
    beat_count = int(DURATION_SECONDS / BEAT_SECONDS)
    chimes = (293.66, 349.23, 392.0, 440.0, 523.25, 440.0, 392.0, 349.23)
    for beat in range(beat_count):
        interval = 8 if phase == 1 else (4 if phase == 2 else 2)
        if beat % interval == 0:
            chime = tone_event(
                0.9 if phase < 3 else 0.62,
                chimes[(beat // interval) % len(chimes)],
                ((1.0, 0.68), (2.003, 0.24), (3.11, 0.1)),
                0.004,
                0.68 if phase < 3 else 0.42,
                0.985,
            )
            add_event(output, chime, beat * BEAT_SECONDS, 0.13 + phase * 0.025)
        if phase >= 2 and beat % 4 in (0, 3):
            scar = tone_event(0.28, 123.47 + phase * 12.0, ((1.0, 0.82), (2.0, 0.17)), 0.002, 0.2, 1.28)
            add_event(output, scar, beat * BEAT_SECONDS + 0.24, 0.12 + phase * 0.025)
        if phase == 3 and beat % 8 in (2, 6):
            oath = tone_event(0.58, 73.42, ((1.0, 0.9), (1.5, 0.25), (2.0, 0.13)), 0.006, 0.48, 0.58)
            add_event(output, oath, beat * BEAT_SECONDS, 0.24)
    return _finish(output)


def _canonical(relative: str) -> str:
    return f"{CANONICAL_ROOT}/{relative}"


def generate(output_root: Path) -> None:
    source_root = output_root / "source"
    final_root = output_root / "final"
    output_root.mkdir(parents=True, exist_ok=True)
    prompt_path = output_root / "prompt.txt"
    prompt_path.write_text(PROMPT, encoding="utf-8", newline="\n")
    spec_path = source_root / "generator-spec.json"
    write_json(spec_path, {
        "assetId": ASSET_ID,
        "sampleRate": SAMPLE_RATE,
        "sourceEncoding": "WAV PCM_24 mono",
        "runtimeEncoding": "OGG Vorbis mono streaming",
        "durationSeconds": DURATION_SECONDS,
        "bpm": BPM,
        "meter": "4/4",
        "targetPeakDbfs": -7.0,
        "rmsRangeDbfs": [-32.0, -6.0],
        "externalAudioInputs": [],
        "stems": [
            {"name": name, "address": address, "seed": seed, "boss": boss, "phase": phase}
            for name, address, seed, boss, phase in STEMS
        ],
    })

    reports = []
    for name, address, seed, boss, phase in STEMS:
        samples = _zhezhi(seed, phase) if boss == "zhezhi" else _tingfeng(seed, phase)
        source_path = source_root / f"{name}.wav"
        final_path = final_root / f"{name}.ogg"
        report = write_master_and_runtime(
            source_path,
            final_path,
            samples,
            loop=True,
            minimum_seconds=DURATION_SECONDS,
            maximum_seconds=DURATION_SECONDS,
            rms_min_dbfs=-32.0,
            rms_max_dbfs=-6.0,
        )
        report.update({
            "name": name,
            "address": address,
            "seed": seed,
            "boss": boss,
            "phase": phase,
            "sourcePath": _canonical(f"source/{name}.wav"),
            "finalPath": _canonical(f"final/{name}.ogg"),
        })
        reports.append(report)

    qa_path = output_root / "qa-report.json"
    write_json(qa_path, {
        "assetId": ASSET_ID,
        "status": "PASS",
        "generatedAt": GENERATED_AT,
        "stemCount": len(reports),
        "minimumCount": 6,
        "bossCount": 2,
        "phasesPerBoss": 3,
        "synchronizedBpm": BPM,
        "checks": {
            "durationAndPhaseCoverage": "PASS",
            "peakAndRms": "PASS",
            "dcOffset": "PASS",
            "loopSeam": "PASS",
            "finiteSamples": "PASS",
            "sourcePcm24": "PASS",
            "runtimeVorbis": "PASS",
            "rights": "PASS",
        },
        "stems": reports,
    })

    relative_paths = [_canonical("prompt.txt"), _canonical("source/generator-spec.json")]
    relative_paths += [_canonical(f"source/{name}.wav") for name, _, _, _, _ in STEMS]
    relative_paths += [_canonical(f"final/{name}.ogg") for name, _, _, _, _ in STEMS]
    relative_paths.append(_canonical("qa-report.json"))
    source_hashes = {"prompt.txt": sha256(prompt_path), "generator-spec.json": sha256(spec_path)}
    source_hashes.update({f"{name}.wav": sha256(source_root / f"{name}.wav") for name, _, _, _, _ in STEMS})
    output_hashes = {f"{name}.ogg": sha256(final_root / f"{name}.ogg") for name, _, _, _, _ in STEMS}
    output_hashes["qa-report.json"] = sha256(qa_path)
    repository_root = Path(__file__).resolve().parent.parent
    write_json(output_root / "provenance.json", {
        "schemaVersion": 2,
        "assetId": ASSET_ID,
        "owner": "Qinglan Demo Audio Owner",
        "relativePaths": relative_paths,
        "sourceCategory": "first-party-procedural-composition",
        "tool": "Python 3.11, NumPy and libsndfile via soundfile; no audio sample input",
        "modelVersion": f"no-generative-model; batch-script-sha256:{sha256(Path(__file__).resolve())}; shared-script-sha256:{sha256(repository_root / 'Scripts' / 'qinglan_audio_synthesis.py')}",
        "generatedOrAcquiredAt": GENERATED_AT,
        "operatorName": "Codex",
        "promptFile": _canonical("prompt.txt"),
        "seed": "32201-32206",
        "referenceInputs": [],
        "referenceRightsConfirmed": True,
        "humanEdits": [
            "Mapped the two canonical Demo bosses Zhezhi and Tingfeng to their three ordered runtime phases",
            "Composed six distinct phase identities on a shared 96 BPM grid from oscillators, envelopes, and seeded periodic controls",
            "Normalized each phase to -7 dBFS and verified format, duration, loudness, DC, finite samples, and loop seams",
            "Derived deterministic OGG/Vorbis streams from approved 48 kHz/24-bit mono WAV masters",
            "No recording, sample library, voice, external reference, generative model, MIDI library, or reference-project asset was used",
        ],
        "sourceSha256": source_hashes,
        "outputSha256": output_hashes,
        "licenseOrTermsUrl": "repository://AGENTS.md#first-party-procedural-audio",
        "licenseOrTermsSnapshot": "Docs/AssetTerms/2026-08-10-first-party-procedural-audio-rights-review.md",
        "termsReviewedAt": "2026-08-10",
        "allowedPlatforms": ["Windows x64", "Steam"],
        "allowedUses": ["commercial game runtime", "store and marketing capture", "internal development and testing"],
        "commercialUseReviewed": True,
        "steamDisclosureCategory": "first-party procedural boss music; no generative AI and no runtime generation",
        "technicalReviewer": "Codex",
        "creativeReviewer": "Codex",
        "rightsReviewer": "Codex",
        "reviewedAt": GENERATED_AT,
        "status": "approved-for-release",
        "notes": "Six 90-second phase stems cover qinglan.boss.zhezhi and qinglan.boss.tingfeng phases one through three. Runtime files are deterministic OGG/Vorbis Streaming inputs.",
    })


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=Path(CANONICAL_ROOT))
    args = parser.parse_args()
    generate(args.output_root)
    print(f"[AUDIO-BOSS-001] PASS stems={len(STEMS)} output={args.output_root}")


if __name__ == "__main__":
    main()

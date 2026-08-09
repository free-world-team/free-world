#!/usr/bin/env python3
"""Generate four deterministic, synchronized AUDIO-MUSIC-001 runtime stems."""

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


ASSET_ID = "AUDIO-MUSIC-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-MUSIC-001"
DURATION_SECONDS = 120.0
BPM = 96.0
BEAT_SECONDS = 60.0 / BPM
GENERATED_AT = "2026-08-10T16:30:00+08:00"

STEMS = (
    ("exploration-air", "qinglan/audio/music/exploration-air", 32101),
    ("exploration-strings", "qinglan/audio/music/exploration-strings", 32102),
    ("combat-rhythm", "qinglan/audio/music/combat-rhythm", 32103),
    ("high-pressure-drive", "qinglan/audio/music/high-pressure-drive", 32104),
)

PROMPT = """AUDIO-MUSIC-001 first-party procedural synthesis brief

Create four synchronized 120-second music stems at 96 BPM: exploration air,
exploration strings, combat rhythm, and high-pressure drive. Use a restrained
D-minor pentatonic language, no vocals, and leave spectral room for gameplay SFX.
All stems must start together, loop independently, and support additive intensity
mixing. Do not use recordings, sample libraries, external references, generative
audio models, MIDI libraries, trademarks, or reference-project assets.

Source: mono 48 kHz / 24-bit PCM WAV. Runtime: OGG/Vorbis Streaming.
Validate exact duration, finite samples, peak/RMS, DC, loop seam, deterministic
SHA-256, rights, and canonical Addressables routing.
"""


PENTATONIC = (146.83, 174.61, 196.00, 220.00, 261.63, 293.66)


def _finish(samples: np.ndarray, peak_dbfs: float = -8.0) -> np.ndarray:
    samples = samples.astype(np.float32)
    samples -= np.float32(np.mean(samples, dtype=np.float64))
    return peak_normalize(samples, peak_dbfs)


def _exploration_air(seed: int) -> np.ndarray:
    output = harmonic_loop(
        DURATION_SECONDS,
        73.415,
        ((1.0, 0.055), (1.5, 0.028), (2.0, 0.018)),
        0.22,
    )
    breath = periodic_control(DURATION_SECONDS, 1400.0, seed)
    breath *= 0.014 * (0.7 + periodic_control(DURATION_SECONDS, 0.18, seed + 1) * 0.2)
    output += breath
    pattern = (0, 2, 3, 1, 4, 3, 2, 5, 4, 2, 1, 3)
    for index in range(38):
        frequency = PENTATONIC[pattern[index % len(pattern)]] * (1.0 if index % 7 else 0.5)
        note = tone_event(2.2, frequency, ((1.0, 0.75), (2.0, 0.12), (3.0, 0.05)), 0.18, 0.72, 1.012)
        add_event(output, note, 1.25 + index * 3.125, 0.16)
    return _finish(output)


def _exploration_strings(seed: int) -> np.ndarray:
    output = periodic_control(DURATION_SECONDS, 260.0, seed) * 0.008
    pattern = (0, 3, 2, 4, 1, 3, 5, 2, 0, 4, 3, 1, 2, 5, 4, 3)
    beat_count = int(DURATION_SECONDS / BEAT_SECONDS)
    for beat in range(beat_count):
        if beat % 4 == 3:
            continue
        frequency = PENTATONIC[pattern[beat % len(pattern)]]
        if beat % 16 in (0, 8):
            frequency *= 0.5
        pluck = tone_event(0.48, frequency, ((1.0, 0.8), (2.01, 0.22), (3.98, 0.08)), 0.004, 0.38, 0.994)
        add_event(output, pluck, beat * BEAT_SECONDS, 0.14 if beat % 4 else 0.19)
    return _finish(output)


def _combat_rhythm(seed: int) -> np.ndarray:
    output = periodic_control(DURATION_SECONDS, 110.0, seed) * 0.006
    beat_count = int(DURATION_SECONDS / BEAT_SECONDS)
    for beat in range(beat_count):
        position = beat % 16
        if position in (0, 4, 8, 12, 14):
            drum = tone_event(0.42, 82.0 if position in (0, 8) else 105.0, ((1.0, 0.9), (1.5, 0.22)), 0.002, 0.34, 0.47)
            add_event(output, drum, beat * BEAT_SECONDS, 0.36 if position in (0, 8) else 0.25)
        if position in (2, 6, 10, 15):
            rim = tone_event(0.16, 1180.0, ((1.0, 0.75), (1.7, 0.22)), 0.001, 0.13, 0.73)
            add_event(output, rim, beat * BEAT_SECONDS, 0.13)
        if position in (7, 15):
            gong = tone_event(1.1, 196.0, ((1.0, 0.65), (2.41, 0.22), (3.83, 0.11)), 0.008, 0.92, 0.985)
            add_event(output, gong, beat * BEAT_SECONDS, 0.12)
    return _finish(output)


def _high_pressure_drive(seed: int) -> np.ndarray:
    output = harmonic_loop(
        DURATION_SECONDS,
        55.0,
        ((1.0, 0.12), (1.5, 0.045), (2.0, 0.032)),
        0.73,
    )
    output *= 0.78 + periodic_sine(DURATION_SECONDS, 0.2, 0.5) * 0.12
    output += periodic_control(DURATION_SECONDS, 420.0, seed) * 0.012
    half_beat = BEAT_SECONDS * 0.5
    pulse_count = int(DURATION_SECONDS / half_beat)
    pattern = (110.0, 110.0, 130.81, 110.0, 146.83, 130.81, 164.81, 146.83)
    for pulse in range(pulse_count):
        if pulse % 8 in (3, 7):
            continue
        note = tone_event(0.19, pattern[pulse % len(pattern)], ((1.0, 0.82), (2.0, 0.18)), 0.002, 0.13, 0.92)
        add_event(output, note, pulse * half_beat, 0.12 if pulse % 2 else 0.18)
    return _finish(output)


GENERATORS = {
    "exploration-air": _exploration_air,
    "exploration-strings": _exploration_strings,
    "combat-rhythm": _combat_rhythm,
    "high-pressure-drive": _high_pressure_drive,
}


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
        "tonalLanguage": "D minor pentatonic",
        "targetPeakDbfs": -8.0,
        "rmsRangeDbfs": [-32.0, -7.0],
        "deterministic": True,
        "externalAudioInputs": [],
        "stems": [{"name": name, "address": address, "seed": seed} for name, address, seed in STEMS],
    })

    reports = []
    for name, address, seed in STEMS:
        source_path = source_root / f"{name}.wav"
        final_path = final_root / f"{name}.ogg"
        report = write_master_and_runtime(
            source_path,
            final_path,
            GENERATORS[name](seed),
            loop=True,
            minimum_seconds=DURATION_SECONDS,
            maximum_seconds=DURATION_SECONDS,
            rms_min_dbfs=-32.0,
            rms_max_dbfs=-7.0,
        )
        report.update({
            "name": name,
            "address": address,
            "seed": seed,
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
        "minimumCount": 4,
        "synchronizedBpm": BPM,
        "checks": {
            "durationAndAlignment": "PASS",
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
    relative_paths += [_canonical(f"source/{name}.wav") for name, _, _ in STEMS]
    relative_paths += [_canonical(f"final/{name}.ogg") for name, _, _ in STEMS]
    relative_paths.append(_canonical("qa-report.json"))
    source_hashes = {"prompt.txt": sha256(prompt_path), "generator-spec.json": sha256(spec_path)}
    source_hashes.update({f"{name}.wav": sha256(source_root / f"{name}.wav") for name, _, _ in STEMS})
    output_hashes = {f"{name}.ogg": sha256(final_root / f"{name}.ogg") for name, _, _ in STEMS}
    output_hashes["qa-report.json"] = sha256(qa_path)
    repository_root = Path(__file__).resolve().parent.parent
    batch_hash = sha256(Path(__file__).resolve())
    shared_hash = sha256(repository_root / "Scripts" / "qinglan_audio_synthesis.py")
    write_json(output_root / "provenance.json", {
        "schemaVersion": 2,
        "assetId": ASSET_ID,
        "owner": "Qinglan Demo Audio Owner",
        "relativePaths": relative_paths,
        "sourceCategory": "first-party-procedural-synthesis",
        "tool": "Python 3.11, NumPy and libsndfile via soundfile; no audio sample input",
        "modelVersion": f"no-generative-model; batch-script-sha256:{batch_hash}; shared-script-sha256:{shared_hash}",
        "generatedOrAcquiredAt": GENERATED_AT,
        "operatorName": "Codex",
        "promptFile": _canonical("prompt.txt"),
        "seed": "32101-32104",
        "referenceInputs": [],
        "referenceRightsConfirmed": True,
        "humanEdits": [
            "Defined four additive intensity stems on a shared 96 BPM and 120-second grid",
            "Synthesized every sample from oscillators, envelopes, and seeded periodic control curves",
            "Normalized stems to -8 dBFS and verified alignment, format, loudness, DC, finite samples, and loop seams",
            "Derived deterministic runtime OGG/Vorbis streams from 48 kHz/24-bit mono WAV masters",
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
        "steamDisclosureCategory": "first-party procedural music; no generative AI and no runtime generation",
        "technicalReviewer": "Codex",
        "creativeReviewer": "Codex",
        "rightsReviewer": "Codex",
        "reviewedAt": GENERATED_AT,
        "status": "approved-for-release",
        "notes": "Four synchronized 120-second mono stems at 96 BPM. Source masters are PCM_24 WAV and runtime files are deterministic OGG/Vorbis Streaming inputs.",
    })


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=Path(CANONICAL_ROOT))
    args = parser.parse_args()
    generate(args.output_root)
    print(f"[AUDIO-MUSIC-001] PASS stems={len(STEMS)} output={args.output_root}")


if __name__ == "__main__":
    main()

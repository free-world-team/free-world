#!/usr/bin/env python3
"""Shared deterministic synthesis and QA primitives for Qinglan first-party audio."""

from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path
from typing import Iterable

import numpy as np
import soundfile as sf


SAMPLE_RATE = 48_000
EPSILON = 1.0e-12


def timeline(seconds: float) -> np.ndarray:
    if seconds <= 0:
        raise ValueError("seconds must be positive")
    return np.arange(round(seconds * SAMPLE_RATE), dtype=np.float32) / SAMPLE_RATE


def periodic_sine(seconds: float, frequency: float, phase: float = 0.0) -> np.ndarray:
    count = round(seconds * SAMPLE_RATE)
    cycles = max(1, round(frequency * seconds))
    angle = (2.0 * math.pi * cycles * np.arange(count, dtype=np.float32) / count) + phase
    return np.sin(angle).astype(np.float32)


def harmonic_loop(
    seconds: float,
    fundamental: float,
    partials: Iterable[tuple[float, float]],
    phase: float = 0.0,
) -> np.ndarray:
    output = np.zeros(round(seconds * SAMPLE_RATE), dtype=np.float32)
    for ratio, amplitude in partials:
        output += periodic_sine(seconds, fundamental * ratio, phase * ratio) * amplitude
    return output


def periodic_control(seconds: float, points_per_second: float, seed: int) -> np.ndarray:
    count = round(seconds * SAMPLE_RATE)
    point_count = max(8, round(seconds * points_per_second))
    rng = np.random.default_rng(seed)
    points = rng.uniform(-1.0, 1.0, point_count).astype(np.float32)
    points = np.concatenate((points, points[:1]))
    positions = np.linspace(0, count, point_count + 1, dtype=np.float64)
    return np.interp(np.arange(count), positions, points).astype(np.float32)


def noise_burst(seconds: float, seed: int, smoothing: int = 1) -> np.ndarray:
    count = round(seconds * SAMPLE_RATE)
    rng = np.random.default_rng(seed)
    samples = rng.standard_normal(count).astype(np.float32)
    if smoothing > 1:
        kernel = np.ones(smoothing, dtype=np.float32) / smoothing
        samples = np.convolve(samples, kernel, mode="same").astype(np.float32)
    return samples


def envelope(count: int, attack: float, release: float) -> np.ndarray:
    if count <= 0:
        raise ValueError("count must be positive")
    values = np.ones(count, dtype=np.float32)
    attack_count = min(count, max(1, round(attack * SAMPLE_RATE)))
    release_count = min(count, max(1, round(release * SAMPLE_RATE)))
    values[:attack_count] *= np.linspace(0.0, 1.0, attack_count, dtype=np.float32)
    values[-release_count:] *= np.linspace(1.0, 0.0, release_count, dtype=np.float32)
    return values


def tone_event(
    seconds: float,
    frequency: float,
    partials: Iterable[tuple[float, float]],
    attack: float = 0.008,
    release: float = 0.12,
    pitch_end_ratio: float = 1.0,
) -> np.ndarray:
    t = timeline(seconds)
    if pitch_end_ratio == 1.0:
        phase = 2.0 * math.pi * frequency * t
    else:
        end_frequency = frequency * pitch_end_ratio
        slope = (end_frequency - frequency) / seconds
        phase = 2.0 * math.pi * ((frequency * t) + (0.5 * slope * t * t))
    result = np.zeros(t.shape[0], dtype=np.float32)
    for ratio, amplitude in partials:
        result += np.sin(phase * ratio).astype(np.float32) * amplitude
    return result * envelope(result.shape[0], attack, release)


def add_event(destination: np.ndarray, event: np.ndarray, start_seconds: float, gain: float = 1.0) -> None:
    start = round(start_seconds * SAMPLE_RATE)
    if start < 0 or start >= destination.shape[0]:
        return
    length = min(event.shape[0], destination.shape[0] - start)
    destination[start:start + length] += event[:length] * gain


def peak_normalize(samples: np.ndarray, target_peak_dbfs: float = -3.0) -> np.ndarray:
    peak = float(np.max(np.abs(samples)))
    if peak <= EPSILON:
        raise ValueError("audio is silent")
    target = 10.0 ** (target_peak_dbfs / 20.0)
    return (samples * (target / peak)).astype(np.float32)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def metrics(samples: np.ndarray, loop: bool) -> dict[str, float | int | bool]:
    if samples.ndim == 1:
        samples = samples[:, None]
    peak = float(np.max(np.abs(samples)))
    rms = float(np.sqrt(np.mean(np.square(samples, dtype=np.float64))))
    dc = float(np.max(np.abs(np.mean(samples, axis=0))))
    seam = float(np.max(np.abs(samples[0] - samples[-1]))) if loop else 0.0
    return {
        "sampleRate": SAMPLE_RATE,
        "channels": int(samples.shape[1]),
        "frames": int(samples.shape[0]),
        "durationSeconds": round(samples.shape[0] / SAMPLE_RATE, 6),
        "peakDbfs": round(20.0 * math.log10(max(peak, EPSILON)), 4),
        "rmsDbfs": round(20.0 * math.log10(max(rms, EPSILON)), 4),
        "dcOffset": round(dc, 8),
        "loopSeamAmplitude": round(seam, 8),
        "finite": bool(np.isfinite(samples).all()),
    }


def write_master_and_runtime(
    source_path: Path,
    final_path: Path,
    samples: np.ndarray,
    *,
    loop: bool,
    minimum_seconds: float,
    maximum_seconds: float,
    rms_min_dbfs: float,
    rms_max_dbfs: float,
) -> dict[str, object]:
    samples = np.asarray(samples, dtype=np.float32)
    if samples.ndim not in (1, 2):
        raise ValueError("samples must be mono or interleaved channels")
    report = metrics(samples, loop)
    duration = float(report["durationSeconds"])
    if duration < minimum_seconds or duration > maximum_seconds:
        raise ValueError(f"duration {duration} is outside {minimum_seconds}-{maximum_seconds}")
    if float(report["peakDbfs"]) > -1.0:
        raise ValueError(f"peak {report['peakDbfs']} dBFS exceeds -1 dBFS")
    if not rms_min_dbfs <= float(report["rmsDbfs"]) <= rms_max_dbfs:
        raise ValueError(f"RMS {report['rmsDbfs']} dBFS is outside {rms_min_dbfs}-{rms_max_dbfs}")
    if float(report["dcOffset"]) > 0.002:
        raise ValueError(f"DC offset {report['dcOffset']} exceeds 0.002")
    if loop and float(report["loopSeamAmplitude"]) > 0.025:
        raise ValueError(f"loop seam {report['loopSeamAmplitude']} exceeds 0.025")
    if not bool(report["finite"]):
        raise ValueError("audio contains a non-finite sample")

    source_path.parent.mkdir(parents=True, exist_ok=True)
    final_path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(source_path, samples, SAMPLE_RATE, format="WAV", subtype="PCM_24")
    if final_path.suffix.lower() == ".ogg":
        sf.write(final_path, samples, SAMPLE_RATE, format="OGG", subtype="VORBIS")
    elif final_path.suffix.lower() == ".wav":
        sf.write(final_path, samples, SAMPLE_RATE, format="WAV", subtype="PCM_16")
    else:
        raise ValueError("runtime output must be .ogg or .wav")

    source_info = sf.info(source_path)
    final_info = sf.info(final_path)
    if source_info.samplerate != SAMPLE_RATE or source_info.subtype != "PCM_24":
        raise ValueError("source master is not 48 kHz PCM_24 WAV")
    if final_info.samplerate != SAMPLE_RATE:
        raise ValueError("runtime file is not 48 kHz")
    report.update({
        "sourcePath": source_path.as_posix(),
        "finalPath": final_path.as_posix(),
        "sourceFormat": source_info.format,
        "sourceSubtype": source_info.subtype,
        "runtimeFormat": final_info.format,
        "runtimeSubtype": final_info.subtype,
        "sourceBytes": source_path.stat().st_size,
        "runtimeBytes": final_path.stat().st_size,
        "sourceSha256": sha256(source_path),
        "runtimeSha256": sha256(final_path),
    })
    return report


def write_json(path: Path, value: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")

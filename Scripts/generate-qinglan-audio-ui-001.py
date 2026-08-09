#!/usr/bin/env python3
"""Generate twelve deterministic AUDIO-UI-001 interface cues."""

from __future__ import annotations

import argparse
from pathlib import Path

from qinglan_short_audio_batch import CueSpec, render_batch

ASSET_ID = "AUDIO-UI-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-UI-001"
GENERATED_AT = "2026-08-10T23:30:00+08:00"


def cue(
    name: str,
    duration: float,
    seed: int,
    frequency: float,
    pitch: float,
    noise: float,
) -> CueSpec:
    return CueSpec(
        name,
        "qinglan/audio/ui/" + name,
        duration,
        seed,
        frequency,
        0.82,
        noise,
        3,
        pitch,
        0.0015,
        min(0.08, duration * 0.42),
        -5.0,
    )


CUES = (
    cue("navigate", 0.08, 32801, 880.00, 1.08, 0.02),
    cue("confirm", 0.18, 32802, 659.25, 1.42, 0.03),
    cue("cancel", 0.16, 32803, 523.25, 0.70, 0.04),
    cue("error", 0.28, 32804, 196.00, 0.58, 0.12),
    cue("page-open", 0.32, 32805, 392.00, 1.64, 0.05),
    cue("page-close", 0.26, 32806, 493.88, 0.62, 0.05),
    cue("tab-change", 0.12, 32807, 783.99, 1.18, 0.02),
    cue("choice-focus", 0.10, 32808, 987.77, 0.94, 0.02),
    cue("choice-select", 0.22, 32809, 698.46, 1.50, 0.03),
    cue("locked", 0.30, 32810, 233.08, 0.66, 0.10),
    cue("notification", 0.42, 32811, 587.33, 1.72, 0.04),
    cue("pause-toggle", 0.20, 32812, 349.23, 1.24, 0.05),
)

PROMPT = """AUDIO-UI-001 first-party procedural synthesis brief

Create twelve concise, non-verbal interface cues for navigation, confirmation,
cancellation, error, page transitions, tab changes, choices, locks,
notifications, and pause toggling. Each cue must remain clear at low playback
level and finish within 0.05-1.0 seconds. No recordings, samples, voices,
external references, generative models, trademarks, or reference-project assets.
Source PCM24 WAV; Runtime PCM16 WAV.
"""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=Path(CANONICAL_ROOT))
    args = parser.parse_args()
    render_batch(
        asset_id=ASSET_ID,
        canonical_root=CANONICAL_ROOT,
        generated_at=GENERATED_AT,
        prompt=PROMPT,
        cues=CUES,
        output_root=args.output_root,
        caller_script=Path(__file__),
        runtime_extension=".wav",
    )
    print(f"[AUDIO-UI-001] PASS cues={len(CUES)} output={args.output_root}")


if __name__ == "__main__":
    main()

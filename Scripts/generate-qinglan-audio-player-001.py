#!/usr/bin/env python3
"""Generate the twelve deterministic AUDIO-PLAYER-001 mechanic/state cues."""

from __future__ import annotations

import argparse
from pathlib import Path

from qinglan_short_audio_batch import CueSpec, render_batch


ASSET_ID = "AUDIO-PLAYER-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-PLAYER-001"
GENERATED_AT = "2026-08-10T18:30:00+08:00"

CUES = (
    CueSpec("dash-start", "qinglan/audio/player/dash-start", 0.34, 32301, 310.0, 0.55, 0.38, 3, 1.75),
    CueSpec("dash-ready", "qinglan/audio/player/dash-ready", 0.28, 32302, 660.0, 0.88, 0.08, 7, 1.28),
    CueSpec("riding-wind-tier", "qinglan/audio/player/riding-wind-tier", 0.72, 32303, 392.0, 0.78, 0.12, 11, 1.45),
    CueSpec("sword-intent-gain", "qinglan/audio/player/sword-intent-gain", 0.24, 32304, 523.25, 0.84, 0.08, 5, 1.18),
    CueSpec("sword-intent-full", "qinglan/audio/player/sword-intent-full", 0.68, 32305, 587.33, 0.9, 0.06, 9, 1.5),
    CueSpec("skill-ready", "qinglan/audio/player/skill-ready", 0.42, 32306, 783.99, 0.88, 0.05, 9, 1.22),
    CueSpec("manifestation-ready", "qinglan/audio/player/manifestation-ready", 0.92, 32307, 329.63, 0.8, 0.1, 13, 1.62),
    CueSpec("pickup", "qinglan/audio/player/pickup", 0.22, 32308, 698.46, 0.86, 0.08, 5, 1.34),
    CueSpec("player-hit", "qinglan/audio/player/player-hit", 0.31, 32309, 138.59, 0.48, 0.48, 2, 0.62),
    CueSpec("low-health", "qinglan/audio/player/low-health", 0.78, 32310, 110.0, 0.82, 0.12, 7, 0.82),
    CueSpec("level-up", "qinglan/audio/player/level-up", 0.84, 32311, 440.0, 0.9, 0.04, 9, 1.72),
    CueSpec("player-defeat", "qinglan/audio/player/player-defeat", 1.2, 32312, 196.0, 0.72, 0.18, 17, 0.42, 0.008, 0.48),
)

PROMPT = """AUDIO-PLAYER-001 first-party procedural synthesis brief

Create twelve short player mechanic/state cues for dash, dash ready, Riding Wind
tier, sword-intent gain/full, skill ready, manifestation ready, pickup, hit,
low-health, level-up, and defeat. Each cue must be recognizable without speech,
fit 0.1-2.0 seconds, and leave combat mix headroom. Do not use recordings, sample
libraries, voices, external references, generative audio models, trademarks, or
reference-project assets. Source: mono 48 kHz/24-bit WAV. Runtime: OGG/Vorbis.
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
    )
    print(f"[AUDIO-PLAYER-001] PASS cues={len(CUES)} output={args.output_root}")


if __name__ == "__main__":
    main()

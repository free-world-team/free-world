#!/usr/bin/env python3
"""Generate twenty-four deterministic AUDIO-ENEMY-001 normal-enemy/Boss cues."""

from __future__ import annotations

import argparse
from pathlib import Path

from qinglan_short_audio_batch import CueSpec, render_batch


ASSET_ID = "AUDIO-ENEMY-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-ENEMY-001"
GENERATED_AT = "2026-08-10T20:30:00+08:00"
IDENTITIES = (
    ("grass-spirit", 261.63), ("paper-crane", 659.25), ("wooden-puppet", 146.83),
    ("stone-lantern", 98.0), ("wind-bell-spirit", 523.25), ("explosive-seed", 174.61),
    ("zhezhi", 123.47), ("tingfeng", 110.0),
)


def build_cues() -> tuple[CueSpec, ...]:
    cues = []
    seed = 32501
    for index, (identity, frequency) in enumerate(IDENTITIES):
        boss = index >= 6
        cues.append(CueSpec(
            identity + "-spawn", "qinglan/audio/enemy/" + identity + "/spawn",
            0.62 if not boss else 1.35, seed, frequency, 0.76, 0.16 if not boss else 0.24, 7, 1.35))
        seed += 1
        cues.append(CueSpec(
            identity + "-attack", "qinglan/audio/enemy/" + identity + "/attack",
            0.38 if not boss else 0.82, seed, frequency * 1.45, 0.68, 0.3, 3, 0.58))
        seed += 1
        cues.append(CueSpec(
            identity + "-death", "qinglan/audio/enemy/" + identity + "/death",
            0.74 if not boss else 1.8, seed, frequency * 0.82, 0.7, 0.28, 11, 0.34,
            0.006, 0.28 if not boss else 0.7))
        seed += 1
    return tuple(cues)


CUES = build_cues()
PROMPT = """AUDIO-ENEMY-001 first-party procedural synthesis brief

Create Spawn, Attack, and Death cues for the six Demo normal enemies (grass
spirit, paper crane, wooden puppet, stone lantern, wind-bell spirit, explosive
seed) and two Bosses (Zhezhi and Tingfeng): 24 cues total. Boss cues may be longer
and heavier, but all cues must preserve telegraph clarity and fit 0.1-4 seconds.
No recordings, samples, voices, references, generative models, trademarks, or
reference-project assets. Source PCM24 WAV; Runtime OGG/Vorbis.
"""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=Path(CANONICAL_ROOT))
    args = parser.parse_args()
    render_batch(asset_id=ASSET_ID, canonical_root=CANONICAL_ROOT, generated_at=GENERATED_AT,
                 prompt=PROMPT, cues=CUES, output_root=args.output_root, caller_script=Path(__file__))
    print(f"[AUDIO-ENEMY-001] PASS cues={len(CUES)} output={args.output_root}")


if __name__ == "__main__":
    main()

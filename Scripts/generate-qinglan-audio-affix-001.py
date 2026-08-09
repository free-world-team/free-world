#!/usr/bin/env python3
"""Generate eight deterministic AUDIO-AFFIX-001 identity cues."""

from __future__ import annotations

import argparse
from pathlib import Path

from qinglan_short_audio_batch import CueSpec, render_batch

ASSET_ID = "AUDIO-AFFIX-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-AFFIX-001"
GENERATED_AT = "2026-08-10T21:30:00+08:00"


def cue(name: str, duration: float, seed: int, frequency: float, pitch: float, noise: float) -> CueSpec:
    return CueSpec(name, "qinglan/audio/affix/" + name.replace("-", "/", 1), duration,
                   seed, frequency, 0.78, noise, 5, pitch)


CUES = (
    cue("rampaging-activate", 0.62, 32601, 174.61, 1.82, 0.28),
    cue("rampaging-pulse", 0.34, 32602, 220.0, 1.45, 0.2),
    cue("barrier-activate", 0.86, 32603, 523.25, 0.72, 0.1),
    cue("barrier-pulse", 0.58, 32604, 659.25, 0.84, 0.08),
    cue("splitting-activate", 0.74, 32605, 293.66, 1.68, 0.18),
    cue("splitting-pulse", 0.46, 32606, 349.23, 1.36, 0.16),
    cue("quaking-activate", 0.92, 32607, 82.41, 0.42, 0.42),
    cue("quaking-pulse", 0.54, 32608, 98.0, 0.56, 0.36),
)

PROMPT = """AUDIO-AFFIX-001 first-party procedural synthesis brief

Create activation and ongoing identity cues for the four canonical Elite Affixes:
Rampaging, Barrier, Splitting, and Quaking. Eight cues total, 0.2-3.0 seconds,
non-verbal, distinct under combat load, and subordinate to critical telegraphs.
No recordings, samples, voices, external references, generative models, trademarks,
or reference-project assets. Source PCM24 WAV; Runtime OGG/Vorbis.
"""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=Path(CANONICAL_ROOT))
    args = parser.parse_args()
    render_batch(asset_id=ASSET_ID, canonical_root=CANONICAL_ROOT, generated_at=GENERATED_AT,
                 prompt=PROMPT, cues=CUES, output_root=args.output_root, caller_script=Path(__file__))
    print(f"[AUDIO-AFFIX-001] PASS cues={len(CUES)} output={args.output_root}")


if __name__ == "__main__":
    main()

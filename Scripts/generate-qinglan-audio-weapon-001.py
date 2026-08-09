#!/usr/bin/env python3
"""Generate the eighteen deterministic AUDIO-WEAPON-001 base/evolution cues."""

from __future__ import annotations

import argparse
from pathlib import Path

from qinglan_short_audio_batch import CueSpec, render_batch


ASSET_ID = "AUDIO-WEAPON-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-WEAPON-001"
GENERATED_AT = "2026-08-10T19:30:00+08:00"


def cue(name: str, duration: float, seed: int, frequency: float, pitch: float, noise: float) -> CueSpec:
    return CueSpec(name, "qinglan/audio/weapon/" + name, duration, seed, frequency, 0.82, noise, 5, pitch)


CUES = (
    cue("yufeng-sword-cast", 0.42, 32401, 392.0, 1.55, 0.12),
    cue("yellow-talisman-cast", 0.58, 32402, 523.25, 0.82, 0.08),
    cue("lihuo-wheel-cast", 0.64, 32403, 246.94, 1.42, 0.22),
    cue("tide-orb-cast", 0.72, 32404, 220.0, 0.74, 0.18),
    cue("zhenyue-seal-cast", 0.88, 32405, 98.0, 0.56, 0.26),
    cue("spirit-vine-seed-cast", 0.54, 32406, 329.63, 0.68, 0.16),
    cue("qinglan-flowing-shadow-sword", 0.96, 32407, 440.0, 1.82, 0.14),
    cue("taiyi-spirit-sealing-array", 1.24, 32408, 587.33, 0.72, 0.12),
    cue("chilu-hundred-craft-wheel", 1.08, 32409, 293.66, 1.67, 0.28),
    cue("mirror-sea-tide-wheel", 1.34, 32410, 196.0, 0.58, 0.22),
    cue("mountain-boundary-seal", 1.42, 32411, 82.41, 0.46, 0.32),
    cue("earth-vein-spring-branch", 1.16, 32412, 349.23, 0.64, 0.18),
    cue("yufeng-return", 0.38, 32413, 466.16, 0.62, 0.1),
    cue("talisman-detonation", 0.74, 32414, 659.25, 0.48, 0.3),
    cue("lihuo-return-explosion", 0.82, 32415, 130.81, 0.38, 0.42),
    cue("tide-phase-shift", 1.02, 32416, 174.61, 1.34, 0.24),
    cue("zhenyue-countershock", 0.86, 32417, 73.42, 1.95, 0.4),
    cue("vine-propagation", 0.92, 32418, 261.63, 1.48, 0.2),
)

PROMPT = """AUDIO-WEAPON-001 first-party procedural synthesis brief

Create eighteen weapon cues grounded in the Demo catalog: six base weapon casts,
six Evolution identities, and six stable hidden/secondary mechanic cues. Distinguish
wind blade, talisman, fire wheel, tide orb, mountain seal, and spirit vine families
without speech. Each cue must fit 0.1-3.0 seconds and preserve enemy telegraph
headroom. No recordings, samples, voices, references, generative audio models,
trademarks, or reference-project assets. Source PCM24 WAV; Runtime OGG/Vorbis.
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
    print(f"[AUDIO-WEAPON-001] PASS cues={len(CUES)} output={args.output_root}")


if __name__ == "__main__":
    main()

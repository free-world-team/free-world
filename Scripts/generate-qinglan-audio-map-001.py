#!/usr/bin/env python3
"""Generate fourteen deterministic AUDIO-MAP-001 objective/event/landmark cues."""

from __future__ import annotations

import argparse
from pathlib import Path

from qinglan_short_audio_batch import CueSpec, render_batch

ASSET_ID = "AUDIO-MAP-001"
CANONICAL_ROOT = "Assets/GameAssets/AI/QinglanDemo/AUDIO-MAP-001"
GENERATED_AT = "2026-08-10T22:30:00+08:00"


def cue(name: str, address: str, duration: float, seed: int, freq: float, pitch: float, noise: float) -> CueSpec:
    return CueSpec(name, address, duration, seed, freq, 0.8, noise, 7, pitch)


CUES = (
    cue("objective-listen", "qinglan/audio/map/objective/listen", 0.82, 32701, 523.25, 1.24, 0.08),
    cue("objective-guide", "qinglan/audio/map/objective/guide", 0.88, 32702, 392.0, 1.52, 0.12),
    cue("objective-stop-balance", "qinglan/audio/map/objective/stop-balance", 0.94, 32703, 261.63, 0.72, 0.14),
    cue("event-wind-vein-riot", "qinglan/audio/map/event/wind-vein-riot", 1.42, 32704, 174.61, 1.86, 0.32),
    cue("event-herb-garden-revival", "qinglan/audio/map/event/herb-garden-revival", 1.28, 32705, 329.63, 1.58, 0.16),
    cue("event-old-sword-resonance", "qinglan/audio/map/event/old-sword-resonance", 1.54, 32706, 220.0, 0.64, 0.18),
    cue("landmark-wind-vein-stele", "qinglan/audio/map/landmark/wind-vein-stele", 0.76, 32707, 440.0, 1.18, 0.08),
    cue("landmark-sealed-sword-cache", "qinglan/audio/map/landmark/sealed-sword-cache", 0.84, 32708, 587.33, 0.78, 0.12),
    cue("landmark-herb-garden-variant", "qinglan/audio/map/landmark/herb-garden-variant", 0.72, 32709, 349.23, 1.42, 0.14),
    cue("landmark-broken-wall-sword-mark", "qinglan/audio/map/landmark/broken-wall-sword-mark", 0.8, 32710, 293.66, 0.68, 0.18),
    cue("landmark-guest-pavilion-letter", "qinglan/audio/map/landmark/guest-pavilion-letter", 0.68, 32711, 659.25, 1.16, 0.06),
    cue("map-boundary-warning", "qinglan/audio/map/system/boundary-warning", 0.46, 32712, 130.81, 0.62, 0.24),
    cue("objective-complete", "qinglan/audio/map/system/objective-complete", 1.12, 32713, 493.88, 1.72, 0.08),
    cue("landmark-claim", "qinglan/audio/map/system/landmark-claim", 0.92, 32714, 698.46, 1.36, 0.08),
)

PROMPT = """AUDIO-MAP-001 first-party procedural synthesis brief

Create fourteen cues covering three wind-altars, three dynamic events, five
landmarks, map-boundary warning, objective completion, and landmark claim. Cues
must be non-verbal, distinct, 0.2-5.0 seconds, and compatible with Pause/Story
ducking. No recordings, samples, voices, external references, generative models,
trademarks, or reference-project assets. Source PCM24 WAV; Runtime OGG/Vorbis.
"""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", type=Path, default=Path(CANONICAL_ROOT))
    args = parser.parse_args()
    render_batch(asset_id=ASSET_ID, canonical_root=CANONICAL_ROOT, generated_at=GENERATED_AT,
                 prompt=PROMPT, cues=CUES, output_root=args.output_root, caller_script=Path(__file__))
    print(f"[AUDIO-MAP-001] PASS cues={len(CUES)} output={args.output_root}")


if __name__ == "__main__":
    main()

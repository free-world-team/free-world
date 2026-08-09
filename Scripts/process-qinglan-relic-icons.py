#!/usr/bin/env python3
"""Create deterministic 256-pixel relic icons from approved transparent ImageGen sources."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


RELICS = (
    "broken-sword-tassel",
    "wind-vein-copper",
    "herb-garden-seed-pod",
    "listening-wind-core",
    "old-court-bell",
    "blank-sword-trial-token",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def alpha_bounds(image: Image.Image, threshold: int = 16) -> tuple[int, int, int, int]:
    mask = image.getchannel("A").point(lambda value: 255 if value >= threshold else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError("transparent input has no alpha content")
    return bounds


def render(image: Image.Image, size: int = 256, margin: int = 16) -> Image.Image:
    left, top, right, bottom = alpha_bounds(image)
    crop = image.crop((left, top, right, bottom))
    available = size - margin * 2
    scale = min(available / crop.width, available / crop.height)
    width = max(1, round(crop.width * scale))
    height = max(1, round(crop.height * scale))
    resized = crop.resize((width, height), Image.Resampling.LANCZOS)
    output = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    output.alpha_composite(resized, ((size - width) // 2, (size - height) // 2))
    return output


def count_magenta(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= 16 and red >= 220 and blue >= 180 and green <= 70
    )


def coverage(image: Image.Image) -> float:
    return sum(image.getchannel("A").histogram()[16:]) / (image.width * image.height)


def main() -> None:
    args = parse_args()
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for name in RELICS:
        image = Image.open(args.working_dir / f"{name}-transparent.png").convert("RGBA")
        output = render(image)
        residual = count_magenta(output)
        if residual:
            raise ValueError(f"{name} contains {residual} magenta residual pixels")
        output_path = args.final_dir / f"{name}-relic-icon.png"
        output.save(output_path, optimize=True)
        print(
            f"relic={name} size=256 coverage={coverage(output):.4f} "
            f"bounds={alpha_bounds(output)} output={output_path}"
        )


if __name__ == "__main__":
    main()

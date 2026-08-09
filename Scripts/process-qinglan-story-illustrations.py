#!/usr/bin/env python3
"""Create deterministic ART-STORY-001 4096 masters and 1920x1080 story finals."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image, ImageOps


STORIES = (
    "hearing-sword",
    "old-sword-and-gourd",
    "refusing-inheritance",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def render(source: Image.Image, size: tuple[int, int]) -> Image.Image:
    return ImageOps.fit(
        source.convert("RGB"),
        size,
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.5),
    )


def count_magenta(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue in image.get_flattened_data()
        if red >= 220 and blue >= 180 and green <= 70
    )


def count_danger_red(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue in image.get_flattened_data()
        if red == 0xE4 and green == 0x5D and blue == 0x45
    )


def luma_metrics(image: Image.Image) -> tuple[int, int, float, float]:
    histogram = image.convert("L").histogram()
    count = sum(histogram)
    minimum = next(index for index, value in enumerate(histogram) if value)
    maximum = next(index for index in range(255, -1, -1) if histogram[index])
    mean = sum(index * value for index, value in enumerate(histogram)) / count
    variance = sum(((index - mean) ** 2) * value for index, value in enumerate(histogram)) / count
    return minimum, maximum, mean, math.sqrt(variance)


def validate(image: Image.Image, expected_size: tuple[int, int], name: str) -> None:
    if image.size != expected_size:
        raise ValueError(f"{name} has unexpected size {image.size}")
    if count_magenta(image):
        raise ValueError(f"{name} contains chroma-key-like magenta pixels")
    if count_danger_red(image):
        raise ValueError(f"{name} contains reserved exact danger-red pixels")
    minimum, maximum, mean, deviation = luma_metrics(image)
    if minimum > 48 or maximum < 208 or deviation < 28:
        raise ValueError(
            f"{name} luma range/contrast is insufficient: min={minimum} max={maximum} sd={deviation:.2f}"
        )
    if not 55 <= mean <= 205:
        raise ValueError(f"{name} mean luma {mean:.2f} is outside 55..205")


def digest(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def grayscale_digest(image: Image.Image) -> str:
    return hashlib.sha256(image.convert("L").tobytes()).hexdigest()


def main() -> None:
    args = parse_args()
    args.working_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    master_hashes = set()
    final_hashes = set()
    grayscale_hashes = set()

    for name in STORIES:
        source = Image.open(args.source_dir / f"{name}-imagegen.png").convert("RGB")
        master = render(source, (4096, 2304))
        final = master.resize((1920, 1080), Image.Resampling.LANCZOS)
        validate(master, (4096, 2304), name + " master")
        validate(final, (1920, 1080), name + " final")

        master_path = args.working_dir / f"{name}-source-master.png"
        final_path = args.final_dir / f"{name}-key-illustration.png"
        master.save(master_path, optimize=True)
        final.save(final_path, optimize=True)
        master_hashes.add(digest(master))
        final_hashes.add(digest(final))
        grayscale_hashes.add(grayscale_digest(final))
        minimum, maximum, mean, deviation = luma_metrics(final)
        print(
            f"story={name} source={source.size} master=4096x2304 final=1920x1080 "
            f"luma_min={minimum} luma_max={maximum} luma_mean={mean:.2f} luma_sd={deviation:.2f}"
        )

    if len(master_hashes) != len(STORIES):
        raise ValueError("story masters are not pixel-distinct")
    if len(final_hashes) != len(STORIES):
        raise ValueError("story finals are not pixel-distinct")
    if len(grayscale_hashes) != len(STORIES):
        raise ValueError("story finals are not distinct after hue is discarded")
    print(f"distinct_master_pixel_hashes={len(master_hashes)}/{len(STORIES)}")
    print(f"distinct_final_pixel_hashes={len(final_hashes)}/{len(STORIES)}")
    print(f"distinct_final_grayscale_hashes={len(grayscale_hashes)}/{len(STORIES)}")


if __name__ == "__main__":
    main()

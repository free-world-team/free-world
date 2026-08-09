#!/usr/bin/env python3
"""Create deterministic ART-UI-001 4096 masters and 2560x1440 finals."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image, ImageFilter, ImageOps


ASSETS = (
    ("title-key-art", (0.04, 0.05, 0.55, 0.64)),
    ("logo-safe-background", (0.20, 0.06, 0.80, 0.62)),
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


def crop_fraction(image: Image.Image, bounds: tuple[float, float, float, float]) -> Image.Image:
    width, height = image.size
    left, top, right, bottom = bounds
    return image.crop(
        (
            round(left * width),
            round(top * height),
            round(right * width),
            round(bottom * height),
        )
    )


def edge_ratio(image: Image.Image, threshold: int = 36) -> float:
    histogram = image.convert("L").filter(ImageFilter.FIND_EDGES).histogram()
    return sum(histogram[threshold:]) / sum(histogram)


def validate(
    image: Image.Image,
    expected_size: tuple[int, int],
    safe_bounds: tuple[float, float, float, float],
    name: str,
) -> tuple[float, float, float]:
    if image.size != expected_size:
        raise ValueError(f"{name} has unexpected size {image.size}")
    if count_magenta(image):
        raise ValueError(f"{name} contains chroma-key-like magenta pixels")
    if count_danger_red(image):
        raise ValueError(f"{name} contains reserved exact danger-red pixels")
    minimum, maximum, mean, deviation = luma_metrics(image)
    if minimum > 48 or maximum < 224 or deviation < 32:
        raise ValueError(
            f"{name} luma range/contrast is insufficient: min={minimum} max={maximum} sd={deviation:.2f}"
        )
    if not 65 <= mean <= 225:
        raise ValueError(f"{name} mean luma {mean:.2f} is outside 65..225")

    safe = crop_fraction(image, safe_bounds)
    _, _, safe_mean, safe_deviation = luma_metrics(safe)
    safe_edge_ratio = edge_ratio(safe)
    if safe_mean < 215:
        raise ValueError(f"{name} title-safe mean luma {safe_mean:.2f} is below 215")
    if safe_deviation > 20:
        raise ValueError(f"{name} title-safe luma deviation {safe_deviation:.2f} exceeds 20")
    if safe_edge_ratio > 0.03:
        raise ValueError(f"{name} title-safe edge ratio {safe_edge_ratio:.5f} exceeds 0.03")
    return safe_mean, safe_deviation, safe_edge_ratio


def pixel_digest(image: Image.Image) -> str:
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

    for name, safe_bounds in ASSETS:
        source = Image.open(args.source_dir / f"{name}-imagegen.png").convert("RGB")
        master = render(source, (4096, 2304))
        final = master.resize((2560, 1440), Image.Resampling.LANCZOS)
        validate(master, (4096, 2304), safe_bounds, name + " master")
        safe_mean, safe_deviation, safe_edges = validate(
            final, (2560, 1440), safe_bounds, name + " final"
        )

        master_path = args.working_dir / f"{name}-source-master.png"
        final_path = args.final_dir / f"{name}.png"
        master.save(master_path, optimize=True)
        final.save(final_path, optimize=True)
        master_hashes.add(pixel_digest(master))
        final_hashes.add(pixel_digest(final))
        grayscale_hashes.add(grayscale_digest(final))
        minimum, maximum, mean, deviation = luma_metrics(final)
        print(
            f"asset={name} source={source.size} master=4096x2304 final=2560x1440 "
            f"luma_min={minimum} luma_max={maximum} luma_mean={mean:.2f} luma_sd={deviation:.2f} "
            f"safe_mean={safe_mean:.2f} safe_sd={safe_deviation:.2f} safe_edge_ratio={safe_edges:.5f}"
        )

    if len(master_hashes) != len(ASSETS):
        raise ValueError("UI masters are not pixel-distinct")
    if len(final_hashes) != len(ASSETS):
        raise ValueError("UI finals are not pixel-distinct")
    if len(grayscale_hashes) != len(ASSETS):
        raise ValueError("UI finals are not distinct after hue is discarded")
    print(f"distinct_master_pixel_hashes={len(master_hashes)}/{len(ASSETS)}")
    print(f"distinct_final_pixel_hashes={len(final_hashes)}/{len(ASSETS)}")
    print(f"distinct_final_grayscale_hashes={len(grayscale_hashes)}/{len(ASSETS)}")


if __name__ == "__main__":
    main()

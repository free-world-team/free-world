#!/usr/bin/env python3
"""Normalize and validate the six ART-UI-003 page backgrounds."""

from __future__ import annotations

import argparse
import hashlib
import math
from pathlib import Path

from PIL import Image


MASTER_SIZE = (2560, 1440)
FINAL_SIZE = (1920, 1080)
SAFE_REGION = (0.04, 0.06, 0.58, 0.94)
VARIANTS = (
    "character-select",
    "map-select",
    "loadout",
    "choice",
    "hub",
    "story-result",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--asset-root", required=True, type=Path)
    return parser.parse_args()


def center_fit(source: Image.Image, size: tuple[int, int]) -> Image.Image:
    scale = max(size[0] / source.width, size[1] / source.height)
    resized = source.resize(
        (math.ceil(source.width * scale), math.ceil(source.height * scale)),
        Image.Resampling.LANCZOS,
    )
    left = (resized.width - size[0]) // 2
    top = (resized.height - size[1]) // 2
    return resized.crop((left, top, left + size[0], top + size[1]))


def apply_first_party_safe_glaze(image: Image.Image) -> Image.Image:
    """Apply a deterministic ivory-teal glaze over the existing left UI layer."""
    width, height = image.size
    wash = Image.new("RGB", image.size, (230, 234, 221))
    alpha = Image.new("L", image.size, 0)
    pixels = alpha.load()
    solid_end = round(width * 0.60)
    fade_end = round(width * 0.74)
    for x in range(width):
        if x <= solid_end:
            value = 168
        elif x >= fade_end:
            value = 0
        else:
            value = round(168 * (fade_end - x) / (fade_end - solid_end))
        for y in range(height):
            pixels[x, y] = value
    return Image.composite(wash, image, alpha)


def avoid_reserved_exact_color(image: Image.Image) -> Image.Image:
    pixels = list(image.get_flattened_data())
    changed = False
    for index, pixel in enumerate(pixels):
        if pixel == (0xE4, 0x5D, 0x45):
            pixels[index] = (0xE3, 0x5E, 0x46)
            changed = True
    if changed:
        image.putdata(pixels)
    return image


def pixel_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def grayscale_hash(image: Image.Image) -> str:
    return hashlib.sha256(image.convert("L").tobytes()).hexdigest()


def validate_final(image: Image.Image, name: str) -> tuple[float, float, float, int, int, int, int]:
    if image.mode != "RGB" or image.size != FINAL_SIZE:
        raise ValueError(f"{name} must be a 1920x1080 RGB image")
    pixels = list(image.get_flattened_data())
    magenta = sum(1 for red, green, blue in pixels if red >= 220 and blue >= 180 and green <= 70)
    danger = sum(1 for pixel in pixels if pixel == (0xE4, 0x5D, 0x45))
    if magenta:
        raise ValueError(f"{name} contains {magenta} chroma-magenta pixels")
    if danger:
        raise ValueError(f"{name} contains {danger} exact reserved-danger pixels")

    width, height = image.size
    left = round(SAFE_REGION[0] * width)
    top = round(SAFE_REGION[1] * height)
    right = round(SAFE_REGION[2] * width)
    bottom = round(SAFE_REGION[3] * height)
    safe_luma: list[int] = []
    edge_count = 0
    for y_top in range(top, bottom):
        y = height - 1 - y_top
        for x in range(left, right):
            red, green, blue = pixels[y * width + x]
            luma = (red * 54 + green * 183 + blue * 19) >> 8
            safe_luma.append(luma)
            if x > left and y_top > top:
                left_pixel = pixels[y * width + x - 1]
                above_pixel = pixels[(y + 1) * width + x]
                left_luma = (left_pixel[0] * 54 + left_pixel[1] * 183 + left_pixel[2] * 19) >> 8
                above_luma = (above_pixel[0] * 54 + above_pixel[1] * 183 + above_pixel[2] * 19) >> 8
                if max(abs(luma - left_luma), abs(luma - above_luma)) >= 36:
                    edge_count += 1
    mean = sum(safe_luma) / len(safe_luma)
    variance = sum((value - mean) ** 2 for value in safe_luma) / len(safe_luma)
    deviation = math.sqrt(variance)
    edge_ratio = edge_count / len(safe_luma)
    all_luma = [((red * 54 + green * 183 + blue * 19) >> 8) for red, green, blue in pixels]
    overall_mean = sum(all_luma) / len(all_luma)
    overall_variance = sum((value - overall_mean) ** 2 for value in all_luma) / len(all_luma)
    if not 170 <= mean <= 245:
        raise ValueError(f"{name} safe-region mean {mean:.2f} is outside 170..245")
    if deviation > 34:
        raise ValueError(f"{name} safe-region deviation {deviation:.2f} exceeds 34")
    if edge_ratio > 0.04:
        raise ValueError(f"{name} safe-region edge ratio {edge_ratio:.5f} exceeds 0.04")
    if min(all_luma) > 96 or max(all_luma) < 220 or math.sqrt(overall_variance) < 24:
        raise ValueError(f"{name} lacks full-frame tonal depth")
    return mean, deviation, edge_ratio, min(all_luma), max(all_luma), magenta, danger


def main() -> None:
    args = parse_args()
    source_dir = args.asset_root / "source"
    working_dir = args.asset_root / "working"
    final_dir = args.asset_root / "final"
    working_dir.mkdir(parents=True, exist_ok=True)
    final_dir.mkdir(parents=True, exist_ok=True)
    color_hashes: set[str] = set()
    grayscale_hashes: set[str] = set()
    for name in VARIANTS:
        source_path = source_dir / f"{name}-imagegen.png"
        with Image.open(source_path) as source:
            source_rgb = source.convert("RGB")
            if source_rgb.width < 1600 or source_rgb.height < 900:
                raise ValueError(f"{source_path} is below the approved source floor")
            master = apply_first_party_safe_glaze(center_fit(source_rgb, MASTER_SIZE))
        master = avoid_reserved_exact_color(master)
        master_path = working_dir / f"{name}-source-master.png"
        master.save(master_path, format="PNG", optimize=False, compress_level=9)
        final = master.resize(FINAL_SIZE, Image.Resampling.LANCZOS)
        final = avoid_reserved_exact_color(final)
        final_path = final_dir / f"{name}-background.png"
        final.save(final_path, format="PNG", optimize=False, compress_level=9)
        metrics = validate_final(final, name)
        color_hashes.add(pixel_hash(final))
        grayscale_hashes.add(grayscale_hash(final))
        print(
            f"variant={name} safe_mean={metrics[0]:.2f} safe_sd={metrics[1]:.2f} "
            f"safe_edge={metrics[2]:.5f} luma={metrics[3]}..{metrics[4]} "
            f"magenta={metrics[5]} danger={metrics[6]}"
        )
    if len(color_hashes) != len(VARIANTS) or len(grayscale_hashes) != len(VARIANTS):
        raise ValueError("page backgrounds are not unique in color and grayscale")


if __name__ == "__main__":
    main()

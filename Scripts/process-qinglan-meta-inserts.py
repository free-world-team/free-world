#!/usr/bin/env python3
"""Create deterministic ART-META-002 source masters and 256-pixel insert icons."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image


INSERTS = (
    "qinglan-wind-pattern",
    "herb-garden-spring-clasp",
    "old-court-vein-needle",
)
ALPHA_THRESHOLD = 16


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def sanitize(image: Image.Image) -> Image.Image:
    pixels = []
    for red, green, blue, alpha in image.convert("RGBA").get_flattened_data():
        if alpha < ALPHA_THRESHOLD:
            pixels.append((0, 0, 0, 0))
        else:
            pixels.append((red, green, blue, alpha))
    clean = Image.new("RGBA", image.size, (0, 0, 0, 0))
    clean.putdata(pixels)
    return clean


def alpha_bounds(image: Image.Image) -> tuple[int, int, int, int]:
    mask = image.getchannel("A").point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError("transparent input has no visible alpha content")
    return bounds


def render(image: Image.Image, size: int, margin: int) -> Image.Image:
    left, top, right, bottom = alpha_bounds(image)
    crop = image.crop((left, top, right, bottom))
    available = size - margin * 2
    scale = min(available / crop.width, available / crop.height)
    width = max(1, round(crop.width * scale))
    height = max(1, round(crop.height * scale))
    resized = crop.resize((width, height), Image.Resampling.LANCZOS)
    output = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    output.alpha_composite(resized, ((size - width) // 2, (size - height) // 2))
    return sanitize(output)


def count_magenta(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= ALPHA_THRESHOLD and red >= 220 and blue >= 180 and green <= 70
    )


def count_danger_red(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= ALPHA_THRESHOLD and red == 0xE4 and green == 0x5D and blue == 0x45
    )


def metrics(image: Image.Image) -> tuple[int, float, tuple[int, int, int, int]]:
    bounds = alpha_bounds(image)
    left, top, right, bottom = bounds
    margin = min(left, top, image.width - right, image.height - bottom)
    visible = sum(image.getchannel("A").histogram()[ALPHA_THRESHOLD:])
    return margin, visible / (image.width * image.height), bounds


def validate(image: Image.Image, expected_size: int, minimum_margin: int, name: str) -> None:
    if image.size != (expected_size, expected_size):
        raise ValueError(f"{name} has unexpected size {image.size}")
    margin, coverage, _ = metrics(image)
    if margin < minimum_margin:
        raise ValueError(f"{name} margin {margin} is below {minimum_margin}")
    if count_magenta(image):
        raise ValueError(f"{name} contains visible magenta residual pixels")
    if count_danger_red(image):
        raise ValueError(f"{name} contains reserved danger-red pixels")
    if not 0.20 <= coverage <= 0.75:
        raise ValueError(f"{name} coverage {coverage:.4f} is outside 0.20..0.75")


def digest(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def main() -> None:
    args = parse_args()
    args.working_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    final_hashes = set()

    for name in INSERTS:
        transparent_path = args.working_dir / f"{name}-transparent.png"
        transparent = sanitize(Image.open(transparent_path))
        transparent.save(transparent_path, optimize=True)

        master = render(transparent, 2048, 128)
        final = render(transparent, 256, 16)
        validate(master, 2048, 127, name + " source master")
        validate(final, 256, 15, name + " final")

        master_path = args.working_dir / f"{name}-source-master.png"
        final_path = args.final_dir / f"{name}-icon.png"
        master.save(master_path, optimize=True)
        final.save(final_path, optimize=True)
        final_hashes.add(digest(final))

        master_margin, master_coverage, master_bounds = metrics(master)
        final_margin, final_coverage, final_bounds = metrics(final)
        print(
            f"insert={name} master_margin={master_margin} master_coverage={master_coverage:.4f} "
            f"master_bounds={master_bounds} final_margin={final_margin} "
            f"final_coverage={final_coverage:.4f} final_bounds={final_bounds}"
        )

    if len(final_hashes) != len(INSERTS):
        raise ValueError("final insert icons are not byte-distinct after decoding")
    print(f"distinct_final_pixel_hashes={len(final_hashes)}/{len(INSERTS)}")


if __name__ == "__main__":
    main()

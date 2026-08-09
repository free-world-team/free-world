#!/usr/bin/env python3
"""Create deterministic ART-COLLECT-001 masters, illustrations, and icons."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image


COLLECTIBLES = tuple(f"old-court-{number:02d}" for number in range(1, 7))
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


def validate(
    image: Image.Image,
    expected_size: int,
    minimum_margin: int,
    minimum_coverage: float,
    name: str,
) -> None:
    if image.size != (expected_size, expected_size):
        raise ValueError(f"{name} has unexpected size {image.size}")
    margin, coverage, _ = metrics(image)
    if margin < minimum_margin:
        raise ValueError(f"{name} margin {margin} is below {minimum_margin}")
    if count_magenta(image):
        raise ValueError(f"{name} contains visible magenta residual pixels")
    if count_danger_red(image):
        raise ValueError(f"{name} contains reserved danger-red pixels")
    if not minimum_coverage <= coverage <= 0.75:
        raise ValueError(
            f"{name} coverage {coverage:.4f} is outside {minimum_coverage:.2f}..0.75"
        )


def digest(image: Image.Image) -> str:
    return hashlib.sha256(image.tobytes()).hexdigest()


def main() -> None:
    args = parse_args()
    args.working_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    illustration_hashes = set()
    icon_hashes = set()

    for name in COLLECTIBLES:
        transparent_path = args.working_dir / f"{name}-transparent.png"
        transparent = sanitize(Image.open(transparent_path))
        transparent.save(transparent_path, optimize=True)

        master = render(transparent, 2048, 128)
        illustration = render(transparent, 1024, 64)
        icon = render(transparent, 256, 16)
        # The three separated chime leaves intentionally carry more negative space than solid artifacts.
        minimum_coverage = 0.13 if name == "old-court-06" else 0.18
        validate(master, 2048, 127, minimum_coverage, name + " source master")
        validate(illustration, 1024, 63, minimum_coverage, name + " illustration")
        validate(icon, 256, 15, minimum_coverage, name + " icon")

        master_path = args.working_dir / f"{name}-source-master.png"
        illustration_path = args.final_dir / f"{name}-illustration.png"
        icon_path = args.final_dir / f"{name}-icon.png"
        master.save(master_path, optimize=True)
        illustration.save(illustration_path, optimize=True)
        icon.save(icon_path, optimize=True)
        illustration_hashes.add(digest(illustration))
        icon_hashes.add(digest(icon))

        master_margin, master_coverage, _ = metrics(master)
        illustration_margin, illustration_coverage, _ = metrics(illustration)
        icon_margin, icon_coverage, _ = metrics(icon)
        print(
            f"collectible={name} master_margin={master_margin} master_coverage={master_coverage:.4f} "
            f"illustration_margin={illustration_margin} illustration_coverage={illustration_coverage:.4f} "
            f"icon_margin={icon_margin} icon_coverage={icon_coverage:.4f}"
        )

    if len(illustration_hashes) != len(COLLECTIBLES):
        raise ValueError("collectible illustrations are not pixel-distinct")
    if len(icon_hashes) != len(COLLECTIBLES):
        raise ValueError("collectible icons are not pixel-distinct")
    print(f"distinct_illustration_pixel_hashes={len(illustration_hashes)}/{len(COLLECTIBLES)}")
    print(f"distinct_icon_pixel_hashes={len(icon_hashes)}/{len(COLLECTIBLES)}")


if __name__ == "__main__":
    main()

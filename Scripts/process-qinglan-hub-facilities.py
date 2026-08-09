#!/usr/bin/env python3
"""Build deterministic Qinglan hub facility panels and icons from approved transparent sources."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image


FACILITIES = (
    "vein-inquiry-platform",
    "scroll-pavilion",
    "hundred-artifact-pavilion",
    "myriad-phenomena-pavilion",
)

ALPHA_THRESHOLD = 16
SOURCE_MASTER_SIZE = 2048
PANEL_SIZE = 1024
ICON_SIZE = 256
SOURCE_MASTER_MARGIN = 128
PANEL_MARGIN = 64
ICON_MARGIN = 16


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def crop_visible(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0).getbbox()
    if bounds is None:
        raise ValueError("approved facility source contains no visible pixels")
    return image.crop(bounds)


def render_square(source: Image.Image, size: int, margin: int) -> Image.Image:
    maximum = size - 2 * margin
    scale = min(maximum / source.width, maximum / source.height)
    width = max(1, round(source.width * scale))
    height = max(1, round(source.height * scale))
    rendered = source.resize((width, height), Image.Resampling.LANCZOS)
    output = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    output.alpha_composite(rendered, ((size - width) // 2, (size - height) // 2))
    return output


def visible_magenta(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= ALPHA_THRESHOLD and red >= 220 and blue >= 180 and green <= 70
    )


def danger_pixels(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= ALPHA_THRESHOLD and red == 0xE4 and green == 0x5D and blue == 0x45
    )


def metrics(image: Image.Image) -> tuple[int, float, str]:
    alpha = image.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0).getbbox()
    if bounds is None:
        raise ValueError("empty facility output")
    margin = min(bounds[0], bounds[1], image.width - bounds[2], image.height - bounds[3])
    covered = sum(1 for value in alpha.get_flattened_data() if value >= ALPHA_THRESHOLD)
    coverage = covered / (image.width * image.height)
    return margin, coverage, hashlib.sha256(image.tobytes()).hexdigest()


def main() -> None:
    args = parse_args()
    args.final_dir.mkdir(parents=True, exist_ok=True)
    output_hashes = set()
    for facility in FACILITIES:
        transparent_path = args.working_dir / f"{facility}-transparent.png"
        transparent = Image.open(transparent_path).convert("RGBA")
        transparent.putalpha(
            transparent.getchannel("A").point(
                lambda value: 0 if value < ALPHA_THRESHOLD else value
            )
        )
        transparent.save(transparent_path, optimize=True)
        cropped = crop_visible(transparent)
        source_master = render_square(cropped, SOURCE_MASTER_SIZE, SOURCE_MASTER_MARGIN)
        panel = render_square(cropped, PANEL_SIZE, PANEL_MARGIN)
        icon = render_square(cropped, ICON_SIZE, ICON_MARGIN)

        source_master_path = args.working_dir / f"{facility}-source-master.png"
        panel_path = args.final_dir / f"{facility}-panel.png"
        icon_path = args.final_dir / f"{facility}-icon.png"
        source_master.save(source_master_path, optimize=True)
        panel.save(panel_path, optimize=True)
        icon.save(icon_path, optimize=True)

        panel_margin, panel_coverage, panel_hash = metrics(panel)
        icon_margin, icon_coverage, icon_hash = metrics(icon)
        if panel_margin < PANEL_MARGIN - 1 or icon_margin < ICON_MARGIN - 1:
            raise ValueError(f"{facility} violates panel/icon safe margins")
        for output, output_hash in ((panel, panel_hash), (icon, icon_hash)):
            magenta = visible_magenta(output)
            danger = danger_pixels(output)
            if magenta:
                raise ValueError(f"{facility} output contains {magenta} visible magenta pixels")
            if danger:
                raise ValueError(f"{facility} output contains {danger} exact P0 danger-red pixels")
            if output_hash in output_hashes:
                raise ValueError(f"{facility} output duplicates another facility output")
            output_hashes.add(output_hash)

        print(
            f"facility={facility} source={transparent.size} source_master={source_master.size} "
            f"panel={panel.size} panel_margin={panel_margin} panel_coverage={panel_coverage:.4f} "
            f"icon={icon.size} icon_margin={icon_margin} icon_coverage={icon_coverage:.4f} "
            f"magenta=0 danger=0"
        )

    if len(output_hashes) != len(FACILITIES) * 2:
        raise ValueError("facility output hashes are not unique")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Build deterministic 16-tile Old Court atlases from approved full-bleed region sources."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image


REGIONS = (
    "central-training-ground",
    "west-herb-garden",
    "east-sword-gallery",
    "north-old-gate",
    "south-guest-court",
)

GRID = 4
TILE_SIZE = 256
MASTER_TILE_SIZE = 512
ATLAS_COLUMNS = 8
ATLAS_ROWS = 2
MAX_MEAN_SEAM_DELTA = 12.0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def danger_pixels(image: Image.Image) -> int:
    return sum(
        1
        for red, green, blue, alpha in image.get_flattened_data()
        if alpha >= 16 and red == 0xE4 and green == 0x5D and blue == 0x45
    )


def mean_internal_seam_delta(image: Image.Image) -> float:
    pixels = image.load()
    total = 0
    samples = 0
    for boundary in range(1, GRID):
        coordinate = boundary * TILE_SIZE
        for offset in range(image.height):
            left = pixels[coordinate - 1, offset]
            right = pixels[coordinate, offset]
            total += abs(left[0] - right[0]) + abs(left[1] - right[1]) + abs(left[2] - right[2])
            samples += 3
        for offset in range(image.width):
            above = pixels[offset, coordinate - 1]
            below = pixels[offset, coordinate]
            total += abs(above[0] - below[0]) + abs(above[1] - below[1]) + abs(above[2] - below[2])
            samples += 3
    return total / samples


def build_atlas(tiles: list[Image.Image], tile_size: int) -> Image.Image:
    atlas = Image.new("RGBA", (ATLAS_COLUMNS * tile_size, ATLAS_ROWS * tile_size), (0, 0, 0, 255))
    for index, tile in enumerate(tiles):
        rendered = tile if tile.width == tile_size else tile.resize((tile_size, tile_size), Image.Resampling.LANCZOS)
        atlas.paste(rendered, ((index % ATLAS_COLUMNS) * tile_size, (index // ATLAS_COLUMNS) * tile_size))
    return atlas


def unique_tile_count(tiles: list[Image.Image]) -> int:
    return len({hashlib.sha256(tile.tobytes()).digest() for tile in tiles})


def main() -> None:
    args = parse_args()
    args.working_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for region in REGIONS:
        source = Image.open(args.source_dir / f"{region}-imagegen.png").convert("RGBA")
        normalized = source.resize((GRID * TILE_SIZE, GRID * TILE_SIZE), Image.Resampling.LANCZOS)
        tiles = []
        for row in range(GRID):
            for column in range(GRID):
                tiles.append(normalized.crop((
                    column * TILE_SIZE,
                    row * TILE_SIZE,
                    (column + 1) * TILE_SIZE,
                    (row + 1) * TILE_SIZE,
                )))

        master = build_atlas(tiles, MASTER_TILE_SIZE)
        final = build_atlas(tiles, TILE_SIZE)
        seam_delta = mean_internal_seam_delta(normalized)
        unique_tiles = unique_tile_count(tiles)
        opaque = final.getextrema()[3] == (255, 255)
        if unique_tiles != GRID * GRID:
            raise ValueError(f"{region} must contain {GRID * GRID} unique tiles, got {unique_tiles}")
        if seam_delta > MAX_MEAN_SEAM_DELTA:
            raise ValueError(
                f"{region} mean seam delta {seam_delta:.4f} exceeds {MAX_MEAN_SEAM_DELTA:.4f}"
            )
        if not opaque:
            raise ValueError(f"{region} final atlas must be fully opaque")
        if danger_pixels(final):
            raise ValueError(f"{region} contains exact P0 danger-red pixels")
        working_path = args.working_dir / f"{region}-tile-kit-master.png"
        final_path = args.final_dir / f"{region}-tile-kit-atlas.png"
        master.save(working_path, optimize=True)
        final.save(final_path, optimize=True)
        print(
            f"region={region} source={source.size} master={master.size} final={final.size} "
            f"tiles={len(tiles)} unique_tiles={unique_tiles} opaque={opaque} "
            f"seam_delta={seam_delta:.4f} output={final_path}"
        )


if __name__ == "__main__":
    main()

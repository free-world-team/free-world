#!/usr/bin/env python3
"""Build deterministic 16-prop Old Court atlases from approved transparent source sheets."""

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

SOURCE_GRID = 4
ATLAS_COLUMNS = 8
ATLAS_ROWS = 2
MASTER_CELL_SIZE = 512
FINAL_CELL_SIZE = 256
ALPHA_THRESHOLD = 16
FINAL_SAFE_MARGIN = 24


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def split_subjects(sheet: Image.Image) -> list[Image.Image]:
    subjects = []
    for row in range(SOURCE_GRID):
        top = round(row * sheet.height / SOURCE_GRID)
        bottom = round((row + 1) * sheet.height / SOURCE_GRID)
        for column in range(SOURCE_GRID):
            left = round(column * sheet.width / SOURCE_GRID)
            right = round((column + 1) * sheet.width / SOURCE_GRID)
            cell = sheet.crop((left, top, right, bottom))
            alpha = cell.getchannel("A")
            thresholded = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
            bounds = thresholded.getbbox()
            if bounds is None:
                raise ValueError(f"empty source cell r{row} c{column}")
            subjects.append(cell.crop(bounds))
    return subjects


def render_subject(subject: Image.Image, cell_size: int, safe_margin: int) -> Image.Image:
    maximum = cell_size - 2 * safe_margin
    scale = min(maximum / subject.width, maximum / subject.height)
    width = max(1, round(subject.width * scale))
    height = max(1, round(subject.height * scale))
    rendered = subject.resize((width, height), Image.Resampling.LANCZOS)
    cell = Image.new("RGBA", (cell_size, cell_size), (0, 0, 0, 0))
    cell.alpha_composite(rendered, ((cell_size - width) // 2, (cell_size - height) // 2))
    return cell


def build_atlas(subjects: list[Image.Image], cell_size: int, safe_margin: int) -> Image.Image:
    atlas = Image.new(
        "RGBA",
        (ATLAS_COLUMNS * cell_size, ATLAS_ROWS * cell_size),
        (0, 0, 0, 0),
    )
    for index, subject in enumerate(subjects):
        cell = render_subject(subject, cell_size, safe_margin)
        atlas.alpha_composite(
            cell,
            ((index % ATLAS_COLUMNS) * cell_size, (index // ATLAS_COLUMNS) * cell_size),
        )
    return atlas


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


def cell_metrics(atlas: Image.Image, cell_size: int) -> tuple[int, int, float, float]:
    hashes = set()
    minimum_margin = cell_size
    minimum_coverage = 1.0
    maximum_coverage = 0.0
    for index in range(ATLAS_COLUMNS * ATLAS_ROWS):
        left = (index % ATLAS_COLUMNS) * cell_size
        top = (index // ATLAS_COLUMNS) * cell_size
        cell = atlas.crop((left, top, left + cell_size, top + cell_size))
        hashes.add(hashlib.sha256(cell.tobytes()).digest())
        alpha = cell.getchannel("A")
        thresholded = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
        bounds = thresholded.getbbox()
        if bounds is None:
            raise ValueError(f"empty output cell {index}")
        minimum_margin = min(
            minimum_margin,
            bounds[0],
            bounds[1],
            cell_size - bounds[2],
            cell_size - bounds[3],
        )
        covered = sum(1 for value in alpha.get_flattened_data() if value >= ALPHA_THRESHOLD)
        coverage = covered / (cell_size * cell_size)
        minimum_coverage = min(minimum_coverage, coverage)
        maximum_coverage = max(maximum_coverage, coverage)
    return len(hashes), minimum_margin, minimum_coverage, maximum_coverage


def main() -> None:
    args = parse_args()
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for region in REGIONS:
        transparent_path = args.working_dir / f"{region}-props-transparent.png"
        sheet = Image.open(transparent_path).convert("RGBA")
        subjects = split_subjects(sheet)
        master = build_atlas(subjects, MASTER_CELL_SIZE, FINAL_SAFE_MARGIN * 2)
        final = build_atlas(subjects, FINAL_CELL_SIZE, FINAL_SAFE_MARGIN)
        unique, margin, minimum_coverage, maximum_coverage = cell_metrics(final, FINAL_CELL_SIZE)
        if unique != SOURCE_GRID * SOURCE_GRID:
            raise ValueError(f"{region} must contain 16 unique prop cells, got {unique}")
        if margin < FINAL_SAFE_MARGIN - 1:
            raise ValueError(f"{region} safe margin {margin} is below {FINAL_SAFE_MARGIN - 1}")
        magenta = visible_magenta(final)
        danger = danger_pixels(final)
        if magenta:
            raise ValueError(f"{region} contains {magenta} visible magenta pixels")
        if danger:
            raise ValueError(f"{region} contains {danger} exact P0 danger-red pixels")

        master_path = args.working_dir / f"{region}-prop-set-master.png"
        final_path = args.final_dir / f"{region}-prop-set-atlas.png"
        master.save(master_path, optimize=True)
        final.save(final_path, optimize=True)
        print(
            f"region={region} source={sheet.size} master={master.size} final={final.size} "
            f"props={len(subjects)} unique={unique} margin={margin} "
            f"coverage={minimum_coverage:.4f}-{maximum_coverage:.4f} "
            f"magenta={magenta} danger={danger} output={final_path}"
        )


if __name__ == "__main__":
    main()

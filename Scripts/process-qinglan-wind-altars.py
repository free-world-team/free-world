#!/usr/bin/env python3
"""Build deterministic three-state Qinglan wind-altar atlases from approved transparent sheets."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image


ALTARS = (
    "listen-wind-altar",
    "guide-wind-altar",
    "stop-balance-wind-altar",
)

STATE_COUNT = 3
ALPHA_THRESHOLD = 16
SOURCE_MASTER_SIZE = 2048
MASTER_CELL_SIZE = 640
FINAL_CELL_SIZE = 320
FINAL_SAFE_MARGIN = 20


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--working-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def split_states(sheet: Image.Image) -> list[Image.Image]:
    states = []
    for index in range(STATE_COUNT):
        left = round(index * sheet.width / STATE_COUNT)
        right = round((index + 1) * sheet.width / STATE_COUNT)
        cell = sheet.crop((left, 0, right, sheet.height))
        alpha = cell.getchannel("A")
        thresholded = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
        bounds = thresholded.getbbox()
        if bounds is None:
            raise ValueError(f"empty source state {index}")
        states.append(cell.crop(bounds))
    return states


def render_state(state: Image.Image, cell_size: int, safe_margin: int) -> Image.Image:
    maximum = cell_size - 2 * safe_margin
    scale = min(maximum / state.width, maximum / state.height)
    width = max(1, round(state.width * scale))
    height = max(1, round(state.height * scale))
    rendered = state.resize((width, height), Image.Resampling.LANCZOS)
    cell = Image.new("RGBA", (cell_size, cell_size), (0, 0, 0, 0))
    cell.alpha_composite(rendered, ((cell_size - width) // 2, (cell_size - height) // 2))
    return cell


def build_atlas(states: list[Image.Image], cell_size: int, safe_margin: int) -> Image.Image:
    atlas = Image.new("RGBA", (STATE_COUNT * cell_size, cell_size), (0, 0, 0, 0))
    for index, state in enumerate(states):
        atlas.alpha_composite(render_state(state, cell_size, safe_margin), (index * cell_size, 0))
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


def cell_metrics(atlas: Image.Image) -> tuple[int, int, float, float]:
    hashes = set()
    minimum_margin = FINAL_CELL_SIZE
    minimum_coverage = 1.0
    maximum_coverage = 0.0
    for index in range(STATE_COUNT):
        cell = atlas.crop(
            (index * FINAL_CELL_SIZE, 0, (index + 1) * FINAL_CELL_SIZE, FINAL_CELL_SIZE)
        )
        hashes.add(hashlib.sha256(cell.tobytes()).digest())
        alpha = cell.getchannel("A")
        thresholded = alpha.point(lambda value: 255 if value >= ALPHA_THRESHOLD else 0)
        bounds = thresholded.getbbox()
        if bounds is None:
            raise ValueError(f"empty output state {index}")
        minimum_margin = min(
            minimum_margin,
            bounds[0],
            bounds[1],
            FINAL_CELL_SIZE - bounds[2],
            FINAL_CELL_SIZE - bounds[3],
        )
        covered = sum(1 for value in alpha.get_flattened_data() if value >= ALPHA_THRESHOLD)
        coverage = covered / (FINAL_CELL_SIZE * FINAL_CELL_SIZE)
        minimum_coverage = min(minimum_coverage, coverage)
        maximum_coverage = max(maximum_coverage, coverage)
    return len(hashes), minimum_margin, minimum_coverage, maximum_coverage


def main() -> None:
    args = parse_args()
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for altar in ALTARS:
        transparent_path = args.working_dir / f"{altar}-transparent.png"
        sheet = Image.open(transparent_path).convert("RGBA")
        source_master = sheet.resize(
            (SOURCE_MASTER_SIZE, SOURCE_MASTER_SIZE), Image.Resampling.LANCZOS
        )
        states = split_states(sheet)
        master = build_atlas(states, MASTER_CELL_SIZE, FINAL_SAFE_MARGIN * 2)
        final = build_atlas(states, FINAL_CELL_SIZE, FINAL_SAFE_MARGIN)
        unique, margin, minimum_coverage, maximum_coverage = cell_metrics(final)
        if unique != STATE_COUNT:
            raise ValueError(f"{altar} must contain three unique states, got {unique}")
        if margin < FINAL_SAFE_MARGIN - 1:
            raise ValueError(f"{altar} safe margin {margin} is below {FINAL_SAFE_MARGIN - 1}")
        magenta = visible_magenta(final)
        danger = danger_pixels(final)
        if magenta:
            raise ValueError(f"{altar} contains {magenta} visible magenta pixels")
        if danger:
            raise ValueError(f"{altar} contains {danger} exact P0 danger-red pixels")

        source_master_path = args.working_dir / f"{altar}-source-master.png"
        atlas_master_path = args.working_dir / f"{altar}-state-atlas-master.png"
        final_path = args.final_dir / f"{altar}-state-atlas.png"
        source_master.save(source_master_path, optimize=True)
        master.save(atlas_master_path, optimize=True)
        final.save(final_path, optimize=True)
        print(
            f"altar={altar} source={sheet.size} source_master={source_master.size} "
            f"atlas_master={master.size} final={final.size} states={len(states)} unique={unique} "
            f"margin={margin} coverage={minimum_coverage:.4f}-{maximum_coverage:.4f} "
            f"magenta={magenta} danger={danger} output={final_path}"
        )


if __name__ == "__main__":
    main()

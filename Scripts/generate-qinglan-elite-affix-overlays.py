#!/usr/bin/env python3
"""Generate four deterministic first-party Qinglan elite-affix overlays."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Iterable, Sequence

from PIL import Image, ImageDraw, ImageFilter


Point = tuple[float, float]
Color = tuple[int, int, int, int]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--spec", required=True, type=Path)
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def rgba(value: str, alpha: int = 255) -> Color:
    value = value.lstrip("#")
    if len(value) != 6:
        raise ValueError(f"Expected six-digit RGB color, received {value!r}.")
    return tuple(int(value[index:index + 2], 16) for index in (0, 2, 4)) + (alpha,)


def polar(center: Point, radii: Point, angle: float) -> Point:
    radians = math.radians(angle)
    return (
        center[0] + math.cos(radians) * radii[0],
        center[1] + math.sin(radians) * radii[1],
    )


def rotate(center: Point, points: Sequence[Point], angle: float) -> list[Point]:
    radians = math.radians(angle)
    cosine = math.cos(radians)
    sine = math.sin(radians)
    return [
        (
            center[0] + x * cosine - y * sine,
            center[1] + x * sine + y * cosine,
        )
        for x, y in points
    ]


class AffixCanvas:
    def __init__(self, size: int, palette: dict[str, str]) -> None:
        self.size = size
        self.image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.glow_draw = ImageDraw.Draw(self.glow)
        self.core_draw = ImageDraw.Draw(self.core)
        self.primary = rgba(palette["primary"])
        self.highlight = rgba(palette["highlight"])
        self.deep = rgba(palette["deep"])
        self.secondary = rgba(palette["secondary"])

    def line(self, points: Iterable[Point], width: int, color: Color | None = None) -> None:
        points = list(points)
        color = color or self.primary
        self.glow_draw.line(points, fill=color[:3] + (90,), width=width + 28, joint="curve")
        self.core_draw.line(points, fill=self.deep, width=width + 10, joint="curve")
        self.core_draw.line(points, fill=color, width=width, joint="curve")

    def arc(
        self,
        center: Point,
        radii: Point,
        start: float,
        end: float,
        width: int,
        color: Color | None = None,
    ) -> None:
        color = color or self.primary
        box = (
            center[0] - radii[0], center[1] - radii[1],
            center[0] + radii[0], center[1] + radii[1],
        )
        self.glow_draw.arc(box, start, end, fill=color[:3] + (90,), width=width + 28)
        self.core_draw.arc(box, start, end, fill=self.deep, width=width + 10)
        self.core_draw.arc(box, start, end, fill=color, width=width)

    def polygon(self, points: Iterable[Point], fill: Color | None = None) -> None:
        points = list(points)
        fill = fill or self.primary
        center = (
            sum(point[0] for point in points) / len(points),
            sum(point[1] for point in points) / len(points),
        )
        expanded = [
            (
                center[0] + (point[0] - center[0]) * 1.18,
                center[1] + (point[1] - center[1]) * 1.18,
            )
            for point in points
        ]
        self.glow_draw.polygon(expanded, fill=fill[:3] + (96,))
        self.core_draw.polygon(expanded, fill=self.deep)
        self.core_draw.polygon(points, fill=fill)

    def chevron(self, center: Point, angle: float, scale: float = 1.0) -> None:
        points = rotate(
            center,
            [(-31 * scale, -23 * scale), (15 * scale, 0), (-31 * scale, 23 * scale)],
            angle,
        )
        self.line(points, max(8, round(14 * scale)), self.highlight)

    def diamond(self, center: Point, radius: float, color: Color | None = None) -> None:
        self.polygon(
            [
                (center[0], center[1] - radius),
                (center[0] + radius * .74, center[1]),
                (center[0], center[1] + radius),
                (center[0] - radius * .74, center[1]),
            ],
            color or self.highlight,
        )

    def finish(self) -> Image.Image:
        self.image.alpha_composite(self.glow.filter(ImageFilter.GaussianBlur(15)))
        self.image.alpha_composite(self.core)
        return self.image


def build_frenzy(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = AffixCanvas(size, palette)
    center = (size * .5, size * .52)
    for index, radii in enumerate(((332, 254), (292, 220), (250, 184))):
        offset = phase + index * 9
        canvas.arc(center, radii, 166 + offset, 326 + offset, 12 - index * 2)
        canvas.arc(center, radii, 344 + offset, 58 + offset + 360, 12 - index * 2)
    for angle in (190, 235, 280, 325, 12, 55):
        point = polar(center, (356, 276), angle + phase)
        canvas.chevron(point, angle + 90 + phase, .82)
    return canvas.finish()


def build_barrier(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = AffixCanvas(size, palette)
    center = (size * .5, size * .51)
    outer_radius = 340
    inner_radius = 278
    for side in range(6):
        start = -90 + side * 60 + 6 + phase * .25
        end = -90 + (side + 1) * 60 - 6 + phase * .25
        segment = [
            polar(center, (outer_radius, outer_radius), start),
            polar(center, (outer_radius, outer_radius), end),
            polar(center, (inner_radius, inner_radius), end),
            polar(center, (inner_radius, inner_radius), start),
        ]
        canvas.polygon(segment, canvas.primary if side % 2 == 0 else canvas.secondary)
        canvas.diamond(polar(center, (370, 370), (start + end) * .5), 16, canvas.highlight)
    return canvas.finish()


def build_splitting(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = AffixCanvas(size, palette)
    center = (size * .5, size * .52)
    canvas.arc(center, (305, 246), 194 + phase, 332 + phase, 11)
    canvas.arc(center, (305, 246), 348 + phase, 71 + phase + 360, 11)
    canvas.arc(center, (238, 188), 212 + phase, 287 + phase, 8, canvas.secondary)
    canvas.arc(center, (238, 188), 311 + phase, 50 + phase + 360, 8, canvas.secondary)

    crack = [(-16, -64), (8, -35), (-10, -8), (16, 21), (-5, 52), (13, 82)]
    for angle in (30 + phase, 150 + phase, 270 + phase):
        anchor = polar(center, (289, 230), angle)
        canvas.line(rotate(anchor, crack, angle + 90), 10, canvas.highlight)

    canvas.diamond((270, 760), 45, canvas.primary)
    canvas.diamond((754, 760), 45, canvas.secondary)
    canvas.chevron((362, 716), 165, .72)
    canvas.chevron((662, 716), 15, .72)
    return canvas.finish()


def build_quake(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = AffixCanvas(size, palette)
    center = (size * .5, size * .61)
    for ring, radii in enumerate(((350, 226), (285, 176), (222, 130))):
        for sector in range(4):
            start = 8 + sector * 90 + phase + ring * 5
            canvas.arc(center, radii, start, start + 62, 15 - ring * 2,
                       canvas.primary if ring != 1 else canvas.secondary)

    for angle in (12, 57, 102, 147, 192, 237, 282, 327):
        point = polar(center, (382, 248), angle + phase)
        triangle = rotate(point, [(0, -31), (25, 24), (-25, 24)], angle + 90 + phase)
        canvas.polygon(triangle, canvas.highlight if angle % 90 else canvas.secondary)
    return canvas.finish()


def main() -> None:
    args = parse_args()
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    source_size = int(spec["sourceSize"])
    runtime_size = int(spec["runtimeSize"])
    phase = float(int(spec["seed"]) % 5)
    builders = {
        "frenzy": build_frenzy,
        "barrier": build_barrier,
        "splitting": build_splitting,
        "quake": build_quake,
    }
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)

    for affix in spec["affixes"]:
        image = builders[affix["name"]](source_size, affix["palette"], phase)
        source_path = args.source_dir / affix["sourceFile"]
        final_path = args.final_dir / affix["finalFile"]
        image.save(source_path, optimize=True)
        runtime = image.resize((runtime_size, runtime_size), Image.Resampling.LANCZOS)
        runtime.save(final_path, optimize=True)
        coverage = sum(runtime.getchannel("A").histogram()[16:]) / (runtime_size * runtime_size)
        print(f"affix={affix['name']} source={source_path} final={final_path} coverage={coverage:.4f}")


if __name__ == "__main__":
    main()

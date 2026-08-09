#!/usr/bin/env python3
"""Generate six deterministic first-party Qinglan base-weapon VFX textures."""

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
    return center[0] + math.cos(radians) * radii[0], center[1] + math.sin(radians) * radii[1]


def rotate(center: Point, points: Sequence[Point], angle: float) -> list[Point]:
    radians = math.radians(angle)
    cosine, sine = math.cos(radians), math.sin(radians)
    return [
        (center[0] + x * cosine - y * sine, center[1] + x * sine + y * cosine)
        for x, y in points
    ]


class Canvas:
    def __init__(self, size: int, palette: dict[str, str]) -> None:
        self.scale = size / 1024
        self.image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.g = ImageDraw.Draw(self.glow)
        self.c = ImageDraw.Draw(self.core)
        self.primary = rgba(palette["primary"])
        self.highlight = rgba(palette["highlight"])
        self.deep = rgba(palette["deep"])
        self.secondary = rgba(palette["secondary"])

    def point(self, point: Point) -> Point:
        return point[0] * self.scale, point[1] * self.scale

    def line(self, points: Iterable[Point], width: int, ink: Color | None = None) -> None:
        points = [self.point(point) for point in points]
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        self.g.line(points, fill=ink[:3] + (88,), width=width + round(26 * self.scale), joint="curve")
        self.c.line(points, fill=self.deep, width=width + round(9 * self.scale), joint="curve")
        self.c.line(points, fill=ink, width=width, joint="curve")

    def arc(self, center: Point, radii: Point, start: float, end: float, width: int, ink: Color | None = None) -> None:
        center = self.point(center)
        radii = self.point(radii)
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        box = (center[0] - radii[0], center[1] - radii[1], center[0] + radii[0], center[1] + radii[1])
        self.g.arc(box, start, end, fill=ink[:3] + (88,), width=width + round(26 * self.scale))
        self.c.arc(box, start, end, fill=self.deep, width=width + round(9 * self.scale))
        self.c.arc(box, start, end, fill=ink, width=width)

    def polygon(self, points: Iterable[Point], ink: Color | None = None) -> None:
        points = [self.point(point) for point in points]
        ink = ink or self.primary
        center = (sum(x for x, _ in points) / len(points), sum(y for _, y in points) / len(points))
        expanded = [
            (center[0] + (x - center[0]) * 1.13, center[1] + (y - center[1]) * 1.13)
            for x, y in points
        ]
        self.g.polygon(expanded, fill=ink[:3] + (92,))
        self.c.polygon(expanded, fill=self.deep)
        self.c.polygon(points, fill=ink)

    def ellipse(self, box: tuple[float, float, float, float], ink: Color | None = None) -> None:
        box = tuple(value * self.scale for value in box)
        ink = ink or self.primary
        grow = 12 * self.scale
        glow_box = (box[0] - grow, box[1] - grow, box[2] + grow, box[3] + grow)
        self.g.ellipse(glow_box, fill=ink[:3] + (92,))
        self.c.ellipse(glow_box, fill=self.deep)
        self.c.ellipse(box, fill=ink)

    def finish(self) -> Image.Image:
        self.image.alpha_composite(self.glow.filter(ImageFilter.GaussianBlur(round(14 * self.scale))))
        self.image.alpha_composite(self.core)
        return self.image


def build_yufeng_sword(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (530, 510)
    canvas.polygon(rotate(center, [(-340, -25), (232, -25), (340, 0), (232, 25), (-340, 25)], -9), canvas.highlight)
    canvas.polygon(rotate(center, [(-365, -58), (-322, -22), (-322, 22), (-365, 58), (-402, 0)], -9), canvas.secondary)
    canvas.line(rotate(center, [(-315, 0), (248, 0)], -9), 10, canvas.primary)
    for offset, width in ((-118, 13), (-62, 10), (-10, 8)):
        canvas.arc((425, 520 + offset), (330, 140), 192 + phase, 345 + phase, width, canvas.primary)
    return canvas.finish()


def build_yellow_talisman(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    canvas.polygon([(374, 180), (650, 180), (684, 230), (654, 836), (370, 836), (340, 230)], canvas.highlight)
    canvas.polygon([(394, 230), (630, 230), (610, 788), (414, 788)], rgba("#E7D8A3", 238))
    canvas.line([(512, 252), (512, 350), (448, 424), (566, 486), (452, 568), (566, 650), (512, 758)], 24, canvas.primary)
    canvas.line([(414, 350), (610, 350)], 14, canvas.secondary)
    canvas.line([(414, 680), (610, 680)], 14, canvas.secondary)
    for y in (290, 734):
        canvas.arc((512, y), (82, 48), 195, 345, 9, canvas.deep)
    return canvas.finish()


def build_lihuo_wheel(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    for start in range(0, 360, 60):
        canvas.arc(center, (284, 284), start + 8 + phase, start + 48 + phase, 28, canvas.primary)
        blade = polar(center, (332, 332), start + 30 + phase)
        canvas.polygon(rotate(blade, [(-42, -20), (55, 0), (-42, 20), (-16, 0)], start + 30 + phase), canvas.highlight)
        canvas.line([polar(center, (104, 104), start + 30), polar(center, (245, 245), start + 30)], 18, canvas.secondary)
    canvas.ellipse((432, 432, 592, 592), canvas.deep)
    canvas.ellipse((468, 468, 556, 556), canvas.highlight)
    for angle in (12, 132, 252):
        point = polar(center, (390, 390), angle + phase)
        canvas.polygon(rotate(point, [(-18, 22), (0, -30), (18, 22)], angle), canvas.primary)
    return canvas.finish()


def build_tide_orb(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    canvas.ellipse((392, 392, 632, 632), canvas.highlight)
    canvas.ellipse((432, 432, 592, 592), canvas.primary)
    for start in (22 + phase, 202 + phase):
        canvas.arc(center, (330, 248), start, start + 128, 28, canvas.secondary)
        canvas.arc(center, (276, 194), start + 18, start + 112, 12, canvas.highlight)
    for angle in (65, 145, 245, 325):
        point = polar(center, (370, 292), angle)
        canvas.polygon([(point[0], point[1] - 34), (point[0] + 23, point[1] + 18), (point[0], point[1] + 35), (point[0] - 23, point[1] + 18)], canvas.primary)
    return canvas.finish()


def build_zhenyue_seal(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 536)
    for side in range(8):
        start = -90 + side * 45 + 5
        end = -90 + (side + 1) * 45 - 5
        canvas.line([polar(center, (350, 290), start), polar(center, (350, 290), end)], 20, canvas.primary)
    canvas.polygon([(350, 650), (438, 455), (494, 548), (560, 378), (692, 650)], canvas.secondary)
    canvas.line([(306, 650), (718, 650)], 20, canvas.highlight)
    for angle in (0, 90, 180, 270):
        point = polar(center, (278, 226), angle)
        canvas.polygon(rotate(point, [(-22, -36), (22, -36), (34, 20), (0, 42), (-34, 20)], angle), canvas.highlight)
    return canvas.finish()


def build_spirit_vine_seed(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    canvas.ellipse((426, 382, 598, 616), canvas.highlight)
    canvas.ellipse((458, 424, 566, 588), canvas.primary)
    paths = [
        [(490, 570), (420, 620), (354, 704), (250, 748)],
        [(520, 574), (548, 654), (526, 758), (446, 842)],
        [(542, 562), (630, 620), (700, 700), (790, 730)],
    ]
    for index, points in enumerate(paths):
        canvas.line(points, 20 - index * 2, canvas.primary if index != 1 else canvas.secondary)
        for point_index in (1, 2):
            x, y = points[point_index]
            side = -1 if (index + point_index) % 2 else 1
            canvas.polygon([(x, y), (x + side * 72, y - 42), (x + side * 48, y + 34)], canvas.highlight)
    for angle in (205, 270, 335):
        canvas.arc((512, 520), (300, 248), angle + phase, angle + 42 + phase, 10, canvas.secondary)
    return canvas.finish()


BUILDERS = {
    "yufeng-sword": build_yufeng_sword,
    "yellow-talisman": build_yellow_talisman,
    "lihuo-wheel": build_lihuo_wheel,
    "tide-orb": build_tide_orb,
    "zhenyue-seal": build_zhenyue_seal,
    "spirit-vine-seed": build_spirit_vine_seed,
}


def main() -> None:
    args = parse_args()
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    source_size = int(spec["sourceSize"])
    runtime_size = int(spec["runtimeSize"])
    phase = float(int(spec["seed"]) % 7)
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for visual in spec["visuals"]:
        image = BUILDERS[visual["name"]](source_size, visual["palette"], phase)
        source_path = args.source_dir / visual["sourceFile"]
        final_path = args.final_dir / visual["finalFile"]
        image.save(source_path, optimize=True)
        runtime = image.resize((runtime_size, runtime_size), Image.Resampling.LANCZOS)
        runtime.save(final_path, optimize=True)
        coverage = sum(runtime.getchannel("A").histogram()[16:]) / (runtime_size * runtime_size)
        print(f"visual={visual['name']} source={source_path} final={final_path} coverage={coverage:.4f}")


if __name__ == "__main__":
    main()

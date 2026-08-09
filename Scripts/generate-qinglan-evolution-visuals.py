#!/usr/bin/env python3
"""Generate six deterministic first-party Qinglan evolution VFX textures."""

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
    return tuple(int(value[index:index + 2], 16) for index in (0, 2, 4)) + (alpha,)


def polar(center: Point, radii: Point, angle: float) -> Point:
    radians = math.radians(angle)
    return center[0] + math.cos(radians) * radii[0], center[1] + math.sin(radians) * radii[1]


def rotate(center: Point, points: Sequence[Point], angle: float) -> list[Point]:
    radians = math.radians(angle)
    cosine, sine = math.cos(radians), math.sin(radians)
    return [(center[0] + x * cosine - y * sine, center[1] + x * sine + y * cosine) for x, y in points]


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

    def p(self, point: Point) -> Point:
        return point[0] * self.scale, point[1] * self.scale

    def line(self, points: Iterable[Point], width: int, ink: Color | None = None) -> None:
        points = [self.p(point) for point in points]
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        self.g.line(points, fill=ink[:3] + (92,), width=width + round(28 * self.scale), joint="curve")
        self.c.line(points, fill=self.deep, width=width + round(10 * self.scale), joint="curve")
        self.c.line(points, fill=ink, width=width, joint="curve")

    def arc(self, center: Point, radii: Point, start: float, end: float, width: int, ink: Color | None = None) -> None:
        center, radii = self.p(center), self.p(radii)
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        box = (center[0] - radii[0], center[1] - radii[1], center[0] + radii[0], center[1] + radii[1])
        self.g.arc(box, start, end, fill=ink[:3] + (92,), width=width + round(28 * self.scale))
        self.c.arc(box, start, end, fill=self.deep, width=width + round(10 * self.scale))
        self.c.arc(box, start, end, fill=ink, width=width)

    def polygon(self, points: Iterable[Point], ink: Color | None = None) -> None:
        points = [self.p(point) for point in points]
        ink = ink or self.primary
        center = (sum(x for x, _ in points) / len(points), sum(y for _, y in points) / len(points))
        expanded = [(center[0] + (x - center[0]) * 1.13, center[1] + (y - center[1]) * 1.13) for x, y in points]
        self.g.polygon(expanded, fill=ink[:3] + (96,))
        self.c.polygon(expanded, fill=self.deep)
        self.c.polygon(points, fill=ink)

    def ellipse(self, box: tuple[float, float, float, float], ink: Color | None = None) -> None:
        box = tuple(value * self.scale for value in box)
        ink = ink or self.primary
        grow = 12 * self.scale
        glow_box = (box[0] - grow, box[1] - grow, box[2] + grow, box[3] + grow)
        self.g.ellipse(glow_box, fill=ink[:3] + (96,))
        self.c.ellipse(glow_box, fill=self.deep)
        self.c.ellipse(box, fill=ink)

    def sword(self, center: Point, angle: float, length: float, ink: Color | None = None) -> None:
        ink = ink or self.highlight
        points = [(-length * .48, -13), (length * .32, -13), (length * .5, 0), (length * .32, 13), (-length * .48, 13)]
        self.polygon(rotate(center, points, angle), ink)
        guard = rotate(center, [(-length * .39, -35), (-length * .34, -12), (-length * .34, 12), (-length * .39, 35)], angle)
        self.line(guard, 12, self.secondary)

    def diamond(self, center: Point, radius: float, ink: Color | None = None) -> None:
        self.polygon([(center[0], center[1] - radius), (center[0] + radius * .72, center[1]),
                      (center[0], center[1] + radius), (center[0] - radius * .72, center[1])], ink or self.highlight)

    def finish(self) -> Image.Image:
        self.image.alpha_composite(self.glow.filter(ImageFilter.GaussianBlur(round(15 * self.scale))))
        self.image.alpha_composite(self.core)
        return self.image


def flowing_shadow(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    trail = [(150, 700), (270, 610), (390, 650), (510, 500), (650, 450), (820, 300)]
    canvas.line(trail, 28, canvas.primary)
    canvas.line([(170, 746), (302, 666), (424, 704), (556, 552), (688, 510), (858, 354)], 10, canvas.secondary)
    canvas.sword((300, 610), -22, 250, canvas.highlight)
    canvas.sword((520, 500), -34, 280, canvas.highlight)
    canvas.sword((742, 366), -42, 320, canvas.highlight)
    for center in ((270, 760), (500, 660), (710, 540)):
        canvas.arc(center, (104, 54), 190 + phase, 344 + phase, 10, canvas.secondary)
    return canvas.finish()


def taiyi_array(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    for side in range(8):
        start = -90 + side * 45 + 5 + phase
        end = -90 + (side + 1) * 45 - 5 + phase
        canvas.line([polar(center, (354, 304), start), polar(center, (354, 304), end)], 18, canvas.primary)
        card = polar(center, (394, 338), (start + end) * .5)
        canvas.polygon(rotate(card, [(-24, -48), (24, -48), (30, 38), (0, 54), (-30, 38)], (start + end) * .5 + 90), canvas.highlight)
        canvas.line([polar(center, (118, 98), (start + end) * .5), polar(center, (300, 250), (start + end) * .5)], 8, canvas.secondary)
    for start in (12, 102, 192, 282):
        canvas.arc(center, (190, 156), start + phase, start + 62 + phase, 13, canvas.highlight)
    canvas.diamond(center, 54, canvas.secondary)
    return canvas.finish()


def hundred_craft(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    for ring, radius in enumerate((330, 244)):
        count = 8 if ring == 0 else 6
        for index in range(count):
            angle = index * 360 / count + phase * (1 if ring == 0 else -1)
            canvas.arc(center, (radius, radius), angle + 7, angle + 32, 22 - ring * 6,
                       canvas.primary if ring == 0 else canvas.secondary)
            point = polar(center, (radius + 48, radius + 48), angle + 20)
            canvas.polygon(rotate(point, [(-34, -17), (48, 0), (-34, 17), (-10, 0)], angle + 20), canvas.highlight)
    for angle in range(0, 360, 45):
        canvas.line([polar(center, (92, 92), angle), polar(center, (202, 202), angle)], 12, canvas.secondary)
    canvas.ellipse((430, 430, 594, 594), canvas.deep)
    canvas.ellipse((470, 470, 554, 554), canvas.highlight)
    return canvas.finish()


def mirror_sea(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    canvas.ellipse((376, 350, 648, 674), canvas.highlight)
    canvas.ellipse((420, 390, 604, 634), canvas.primary)
    for ring, radii in enumerate(((370, 292), (306, 232), (248, 176))):
        canvas.arc(center, radii, 198 + phase + ring * 8, 342 + phase + ring * 8, 16 - ring * 4,
                   canvas.secondary if ring == 1 else canvas.primary)
        canvas.arc(center, radii, 18 + phase + ring * 8, 162 + phase + ring * 8, 16 - ring * 4,
                   canvas.highlight if ring == 1 else canvas.primary)
    for angle in range(0, 360, 45):
        point = polar(center, (426, 344), angle)
        canvas.diamond(point, 20, canvas.secondary if angle % 90 else canvas.highlight)
    return canvas.finish()


def mountain_boundary(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    corners = [((190, 190), (360, 190), (190, 360)), ((834, 190), (664, 190), (834, 360)),
               ((190, 834), (360, 834), (190, 664)), ((834, 834), (664, 834), (834, 664))]
    for corner, horizontal, vertical in corners:
        canvas.line([horizontal, corner, vertical], 22, canvas.primary)
        canvas.diamond(corner, 26, canvas.highlight)
    canvas.polygon([(252, 690), (382, 430), (470, 570), (570, 318), (772, 690)], canvas.secondary)
    canvas.polygon([(324, 690), (430, 512), (506, 616), (592, 430), (700, 690)], canvas.primary)
    canvas.line([(230, 690), (794, 690)], 24, canvas.highlight)
    canvas.arc((512, 548), (342, 300), 202 + phase, 338 + phase, 18, canvas.highlight)
    canvas.arc((512, 548), (286, 246), 206 + phase, 334 + phase, 10, canvas.primary)
    return canvas.finish()


def earth_vein(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    nodes = [(512, 330), (320, 650), (704, 650)]
    canvas.line([nodes[0], nodes[1], nodes[2], nodes[0]], 24, canvas.secondary)
    canvas.line([(512, 330), (512, 804)], 14, canvas.primary)
    for index, center in enumerate(nodes):
        canvas.ellipse((center[0] - 76, center[1] - 76, center[0] + 76, center[1] + 76), canvas.highlight)
        canvas.ellipse((center[0] - 43, center[1] - 43, center[0] + 43, center[1] + 43), canvas.primary)
        for angle in (210 + index * 18, 270 + index * 18, 330 + index * 18):
            start = polar(center, (68, 68), angle)
            end = polar(center, (172, 142), angle)
            canvas.line([start, end], 13, canvas.primary)
            canvas.polygon(rotate(end, [(-15, 0), (0, -44), (38, 0), (0, 22)], angle + 90), canvas.highlight)
    for start in (12, 132, 252):
        canvas.arc((512, 548), (364, 300), start + phase, start + 72 + phase, 10, canvas.secondary)
    return canvas.finish()


BUILDERS = {
    "qinglan-flowing-shadow-sword": flowing_shadow,
    "taiyi-spirit-sealing-array": taiyi_array,
    "chilu-hundred-craft-wheel": hundred_craft,
    "mirror-sea-tide-wheel": mirror_sea,
    "mountain-boundary-seal": mountain_boundary,
    "earth-vein-spring-branch": earth_vein,
}


def main() -> None:
    args = parse_args()
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    source_size, runtime_size = int(spec["sourceSize"]), int(spec["runtimeSize"])
    phase = float(int(spec["seed"]) % 9)
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for visual in spec["visuals"]:
        image = BUILDERS[visual["name"]](source_size, visual["palette"], phase)
        source_path, final_path = args.source_dir / visual["sourceFile"], args.final_dir / visual["finalFile"]
        image.save(source_path, optimize=True)
        runtime = image.resize((runtime_size, runtime_size), Image.Resampling.LANCZOS)
        runtime.save(final_path, optimize=True)
        coverage = sum(runtime.getchannel("A").histogram()[16:]) / (runtime_size * runtime_size)
        print(f"visual={visual['name']} source={source_path} final={final_path} coverage={coverage:.4f}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Generate six deterministic first-party Qinglan boss phase telegraphs."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Iterable, Sequence

from PIL import Image, ImageDraw, ImageFilter


Point = tuple[float, float]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--spec", required=True, type=Path)
    parser.add_argument("--source-dir", required=True, type=Path)
    parser.add_argument("--final-dir", required=True, type=Path)
    return parser.parse_args()


def color(value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    value = value.lstrip("#")
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
        self.size = size
        self.scale = size / 1024
        self.image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.core = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        self.g = ImageDraw.Draw(self.glow)
        self.c = ImageDraw.Draw(self.core)
        self.primary = color(palette["primary"])
        self.highlight = color(palette["highlight"])
        self.deep = color(palette["deep"])
        self.secondary = color(palette["secondary"])

    def point(self, point: Point) -> Point:
        return point[0] * self.scale, point[1] * self.scale

    def line(self, points: Iterable[Point], width: int, ink=None) -> None:
        points = [self.point(point) for point in points]
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        self.g.line(points, fill=ink[:3] + (92,), width=width + round(24 * self.scale), joint="curve")
        self.c.line(points, fill=self.deep, width=width + round(8 * self.scale), joint="curve")
        self.c.line(points, fill=ink, width=width, joint="curve")

    def arc(self, center: Point, radii: Point, start: float, end: float, width: int, ink=None) -> None:
        center = self.point(center)
        radii = self.point(radii)
        width = max(1, round(width * self.scale))
        ink = ink or self.primary
        box = (center[0] - radii[0], center[1] - radii[1], center[0] + radii[0], center[1] + radii[1])
        self.g.arc(box, start, end, fill=ink[:3] + (92,), width=width + round(24 * self.scale))
        self.c.arc(box, start, end, fill=self.deep, width=width + round(8 * self.scale))
        self.c.arc(box, start, end, fill=ink, width=width)

    def polygon(self, points: Iterable[Point], ink=None) -> None:
        points = [self.point(point) for point in points]
        ink = ink or self.primary
        center = (sum(x for x, _ in points) / len(points), sum(y for _, y in points) / len(points))
        expanded = [
            (center[0] + (x - center[0]) * 1.15, center[1] + (y - center[1]) * 1.15)
            for x, y in points
        ]
        self.g.polygon(expanded, fill=ink[:3] + (98,))
        self.c.polygon(expanded, fill=self.deep)
        self.c.polygon(points, fill=ink)

    def chevron(self, center: Point, angle: float, scale: float = 1.0, ink=None) -> None:
        self.line(rotate(center, [(-25 * scale, -19 * scale), (13 * scale, 0), (-25 * scale, 19 * scale)], angle), 10, ink or self.highlight)

    def diamond(self, center: Point, radius: float, ink=None) -> None:
        self.polygon(
            [(center[0], center[1] - radius), (center[0] + radius * .72, center[1]),
             (center[0], center[1] + radius), (center[0] - radius * .72, center[1])],
            ink or self.highlight,
        )

    def finish(self) -> Image.Image:
        self.image.alpha_composite(self.glow.filter(ImageFilter.GaussianBlur(round(14 * self.scale))))
        self.image.alpha_composite(self.core)
        return self.image


def zhezhi_phase_1(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    canvas.line([(92, 421), (878, 421)], 13)
    canvas.line([(92, 603), (878, 603)], 13)
    canvas.polygon([(878, 386), (946, 512), (878, 638)], canvas.highlight)
    for x in (250, 390, 530, 670, 810):
        canvas.line([(x - 30, 575), (x + 30, 449)], 7, canvas.secondary)
    for x in (700, 800):
        canvas.chevron((x, 512), 0, .9)
    return canvas.finish()


def zhezhi_phase_2(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    targets = [((300, 640), (150, 106)), ((512, 365), (170, 118)), ((724, 640), (150, 106))]
    for center, radii in targets:
        for start in (8, 98, 188, 278):
            canvas.arc(center, radii, start + phase, start + 64 + phase, 11)
        canvas.polygon([(center[0], center[1] - radii[1] - 62),
                        (center[0] + 24, center[1] - radii[1] - 18),
                        (center[0] - 24, center[1] - radii[1] - 18)], canvas.highlight)
        canvas.line([(center[0], center[1] - radii[1] - 52), (center[0], center[1] - 22)], 7, canvas.secondary)
    return canvas.finish()


def zhezhi_phase_3(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 522)
    for side in range(8):
        start = -90 + side * 45 + 7
        end = -90 + (side + 1) * 45 - 7
        canvas.line([polar(center, (286, 236), start), polar(center, (286, 236), end)], 13)
    for angle in (0, 90, 180, 270):
        point = polar(center, (354, 288), angle)
        canvas.polygon(rotate(point, [(-28, -48), (28, -48), (38, 34), (0, 54), (-38, 34)], angle), canvas.secondary)
        canvas.line([polar(center, (294, 242), angle), polar(center, (326, 270), angle)], 8, canvas.highlight)
    return canvas.finish()


def tingfeng_phase_1(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    angle = -36
    for offset in (-68, 68):
        points = rotate(center, [(-430, offset), (348, offset)], angle)
        canvas.line(points, 13)
    arrow = rotate(center, [(348, -114), (465, 0), (348, 114)], angle)
    canvas.polygon(arrow, canvas.highlight)
    for distance in (-180, 10, 200):
        point = rotate(center, [(distance, 0)], angle)[0]
        canvas.chevron(point, angle, .9, canvas.secondary)
    return canvas.finish()


def tingfeng_phase_2(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 522)
    for ring, radii in enumerate(((330, 268), (264, 210), (198, 154))):
        for sector in range(3):
            start = 14 + sector * 120 + ring * 13 + phase
            canvas.arc(center, radii, start, start + 76, 12 - ring * 2,
                       canvas.primary if ring != 1 else canvas.secondary)
    for angle in (25, 115, 205, 295):
        canvas.diamond(polar(center, (370, 302), angle), 20, canvas.highlight)
    for angle in (70, 250):
        point = polar(center, (292, 236), angle)
        canvas.line(rotate(point, [(-32, -16), (-8, 16), (14, -16), (36, 16)], angle), 7, canvas.secondary)
    return canvas.finish()


def tingfeng_phase_3(size: int, palette: dict[str, str], phase: float) -> Image.Image:
    canvas = Canvas(size, palette)
    center = (512, 512)
    for angle in (43, -43):
        for offset in (-58, 58):
            canvas.line(rotate(center, [(-430, offset), (430, offset)], angle), 13)
    for angle in (43, 137, 223, 317):
        canvas.chevron(polar(center, (350, 350), angle), angle + 90, .9, canvas.highlight)
    for start in (12, 102, 192, 282):
        canvas.arc(center, (154, 154), start, start + 58, 10, canvas.secondary)
    return canvas.finish()


BUILDERS = {
    "zhezhi-phase-1": zhezhi_phase_1,
    "zhezhi-phase-2": zhezhi_phase_2,
    "zhezhi-phase-3": zhezhi_phase_3,
    "tingfeng-phase-1": tingfeng_phase_1,
    "tingfeng-phase-2": tingfeng_phase_2,
    "tingfeng-phase-3": tingfeng_phase_3,
}


def main() -> None:
    args = parse_args()
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    source_size = int(spec["sourceSize"])
    runtime_size = int(spec["runtimeSize"])
    phase = float(int(spec["seed"]) % 5)
    args.source_dir.mkdir(parents=True, exist_ok=True)
    args.final_dir.mkdir(parents=True, exist_ok=True)
    for overlay in spec["overlays"]:
        image = BUILDERS[overlay["name"]](source_size, overlay["palette"], phase)
        source_path = args.source_dir / overlay["sourceFile"]
        final_path = args.final_dir / overlay["finalFile"]
        image.save(source_path, optimize=True)
        runtime = image.resize((runtime_size, runtime_size), Image.Resampling.LANCZOS)
        runtime.save(final_path, optimize=True)
        coverage = sum(runtime.getchannel("A").histogram()[16:]) / (runtime_size * runtime_size)
        print(f"overlay={overlay['name']} source={source_path} final={final_path} coverage={coverage:.4f}")


if __name__ == "__main__":
    main()

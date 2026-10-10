import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parents[1] / "Assets" / "_Project" / "Art" / "UI"
SS = 4
WHITE = (255, 255, 255, 255)


def canvas(size):
    return Image.new("RGBA", (size * SS, size * SS), (0, 0, 0, 0))


def finish(img, size):
    return img.resize((size, size), Image.LANCZOS)


def circle(size=128):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).ellipse([2, 2, s - 3, s - 3], fill=WHITE)
    return finish(img, size)


def rounded(size=64, radius=20):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).rounded_rectangle([0, 0, s - 1, s - 1], radius=radius * SS, fill=WHITE)
    return finish(img, size)


def button(size=96, radius=30):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    r = radius * SS
    d.rounded_rectangle([0, 0, s - 1, s - 1], radius=r, fill=(150, 150, 150, 255))
    d.rounded_rectangle([0, 0, s - 1, s - 1 - s * 0.09], radius=r, fill=WHITE)
    shine = canvas(size)
    ImageDraw.Draw(shine).rounded_rectangle([s * 0.08, s * 0.06, s * 0.92, s * 0.4], radius=r * 0.7, fill=(255, 255, 255, 60))
    base = Image.new("RGBA", img.size, (0, 0, 0, 0))
    base.alpha_composite(img)
    tinted = Image.new("RGBA", img.size, (235, 235, 235, 255))
    tinted.putalpha(shine.split()[3])
    return finish(base, size)


def gear(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    c = s / 2
    teeth = 8
    points = []
    for i in range(teeth * 4):
        angle = i * math.pi * 2 / (teeth * 4)
        radius = s * (0.46 if i % 4 in (0, 1) else 0.36)
        points.append((c + math.cos(angle - 0.1) * radius, c + math.sin(angle - 0.1) * radius))
    d.polygon(points, fill=WHITE)
    d.ellipse([c - s * 0.15, c - s * 0.15, c + s * 0.15, c + s * 0.15], fill=(0, 0, 0, 0))
    return finish(img, size)


def restart(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    box = [s * 0.16, s * 0.16, s * 0.84, s * 0.84]
    d.arc(box, start=-50, end=250, fill=WHITE, width=int(s * 0.11))
    tip = (s * 0.5 + math.cos(math.radians(-50)) * s * 0.34, s * 0.5 + math.sin(math.radians(-50)) * s * 0.34)
    d.polygon([(tip[0] - s * 0.2, tip[1] - s * 0.06), (tip[0] + s * 0.1, tip[1] - s * 0.16), (tip[0] + s * 0.02, tip[1] + s * 0.16)], fill=WHITE)
    return finish(img, size)


def bulb(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.ellipse([s * 0.22, s * 0.08, s * 0.78, s * 0.64], fill=WHITE)
    d.polygon([(s * 0.34, s * 0.52), (s * 0.66, s * 0.52), (s * 0.6, s * 0.74), (s * 0.4, s * 0.74)], fill=WHITE)
    d.rounded_rectangle([s * 0.39, s * 0.77, s * 0.61, s * 0.84], radius=s * 0.02, fill=WHITE)
    d.rounded_rectangle([s * 0.42, s * 0.87, s * 0.58, s * 0.93], radius=s * 0.02, fill=WHITE)
    return finish(img, size)


def parking_plus(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([s * 0.1, s * 0.16, s * 0.68, s * 0.9], radius=s * 0.1, outline=WHITE, width=int(s * 0.07))
    d.rounded_rectangle([s * 0.27, s * 0.32, s * 0.36, s * 0.76], radius=s * 0.02, fill=WHITE)
    d.pieslice([s * 0.24, s * 0.32, s * 0.56, s * 0.58], start=-90, end=90, fill=WHITE)
    d.ellipse([s * 0.34, s * 0.39, s * 0.46, s * 0.51], fill=(0, 0, 0, 0))
    d.rounded_rectangle([s * 0.74, s * 0.06, s * 0.82, s * 0.36], radius=s * 0.02, fill=WHITE)
    d.rounded_rectangle([s * 0.63, s * 0.17, s * 0.93, s * 0.25], radius=s * 0.02, fill=WHITE)
    return finish(img, size)


def coin(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.ellipse([s * 0.06, s * 0.1, s * 0.94, s * 0.98], fill=(214, 140, 20, 255))
    d.ellipse([s * 0.06, s * 0.04, s * 0.94, s * 0.92], fill=(255, 200, 50, 255))
    d.ellipse([s * 0.18, s * 0.16, s * 0.82, s * 0.8], fill=(255, 222, 96, 255), outline=(232, 160, 30, 255), width=int(s * 0.03))
    star_points = []
    for i in range(10):
        angle = -math.pi / 2 + i * math.pi / 5
        radius = s * (0.22 if i % 2 == 0 else 0.1)
        star_points.append((s * 0.5 + math.cos(angle) * radius, s * 0.48 + math.sin(angle) * radius))
    d.polygon(star_points, fill=(232, 150, 24, 255))
    return finish(img, size)


def star(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    for scale, color, shift in ((0.5, (214, 140, 20, 255), 0.03), (0.46, (255, 208, 64, 255), 0.0)):
        points = []
        for i in range(10):
            angle = -math.pi / 2 + i * math.pi / 5
            radius = s * (scale if i % 2 == 0 else scale * 0.46)
            points.append((s * 0.5 + math.cos(angle) * radius, s * (0.52 + shift) + math.sin(angle) * radius))
        d.polygon(points, fill=color)
    return finish(img, size)


def hand(size=160):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    outline = (60, 60, 80, 255)
    skin = WHITE
    for inflate, color in ((s * 0.035, outline), (0, skin)):
        r = s * 0.075 + inflate
        d.rounded_rectangle([s * 0.3 - inflate, s * 0.06 - inflate, s * 0.3 + s * 0.15 + inflate, s * 0.6 + inflate], radius=r, fill=color)
        for i in range(3):
            x = s * 0.47 + i * s * 0.14
            d.rounded_rectangle([x - inflate, s * 0.36 - inflate + i * s * 0.02, x + s * 0.13 + inflate, s * 0.7 + inflate], radius=r, fill=color)
        d.rounded_rectangle([s * 0.3 - inflate, s * 0.46 - inflate, s * 0.88 + inflate, s * 0.92 + inflate], radius=s * 0.16 + inflate, fill=color)
        d.polygon([(s * 0.3 - inflate, s * 0.62), (s * 0.12 - inflate, s * 0.46 - inflate), (s * 0.2, s * 0.4 - inflate), (s * 0.38, s * 0.52)], fill=color)
    return finish(img, size)


def close(size=96):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    w = int(s * 0.14)
    d.line([(s * 0.24, s * 0.24), (s * 0.76, s * 0.76)], fill=WHITE, width=w)
    d.line([(s * 0.76, s * 0.24), (s * 0.24, s * 0.76)], fill=WHITE, width=w)
    for x, y in ((0.24, 0.24), (0.76, 0.76), (0.76, 0.24), (0.24, 0.76)):
        d.ellipse([s * x - w / 2, s * y - w / 2, s * x + w / 2, s * y + w / 2], fill=WHITE)
    return finish(img, size)


def soft_shadow(size=128):
    img = canvas(size)
    s = img.size[0]
    ImageDraw.Draw(img).rounded_rectangle([s * 0.14, s * 0.14, s * 0.86, s * 0.86], radius=s * 0.16, fill=(0, 0, 0, 150))
    return finish(img.filter(ImageFilter.GaussianBlur(s * 0.06)), size)


def undo(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    width = int(s * 0.11)
    d.arc([s * 0.2, s * 0.3, s * 0.84, s * 0.94], start=200, end=70, fill=WHITE, width=width)
    tip_x, tip_y = s * 0.22, s * 0.5
    d.polygon([(tip_x - s * 0.14, tip_y + s * 0.06), (tip_x + s * 0.2, tip_y + s * 0.12), (tip_x + s * 0.06, tip_y - s * 0.22)], fill=WHITE)
    return finish(img, size)


def parking(size=96):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([s * 0.1, s * 0.06, s * 0.9, s * 0.94], radius=s * 0.18, fill=WHITE)
    cut = (0, 0, 0, 0)
    d.rounded_rectangle([s * 0.3, s * 0.22, s * 0.42, s * 0.8], radius=s * 0.02, fill=cut)
    d.pieslice([s * 0.24, s * 0.22, s * 0.72, s * 0.58], start=-90, end=90, fill=cut)
    d.ellipse([s * 0.4, s * 0.32, s * 0.56, s * 0.48], fill=WHITE)
    return finish(img, size)


def shop(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.arc([s * 0.3, s * 0.08, s * 0.7, s * 0.5], start=180, end=360, fill=WHITE, width=int(s * 0.08))
    d.rounded_rectangle([s * 0.14, s * 0.3, s * 0.86, s * 0.92], radius=s * 0.1, fill=WHITE)
    d.ellipse([s * 0.31, s * 0.4, s * 0.41, s * 0.5], fill=(0, 0, 0, 0))
    d.ellipse([s * 0.59, s * 0.4, s * 0.69, s * 0.5], fill=(0, 0, 0, 0))
    return finish(img, size)


def video(size=128):
    img = canvas(size)
    s = img.size[0]
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([s * 0.08, s * 0.2, s * 0.92, s * 0.8], radius=s * 0.14, fill=WHITE)
    d.polygon([(s * 0.4, s * 0.34), (s * 0.4, s * 0.66), (s * 0.66, s * 0.5)], fill=(0, 0, 0, 0))
    return finish(img, size)


def app_icon(size=512):
    img = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    s = img.size[0]
    d = ImageDraw.Draw(img)
    for y in range(s):
        t = y / s
        d.line([(0, y), (s, y)], fill=(int(74 - 24 * t), int(150 - 40 * t), int(250 - 30 * t), 255))
    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, s - 1, s - 1], radius=s * 0.22, fill=255)
    img.putalpha(mask)
    d = ImageDraw.Draw(img)

    d.rounded_rectangle([s * 0.12, s * 0.12, s * 0.88, s * 0.88], radius=s * 0.1, outline=(255, 255, 255, 235), width=int(s * 0.035))
    for i in range(1, 3):
        x = s * (0.12 + 0.76 * i / 3)
        d.line([(x, s * 0.12), (x, s * 0.5)], fill=(255, 255, 255, 150), width=int(s * 0.018))

    def car(cx, cy, w, h, body, glass=(40, 60, 96, 255)):
        d.rounded_rectangle([cx - w / 2 + s * 0.012, cy - h / 2 + s * 0.02, cx + w / 2 + s * 0.012, cy + h / 2 + s * 0.02], radius=w * 0.28, fill=(10, 30, 80, 90))
        d.rounded_rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], radius=w * 0.28, fill=body)
        d.rounded_rectangle([cx - w * 0.36, cy - h * 0.22, cx + w * 0.36, cy + h * 0.3], radius=w * 0.16, fill=glass)
        d.rounded_rectangle([cx - w * 0.3, cy - h * 0.1, cx + w * 0.3, cy + h * 0.2], radius=w * 0.12, fill=body)
        d.polygon([(cx, cy - h * 0.06), (cx + w * 0.16, cy + h * 0.06), (cx - w * 0.16, cy + h * 0.06)], fill=(255, 255, 255, 255))

    car(s * 0.245, s * 0.33, s * 0.17, s * 0.32, (240, 78, 82, 255))
    car(s * 0.5, s * 0.33, s * 0.17, s * 0.32, (255, 198, 44, 255))
    car(s * 0.755, s * 0.6, s * 0.17, s * 0.32, (66, 196, 110, 255))

    d.rounded_rectangle([s * 0.2, s * 0.56, s * 0.29, s * 0.82], radius=s * 0.02, fill=(255, 255, 255, 255))
    d.pieslice([s * 0.18, s * 0.56, s * 0.5, s * 0.74], start=-90, end=90, fill=(255, 255, 255, 255))
    d.ellipse([s * 0.29, s * 0.605, s * 0.39, s * 0.695], fill=(62, 128, 236, 255))
    return img.resize((size, size), Image.LANCZOS)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    icon_dir = OUT.parent / "Icon"
    icon_dir.mkdir(parents=True, exist_ok=True)
    app_icon().save(icon_dir / "AppIcon.png")
    circle().save(OUT / "Circle.png")
    rounded().save(OUT / "Rounded_9s.png")
    rounded(64, 31).save(OUT / "Pill_9s.png")
    button().save(OUT / "Button_9s.png")
    gear().save(OUT / "Gear.png")
    restart().save(OUT / "Restart.png")
    bulb().save(OUT / "Bulb.png")
    parking_plus().save(OUT / "ParkingPlus.png")
    coin().save(OUT / "Coin.png")
    star().save(OUT / "Star.png")
    hand().save(OUT / "Hand.png")
    close().save(OUT / "Close.png")
    undo().save(OUT / "Undo.png")
    shop().save(OUT / "Shop.png")
    video().save(OUT / "Video.png")
    parking().save(OUT / "Parking.png")
    soft_shadow().save(OUT / "SoftShadow.png")
    print("done ->", OUT)


if __name__ == "__main__":
    main()

import math

from lowpoly import Model

PAINT = (255, 255, 255)
GLASS = (52, 74, 104)
TYRE = (38, 38, 46)
HUB = (190, 194, 204)
BUMPER = (70, 74, 86)
HEADLIGHT = (255, 246, 196)
TAILLIGHT = (232, 58, 58)
ARROW = (255, 255, 255)
CONCRETE = (226, 228, 236)
CONCRETE_DARK = (176, 180, 194)
LINE = (250, 250, 252)
WOOD = (168, 116, 74)
LEAF = (96, 190, 96)
LEAF_DARK = (66, 158, 84)
TRUNK = (128, 88, 62)
ORANGE = (255, 138, 40)
METAL = (120, 126, 142)
LAMP = (255, 240, 170)


def _arrow(m, y, z, scale=1.0):
    s = scale
    outline = [(0.0, 0.26 * s), (0.2 * s, 0.02 * s), (0.08 * s, 0.02 * s), (0.08 * s, -0.24 * s),
               (-0.08 * s, -0.24 * s), (-0.08 * s, 0.02 * s), (-0.2 * s, 0.02 * s)]
    m.paint(ARROW)
    m.extrude("arrow", outline, 0.025, loc=(0, y, z))


def _wheels(m, x, ys, radius=0.155, width=0.12):
    for i, y in enumerate(ys):
        for side in (-1, 1):
            m.paint(TYRE)
            m.wheel(f"tyre_{i}_{side}", radius, width, (side * x, y, radius))
            m.paint(HUB)
            m.wheel(f"hub_{i}_{side}", radius * 0.5, 0.02, (side * (x + width * 0.5 + 0.005), y, radius), sides=8)


def _lights(m, half_length, z, x=0.24):
    for side in (-1, 1):
        m.paint(HEADLIGHT)
        m.box(f"head_{side}", (0.17, 0.05, 0.09), (side * x, half_length - 0.01, z), bevel=0.015)
        m.paint(TAILLIGHT)
        m.box(f"tail_{side}", (0.17, 0.05, 0.08), (side * x, -half_length + 0.01, z), bevel=0.015)
    m.paint(BUMPER)
    m.box("bumper_f", (0.7, 0.07, 0.08), (0, half_length - 0.005, z - 0.13), bevel=0.02)
    m.box("bumper_r", (0.7, 0.07, 0.08), (0, -half_length + 0.005, z - 0.13), bevel=0.02)


def car():
    m = Model("Car")
    m.paint(PAINT, tint=1.0)
    m.box("body", (0.76, 1.72, 0.28), (0, 0, 0.3), bevel=0.07)
    m.box("hood", (0.7, 0.5, 0.06), (0, 0.5, 0.46), taper=(0.94, 0.9), bevel=0.02)
    m.paint(GLASS)
    m.box("glass", (0.66, 0.92, 0.2), (0, -0.14, 0.54), taper=(0.84, 0.74), bevel=0.02)
    m.paint(PAINT, tint=1.0)
    m.box("roof", (0.57, 0.7, 0.05), (0, -0.14, 0.66), bevel=0.02)
    _arrow(m, -0.14, 0.685, 0.82)
    _lights(m, 0.86, 0.33)
    _wheels(m, 0.34, (0.52, -0.52))
    return m


def van():
    m = Model("Van")
    m.paint(PAINT, tint=1.0)
    m.box("body", (0.78, 2.72, 0.36), (0, 0, 0.34), bevel=0.07)
    m.paint(GLASS)
    m.box("glass", (0.7, 2.3, 0.22), (0, -0.08, 0.63), taper=(0.9, 0.93), bevel=0.02)
    m.paint(PAINT, tint=1.0)
    m.box("roof", (0.64, 2.16, 0.06), (0, -0.08, 0.76), bevel=0.02)
    for i in range(3):
        m.box(f"pillar_l_{i}", (0.04, 0.07, 0.22), (-0.335, -0.9 + i * 0.72, 0.63))
        m.box(f"pillar_r_{i}", (0.04, 0.07, 0.22), (0.335, -0.9 + i * 0.72, 0.63))
    _arrow(m, -0.08, 0.79, 1.0)
    _lights(m, 1.36, 0.36, x=0.25)
    _wheels(m, 0.35, (0.92, -0.92), radius=0.165)
    return m


def question():
    m = Model("Question")
    m.paint((255, 255, 255))
    t = 0.09
    m.box("top", (0.34, t, t), (0, 0, 0.62), bevel=0.02)
    m.box("left", (t, t, 0.14), (-0.125, 0, 0.55), bevel=0.02)
    m.box("right", (t, t, 0.2), (0.125, 0, 0.52), bevel=0.02)
    m.box("mid", (0.2, t, t), (0.07, 0, 0.4), bevel=0.02)
    m.box("stem", (t, t, 0.16), (0.0, 0, 0.32), bevel=0.02)
    m.box("dot", (0.11, t, 0.11), (0.0, 0, 0.12), bevel=0.03)
    return m


def garage():
    m = Model("Garage")
    width, depth = 3.1, 3.2
    m.paint(CONCRETE_DARK)
    m.box("slab", (width + 0.3, depth + 0.3, 0.08), (0, 0, 0.04), bevel=0.03)
    m.paint((238, 240, 248))
    m.box("floor", (width, depth, 0.03), (0, 0, 0.095))
    m.paint(PAINT, tint=1.0)
    m.box("threshold", (width, 0.28, 0.014), (0, -depth * 0.5 + 0.14, 0.117))
    for x in (-0.5, 0.5):
        m.box(f"line_{x}", (0.07, depth - 0.34, 0.012), (x * (width / 3), 0.14, 0.116))
    m.paint(CONCRETE)
    m.box("back", (width + 0.3, 0.16, 0.95), (0, depth * 0.5 + 0.07, 0.55), bevel=0.03)
    m.box("left", (0.16, depth * 0.62, 0.7), (-(width * 0.5 + 0.07), depth * 0.19, 0.43), bevel=0.03)
    m.box("right", (0.16, depth * 0.62, 0.7), (width * 0.5 + 0.07, depth * 0.19, 0.43), bevel=0.03)
    m.paint(PAINT, tint=1.0)
    m.box("sign", (width + 0.34, 0.2, 0.3), (0, depth * 0.5 + 0.07, 1.14), bevel=0.04)
    m.box("cap_l", (0.2, depth * 0.62, 0.12), (-(width * 0.5 + 0.07), depth * 0.19, 0.83), bevel=0.03)
    m.box("cap_r", (0.2, depth * 0.62, 0.12), (width * 0.5 + 0.07, depth * 0.19, 0.83), bevel=0.03)
    m.box("apron_l", (0.16, depth * 0.38, 0.1), (-(width * 0.5 + 0.07), -depth * 0.31, 0.13), bevel=0.02)
    m.box("apron_r", (0.16, depth * 0.38, 0.1), (width * 0.5 + 0.07, -depth * 0.31, 0.13), bevel=0.02)
    return m


def waiting_slot():
    m = Model("WaitingSlot")
    w, d, t = 0.92, 2.95, 0.07
    m.paint(CONCRETE_DARK)
    m.box("pad", (w, d, 0.03), (0, 0, 0.015))
    m.paint(LINE)
    m.box("l", (t, d, 0.012), (-w * 0.5 + t * 0.5, 0, 0.036))
    m.box("r", (t, d, 0.012), (w * 0.5 - t * 0.5, 0, 0.036))
    m.box("b", (w, t, 0.012), (0, d * 0.5 - t * 0.5, 0.036))
    return m


def cone():
    m = Model("Cone")
    m.paint(BUMPER)
    m.box("base", (0.36, 0.36, 0.05), (0, 0, 0.025), bevel=0.015)
    m.paint(ORANGE)
    m.prism("body", 0.14, 0.44, (0, 0, 0.05), sides=8, top_radius=0.035)
    m.paint(LINE)
    m.prism("stripe", 0.108, 0.09, (0, 0, 0.2), sides=8, top_radius=0.084)
    return m


def planter():
    m = Model("Planter")
    m.paint(WOOD)
    m.box("box", (0.82, 0.82, 0.3), (0, 0, 0.15), taper=(1.06, 1.06), bevel=0.03)
    m.paint((92, 66, 50))
    m.box("soil", (0.74, 0.74, 0.03), (0, 0, 0.3))
    m.paint(LEAF)
    m.box("bush_a", (0.56, 0.56, 0.34), (0.03, -0.02, 0.48), taper=(0.7, 0.7), bevel=0.09)
    m.paint(LEAF_DARK)
    m.box("bush_b", (0.36, 0.36, 0.26), (-0.1, 0.1, 0.66), taper=(0.6, 0.6), bevel=0.07)
    return m


def tree():
    m = Model("Tree")
    m.paint(TRUNK)
    m.prism("trunk", 0.1, 0.55, (0, 0, 0), sides=6, top_radius=0.07)
    m.paint(LEAF_DARK)
    m.prism("crown_a", 0.55, 0.7, (0, 0, 0.45), sides=7, top_radius=0.22)
    m.paint(LEAF)
    m.prism("crown_b", 0.4, 0.6, (0, 0, 0.95), sides=7, top_radius=0.06, spin=0.4)
    return m


def bush():
    m = Model("Bush")
    m.paint(LEAF)
    m.box("a", (0.7, 0.6, 0.36), (0, 0, 0.18), taper=(0.7, 0.7), bevel=0.1)
    m.paint(LEAF_DARK)
    m.box("b", (0.44, 0.42, 0.3), (0.2, 0.1, 0.36), taper=(0.6, 0.6), bevel=0.08)
    return m


def lamp():
    m = Model("Lamp")
    m.paint(METAL)
    m.prism("foot", 0.11, 0.08, (0, 0, 0), sides=8, top_radius=0.07)
    m.prism("pole", 0.04, 1.7, (0, 0, 0.08), sides=6, top_radius=0.03)
    m.box("arm", (0.07, 0.5, 0.06), (0, -0.2, 1.76), bevel=0.015)
    m.paint(LAMP)
    m.box("light", (0.16, 0.26, 0.06), (0, -0.36, 1.71), bevel=0.02)
    return m


def booth():
    m = Model("Booth")
    m.paint(CONCRETE)
    m.box("body", (0.9, 0.9, 0.8), (0, 0, 0.4), bevel=0.04)
    m.paint(GLASS)
    m.box("window", (0.94, 0.5, 0.3), (0, -0.05, 0.56), bevel=0.02)
    m.paint((250, 96, 96))
    m.box("roof", (1.08, 1.08, 0.12), (0, 0, 0.86), bevel=0.04)
    m.paint(LINE)
    m.box("bar", (0.07, 1.5, 0.07), (0.56, -0.75, 0.5), bevel=0.015)
    return m


def tunnel_base():
    m = Model("TunnelBase")
    stone = (104, 114, 150)
    dark = (40, 44, 66)
    m.paint(stone)
    m.box("left", (0.14, 0.92, 0.62), (-0.39, 0, 0.31), bevel=0.03)
    m.box("right", (0.14, 0.92, 0.62), (0.39, 0, 0.31), bevel=0.03)
    m.box("back", (0.92, 0.14, 0.62), (0, -0.39, 0.31), bevel=0.03)
    m.box("roof", (0.92, 0.92, 0.16), (0, 0, 0.66), taper=(0.9, 0.9), bevel=0.05)
    m.paint(dark)
    m.box("mouth", (0.64, 0.06, 0.5), (0, 0.44, 0.27))
    m.box("floor", (0.64, 0.8, 0.02), (0, 0.02, 0.01))
    m.paint((255, 205, 60))
    for i in range(3):
        m.box(f"stripe_{i}", (0.1, 0.04, 0.07), (-0.24 + i * 0.24, 0.47, 0.585))
    return m


def tunnel_light():
    m = Model("TunnelLight")
    m.paint(PAINT, tint=1.0)
    m.box("plate", (0.6, 0.6, 0.05), (0, -0.02, 0.765), bevel=0.02)
    m.box("lamp_l", (0.1, 0.08, 0.1), (-0.39, 0.44, 0.67), bevel=0.02)
    m.box("lamp_r", (0.1, 0.08, 0.1), (0.39, 0.44, 0.67), bevel=0.02)
    outline = [(0.0, 0.2), (0.16, 0.0), (0.06, 0.0), (0.06, -0.18), (-0.06, -0.18), (-0.06, 0.0), (-0.16, 0.0)]
    m.paint(ARROW)
    m.extrude("arrow", outline, 0.02, loc=(0, -0.02, 0.79))
    return m


ALL = [car, van, question, garage, waiting_slot, cone, planter, tree, bush, lamp, booth, tunnel_base, tunnel_light]

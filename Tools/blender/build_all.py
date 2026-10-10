import importlib
import math
import os

import bpy
from mathutils import Vector

import lowpoly
import models


def run(repo, previews=True, save=True):
    for module in (lowpoly, models):
        importlib.reload(module)

    out = os.path.join(repo, "Assets", "_Project", "Art", "Models")
    preview_dir = os.path.join(repo, "Temp", "ModelPreviews")
    os.makedirs(out, exist_ok=True)
    os.makedirs(preview_dir, exist_ok=True)

    built = [make() for make in models.ALL]
    report = {}
    for model in built:
        report[model.name] = model.export(os.path.join(out, model.name + ".lpmesh"))

    if previews:
        _setup_preview_render()
        for model in built:
            _render_preview(model, built, os.path.join(preview_dir, model.name + ".png"))
        for model in built:
            model.col.hide_render = False
            model.col.hide_viewport = False

    if save:
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(repo, "Tools", "blender", "models.blend"))
    return report


def _setup_preview_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "OBJECT"
    shading.show_cavity = True
    shading.show_shadows = False
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    if scene.world is not None:
        scene.world.color = (0.55, 0.62, 0.72)


def _render_preview(model, all_models, path):
    scene = bpy.context.scene
    for other in all_models:
        other.col.hide_render = other is not model

    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in model.col.objects:
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
    center = (lo + hi) * 0.5
    radius = max((hi - lo).length * 0.5, 0.2)

    cam = bpy.data.objects.get("PS_PreviewCam")
    if cam is None:
        cam = bpy.data.objects.new("PS_PreviewCam", bpy.data.cameras.new("PS_PreviewCam"))
        scene.collection.objects.link(cam)
    cam.data.lens = 70
    direction = Vector((0.55, -0.75, 0.95)).normalized()
    distance = radius / math.tan(math.atan(18.0 / 70.0)) * 1.15
    cam.location = center + direction * distance
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def render_icons(repo, size=256):
    out = os.path.join(repo, "Assets", "_Project", "Art", "UI")
    scene = bpy.context.scene
    _setup_preview_render()
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"

    icons = {
        "Feature_Van": (["Van"], (62, 136, 246)),
        "Feature_Mystery": (["Car", "Question"], (150, 156, 176)),
        "Feature_Obstacle": (["Planter"], (255, 255, 255)),
        "Feature_Tunnel": (["TunnelBase", "TunnelLight"], (66, 196, 110)),
    }
    collections = [c for c in bpy.data.collections if c.name.startswith("PS_")]
    for name, (parts, tint) in icons.items():
        wanted = ["PS_" + p for p in parts]
        restore = []
        lo = Vector((1e9, 1e9, 1e9))
        hi = Vector((-1e9, -1e9, -1e9))
        for col in collections:
            col.hide_render = col.name not in wanted
            if col.hide_render:
                continue
            for obj in col.objects:
                if col.name == "PS_Question":
                    restore.append((obj, obj.location.copy()))
                    obj.location.z += 0.72
                if obj.get("tint", 0.0) > 0.5:
                    restore.append((obj, None, tuple(obj.color)))
                    obj.color = (tint[0] / 255, tint[1] / 255, tint[2] / 255, 1.0)
        bpy.context.view_layer.update()
        for col in collections:
            if col.hide_render:
                continue
            for obj in col.objects:
                for corner in obj.bound_box:
                    p = obj.matrix_world @ Vector(corner)
                    lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
                    hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
        center = (lo + hi) * 0.5
        radius = max((hi - lo).length * 0.5, 0.2)
        cam = bpy.data.objects.get("PS_PreviewCam")
        if cam is None:
            cam = bpy.data.objects.new("PS_PreviewCam", bpy.data.cameras.new("PS_PreviewCam"))
            scene.collection.objects.link(cam)
        cam.data.lens = 70
        direction = Vector((0.6, 0.8, 0.9)).normalized()
        cam.location = center + direction * (radius / math.tan(math.atan(18.0 / 70.0)) * 1.08)
        cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        scene.render.filepath = os.path.join(out, name + ".png")
        bpy.ops.render.render(write_still=True)
        for item in restore:
            if item[1] is not None:
                item[0].location = item[1]
            else:
                item[0].color = item[2]

    for col in collections:
        col.hide_render = False
    scene.render.film_transparent = False
    return list(icons)

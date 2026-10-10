import json
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


def _reset_collection(name):
    col = bpy.data.collections.get(name)
    if col is not None:
        for obj in list(col.objects):
            mesh = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if mesh is not None and mesh.users == 0:
                bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(col)
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


class Model:
    def __init__(self, name):
        self.name = name
        self.col = _reset_collection("PS_" + name)
        self._rgb = (255, 255, 255)
        self._tint = 0.0

    def paint(self, rgb, tint=0.0):
        self._rgb = tuple(rgb)
        self._tint = tint
        return self

    def box(self, name, size, loc, rot=(0, 0, 0), taper=(1.0, 1.0), top_shift=(0.0, 0.0), bevel=0.0):
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        sx, sy, sz = size
        for v in bm.verts:
            top = v.co.z > 0
            v.co.x *= sx * (taper[0] if top else 1.0)
            v.co.y *= sy * (taper[1] if top else 1.0)
            v.co.z *= sz
            if top:
                v.co.x += top_shift[0]
                v.co.y += top_shift[1]
        return self._finish(bm, name, loc, rot, bevel)

    def prism(self, name, radius, height, loc, rot=(0, 0, 0), sides=8, top_radius=None, bevel=0.0, spin=0.0):
        bm = bmesh.new()
        r2 = radius if top_radius is None else top_radius
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=sides,
                              radius1=radius, radius2=max(r2, 0.0), depth=height,
                              matrix=Matrix.Rotation(spin, 4, "Z"))
        bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.0005)
        for v in bm.verts:
            v.co.z += height * 0.5
        return self._finish(bm, name, loc, rot, bevel)

    def wheel(self, name, radius, width, loc, sides=10):
        return self.prism(name, radius, width, (loc[0] - width * 0.5, loc[1], loc[2]),
                          rot=(0, math.radians(90), 0), sides=sides)

    def poly(self, name, verts, faces, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.0):
        bm = bmesh.new()
        bverts = [bm.verts.new(v) for v in verts]
        for f in faces:
            bm.faces.new([bverts[i] for i in f])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return self._finish(bm, name, loc, rot, bevel)

    def extrude(self, name, outline, height, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.0):
        n = len(outline)
        verts = [(x, y, 0.0) for x, y in outline] + [(x, y, height) for x, y in outline]
        faces = [list(range(n - 1, -1, -1)), list(range(n, 2 * n))]
        for i in range(n):
            j = (i + 1) % n
            faces.append([i, j, n + j, n + i])
        return self.poly(name, verts, faces, loc, rot, bevel)

    def _finish(self, bm, name, loc, rot, bevel):
        if bevel > 0:
            bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=1, profile=0.5, affect="EDGES")
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        mesh = bpy.data.meshes.new(f"{self.name}_{name}")
        bm.to_mesh(mesh)
        bm.free()

        obj = bpy.data.objects.new(f"{self.name}_{name}", mesh)
        self.col.objects.link(obj)
        obj.location = Vector(loc)
        obj.rotation_euler = rot if isinstance(rot, Euler) else Euler(rot)
        obj["rgb"] = list(self._rgb)
        obj["tint"] = float(self._tint)
        obj.color = (self._rgb[0] / 255, self._rgb[1] / 255, self._rgb[2] / 255, 1.0)
        return obj

    def export(self, path):
        deps = bpy.context.evaluated_depsgraph_get()
        deps.update()
        positions, normals, colors, triangles = [], [], [], []

        for obj in self.col.objects:
            if obj.type != "MESH":
                continue
            evaluated = obj.evaluated_get(deps)
            mesh = evaluated.to_mesh()
            mesh.calc_loop_triangles()
            world = obj.matrix_world
            normal_matrix = world.to_3x3().inverted().transposed()
            rgb = [c / 255.0 for c in obj["rgb"]]
            tint = float(obj["tint"])

            for tri in mesh.loop_triangles:
                n = (normal_matrix @ mesh.polygons[tri.polygon_index].normal).normalized()
                base = len(positions) // 3
                for vi in tri.vertices:
                    p = world @ mesh.vertices[vi].co
                    positions += [round(p.x, 4), round(p.z, 4), round(p.y, 4)]
                    normals += [round(n.x, 4), round(n.z, 4), round(n.y, 4)]
                    colors += [round(rgb[0], 4), round(rgb[1], 4), round(rgb[2], 4), round(tint, 3)]
                triangles += [base, base + 2, base + 1]
            evaluated.to_mesh_clear()

        data = {"name": self.name, "positions": positions, "normals": normals, "colors": colors, "triangles": triangles}
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, separators=(",", ":"))
        return len(triangles) // 3

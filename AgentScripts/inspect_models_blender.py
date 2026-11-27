import bpy, math
from mathutils import Vector
from pathlib import Path

root = Path(r'D:\67\Last67Seconds-Maps')
models = [('Hellhound', root / 'Assets/03_Models/Enemies/Hellhound/hellhound_3d_model.obj'),
          ('Inquisitor', root / 'Assets/03_Models/Enemies/HorrorInquisitor/horror_inquisitor_3d_model.obj')]
for name, path in models:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.wm.obj_import(filepath=str(path))
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(v) for o in objects for v in o.bound_box]
    lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center = (lo + hi) / 2
    size = max(hi - lo)
    print(name, 'bounds', list(lo), list(hi), flush=True)
    bpy.ops.object.camera_add(location=center + Vector((size * 2.4, -size * 3, size * 1.3)))
    camera = bpy.context.object
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = size * 1.65
    scene = bpy.context.scene
    scene.camera = camera
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.render.resolution_x = 800
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(root / 'Docs' / (name + '_inspection.png'))
    bpy.ops.render.render(write_still=True)

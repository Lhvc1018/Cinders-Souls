import bpy
from mathutils import Vector
from pathlib import Path
root=Path(r'D:\67\Last67Seconds-Maps')
for name in ('Hellhound','HorrorInquisitor'):
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    folder=root/'Assets/03_Models/Enemies'/name
    bpy.ops.wm.obj_import(filepath=str(next(folder.glob('*.obj'))))
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(v) for o in objs for v in o.bound_box]
    low=Vector(tuple(min(p[i] for p in points) for i in range(3))); high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(low+high)/2; size=max(high-low)
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    for label,vec in [('minusY',(0,-3,0)),('plusY',(0,3,0)),('plusX',(3,0,0)),('top',(0,0,3))]:
        bpy.ops.object.camera_add(location=center+size*Vector(vec));c=bpy.context.object;c.rotation_euler=(center-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=size*1.3;scene.camera=c
        scene.render.filepath=str(root/'Docs'/f'{name}_{label}.png');bpy.ops.render.render(write_still=True);bpy.data.objects.remove(c,do_unlink=True)

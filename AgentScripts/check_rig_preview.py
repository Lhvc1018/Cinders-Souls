import bpy
from pathlib import Path
from mathutils import Vector
root=Path(r'D:\67\Last67Seconds-Maps')
for name in ('Hellhound','HorrorInquisitor'):
    bpy.ops.wm.open_mainfile(filepath=str(root/'Docs'/('BORRADOR_'+name+'_rig.blend')))
    scene=bpy.context.scene;rig=next(o for o in scene.objects if o.type=='ARMATURE');mesh=next(o for o in scene.objects if o.type=='MESH')
    rig.animation_data.action=bpy.data.actions['Melee'];scene.frame_set(9)
    points=[mesh.matrix_world@Vector(v) for v in mesh.bound_box];low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)));center=(low+high)/2;size=max(high-low)
    bpy.ops.object.camera_add(location=center+Vector((size*1.5,-size*3,size*.8)));camera=bpy.context.object;camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=size*1.7
    scene.camera=camera;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
    scene.render.filepath=str(root/'Docs'/(name+'_attack_draft.png'));bpy.ops.render.render(write_still=True)

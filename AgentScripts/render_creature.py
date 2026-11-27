import bpy, math
from mathutils import Vector
from pathlib import Path
root=Path(r'D:/67/Last67Seconds-Maps')
bpy.ops.wm.open_mainfile(filepath=str(root/'Docs/BORRADOR_Hellhound_rig.blend'))
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1100;scene.render.resolution_y=650;scene.render.resolution_percentage=100
scene.world.color=(.23,.23,.23)
rig=next(o for o in scene.objects if o.type=='ARMATURE')
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1)
bpy.ops.object.camera_add(location=(0,-2,.38));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.25))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.4;scene.camera=camera
for pos,energy,size in [((1,-1,2),120,2),((-1,1,1),80,1)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=energy;l.data.shape='DISK';l.data.size=size;l.rotation_euler=(Vector((0,0,.3))-l.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(root/'Docs/Hound_rest_side.png');bpy.ops.render.render(write_still=True)



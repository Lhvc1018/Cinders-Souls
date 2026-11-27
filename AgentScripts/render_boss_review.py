import bpy
from mathutils import Vector
from pathlib import Path
root=Path(r'D:/67/Last67Seconds-Maps')
bpy.ops.wm.open_mainfile(filepath=str(root/'Docs/BORRADOR_HorrorInquisitor_rig.blend'))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=800;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.world.color=(.18,.18,.18)
rig=next(o for o in scene.objects if o.type=='ARMATURE')
bpy.ops.object.camera_add(location=(1,-2,.95));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.50))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.18;scene.camera=camera
for pos,energy in [((1,-1,2),100),((-1,1,1.3),90)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=energy;l.data.size=2;l.rotation_euler=(Vector((0,0,.5))-l.location).to_track_quat('-Z','Y').to_euler()
for name,frame in [('Idle',1),('Chase',1),('Melee',17),('Slam',17),('Death',36)]:
 rig.animation_data.action=bpy.data.actions[name];scene.frame_set(frame)
 scene.render.filepath=str(root/'Docs'/('Inquisitor_'+name+'.png'));bpy.ops.render.render(write_still=True)

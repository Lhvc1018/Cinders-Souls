import bpy, sys
from mathutils import Vector
from pathlib import Path
root=Path(r'D:/67/Last67Seconds-Maps');bpy.ops.wm.open_mainfile(filepath=str(root/'Docs/AshenHound.blend'))
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=1280;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.world.color=(.12,.12,.12)
rig=next(o for o in scene.objects if o.type=='ARMATURE')
bpy.ops.object.camera_add(location=(2.3,-4,1.7));camera=bpy.context.object
camera.rotation_euler=(Vector((-.02,0,.55))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=3.1;scene.camera=camera
for pos,energy,size in [((1,-3,4),450,4),((-2,2,2),350,3),((3,1,1),120,2)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=energy;l.data.size=size;l.rotation_euler=(Vector((0,0,.5))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object
m=bpy.data.materials.new('Charcoal ground');m.diffuse_color=(.065,.065,.065,1);floor.data.materials.append(m)
for name,frame in [('Idle',1),('Chase',5),('Chase',12),('Melee',15),('Death',39)]:
 rig.animation_data.action=bpy.data.actions[name];scene.frame_set(frame);scene.render.filepath=str(root/'Docs'/('AshenHound_'+name+str(frame)+'.png'));bpy.ops.render.render(write_still=True)

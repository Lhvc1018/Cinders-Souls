import bpy, math
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Knight/armor.blend')
for o in list(bpy.data.objects):
 if o.type!='MESH': bpy.data.objects.remove(o,do_unlink=True)
m=bpy.data.materials.new('Preview');m.use_nodes=True
bs=m.node_tree.nodes.get('Principled BSDF');tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['armor_default_color.png'];m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);bs.inputs['Roughness'].default_value=.55
for o in bpy.data.objects:
 if o.type=='MESH':o.data.materials.clear();o.data.materials.append(m)
bpy.ops.object.camera_add(location=(0,-13,.5));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=6.8;bpy.context.scene.camera=cam
for p,e,s in [((3,-5,7),1700,5),((-4,-3,2),1000,4)]:
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=e;o.data.shape='DISK';o.data.size=s;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=16;sc.world.color=(.25,.25,.25);sc.render.resolution_x=800;sc.render.resolution_y=800;sc.render.resolution_percentage=100;sc.render.filepath='D:/67/CindersBackups/ModelResearch/knight-source.png';bpy.ops.render.render(write_still=True)

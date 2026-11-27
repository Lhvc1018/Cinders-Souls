import bpy
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Defender/Defender.blend')
for o in bpy.data.objects:
 if o.type=='ARMATURE':
  print('ARM',o.name,tuple(o.scale));print([(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in o.data.bones][:30])
 if o.type=='MESH' and len(o.data.vertices)>100:print('MESH',o.name,len(o.data.vertices),[m.name for m in o.data.materials],len(o.vertex_groups),tuple(o.dimensions))
print('IMAGES',[(i.name,bool(i.packed_file)) for i in bpy.data.images])

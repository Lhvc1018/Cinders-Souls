import bpy,json
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Knight/armor.blend')
for o in bpy.data.objects:
 if o.type in ['MESH','ARMATURE']:
  print('MODEL',o.name,o.type,tuple(o.dimensions),tuple(o.location),len(o.data.vertices) if o.type=='MESH' else len(o.data.bones),[m.name for m in o.data.materials] if o.type=='MESH' else '')
print('IMAGES',[(i.name,tuple(i.size),bool(i.packed_file)) for i in bpy.data.images])

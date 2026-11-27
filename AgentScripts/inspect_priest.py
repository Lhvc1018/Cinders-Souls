import bpy
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/priest.blend')
a=bpy.data.objects['metarig'];print('RIG',a.matrix_world)
for b in a.data.bones:
 print('BONE',b.name,tuple(a.matrix_world@b.head_local),tuple(a.matrix_world@b.tail_local),b.use_deform)
for name in ['body','body.001','helmet','robe','mantle','stripes']:
 o=bpy.data.objects[name];print('OBJ',name,'hide',o.hide_render,'groups',len(o.vertex_groups),'mats',[m.name for m in o.data.materials],'mods',[(m.type,m.show_render) for m in o.modifiers])
print('IMAGES',[(i.name,i.filepath,bool(i.packed_file)) for i in bpy.data.images])

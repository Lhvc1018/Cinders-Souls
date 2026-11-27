import bpy
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Defender/Defender.blend')
a=bpy.data.objects['Defender_Ske'];o=bpy.data.objects['Defender'];print('MAT',a.matrix_world,o.matrix_world)
print('GROUPS',[g.name for g in o.vertex_groups]);print('BONES',[b.name for b in a.data.bones]);print('BOUNDS',[(min((o.matrix_world@v.co)[i] for v in o.data.vertices),max((o.matrix_world@v.co)[i] for v in o.data.vertices)) for i in range(3)])

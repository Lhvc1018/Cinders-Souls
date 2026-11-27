import bpy,json
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Knight/armor.blend')
for o in bpy.data.objects:
 if o.type=='MESH':
  vs=[o.matrix_world@v.co for v in o.data.vertices]
  print(o.name,'BOUND',[(min(v[i] for v in vs),max(v[i] for v in vs)) for i in range(3)])
  print('MATRIX',o.matrix_world)

import bpy,collections
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Knight/armor.blend')
o=bpy.data.objects['Group61487'];m=o.data
adj=[[] for v in m.vertices]
for e in m.edges:
 a,b=e.vertices;adj[a].append(b);adj[b].append(a)
seen=set()
for i in range(len(adj)):
 if i in seen:continue
 stack=[i];seen.add(i);vs=[]
 while stack:
  k=stack.pop();vs.append(o.matrix_world@m.vertices[k].co)
  for n in adj[k]:
   if n not in seen:seen.add(n);stack.append(n)
 if len(vs)>15:print('ISLAND',i,len(vs),[(round(min(v[j] for v in vs),3),round(max(v[j] for v in vs),3)) for j in range(3)])
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/priest.blend')
for o in bpy.data.objects:
 if o.type in ['MESH','ARMATURE']:print('PRIEST',o.name,o.type,tuple(o.dimensions),len(o.data.vertices) if o.type=='MESH' else len(o.data.bones))

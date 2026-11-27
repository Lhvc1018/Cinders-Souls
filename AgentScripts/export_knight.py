import bpy,json,os
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Knight/armor.blend')
out='D:/67/Last67Seconds-Maps/Assets/03_Models/Characters/CindersDetailed/Knight';os.makedirs(out,exist_ok=True)
o=bpy.data.objects['Group61487'];m=o.data
adj=[[] for v in m.vertices]
for e in m.edges:
 a,b=e.vertices;adj[a].append(b);adj[b].append(a)
shield=set([1099]);stack=[1099]
while stack:
 for n in adj[stack.pop()]:
  if n not in shield:shield.add(n);stack.append(n)
m.calc_loop_triangles();verts=[];uv=[];norm=[]
for tri in m.loop_triangles:
 if any(i in shield for i in tri.vertices):continue
 for li in reversed(tri.loops):
  vi=m.loops[li].vertex_index;v=o.matrix_world@m.vertices[vi].co
  verts.extend([v.x*1.8/5.594307,(v.z+2.385746)*1.8/5.594307,-v.y*1.8/5.594307]);uv.extend(m.uv_layers.active.data[li].uv);n=o.matrix_world.to_3x3()@m.vertices[vi].normal;n.normalize();norm.extend([n.x,n.z,-n.y])
json.dump({'vertices':verts,'uv':uv,'normals':norm},open(out+'/KnightGeometry.json','w'))
for im in bpy.data.images:
 if im.name.startswith('armor_default'):
  im.filepath_raw=out+'/'+im.name;im.file_format='PNG';im.save()
print('EXPORTED',len(verts)//3,'vertices without sword/shield')


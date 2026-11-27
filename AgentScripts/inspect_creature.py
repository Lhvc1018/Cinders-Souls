import bpy, json
from pathlib import Path
root=Path(r'D:/67/Last67Seconds-Maps')
bpy.ops.wm.open_mainfile(filepath=str(root/'Docs/BORRADOR_Hellhound_rig.blend'))
mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
import numpy as np, math
from mathutils import Matrix
r=Matrix.Rotation(math.radians(45),4,'Z')
for v in mesh.data.vertices:v.co=r@v.co
for z in [.035,.1,.18,.25,.32,.4,.5]:
 a=np.array([tuple(v.co) for v in mesh.data.vertices if abs(v.co.z-z)<.012])
 centers=np.array([[-.2,-.16,z],[-.2,.16,z],[.3,-.16,z],[.3,.16,z]])
 for _ in range(25):
  groups=np.argmin(np.linalg.norm(a[:,None,:]-centers[None,:,:],axis=2),axis=1)
  centers=np.array([a[groups==j].mean(axis=0) if np.any(groups==j) else centers[j] for j in range(4)])
 print('CLUSTERS',z,np.round(centers,3).tolist())
print('MESH_BOUNDS',[(min(v.co[i] for v in mesh.data.vertices),max(v.co[i] for v in mesh.data.vertices)) for i in range(3)])
for ysign in [-1,1]:
 for z in [.04,.1,.18,.25,.32,.4]:
  vs=[v.co for v in mesh.data.vertices if v.co.y*ysign>.06 and abs(v.co.z-z)<.015]
  print('SLICE',ysign,z,[(round(min(v[i] for v in vs),3),round(max(v[i] for v in vs),3)) for i in range(3)] if vs else [])

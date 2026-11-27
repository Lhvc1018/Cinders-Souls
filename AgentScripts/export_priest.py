import bpy,json,os,shutil
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/priest.blend')
out='D:/67/Last67Seconds-Maps/Assets/03_Models/Characters/CindersDetailed/Priest';os.makedirs(out,exist_ok=True)
a=bpy.data.objects['metarig'];scale=1.8/3.86
mapping={'hips':'Hips','hips.001':'Hips','spine':'Spine','chest':'Chest','neck':'Neck','head':'Head'}
for side,hum in [('L','Right'),('R','Left')]:
 for s,h in [('shoulder','Shoulder'),('upper_arm','UpperArm'),('forearm','LowerArm'),('hand','Hand'),('thigh','UpperLeg'),('shin','LowerLeg'),('foot','Foot'),('toe','Toes')]:mapping[s+'.'+side]=hum+h
 for s,h in [('thumb','Thumb'),('f_index','Index'),('f_middle','Middle'),('f_ring','Ring'),('f_pinky','Little')]:
  for idx,part in [('01','Proximal'),('02','Intermediate'),('03','Distal')]:mapping[s+'.'+idx+'.'+side]=hum+h+part
 for i in range(1,5):mapping['palm.0'+str(i)+'.'+side]=hum+'Hand'
def xyz(v):return [v.x*scale,v.z*scale,-v.y*scale]
bones=[];index={}
for name,hum in mapping.items():
 if name in a.data.bones:
  b=a.data.bones[name];index[name]=len(bones);bones.append({'name':hum,'head':xyz(a.matrix_world@b.head_local),'tail':xyz(a.matrix_world@b.tail_local)})
parts=[]
for name in ['body','helmet','robe','mantle','stripes']:
 o=bpy.data.objects[name];m=o.data;m.calc_loop_triangles()
 for mi in range(len(m.materials)):
  v=[];uv=[];nn=[];bi=[];bw=[]
  for tri in m.loop_triangles:
   if tri.material_index!=mi:continue
   for li in tri.loops:
    vi=m.loops[li].vertex_index;vert=m.vertices[vi];v.extend(xyz(o.matrix_world@vert.co));uv.extend(m.uv_layers.active.data[li].uv);n=o.matrix_world.to_3x3()@vert.normal;n.normalize();nn.extend([n.x,n.z,-n.y])
    groups=[]
    for g in vert.groups:
     gn=o.vertex_groups[g.group].name
     if gn in ['left eye','right eye','lip top','lip bottom']:gn='head'
     if gn=='hips.001':gn='hips'
     for side in ['L','R']:
      if gn in ['roll_main.'+side,'roll_back.'+side,'roll_front.'+side,'foot_IK.'+side,'foot_IK_main.'+side]:gn='foot.'+side
      if gn=='pole_target.'+side:gn='shin.'+side
     if gn in index and g.weight>0.00001:groups.append((index[gn],g.weight))
    groups=sorted(groups,key=lambda z:-z[1])[:4]
    if not groups:groups=[(index['hips'],1)]
    total=sum(g[1] for g in groups)
    while len(groups)<4:groups.append((0,0))
    bi.extend(g[0] for g in groups);bw.extend(g[1]/total for g in groups)
  if v:parts.append({'name':name+'_'+str(mi),'vertices':v,'normals':nn,'uv':uv,'indices':bi,'weights':bw})
json.dump({'bones':bones,'parts':parts},open(out+'/PriestGeometry.json','w'))
for path in os.listdir('D:/67/CindersBackups/ModelResearch/PriestTextures'):
 if path.endswith('.png'):shutil.copy2('D:/67/CindersBackups/ModelResearch/PriestTextures/'+path,out+'/'+path)
print('PRIEST',len(parts),len(bones))



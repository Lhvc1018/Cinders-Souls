import bpy,json,os
bpy.ops.wm.open_mainfile(filepath='D:/67/CindersBackups/ModelResearch/Defender/Defender.blend')
a=bpy.data.objects['Defender_Ske'];o=bpy.data.objects['Defender'];out='D:/67/Last67Seconds-Maps/Assets/03_Models/Characters/CindersDetailed/Defender';os.makedirs(out,exist_ok=True)
mapping={'Hips':'Hips','LowerBack':'Spine','Spine':'Chest','Spine1':'Chest','Neck':'Neck','Neck1':'Neck','Head':'Head'}
for s,h in [('Left','Right'),('Right','Left')]:
 for suffix,target in [('UpLeg','UpperLeg'),('Leg','LowerLeg'),('Foot','Foot'),('ToeBase','Toes'),('Shoulder','Shoulder'),('Arm','UpperArm'),('ForeArm','LowerArm'),('Hand','Hand')]:mapping[s+suffix]=h+target
aliases={'LHipJoint':'Hips','RHipJoint':'Hips','LeftFingerBase':'LeftHand','LeftHandIndex1':'LeftHand','LThumb':'LeftHand','RightFingerBase':'RightHand','RightHandIndex1':'RightHand','RThumb':'RightHand','Spine1':'Spine','Neck1':'Neck'}
def xyz(v):return [v.x,v.z,-v.y]
bones=[];index={}
for name,hum in mapping.items():
 b=a.data.bones[name];index[name]=len(bones);bones.append({'name':hum,'head':xyz(a.matrix_world@b.head_local),'tail':xyz(a.matrix_world@b.tail_local)})
m=o.data;m.calc_loop_triangles();v=[];uv=[];nn=[];bi=[];bw=[]
for tri in m.loop_triangles:
 if tri.material_index==0:continue
 for li in tri.loops:
  vi=m.loops[li].vertex_index;vert=m.vertices[vi];v.extend(xyz(o.matrix_world@vert.co));uv.extend(m.uv_layers.active.data[li].uv);n=o.matrix_world.to_3x3()@vert.normal;n.normalize();nn.extend([n.x,n.z,-n.y]);groups=[]
  for g in vert.groups:
   gn=o.vertex_groups[g.group].name;gn=aliases.get(gn,gn)
   if gn in index and g.weight>.00001:groups.append((index[gn],g.weight))
  groups=sorted(groups,key=lambda z:-z[1])[:4]
  if not groups:groups=[(index['Hips'],1)]
  total=sum(g[1] for g in groups)
  while len(groups)<4:groups.append((0,0))
  bi.extend(g[0] for g in groups);bw.extend(g[1]/total for g in groups)
json.dump({'bones':bones,'parts':[{'name':'body_0','vertices':v,'normals':nn,'uv':uv,'indices':bi,'weights':bw}]},open(out+'/DefenderGeometry.json','w'))
for name,target in [('Done3.png','body text.png'),('Done2_NMoverlay2.png','body normal.png')]:
 im=bpy.data.images[name];im.filepath_raw=out+'/'+target;im.file_format='PNG';im.save()
print('DEFENDER',len(v)//3)

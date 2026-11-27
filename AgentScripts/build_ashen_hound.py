"""Original undead canine, anatomically fitted rig and baked foot-target locomotion.
No downloaded game mesh or motion is used. Source remains editable in Docs.
"""
import bpy, math, random
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
import numpy as np
ROOT=Path(r'D:/67/Last67Seconds-Maps');OUT=ROOT/'Assets/03_Models/Enemies/AshenHound';OUT.mkdir(parents=True,exist_ok=True)
random.seed(67)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
def mat(name,color,rough=.85,emission=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough
 if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
 return m
skin=mat('AshenSkin',(.18,.16,.12));boneMat=mat('WeatheredBone',(.28,.26,.20));flesh=mat('DriedWound',(.055,.017,.013));horn=mat('BlackClaw',(.028,.026,.021));eye=mat('FeralEye',(.32,.15,.035),.4,.4);hide=mat('RaggedHide',(.13,.12,.09))
pieces=[];decor=[]
def ellipsoid(name,location,scale,material,group=None):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=location);o=bpy.context.object;o.name=name;o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 for p in o.data.polygons:p.use_smooth=True
 (decor if group else pieces).append((o,group) if group else o);return o
def tube(name,points,radii,material,group=None,segments=12):
 vertices=[];faces=[]
 for i,point in enumerate(points):
  point=Vector(point);tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
  tangent.normalize();u=tangent.cross(Vector((0,1,0))).normalized();v=tangent.cross(u).normalized()
  for j in range(segments):vertices.append(point+(u*math.cos(j*math.tau/segments)+v*math.sin(j*math.tau/segments))*radii[i])
 for i in range(len(points)-1):
  for j in range(segments):a=i*segments+j;b=i*segments+(j+1)%segments;faces.append((a,b,b+segments,a+segments))
 faces.append(tuple(range(segments-1,-1,-1)));faces.append(tuple((len(points)-1)*segments+j for j in range(segments)))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.data.materials.append(material)
 for p in o.data.polygons:p.use_smooth=True
 (decor if group else pieces).append((o,group) if group else o);return o
def fang(name,a,b,radius,group):tube(name,[a,b],[radius,.0008],boneMat,group,8)

# Lean torso with a tucked abdomen, distinct shoulders, long muzzle and canine hocks.
ellipsoid('Rib cage',(.10,0,.69),(.36,.205,.235),skin)
ellipsoid('Tucked waist',(-.27,0,.70),(.29,.105,.11),skin)
ellipsoid('Pelvis',(-.50,0,.66),(.19,.155,.17),skin)
ellipsoid('Withers',(.30,0,.79),(.19,.14,.12),skin)
tube('Neck',[(.30,0,.70),(.46,0,.81),(.62,0,.88)],[.15,.14,.115],skin)
ellipsoid('Skull',(.70,0,.89),(.18,.102,.105),skin)
ellipsoid('Upper muzzle',(.91,0,.835),(.20,.070,.043),skin)
ellipsoid('Cheek L',(.71,-.087,.83),(.065,.035,.060),skin)
ellipsoid('Cheek R',(.71,.087,.83),(.065,.035,.060),skin)
ellipsoid('Nose', (1.075,0,.835),(.038,.060,.031),horn,'head')
ellipsoid('Mandible',(.91,0,.763),(.178,.054,.018),hide,'jaw')
ellipsoid('Exposed gums',(.93,0,.777),(.16,.051,.009),flesh,'jaw')
for side in (-1,1):
 y=side*.101
 ellipsoid('Eye socket',(.76,y*.86,.919),(.030,.014,.020),horn,'head')
 ellipsoid('Clouded eye',(.772,y*.97,.919),(.009,.006,.009),eye,'head')
 tube('Brow ridge',[(.715,y*.88,.947),(.752,y*.98,.949),(.793,y*.83,.940)],[.018,.016,.008],hide,'head',8)
 for i in range(6):
  x=.81+i*.042;length=.048 if i in (0,4) else random.uniform(.012,.023)
  fang('Upper tooth',(x,side*.052,.798),(x+.005,side*.050,.798-length),.009 if i in (0,4) else .004,'head')
 for i in range(5):
  x=.82+i*.047;fang('Lower tooth',(x,side*.047,.78),(x+.008,side*.046,.802),.004,'jaw')
 # Irregular thin ears, deliberately asymmetrical and torn.
 verts=[(.59,y*.75,.96),(.70,y*.75,.97),(.675,y*1.2,1.08 if side<0 else 1.02),(.625,y*1.5,1.16 if side<0 else 1.07),(.60,y*1.2,1.07),(.66,y*.65,1.025)]
 mesh=bpy.data.meshes.new('Ragged ear');mesh.from_pydata(verts,[],[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5)]);mesh.update()
 o=bpy.data.objects.new('Torn ear',mesh);bpy.context.collection.objects.link(o);o.data.materials.append(hide)
 mod=o.modifiers.new('Ear thickness','SOLIDIFY');mod.thickness=.009;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 mod=o.modifiers.new('Rounded torn edges','BEVEL');mod.width=.008;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
 for p in o.data.polygons:p.use_smooth=True
 decor.append((o,'ear_L' if side<0 else 'ear_R'))

legs={}
for side,y in [('L',-.155),('R',.155)]:
 front=[(.40,y,.70),(.27,y,.43),(.40,y,.09),(.53,y,.045)]
 rear=[(-.50,y,.66),(-.29,y,.40),(-.59,y,.18),(-.50,y,.045)]
 legs['front_'+side]=front;legs['back_'+side]=rear
 for name,chain,radii in [('front_'+side,front,[.105,.055,.030,.047]),('back_'+side,rear,[.12,.067,.028,.045])]:
  tube(name+' skin',chain,radii,skin)
  ellipsoid(name+' joint',chain[1],(.067,.055,.065),skin)
  p=chain[-1];ellipsoid(name+' paw',(p[0]+.035,y,p[2]),(.09,.06,.043),skin)
  for j in (-1,0,1):
   ellipsoid(name+' toe',(p[0]+.095,y+j*.035,.034),(.047,.021,.026),skin)
   tube('Claw',[(p[0]+.11,y+j*.035,.037),(p[0]+.155,y+j*.035,.027)],[.011,.0008],horn,name+'_paw',8)
tube('Tail',[(-.61,0,.69),(-.84,0,.59),(-1.04,.025,.56),(-1.22,.06,.61)],[.07,.045,.026,.006],skin)

# Exposed ribs sit over a dark, recessed wound, with no oversized monster ornaments.
for side in (-1,1):
 ellipsoid('Old flank wound',(.03,side*.115,.665),(.24,.012,.125),flesh,'spine')
 for i in range(6):
  x=-.15+i*.065+random.uniform(-.008,.008)
  points=[]
  for j in range(9):
   t=j/8;z=.805-(.235+.025*math.sin(i))*t;surface=.205*math.sqrt(max(.1,1-((z-.69)/.235)**2));points.append((x-.04*t+.025*math.sin(t*math.pi),side*(surface-.018+.008*math.sin(t*math.pi)),z))
  tube('Exposed rib',points,[.003+.005*math.sin(j/8*math.pi) for j in range(9)],boneMat,'spine',8)
 for i in range(12):
  a=i/12*math.tau;x=.03+.226*math.cos(a);z=.67+.128*math.sin(a)
  ellipsoid('Torn wound margin',(x,side*.188,z),(.026,.013,random.uniform(.012,.024)),flesh,'spine')
for i in range(9):
 ellipsoid('Dorsal vertebra',(-.43+i*.085,0,.805+(.02 if i>4 else -.01)),(.022,.027,.018),boneMat,'spine' if i>2 else 'pelvis')
for side in (-1,1):
 for i in range(8):
  x=random.uniform(-.35,.48);y=side*random.uniform(.03,.09);z=.82+(.015 if x>.2 else 0)
  tube('Matted bristle',[(x,y,z),(x-.07,y*1.3,z+.022),(x-.12,y*1.5,z+.015)],[.006,.003,0.0004],hide,'spine',5)

# A watertight, smoothed skin mesh makes heat weights reliable.
bpy.ops.object.select_all(action='DESELECT')
for o in pieces:o.select_set(True)
bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();body=bpy.context.object;body.name='AshenHound_Skin'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
remesh=body.modifiers.new('Continuous anatomy','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.014;remesh.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth=body.modifiers.new('Organic surface','SMOOTH');smooth.factor=.65;smooth.iterations=4;bpy.ops.object.modifier_apply(modifier=smooth.name)
for side in (-1,1):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,location=(.03,side*.19,.67));cutter=bpy.context.object;cutter.scale=(.245,.125,.138);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bpy.context.view_layer.objects.active=body;cut=body.modifiers.new('Recessed rib wound','BOOLEAN');cut.operation='DIFFERENCE';cut.object=cutter;bpy.ops.object.modifier_apply(modifier=cut.name);bpy.data.objects.remove(cutter,do_unlink=True)
body.data.materials.clear();body.data.materials.append(skin)
for p in body.data.polygons:p.use_smooth=True;p.material_index=0

bpy.ops.object.select_all(action='DESELECT');bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='AshenHoundRig'
bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
def bone(name,a,b,parent=None):
 pb=rig.data.edit_bones.new(name);pb.head=a;pb.tail=b
 if parent:pb.parent=rig.data.edit_bones[parent]
 return pb
bone('root',(0,0,0),(0,0,.15)).use_deform=False
bone('pelvis',(-.50,0,.66),(-.27,0,.67),'root');bone('spine',(-.27,0,.67),(.20,0,.74),'pelvis');bone('chest',(.20,0,.74),(.42,0,.77),'spine')
bone('neck',(.42,0,.77),(.64,0,.88),'chest');bone('head',(.64,0,.88),(.98,0,.86),'neck');bone('jaw',(.70,0,.78),(1.07,0,.76),'head')
bone('ear_L',(.64,-.10,.98),(.63,-.16,1.2),'head');bone('ear_R',(.64,.10,.98),(.63,.16,1.13),'head')
bone('tail',(-.61,0,.69),(-.94,0,.57),'pelvis');bone('tail_tip',(-.94,0,.57),(-1.22,.06,.61),'tail')
for name,chain in legs.items():
 bone(name,chain[0],chain[1],'chest' if name.startswith('front') else 'pelvis')
 bone(name+'_lower',chain[1],chain[2],name);bone(name+'_paw',chain[2],chain[3],name+'_lower')
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
if any(not v.groups for v in body.data.vertices):raise RuntimeError('Heat weighting left unweighted skin vertices')
for o,group in decor:
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.select_set(False)
 vg=o.vertex_groups.new(name=group);vg.add(list(range(len(o.data.vertices))),1,'REPLACE');o.parent=rig;modifier=o.modifiers.new('Skeletal binding','ARMATURE');modifier.object=rig

# Bake a mottled dry-hide texture for Unity; the other materials use defined colors.
nodes=skin.node_tree.nodes;links=skin.node_tree.links;principled=nodes.get('Principled BSDF')
noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=9;noise.inputs['Detail'].default_value=5;noise.inputs['Roughness'].default_value=.7
ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.24;ramp.color_ramp.elements[0].color=(.045,.043,.034,1);ramp.color_ramp.elements[1].position=.76;ramp.color_ramp.elements[1].color=(.24,.22,.17,1)
links.new(noise.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs['Color'],principled.inputs['Base Color'])
image=bpy.data.images.new('AshenHound_Skin',1024,1024);texture=nodes.new('ShaderNodeTexImage');texture.image=image;nodes.active=texture
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=.85,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.scene.render.engine='CYCLES';bpy.context.scene.cycles.samples=1
bpy.context.scene.render.bake.use_pass_direct=False;bpy.context.scene.render.bake.use_pass_indirect=False;bpy.context.scene.render.bake.use_pass_color=True
bpy.ops.object.bake(type='DIFFUSE');image.filepath_raw=str(OUT/'AshenHound_Skin.png');image.file_format='PNG';image.save()
links.new(texture.outputs['Color'],principled.inputs['Base Color'])

scene=bpy.context.scene;scene.render.fps=30
rig.animation_data_create()
def rotate(name,angles):
 pb=rig.pose.bones[name];basis=pb.bone.matrix_local.to_quaternion()
 from mathutils import Euler
 pb.rotation_quaternion=basis.inverted()@Euler(tuple(math.radians(v) for v in angles)).to_quaternion()@basis
def point(name,p):return rig.pose.bones[name].matrix@rig.data.bones[name].matrix_local.inverted()@Vector(p)
def aim_bone(name,a,b):
 pb=rig.pose.bones[name];rest=pb.bone.matrix_local.to_quaternion();direction=pb.bone.tail_local-pb.bone.head_local
 q=direction.rotation_difference(b-a)@rest;pb.matrix=Matrix.Translation(a)@q.to_matrix().to_4x4();bpy.context.view_layer.update()
max_reach_error=0
def ik(name,foot):
 global max_reach_error
 chain=legs[name];parent='chest' if name.startswith('front') else 'pelvis'
 a=point(parent,chain[0]);foot=Vector(foot);c=foot+(Vector(chain[2])-Vector(chain[3]))
 l1=(Vector(chain[1])-Vector(chain[0])).length;l2=(Vector(chain[2])-Vector(chain[1])).length
 d=c-a;dist=d.length;max_reach_error=max(max_reach_error,dist-l1-l2)
 dist=min(l1+l2-.0001,max(abs(l1-l2)+.0001,dist));direction=d.normalized();c=a+direction*dist
 along=(l1*l1-l2*l2+dist*dist)/(2*dist);h=math.sqrt(max(0,l1*l1-along*along))
 perpendicular=Vector((direction.z,0,-direction.x)).normalized()*(1 if name.startswith('front') else -1)
 b=a+direction*along+perpendicular*h
 aim_bone(name,a,b);aim_bone(name+'_lower',b,c);aim_bone(name+'_paw',c,c+Vector(chain[3])-Vector(chain[2]))
def smooth(t):return t*t*(3-2*t)
def lift_root(z):
 pb=rig.pose.bones['root'];pb.location=pb.bone.matrix_local.to_3x3().inverted()@Vector((0,0,z))
for action_name,seconds in [('Idle',2),('Walk',1.1),('Chase',.6),('Melee',.75),('Jump',.8),('Hit',.3),('Stun',.8),('Slam',.75),('Death',1.25)]:
 action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action
 frames=round(seconds*30)
 for frame in range(frames+1):
  t=frame/frames;scene.frame_set(frame+1)
  for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion();pb.location=(0,0,0);pb.scale=(1,1,1)
  root=rig.pose.bones['root'];lift_root(0)
  rotate('spine',(0,math.sin(t*math.tau)*1.2,0));rotate('head',(0,-2+math.sin(t*math.tau)*1,0));rotate('tail',(math.sin(t*math.tau)*3,0,math.sin(t*math.tau)*5));rotate('tail_tip',(0,0,math.sin(t*math.tau-.6)*7))
  hop=0
  if action_name in ('Chase','Walk'):
   lift_root(-.025+(.012 if action_name=='Chase' else .010)*math.sin(t*math.tau*2))
   rotate('spine',(0,math.sin(t*math.tau*2)*1.5,0));rotate('neck',(0,-math.sin(t*math.tau*2)*1.5,0));rotate('jaw',(0,7 if action_name=='Chase' else 2,0))
  elif action_name in ('Melee','Slam','Jump'):
   if t<.30:hop=-.065*math.sin(t/.3*math.pi/2)
   elif t<.72:hop=.24*math.sin((t-.30)/.42*math.pi)
   else:hop=-.035*math.sin((t-.72)/.28*math.pi)
   lift_root(hop)
   rotate('neck',(0,-12*math.sin(t*math.pi),0));rotate('head',(0,12*math.sin(t*math.pi),0));rotate('jaw',(0,26*max(0,math.sin((t-.15)/.6*math.pi)) if t<.75 else 0,0))
   rotate('spine',(0,-8*math.sin(t*math.tau),0))
  elif action_name=='Hit':
   rotate('spine',(0,-10*math.sin(t*math.pi),-6*math.sin(t*math.pi)));rotate('head',(0,-12*math.sin(t*math.pi),0))
  elif action_name=='Stun':
   lift_root(-.045);rotate('neck',(0,8,0));rotate('head',(0,9,4*math.sin(t*math.tau)));rotate('jaw',(0,5,0))
  elif action_name=='Death':
   f=smooth(min(1,t/.72));rotate('root',(90*f,0,0));lift_root(-.36*f)
   rotate('neck',(0,15*f,0));rotate('jaw',(0,14*f,0))
  bpy.context.view_layer.update()
  for name,chain in legs.items():
   foot=Vector(chain[-1])
   if action_name in ('Walk','Chase'):
    offsets={'front_L':0,'front_R':.5,'back_L':.5,'back_R':0} if action_name=='Walk' else {'front_L':0,'front_R':.12,'back_L':.50,'back_R':.62}
    phase=(t+offsets[name])%1;stance=.62 if action_name=='Walk' else .34;stride=.27 if action_name=='Walk' else .46
    if phase<stance:foot.x+=stride*(.5-phase/stance)
    else:u=(phase-stance)/(1-stance);foot.x+=stride*(-.5+smooth(u));foot.z+=(.06 if action_name=='Walk' else .13)*math.sin(u*math.pi)
   elif action_name in ('Melee','Slam','Jump'):
    if .30<t<.72:foot.z+=max(0,hop)+.045;foot.x+=(.14 if name.startswith('front') else -.12)*math.sin((t-.3)/.42*math.pi)
   if action_name=='Death':
    f=smooth(min(1,t/.72));rotate(name,(0,(25 if name.startswith('front') else -25)*f,0));rotate(name+'_lower',(0,35*f,0));continue
   ik(name,foot)
  rotate('ear_L',(0,3*math.sin(t*math.tau),0));rotate('ear_R',(0,5*math.sin(t*math.tau-.3),0))
  bpy.context.view_layer.update()
  # Ground correction for a collapsed body only; gait uses planted foot targets.
  if action_name=='Death':
   evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh();minimum=min(v.co.z for v in mesh.vertices);evaluated.to_mesh_clear()
   if minimum<0:root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-minimum))
  for pb in rig.pose.bones:
   pb.keyframe_insert('rotation_quaternion',frame=frame+1,group=pb.name);pb.keyframe_insert('location',frame=frame+1,group=pb.name)
 print('ANIMATION',action_name,frames+1,'frames',flush=True)
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);scene.frame_start=1;scene.frame_end=61
# One skinned renderer, with shared material submeshes rather than dozens of draws.
bpy.ops.object.select_all(action='DESELECT');body.select_set(True)
for o,g in decor:o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();decor=[]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True)
for o,g in decor:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'AshenHound_rigged.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Docs/AshenHound.blend'))
print('ASHEN_HOUND_COMPLETE',len(body.data.vertices),'skin vertices','max IK reach correction',round(max_reach_error,4),flush=True)

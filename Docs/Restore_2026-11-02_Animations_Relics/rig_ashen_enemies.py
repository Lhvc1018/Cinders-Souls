"""Generate provisional, editable skeletal assets from the supplied OBJ meshes."""
import bpy, math, sys
import numpy as np
from mathutils import Vector, Euler
from pathlib import Path

ROOT=Path(r'D:\67\Last67Seconds-Maps')
for kind in ('Hellhound','HorrorInquisitor'):
    if '--hound-only' in sys.argv and kind != 'Hellhound': continue
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions): bpy.data.actions.remove(action)
    folder=ROOT/'Assets/03_Models/Enemies'/kind
    bpy.ops.wm.obj_import(filepath=str(next(folder.glob('*.obj'))))
    mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.context.view_layer.objects.active=mesh
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.armature_add(); rig=bpy.context.object;rig.name=kind+'Rig'
    bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
    specs=[]
    def bone(name,head,tail,parent=None):
        b=rig.data.edit_bones.new(name);b.head=head;b.tail=tail
        if parent:b.parent=rig.data.edit_bones[parent]
        specs.append((name,Vector(head),Vector(tail)))
    if kind=='Hellhound':
        bone('root',(-.17,0,.32),(.02,0,.40));bone('chest',(.02,0,.40),(.18,0,.43),'root');bone('head',(.18,0,.43),(.36,0,.49),'chest')
        bone('tail',(-.17,0,.32),(-.42,0,.24),'root')
        for side,y in [('L',-.12),('R',.12)]:
            bone('front_'+side,(.12,y,.38),(.18,y,.19),'chest');bone('forefoot_'+side,(.18,y,.19),(.29,y,.035),'front_'+side)
            bone('back_'+side,(-.19,y,.33),(-.25,y,.15),'root');bone('hindfoot_'+side,(-.25,y,.15),(-.21,y,.035),'back_'+side)
    else:
        bone('root',(0,0,.46),(0,0,.59));bone('chest',(0,0,.59),(0,0,.75),'root');bone('head',(0,0,.75),(0,0,.97),'chest')
        for side,sgn in [('L',-1),('R',1)]:
            bone('upperarm_'+side,(sgn*.10,0,.73),(sgn*.20,0,.65),'chest')
            bone('forearm_'+side,(sgn*.20,0,.65),(sgn*.30,0,.61),'upperarm_'+side)
            bone('thigh_'+side,(sgn*.065,0,.46),(sgn*.07,0,.22),'root')
            bone('shin_'+side,(sgn*.07,0,.22),(sgn*.07,0,.025),'thigh_'+side)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Two nearest bones per vertex, smoothly blended. These weights are provisional.
    count=len(mesh.data.vertices);coords=np.empty(count*3,dtype=np.float32);mesh.data.vertices.foreach_get('co',coords);coords=coords.reshape((-1,3))
    distances=[]
    for name,a,b in specs:
        a=np.array(a,dtype=np.float32); b=np.array(b,dtype=np.float32);v=b-a;t=np.clip(((coords-a)@v)/(v@v),0,1)
        distances.append(np.linalg.norm(coords-(a+t[:,None]*v),axis=1))
    distances=np.stack(distances,axis=1)
    # The inquisitor's censer is fused to its hand; keep the whole prop attached.
    if kind=='HorrorInquisitor':
        prop=(coords[:,0]<-.17)&(coords[:,2]<.66)
        i=next(i for i,s in enumerate(specs) if s[0]=='forearm_L')
        distances[prop,:]+=1.;distances[prop,i]=.001
        cloak=(abs(coords[:,0])<.15)&(coords[:,2]>.20)&(coords[:,2]<.52)
        i=next(i for i,s in enumerate(specs) if s[0]=='root')
        distances[cloak,i]*=.35
    nearest=np.argpartition(distances,1,axis=1)[:,:2]
    d=np.take_along_axis(distances,nearest,axis=1);weights=1/(d+.012)**3;weights/=weights.sum(axis=1,keepdims=True)
    groups={name:mesh.vertex_groups.new(name=name) for name,_,_ in specs}
    for i in range(count):
        for j in range(2):
            if weights[i,j]>.015: groups[specs[nearest[i,j]][0]].add([i],float(weights[i,j]),'REPLACE')
    mesh.parent=rig;mod=mesh.modifiers.new('Rig deformation','ARMATURE');mod.object=rig
    rig.animation_data_create()
    def key(action_name,poses):
        action=bpy.data.actions.new(action_name);action.use_fake_user=True;rig.animation_data.action=action
        for frame, mapping in poses:
            for pb in rig.pose.bones:
                pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0);pb.location=(0,0,0)
                if pb.name in mapping:
                    angles=tuple(math.radians(v) for v in mapping[pb.name])
                    if kind=='Hellhound':
                        basis=pb.bone.matrix_local.to_quaternion()
                        pb.rotation_euler=(basis.inverted() @ Euler(angles).to_quaternion() @ basis).to_euler()
                    else: pb.rotation_euler=angles
                if kind=='Hellhound' and pb.name=='root':
                    lift = (.045*(1-math.cos((frame-1)/24*4*math.pi)) if action_name=='Chase' else .13*math.sin(math.pi*(frame-1)/24) if action_name=='Melee' else 0)
                    pb.location=pb.bone.matrix_local.to_3x3().inverted() @ Vector((0,0,lift))
                pb.keyframe_insert('location',frame=frame,group=pb.name)
                pb.keyframe_insert('rotation_euler',frame=frame,group=pb.name)
        return action
    empty={}
    key('Idle',[(1,empty),(13,{'chest':(2,0,0),'head':(-3,0,0)}),(25,empty)])
    if kind=='Hellhound':
        a={'front_L':(0,32,0),'front_R':(0,-32,0),'back_L':(0,-30,0),'back_R':(0,30,0),'forefoot_L':(0,-25,0),'forefoot_R':(0,12,0),'hindfoot_L':(0,12,0),'hindfoot_R':(0,-25,0),'head':(0,-4,0),'tail':(0,0,12)}
        b={n:tuple(-v for v in rot) for n,rot in a.items()}
        attack={'chest':(0,-14,0),'head':(0,22,0),'front_L':(0,-28,0),'front_R':(0,-28,0)}
        death={'root':(65,0,0),'head':(0,0,-14),'front_L':(0,-25,0),'back_R':(0,25,0)}
    else:
        a={'thigh_L':(20,0,0),'thigh_R':(-20,0,0),'upperarm_L':(-10,0,0),'upperarm_R':(10,0,0)}
        b={n:tuple(-v for v in rot) for n,rot in a.items()}
        attack={'chest':(0,0,-12),'upperarm_L':(0,-30,-22),'forearm_L':(0,35,0),'head':(0,0,10)}
        death={'root':(70,0,0),'head':(20,0,0),'upperarm_L':(0,0,-20),'upperarm_R':(0,0,20)}
    key('Chase',[(1,a),(7,empty),(13,b),(19,empty),(25,a)])
    key('Melee',[(1,empty),(9,attack),(15,{n:tuple(-v*.45 for v in rot) for n,rot in attack.items()}),(25,empty)])
    key('Hit',[(1,empty),(6,{'chest':(-12,0,0),'head':(10,0,0)}),(13,empty)])
    key('Death',[(1,empty),(20,death),(36,death)])
    rig.animation_data.action=bpy.data.actions['Idle']
    scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=25;scene.render.fps=24
    output=folder/(kind+'_rigged.fbx'); bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y')
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Docs'/('BORRADOR_'+kind+'_rig.blend')))
    print('EXPORTED',output, 'vertices',count,flush=True)

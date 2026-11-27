"""Generate provisional, editable skeletal assets from the supplied OBJ meshes."""
import bpy, math, sys
import numpy as np
from mathutils import Vector, Euler
from pathlib import Path

ROOT=Path(r'D:\67\Last67Seconds-Maps')
for kind in ('Hellhound','HorrorInquisitor'):
    if '--hound-only' in sys.argv and kind != 'Hellhound': continue
    if '--boss-only' in sys.argv and kind != 'HorrorInquisitor': continue
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions): bpy.data.actions.remove(action)
    folder=ROOT/'Assets/03_Models/Enemies'/kind
    bpy.ops.wm.obj_import(filepath=str(next(folder.glob('*.obj'))))
    mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.context.view_layer.objects.active=mesh
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    if kind=='Hellhound':
        mesh.rotation_euler.z=math.radians(45)
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.object.armature_add(); rig=bpy.context.object;rig.name=kind+'Rig'
    bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
    specs=[]
    def bone(name,head,tail,parent=None):
        b=rig.data.edit_bones.new(name);b.head=head;b.tail=tail
        if parent:b.parent=rig.data.edit_bones[parent]
        specs.append((name,Vector(head),Vector(tail)))
    if kind=='Hellhound':
        bone('root',(-.15,0,.35),(.035,0,.39));bone('chest',(.035,0,.39),(.23,0,.46),'root');bone('head',(.23,0,.46),(.45,.015,.46),'chest')
        bone('tail',(-.21,0,.34),(-.4,-.08,.25),'root');bone('tail_tip',(-.4,-.08,.25),(-.55,-.13,.26),'tail')
        bone('front_L',(.16,-.10,.40),(.08,-.13,.25),'chest');bone('forefoot_L',(.08,-.13,.25),(.05,-.19,.13),'front_L')
        bone('front_R',(.16,.075,.40),(.25,.09,.20),'chest');bone('forefoot_R',(.25,.09,.20),(.46,.13,.03),'front_R')
        bone('back_L',(-.16,-.12,.34),(-.07,-.20,.18),'root');bone('hindfoot_L',(-.07,-.20,.18),(-.15,-.26,.03),'back_L')
        bone('back_R',(-.15,.10,.34),(-.02,.15,.18),'root');bone('hindfoot_R',(-.02,.15,.18),(-.17,.26,.03),'back_R')
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
    if kind=='Hellhound':
        # Restrict limb influences to their anatomical side, preventing skin bridges
        # between feet and the skull ornaments fused into the torso.
        for i,(name,_,_) in enumerate(specs):
            if name.endswith('_L'): distances[:,i]+=np.maximum(0,coords[:,1]-.015)*6
            if name.endswith('_R'): distances[:,i]+=np.maximum(0,-coords[:,1]-.035)*6
            if name.startswith(('front_','forefoot_','back_','hindfoot_')):
                distances[:,i]+=np.maximum(0,coords[:,2]-.32)*4
            if name.startswith(('front_','forefoot_')):distances[:,i]+=np.maximum(0,-coords[:,0]-.025)*6
            if name.startswith(('back_','hindfoot_')):distances[:,i]+=np.maximum(0,coords[:,0]-.06)*6
            if name.startswith('tail'):distances[:,i]+=np.maximum(0,coords[:,0]+.25)*6
        head=(coords[:,0]>.30)&(coords[:,2]>.27)
        hi=next(i for i,s in enumerate(specs) if s[0]=='head')
        distances[head,:]+=2;distances[head,hi]=.002
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
    # Smooth along mesh topology, avoiding abrupt two-bone seams at shoulders.
    dense=np.zeros((count,len(specs)),dtype=np.float32)
    for j in range(2):dense[np.arange(count),nearest[:,j]]=weights[:,j]
    edges=np.array([tuple(e.vertices) for e in mesh.data.edges],dtype=np.int32)
    degree=np.zeros(count,dtype=np.float32)
    np.add.at(degree,edges[:,0],1);np.add.at(degree,edges[:,1],1)
    for _ in range(12):
        neighbors=np.zeros_like(dense)
        np.add.at(neighbors,edges[:,0],dense[edges[:,1]]);np.add.at(neighbors,edges[:,1],dense[edges[:,0]])
        dense=.4*dense+.6*neighbors/np.maximum(degree[:,None],1)
    if kind=='HorrorInquisitor':
        # The fused censer and chain are rigid equipment, not soft tissue.
        prop=(coords[:,0]<-.22)&(coords[:,2]<.82)
        index=next(i for i,s in enumerate(specs) if s[0]=='forearm_L')
        dense[prop,:]=0;dense[prop,index]=1
    nearest=np.argpartition(-dense,3,axis=1)[:,:4]
    weights=np.take_along_axis(dense,nearest,axis=1);weights/=np.maximum(weights.sum(axis=1,keepdims=True),.0001)
    groups={name:mesh.vertex_groups.new(name=name) for name,_,_ in specs}
    for i in range(count):
        for j in range(4):
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
                    basis=pb.bone.matrix_local.to_quaternion()
                    pb.rotation_euler=(basis.inverted() @ Euler(angles).to_quaternion() @ basis).to_euler()
                if pb.name=='root':
                    t=(frame-1)/24
                    if kind=='Hellhound':
                        lift = .018*(1-math.cos(t*4*math.pi)) if action_name=='Chase' else (-.025*math.sin(math.pi*t/.32) if t<.32 else .105*math.sin(math.pi*(t-.32)/.5) if t<.82 else 0) if action_name=='Melee' else -.23*min(1,t/.8) if action_name=='Death' else 0
                    else: lift=-.34*min(1,t/.9) if action_name=='Death' else .006*math.sin(t*4*math.pi) if action_name=='Chase' else 0
                    pb.location=pb.bone.matrix_local.to_3x3().inverted() @ Vector((0,0,lift))
                pb.keyframe_insert('location',frame=frame,group=pb.name)
                pb.keyframe_insert('rotation_euler',frame=frame,group=pb.name)
        return action
    empty={}
    key('Idle',[(1,empty),(25,{'chest':(0,-1.5,0),'head':(0,2,1),'tail':(0,0,4)}),(49,empty)])
    if kind=='Hellhound':
        a={'front_L':(0,22,0),'front_R':(0,-22,0),'back_L':(0,-20,0),'back_R':(0,20,0),'forefoot_L':(0,-24,0),'forefoot_R':(0,10,0),'hindfoot_L':(0,10,0),'hindfoot_R':(0,-24,0),'head':(0,-3,0),'tail':(0,0,8),'tail_tip':(0,0,6)}
        b={n:tuple(-v for v in rot) for n,rot in a.items()}
        attack={'chest':(0,7,0),'head':(0,12,0),'front_L':(0,-18,0),'front_R':(0,-18,0),'back_L':(0,20,0),'back_R':(0,20,0)}
        death={'root':(82,0,0),'head':(0,0,-14),'front_L':(0,-25,0),'back_R':(0,25,0)}
    else:
        a={'thigh_L':(20,0,0),'thigh_R':(-20,0,0),'shin_L':(-12,0,0),'shin_R':(10,0,0),'upperarm_L':(-8,0,0),'upperarm_R':(10,0,0),'chest':(0,0,3),'head':(0,0,-3)}
        b={n:tuple(-v for v in rot) for n,rot in a.items()}
        attack={'chest':(8,0,18),'upperarm_L':(0,-20,65),'forearm_L':(0,-15,15),'head':(0,0,-10),'upperarm_R':(0,0,-12)}
        death={'root':(87,0,0),'head':(10,0,0),'upperarm_L':(0,0,-20),'upperarm_R':(0,0,20),'thigh_L':(-15,0,0),'shin_R':(20,0,0)}
    key('Chase',[(f,{n:tuple(v*math.cos((f-1)/24*math.tau) for v in rot) for n,rot in a.items()}) for f in range(1,26,2)])
    key('Melee',[(1,empty),(8,{n:tuple(-v*.45 for v in rot) for n,rot in attack.items()}),(12,{'head':(0,-8,0),'front_L':(0,-25,0),'front_R':(0,-25,0)} if kind=='Hellhound' else attack),(17,attack),(21,{n:tuple(v*.35 for v in rot) for n,rot in attack.items()}),(25,empty)])
    key('Slam',[(1,empty),(12,{'chest':(-8,0,0),'upperarm_L':(0,85,0),'forearm_L':(0,25,0)}),(17,{'chest':(18,0,0),'upperarm_L':(0,-30,60),'forearm_L':(0,-20,0)}),(25,empty)])
    key('Hit',[(1,empty),(4,{'chest':(0,-7,0),'head':(0,-10,0)}),(9,{'chest':(0,3,0)}),(13,empty)])
    key('Stun',[(1,{'chest':(0,4,0),'head':(0,6,-4)}),(9,{'chest':(0,5,0),'head':(0,7,4)}),(17,{'chest':(0,4,0),'head':(0,6,-4)})])
    key('Death',[(1,empty),(20,death),(36,death)])
    # Bake ground correction from the deformed mesh, not the rest skeleton.
    # Positive clearance is retained for the attack hop; only penetration is fixed.
    for action in list(bpy.data.actions):
        rig.animation_data.action=action
        first,last=map(int,action.frame_range)
        for frame in range(first,last+1):
            bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
            evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());posed=evaluated.to_mesh()
            vertices=np.empty(len(posed.vertices)*3,dtype=np.float32);posed.vertices.foreach_get('co',vertices)
            minimum=float(vertices[2::3].min());evaluated.to_mesh_clear()
            if minimum<-.001:
                pb=rig.pose.bones['root'];pb.location+=pb.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-minimum))
                pb.keyframe_insert('location',frame=frame,group='root')
    rig.animation_data.action=bpy.data.actions['Idle']
    scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=25;scene.render.fps=24
    output=folder/(kind+'_rigged.fbx'); bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(output),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y')
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Docs'/('BORRADOR_'+kind+'_rig.blend')))
    print('EXPORTED',output, 'vertices',count,flush=True)

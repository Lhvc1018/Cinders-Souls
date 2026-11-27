import bpy,numpy as np
bpy.ops.wm.open_mainfile(filepath=r'D:/67/Last67Seconds-Maps/Docs/BORRADOR_HorrorInquisitor_rig.blend')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
rig.animation_data.action=bpy.data.actions['Melee'];bpy.context.scene.frame_set(17);bpy.context.view_layer.update()
posed=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
rest=np.array([v.co[:] for v in mesh.data.vertices]);after=np.array([v.co[:] for v in posed.vertices]);edges=np.array([e.vertices[:] for e in mesh.data.edges])
length=np.linalg.norm(rest[edges[:,0]]-rest[edges[:,1]],axis=1);current=np.linalg.norm(after[edges[:,0]]-after[edges[:,1]],axis=1)
for i in np.argsort(current-length)[-15:]:
 print('EDGE',round(float(current[i]-length[i]),3),'rest',rest[edges[i]].round(3).tolist(),flush=True)

exec(open('D:/67/Last67Seconds-Maps/AgentScripts/export_priest.py').read().split('parts=[]')[0])
for name in ['body','helmet','robe','mantle','stripes']:
 o=bpy.data.objects[name];t={}
 for v in o.data.vertices:
  for g in v.groups:
   if g.weight>.01:t[o.vertex_groups[g.group].name]=t.get(o.vertex_groups[g.group].name,0)+g.weight
 print(name,sorted(t.items(),key=lambda x:-x[1])[:25])

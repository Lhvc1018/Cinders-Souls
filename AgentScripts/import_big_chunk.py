from pathlib import Path
import re,shutil,json
src=Path('D:/67/MuckAnimationResearch/Export/ExportedProject/Assets');dst=Path('D:/67/Last67Seconds-Maps/Assets/Research/MuckNative');dst.mkdir(parents=True,exist_ok=True)
index={}
for m in src.rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(errors='replace'),re.M)
 if g:index[g[1]]=Path(str(m)[:-5])
existing={}
for m in Path('D:/67/Last67Seconds-Maps/Assets/Research/MuckAnimations').rglob('*.meta'):
 g=re.search(r'^guid: (\w+)',m.read_text(errors='replace'),re.M)
 if g:existing[g[1]]=m
allowed={1,4,23,33,95,137}
def clean(text):
 docs=re.split(r'(?=^--- !u!)',text,flags=re.M);kept=[];ids=set()
 for doc in docs:
  h=re.match(r'--- !u!(\d+) &(\d+)',doc)
  if not h:kept.append(doc)
  elif int(h[1]) in allowed:kept.append(doc);ids.add(h[2])
 text=''.join(kept)
 return re.sub(r'^  - component: \{fileID: (\d+)\}\n',lambda m:m[0] if m[1] in ids else '',text,flags=re.M)
pending=[src/'GameObject'/f'{n}.prefab' for n in ['StoneTitan']];done=set()
while pending:
 p=pending.pop()
 if p in done or (dst/p.relative_to(src)).is_file():continue
 done.add(p)
 if p.suffix in ('.cs','.shader','.dll','.unity'):continue
 text=None
 if p.suffix in ('.prefab','.asset','.mat','.controller','.anim'):
  try:text=p.read_text(encoding='utf-8')
  except UnicodeDecodeError:pass
 if text and p.suffix=='.prefab':text=clean(text)
 if text:
  for g in set(re.findall(r'guid: (\w+)',text)):
   if g in index and g not in existing:pending.append(index[g])
 target=dst/p.relative_to(src);target.parent.mkdir(parents=True,exist_ok=True)
 if text is not None:target.write_text(text,encoding='utf-8')
 else:shutil.copy2(p,target)
 meta=Path(str(p)+'.meta')
 if meta.exists():shutil.copy2(meta,Path(str(target)+'.meta'))
print('Imported native dependency assets:',len(done));print('\n'.join(str(p) for p in done if p.suffix=='.prefab'))



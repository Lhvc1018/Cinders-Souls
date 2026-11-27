import io,sys
root=sys.argv[1]
def patch(p,pairs):
    path=root+'/'+p;s=io.open(path,encoding='utf-8-sig').read()
    for a,b in pairs:
        n=s.count(a);assert n==1,(p,n,a[:70]);s=s.replace(a,b)
    io.open(path,'w',encoding='utf-8').write(s)
patch('UI/MainMenuUI.cs',[
 ('()=>PreviewClass(index),false);','()=>ClassPressed(index),false);'),
 ('Button(classes,"DESCENDER",1200,810,400,55,()=>{if(choosingEndless)RunNavigation.StartEndless(classKinds[selectedClass]);else if(CheatsUnlocked)RunNavigation.StartExpeditionTest(classKinds[selectedClass],CheatRound);else RunNavigation.StartGame(classKinds[selectedClass],1);},true);',
  'Button(classes,"DESCENDER",1200,810,400,55,()=>AskConfirm(),true);'),
 ('            BuildSaveMenus(root.transform);RefreshOptions();','            BuildSaveMenus(root.transform);BuildClassConfirm(root.transform);TuneMenuNavigation();RefreshOptions();'),
 ('            bool back=Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Back);','            bool back=Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Back);\n            if(confirmPanel&&confirmPanel.gameObject.activeSelf){if(back)CloseConfirm();return;}\n            if(classHint&&classes.gameObject.activeSelf&&Time.frameCount%15==0)RefreshClassHint();'),
 ('        void ShowClasses(bool show){home.gameObject.SetActive(!show);','        void ShowClasses(bool show){armedClass=-1;RefreshClassHint();if(show)classShowcase.Preload(System.Array.ConvertAll(new[]{"Warrior","Archer","Tank","Mage","Shinobi","Berserker"},n=>"Class_"+n));home.gameObject.SetActive(!show);'),
 ('            classShowcase.SetModel(new[]{"Class_Warrior","Class_Archer","Class_Tank","Class_Mage","Class_Shinobi","Class_Berserker"}[index]);',
  '            classShowcase.SetModel(new[]{"Class_Warrior","Class_Archer","Class_Tank","Class_Mage","Class_Shinobi","Class_Berserker"}[index]);\n            RefreshClassHint();'),
])
patch('UI/MenuModelShowcase.cs',[
 ('        float nextChange;','        float nextChange;\n        readonly System.Collections.Generic.Dictionary<string, GameObject> cache = new System.Collections.Generic.Dictionary<string, GameObject>();\n        Coroutine preloading;\n        /// Instantiates the portraits ahead of time (one per frame) so browsing classes never hitches.\n        public void Preload(string[] resources){if(stage&&preloading==null&&isActiveAndEnabled)preloading=StartCoroutine(PreloadRoutine(resources));}\n        System.Collections.IEnumerator PreloadRoutine(string[] resources){foreach(var r in resources){if(!cache.ContainsKey(r)){var m=Spawn(r);if(m&&m!=model)m.SetActive(false);}yield return null;}}'),
 ('''            if (model && currentResource == resource) return;
            if (model) Destroy(model);
            var prefab''','''            if (model && currentResource == resource) return;
            if (model) model.SetActive(false);
            model = Spawn(resource); if (!model) return;
            model.SetActive(true); currentResource = resource;
            model.transform.localPosition = Vector3.zero;
        }
        GameObject Spawn(string resource)
        {
            if (cache.TryGetValue(resource, out var cached) && cached) return cached;
            var prefab'''),
 ('''            if (!prefab) { Debug.LogWarning("Missing Cinders menu model: " + resource); return; }
            model = Instantiate(prefab, stage.transform, false);
            currentResource = resource;
            model.transform.localPosition = Vector3.zero;
            foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            if (vigia) { Codice.Presentation.VigiaLook.Dress(model); foreach (var light in model.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << 30; }''',
  '''            if (!prefab) { Debug.LogWarning("Missing Cinders menu model: " + resource); return null; }
            var spawned = Instantiate(prefab, stage.transform, false);
            spawned.transform.localPosition = Vector3.zero;
            foreach (var child in spawned.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            if (vigia) { Codice.Presentation.VigiaLook.Dress(spawned); foreach (var light in spawned.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << 30; }
            cache[resource] = spawned; return spawned;'''),
])
print('ok')

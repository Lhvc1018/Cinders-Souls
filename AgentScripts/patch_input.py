import sys, io
root = sys.argv[1]
def patch(p, pairs):
    path = root + '/' + p
    s = io.open(path, encoding='utf-8-sig').read()
    for a, b in pairs:
        n = s.count(a)
        assert n == 1, (p, n, a[:80])
        s = s.replace(a, b)
    io.open(path, 'w', encoding='utf-8').write(s)
GI = 'Codice.Mechanics.GameInput'
GA = 'Codice.Mechanics.GameAction'

patch('Characters/PlayerCharacter.cs', [
 ("            if (Cursor.lockState != CursorLockMode.Locked)\n            { if (mouse.leftButton.wasPressedThisFrame) CaptureCursor(true); return; }",
  "            if (Cursor.lockState != CursorLockMode.Locked)\n            { if (mouse.leftButton.wasPressedThisFrame || " + GI + ".UsingGamepad) CaptureCursor(true); return; }"),
 ("            if (keys.escapeKey.wasPressedThisFrame) { CaptureCursor(false); return; }",
  "            if (keys.escapeKey.wasPressedThisFrame && !" + GI + ".UsingGamepad) { CaptureCursor(false); return; }"),
 ("            if (keys.fKey.wasPressedThisFrame) GetComponent<Codice.Mechanics.RunRelics>().Activate(0);\n            if (keys.gKey.wasPressedThisFrame) GetComponent<Codice.Mechanics.RunRelics>().Activate(1);\n            var delta = mouse.delta.ReadValue() * mouseSensitivity;",
  "            if (" + GI + ".Pressed(" + GA + ".Relic1)) GetComponent<Codice.Mechanics.RunRelics>().Activate(0);\n            if (" + GI + ".Pressed(" + GA + ".Relic2)) GetComponent<Codice.Mechanics.RunRelics>().Activate(1);\n            var delta = " + GI + ".Look(mouseSensitivity);"),
 ("            if(mouse.middleButton.wasPressedThisFrame)lockOn.Toggle();",
  "            if(" + GI + ".Pressed(" + GA + ".LockOn))lockOn.Toggle();"),
 ("            Vector2 input = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));\n            Vector3 direction = transform.TransformDirection(new Vector3(input.x, 0, input.y).normalized);\n            if (keys.leftShiftKey.wasPressedThisFrame) HandleDodgePress(direction);\n            if (keys.spaceKey.wasPressedThisFrame) jumpQueuedUntil = Time.time + .12f;\n            SprintRequested=keys.leftCtrlKey.isPressed;\n            SimulateMotor(direction, keys.spaceKey.isPressed, Time.deltaTime);",
  "            Vector2 input = " + GI + ".Move();\n            Vector3 direction = transform.TransformDirection(new Vector3(input.x, 0, input.y));\n            if (" + GI + ".Pressed(" + GA + ".Dodge)) HandleDodgePress(direction);\n            if (" + GI + ".Pressed(" + GA + ".Jump)) jumpQueuedUntil = Time.time + .12f;\n            SprintRequested=" + GI + ".Held(" + GA + ".Sprint);\n            SimulateMotor(direction, " + GI + ".Held(" + GA + ".Jump), Time.deltaTime);"),
 ("                if(spell>=0)state.SelectSpell(spell);",
  "                if(spell<0&&" + GI + ".PadPressed(" + GA + ".NextSpell))spell=((int)state.SelectedSpell+1)%4;\n                if(spell>=0)state.SelectSpell(spell);"),
 ("            if (keys.tabKey.wasPressedThisFrame || mouse.scroll.ReadValue().y != 0) state.SelectWeapon(1 - state.ActiveSlot);\n            if (keys.eKey.wasPressedThisFrame) TryInteract();\n            if (!ControlsBlocked && keys.qKey.wasPressedThisFrame) SpecialRequested?.Invoke();",
  "            if (" + GI + ".Pressed(" + GA + ".SwapWeapon) || " + GI + ".WeaponScroll() != 0) state.SelectWeapon(1 - state.ActiveSlot);\n            if (" + GI + ".Pressed(" + GA + ".Interact)) TryInteract();\n            if (!ControlsBlocked && " + GI + ".Pressed(" + GA + ".Special)) SpecialRequested?.Invoke();"),
 ("                if (keys.escapeKey.wasPressedThisFrame || keys.eKey.wasPressedThisFrame) merchant.CloseShop();\n                else if (keys.enterKey.wasPressedThisFrame) merchant.ContinueRound();",
  "                if (keys.escapeKey.wasPressedThisFrame || " + GI + ".Pressed(" + GA + ".Interact) || " + GI + ".PadPressed(" + GA + ".Back)) merchant.CloseShop();\n                else if (keys.enterKey.wasPressedThisFrame) merchant.ContinueRound();"),
])
patch('Characters/PlayerCombat.cs', [
 ("Cursor.lockState == CursorLockMode.Locked && Mouse.current != null && Mouse.current.rightButton.isPressed);",
  "Cursor.lockState == CursorLockMode.Locked && " + GI + ".Held(" + GA + ".Block));"),
 ("Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) TryReload();",
  "(" + GI + ".KeyPressed(" + GA + ".Reload) || " + GI + ".PadPressed(" + GA + ".Reload) && weapon.Ranged && weapon.kind != WeaponKind.Staff)) TryReload();"),
 ("Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0)",
  GI + ".Pressed(" + GA + ".Attack) && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0)"),
])
patch('Characters/RunSpellbook.cs', [
 ("var k=Keyboard.current;if(k==null)return;if(k.cKey.wasPressedThisFrame&&Known.Count>0)Selected=(Selected+1)%Known.Count;if(k.tKey.wasPressedThisFrame)Cast();",
  "var aw=GetComponent<PlayerCharacter>()?.State.ActiveWeapon;if((" + GI + ".KeyPressed(" + GA + ".NextSpell)||" + GI + ".PadPressed(" + GA + ".NextSpell)&&!(aw!=null&&aw.Ranged))&&Known.Count>0)Selected=(Selected+1)%Known.Count;if(" + GI + ".Pressed(" + GA + ".CastSpell))Cast();"),
])
patch('Expedition/DestinyEncounter.cs', [
 ("Keyboard.current==null||!Keyboard.current.eKey.wasPressedThisFrame)return;", "!" + GI + ".Pressed(" + GA + ".Interact))return;"),
])
patch('Mechanics/EstusFlask.cs', [
 ("Keyboard.current!=null&&Keyboard.current.hKey.wasPressedThisFrame)TryDrink();", GI + ".Pressed(" + GA + ".Flask))TryDrink();"),
])
patch('Mechanics/RunDirector.cs', [
 ("(Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)) merchant.ContinueRound();", GI + ".KeyPressed(" + GA + ".Confirm)) merchant.ContinueRound();"),
 ("Phase == RunPhase.Defeated && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RestartRun();", "Phase == RunPhase.Defeated && (" + GI + ".KeyPressed(" + GA + ".Restart) || " + GI + ".PadPressed(" + GA + ".Restart))) RestartRun();"),
])
patch('Presentation/HumanPreviewCamera.cs', [
 ("Keyboard.current!=null&&Keyboard.current.vKey.wasPressedThisFrame&&", GI + ".Pressed(" + GA + ".Camera)&&"),
])
patch('UI/RunHUD.cs', [
 ("if(keys!=null&&(keys.escapeKey.wasPressedThisFrame||keys.iKey.wasPressedThisFrame)&&Player.State.IsAlive&&!ShopVisible&&!ResultsVisible)",
  "if((" + GI + ".Pressed(" + GA + ".Pause)||Paused&&" + GI + ".PadPressed(" + GA + ".Back)&&!RelicSelectionVisible&&!WeaponSelectionVisible)&&Player.State.IsAlive&&!ShopVisible&&!ResultsVisible)"),
])
patch('Prologue/PrologueDirector.cs', [
 ("if(keys != null && (keys.eKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame)) AdvanceDialogue();",
  "if(" + GI + ".Pressed(" + GA + ".Interact) || " + GI + ".Pressed(" + GA + ".Confirm) || " + GI + ".KeyPressed(" + GA + ".Jump)) AdvanceDialogue();"),
 ("if(keys != null && keys.escapeKey.wasPressedThisFrame) { SetPaused(!pause.activeSelf); return; }",
  "if(" + GI + ".Pressed(" + GA + ".Pause) || pause.activeSelf && " + GI + ".PadPressed(" + GA + ".Back)) { SetPaused(!pause.activeSelf); return; }"),
 ('prompt.text = nearNpc ? "[ E ]  HABLAR CON EL VIGÍA" : nearExit ? "[ E ]  CRUZAR EL UMBRAL" : "WASD  Moverte     MAYÚS  Esquivar     ESC  Pausa";',
  'prompt.text = ' + GI + '.Prompt(nearNpc ? "[E]  HABLAR CON EL VIGÍA" : nearExit ? "[E]  CRUZAR EL UMBRAL" : "[WASD]  Moverte     [Mayús]  Esquivar     [Esc]  Pausa");'),
 ("if(keys != null && keys.eKey.wasPressedThisFrame) { if(nearNpc)OpenDialogue(); else if(nearExit)EnterThreshold(); }",
  "if(" + GI + ".Pressed(" + GA + ".Interact)) { if(nearNpc)OpenDialogue(); else if(nearExit)EnterThreshold(); }"),
 ('"No dudes tú. Muévete con WASD, mira con el ratón y esquiva con MAYÚS. El calabozo cambia de forma, pero castiga igual.",',
  '"No dudes tú. El calabozo cambia de forma, pero castiga igual.",'),
])
patch('UI/MainMenuUI.cs', [
 ("if(cheats&&cheats.gameObject.activeSelf&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){ShowCheats(false);return;}",
  "bool back=" + GI + ".Pressed(" + GA + ".Back);\n            if(controlsSheet&&controlsSheet.gameObject.activeSelf){if(back)ShowControls(false);return;}\n            if(slotsPanel&&slotsPanel.gameObject.activeSelf){if(back)ShowSlots(false);return;}\n            if(cheats&&cheats.gameObject.activeSelf&&back){ShowCheats(false);return;}"),
 ("if(classes && classes.gameObject.activeSelf && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)ShowClasses(false);",
  "if(classes && classes.gameObject.activeSelf && back)ShowClasses(false);"),
 ("if(options && options.gameObject.activeSelf && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)ShowOptions(false);",
  "if(options && options.gameObject.activeSelf && back)ShowOptions(false);\n            if(footer)footer.text=" + GI + ".UsingGamepad?$\"{" + GI + ".PadLabel(" + GA + ".Move)}  Seleccionar      {" + GI + ".PadLabel(" + GA + ".Confirm)}  Confirmar      {" + GI + ".PadLabel(" + GA + ".Back)}  Volver\":\"Flechas  Seleccionar      Enter  Confirmar      Esc  Volver\";"),
])
patch('Multiplayer/LanSession.cs', [
 ("if(allowed){var look=mouse.delta.ReadValue()*p.mouseSensitivity;", "if(!allowed&&" + GI + ".UsingGamepad&&!shopping&&!(RunHUD.Instance&&RunHUD.Instance.Paused)){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}\n            if(allowed){var look=" + GI + ".Look(p.mouseSensitivity);"),
 ("msg.move=allowed?new Vector2((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0)).normalized:Vector2.zero;",
  "msg.move=allowed?" + GI + ".Move():Vector2.zero;"),
 ("msg.flask=allowed&&keyboard.hKey.wasPressedThisFrame;msg.relic=allowed&&keyboard.fKey.wasPressedThisFrame?0:allowed&&keyboard.gKey.wasPressedThisFrame?1:-1;",
  "msg.flask=allowed&&" + GI + ".Pressed(" + GA + ".Flask);msg.relic=allowed&&" + GI + ".Pressed(" + GA + ".Relic1)?0:allowed&&" + GI + ".Pressed(" + GA + ".Relic2)?1:-1;"),
 ("msg.attack=allowed&&mouse.leftButton.isPressed;msg.block=allowed&&mouse.rightButton.isPressed;",
  "msg.attack=allowed&&" + GI + ".Held(" + GA + ".Attack);msg.block=allowed&&" + GI + ".Held(" + GA + ".Block);"),
 ("msg.jump=allowed&&keyboard.spaceKey.wasPressedThisFrame;msg.dash=allowed&&keyboard.leftShiftKey.wasPressedThisFrame;msg.special=allowed&&keyboard.qKey.wasPressedThisFrame;msg.reload=allowed&&keyboard.rKey.wasPressedThisFrame;",
  "msg.jump=allowed&&" + GI + ".Pressed(" + GA + ".Jump);msg.dash=allowed&&" + GI + ".Pressed(" + GA + ".Dodge);msg.special=allowed&&" + GI + ".Pressed(" + GA + ".Special);msg.reload=allowed&&" + GI + ".Pressed(" + GA + ".Reload);msg.interact=allowed&&" + GI + ".Pressed(" + GA + ".Interact);"),
 ("msg.interact=allowed&&keyboard.eKey.wasPressedThisFrame;",""),
 ("if(keyboard.tabKey.wasPressedThisFrame||mouse.scroll.ReadValue().y!=0)msg.slot=1-p.State.ActiveSlot;","if(" + GI + ".Pressed(" + GA + ".SwapWeapon)||" + GI + ".WeaponScroll()!=0)msg.slot=1-p.State.ActiveSlot;"),
 ("msg.closeShop=keyboard.eKey.wasPressedThisFrame||keyboard.escapeKey.wasPressedThisFrame;","msg.closeShop=" + GI + ".Pressed(" + GA + ".Interact)||keyboard.escapeKey.wasPressedThisFrame||" + GI + ".PadPressed(" + GA + ".Back);"),
])
print('patched')
patch('Characters/ShinobiGuard.cs', [("Cursor.lockState==CursorLockMode.Locked&&Mouse.current!=null&&Mouse.current.rightButton.isPressed);", "Cursor.lockState==CursorLockMode.Locked&&" + GI + ".Held(" + GA + ".Block));")])
patch('UI/MainMenuUI.cs', [("            var foot=Label(home,\"Footer\"", "            var foot=footer=Label(home,\"Footer\"")])
print('patched 2')

patch('UI/MainMenuUI.cs', [
 ("            first.Select();RefreshOptions();\n            if(Application.isPlaying){ApplyAuthoredLayout(root.transform);FadeIn(home);}",
  "            BuildSaveMenus(root.transform);RefreshOptions();\n            if(Application.isPlaying){ApplyAuthoredLayout(root.transform);FadeIn(home);}\n            RefreshHome();(continueButton.gameObject.activeSelf?continueButton:first).Select();"),
 ('void RefreshOptions(){values.text=',
  'void RefreshOptions(){values.text=$"VOLUMEN GENERAL                         {AudioListener.volume:P0}\nSENSIBILIDAD DEL RATÓN                 {PlayerPrefs.GetFloat("Codice.Sensitivity",.09f):0.00}\nSENSIBILIDAD DEL MANDO                 {Codice.Mechanics.GameInput.PadSensitivity:0.0}x{(Codice.Mechanics.GameInput.InvertY?"  ·  EJE Y INVERTIDO":"")}\n<size=18><color=#A69F8B>{DeviceLine()}</color></size>";}\n        void RefreshOptionsLegacy(){values.text='),
])
print('patched 3')
SS='Codice.Mechanics.SaveSlots'
patch('UI/RunNavigation.cs', [
 ("        public static Codice.Characters.PlayerClass SelectedClass { get; private set; }",
  "        public static Codice.Characters.PlayerClass SelectedClass { get; private set; }\n        static SaveSlots.SaveData resume;\n        /// Picked up once by the next RunDirector: the floor checkpoint of the active save slot.\n        public static SaveSlots.SaveData TakeResume(){var r=resume;resume=null;return r;}\n        public static void ContinueSaved()\n        {\n            if(Loading||!SaveSlots.CanContinue||Codice.Multiplayer.LanSession.Active)return;\n            var d=SaveSlots.Current;resume=d;SelectedClass=(Codice.Characters.PlayerClass)d.runClass;\n            RequestedExpeditionFloor=Mathf.Clamp(d.runFloor,1,RunDirector.FinalExpeditionFloor);EndlessMode=false;PlayerPrefs.DeleteKey(\"Codice.TestRound\");\n            Load(\"Descenso\");\n        }"),
 ("static void Reset(){loading=null;", "static void Reset(){resume=null;loading=null;"),
 ("        { if(Loading)return;RequestedExpeditionFloor=1;EndlessMode=false;SelectedClass=selected;",
  "        { if(Loading)return;SaveSlots.EnsureActive();RequestedExpeditionFloor=1;EndlessMode=false;SelectedClass=selected;"),
])
patch('Mechanics/RunDirector.cs', [
 ("            Instance = this; FloorNumber=Codice.UI.RunNavigation.RequestedExpeditionFloor; DontDestroyOnLoad(gameObject);",
  "            Instance = this; FloorNumber=Codice.UI.RunNavigation.RequestedExpeditionFloor; DontDestroyOnLoad(gameObject);\n            var resume=Codice.UI.RunNavigation.TakeResume();\n            if(resume!=null)\n            {\n                snapshot=resume.runState;carriedSpells=System.Array.ConvertAll(resume.runSpells??new int[0],s=>(RunSpell)s);carriedSpellSelection=resume.runSpellSelection;\n                DestinyOrbs=resume.runOrbs;LastBellSeal=resume.runBell;WatcherEncounterComplete=resume.runWatcher;HiredPilgrim=resume.runPilgrim;BondedWisp=resume.runWisp;\n                Elapsed=resume.runElapsed;TotalKills=resume.runKills;Debug.Log($\"[Saves] Continuing descent on floor {FloorNumber}\");\n            }"),
 ("            if(context.Expedition){Level=1;transition=false;checkpoint=null;context.Expedition.Begin(this);return;}",
  "            if(context.Expedition){Level=1;transition=false;checkpoint=null;context.Expedition.Begin(this);if(!Codice.UI.MainMenuUI.CheatsUnlocked)SaveSlots.Checkpoint(this);return;}"),
 ("            StopAllCoroutines();if(Endless)Endless.Halt(); SetPhase(RunPhase.Defeated);",
  "            StopAllCoroutines();if(Endless)Endless.Halt(); SetPhase(RunPhase.Defeated);\n            if(Expedition&&!Codice.UI.MainMenuUI.CheatsUnlocked)SaveSlots.EndRun(false);"),
])
patch('Mechanics/RunDirector.Ending.cs', [
 ("SetExpeditionPhase(RunPhase.Victory,EndingTitle);return true;", "if(!Codice.UI.MainMenuUI.CheatsUnlocked)SaveSlots.EndRun(true);SetExpeditionPhase(RunPhase.Victory,EndingTitle);return true;"),
])
patch('UI/RunHUD.Inventory.cs', [
 ('   UIWidgets.Label(pausePanel,"Inventory help",592,786,500,48,18).text="[V] Primera / tercera persona · Selecciona un objeto.";',
  '   UIWidgets.Button(pausePanel,"CONTROLES",592,780,480,48,()=>{controlsSheet.gameObject.SetActive(true);controlsSheet.GetComponentInChildren<Button>()?.Select();});\n   controlsSheet=ControlsSheet.Build(transform,()=>{controlsSheet.gameObject.SetActive(false);pausePanel.GetComponentInChildren<Button>()?.Select();});controlsSheet.gameObject.SetActive(false);'),
])
patch('UI/RunHUD.cs', [
 ("            if((" + GI + ".Pressed(" + GA + ".Pause)",
  "            if(controlsSheet&&controlsSheet.gameObject.activeSelf){if(!Paused)controlsSheet.gameObject.SetActive(false);else if(" + GI + ".Pressed(" + GA + ".Pause)||" + GI + ".PadPressed(" + GA + ".Back)){escapeFrame=Time.frameCount;controlsSheet.gameObject.SetActive(false);pausePanel.GetComponentInChildren<Button>()?.Select();}}\n            else if((" + GI + ".Pressed(" + GA + ".Pause)"),
 ("        void Died(){SetPaused(false);}", "        ControlsSheet controlsSheet;\n        void Died(){SetPaused(false);}"),
])
print('patched 4')
patch('Prologue/VigiaLessons.cs', [
 ("            foreach (var l in Text.Keys) if (Available(l) && !Heard(l))",
  "            // Like any game: the Vigía reads what is in your hands and teaches those controls.\n            string hands = Codice.Mechanics.GameInput.UsingGamepad ? \"pad\" : \"keys\";\n            if (first || PlayerPrefs.GetString(DeviceKey, \"\") != hands) { lines.Add(ControlsLesson(first)); PlayerPrefs.SetString(DeviceKey, hands); PlayerPrefs.Save(); }\n            foreach (var l in Text.Keys) if (Available(l) && !Heard(l))"),
 ("        public static void MarkHeard(IEnumerable<Lesson> lessons)",
  "        const string DeviceKey = \"Cinders.Vigia.Device\";\n        public static string ControlsLesson(bool first)\n        {\n            string L(Codice.Mechanics.GameAction a) => \"<color=#E7C46A>\" + Codice.Mechanics.GameInput.Label(a) + \"</color>\";\n            if (Codice.Mechanics.GameInput.UsingGamepad)\n                return (first ? \"Veo un mando en tus manos\" : \"Ahora empuñas un mando\") + $\". {L(Codice.Mechanics.GameAction.Move)} te mueve y {L(Codice.Mechanics.GameAction.Look)} gira la mirada. {L(Codice.Mechanics.GameAction.Attack)} golpea, {L(Codice.Mechanics.GameAction.Block)} bloquea —justo antes del impacto es un parry—, {L(Codice.Mechanics.GameAction.Dodge)} esquiva y {L(Codice.Mechanics.GameAction.Jump)} salta. {L(Codice.Mechanics.GameAction.Interact)} toma lo que encuentres, {L(Codice.Mechanics.GameAction.Flask)} bebe del frasco y {L(Codice.Mechanics.GameAction.Special)} despierta el arte de tu arma. {L(Codice.Mechanics.GameAction.Pause)} abre tu inventario.\";\n            return (first ? \"Tus manos están sobre las teclas\" : \"Volviste a las teclas\") + $\". {L(Codice.Mechanics.GameAction.Move)} te mueve y el ratón gira la mirada. {L(Codice.Mechanics.GameAction.Attack)} golpea, {L(Codice.Mechanics.GameAction.Block)} bloquea —justo antes del impacto es un parry—, {L(Codice.Mechanics.GameAction.Dodge)} esquiva y {L(Codice.Mechanics.GameAction.Jump)} salta. {L(Codice.Mechanics.GameAction.Interact)} toma lo que encuentres, {L(Codice.Mechanics.GameAction.Flask)} bebe del frasco y {L(Codice.Mechanics.GameAction.Special)} despierta el arte de tu arma. Si un día empuñas un mando, lo notaré.\";\n        }\n        public static void MarkHeard(IEnumerable<Lesson> lessons)"),
])
print('patched 5')

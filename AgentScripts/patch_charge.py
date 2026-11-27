import io,sys
root=sys.argv[1]
def patch(p,pairs):
    path=root+'/'+p;s=io.open(path,encoding='utf-8-sig').read()
    for a,b in pairs:
        n=s.count(a);assert n==1,(p,n,a[:80]);s=s.replace(a,b)
    io.open(path,'w',encoding='utf-8').write(s)
patch('Characters/PlayerCombat.cs',[
 ('        void Awake() { player = GetComponent<PlayerCharacter>(); }','        void Awake() { player = GetComponent<PlayerCharacter>(); if (!GetComponent<ChargedAttack>()) gameObject.AddComponent<ChargedAttack>(); }'),
 ('            if((GetComponent<RunSpellbook>()?.Casting??false)||(GetComponent<StaffSpells>()?.Casting??false))return false;','            if((GetComponent<RunSpellbook>()?.Casting??false)||(GetComponent<StaffSpells>()?.Casting??false)||(GetComponent<ChargedAttack>()?.Charging??false))return false;'),
])
patch('Characters/PlayerCombat.Combo.cs',[
 ('   GetComponent<ShinobiGuard>()?.SetGuard(false);','   GetComponent<ShinobiGuard>()?.SetGuard(false);\n   GetComponent<ChargedAttack>()?.Cancel();'),
])
patch('Prologue/PrologueDirector.cs',[
 ('    public sealed class PrologueDirector : MonoBehaviour','    public sealed partial class PrologueDirector : MonoBehaviour'),
 ('label.transform.localRotation = Quaternion.Euler(0,180,0);','label.transform.localRotation = Quaternion.identity;'),
 ('            ExitPosition = World(Layout.Exit) + Vector3.forward * 3;','            ExitPosition = World(Layout.Exit) + Vector3.forward * 1;'),
 ('            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;','            BuildPaths(root);\n            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;'),
 ('            if(leaving || !HasSpoken || DialogueOpen || !IntroFinished || Vector3.Distance(player.transform.position,ExitPosition)>3.2f) return;','            if(leaving || !HasSpoken || DialogueOpen || !IntroFinished || !NearAnyExit()) return;'),
 ('            bool nearExit = Vector3.Distance(player.transform.position,ExitPosition)<3.2f;','            bool nearExit = HasSpoken && NearAnyExit();\n            TickTrial();'),
 ('objective.text = "Encuentra el umbral rojo · Sigue las antorchas\\n<size=18>" + VigiaLessons.Unlocks() + "</size>";','OpenPaths();'),
 ('            speech = VigiaLessons.Build(player.State.Class, intro, out freshLessons);','            speech = VigiaLessons.Build(player.State.Class, intro, out freshLessons);\n            speech.Add(PathsLine());'),
])
print('ok')

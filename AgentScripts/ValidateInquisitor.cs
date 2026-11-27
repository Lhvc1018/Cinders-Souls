using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Codice.Bosses;
using Codice.Characters;
using Codice.Expedition;
using Codice.UI;
public static class ValidateInquisitor
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Field(BossCharacter b,string field,object value)=>typeof(BossCharacter).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,value);
    static void Property(BossCharacter b,string field,object value)=>typeof(BossCharacter).GetProperty(field).SetValue(b,value);
    static async Task Until(Func<bool> condition,string message,int ms=4000)
    {for(int i=0;i<ms/40;i++){if(condition())return;await Task.Delay(40);}throw new Exception(message);}
    static void Move(PlayerCharacter p,Vector3 position){var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=position+Vector3.up*.1f;c.enabled=true;c.Move(Vector3.down*.3f);}
    public static async Task<string> Preview()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var boss=floor.context.arena.ActiveBoss;var p=floor.context.player;
        p.enabled=false;RunHUD.Instance.SetPaused(false);Time.timeScale=1;boss.SetPaused(false);boss.GetComponent<NavMeshAgent>().Warp(floor.Center(8));
        Move(p,floor.Center(8)+Vector3.forward*3.3f);p.State.Initialize(PlayerClass.Warrior);p.view.transform.LookAt(boss.transform.position+Vector3.up*1.3f);
        Field(boss,"sequence",1);Field(boss,"attackTimer",-.1f);Property(boss,"State",BossState.Idle);
        await Until(()=>boss.State==BossState.Windup,"No attack preview");await Task.Delay(800);Time.timeScale=0;
        return "Actual slam windup paused for screenshot.";
    }
    public static async Task<string> ImpactPreview(){Time.timeScale=1;await Task.Delay(1300);Time.timeScale=0;return "Actual slam impact and toxic blood paused for screenshot.";}
    public static async Task<string> Combat()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var boss=floor.context.arena.ActiveBoss;var p=floor.context.player;
        Check(boss&&boss.bloodInquisitor,"Stage boss room first");p.enabled=false;RunHUD.Instance.SetPaused(false);Time.timeScale=1;
        var report=new System.Text.StringBuilder();
        async Task Setup(int sequence,float distance,float health=1)
        {
            boss.ApplyStun(.01f);var previousPoison=p.GetComponent<InquisitorPoison>();if(previousPoison)UnityEngine.Object.Destroy(previousPoison);await Task.Delay(80);boss.SetPaused(false);boss.GetComponent<NavMeshAgent>().Warp(floor.Center(8));boss.transform.rotation=Quaternion.identity;
            Move(p,floor.Center(8)+Vector3.forward*distance);p.State.Initialize(PlayerClass.Warrior);
            Property(boss,"Health",boss.MaxHealth*health);Property(boss,"State",BossState.Idle);Field(boss,"stunnedUntil",0f);Field(boss,"sequence",sequence);Field(boss,"attackTimer",-.1f);
        }
        foreach(var attack in new[]{(0,BossAttack.Sweep,28f),(1,BossAttack.Slam,38f)})
        {
            await Setup(attack.Item1,3);float before=p.State.Health+p.State.Armor;
            await Until(()=>boss.State==BossState.Windup,"Missing telegraph");Check(boss.CurrentAttack==attack.Item2,"Incorrect melee selection");
            await Task.Delay(200);Check(p.State.Health+p.State.Armor==before,"Damage before impact");
            await Until(()=>boss.State==BossState.Recovery,"Attack never resolves");
            Check(Mathf.Abs(before-p.State.Health-p.State.Armor-attack.Item3)<.1f,"Wrong impact damage "+attack.Item2);
            Check(UnityEngine.Object.FindObjectsByType<InquisitorBloodPool>().Any(),"Missing blood pool");report.AppendLine(attack.Item2+": telegraph, delayed hit and blood pool passed.");
        }
        for(int phase=1;phase<=3;phase++)
        {
            await Setup(2,12,phase==1?1:phase==2?.5f:.2f);await Until(()=>boss.State==BossState.Windup,"Ranged windup missing");
            await Until(()=>boss.State==BossState.Recovery,"Volley did not resolve");int bolts=UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length;
            Check(bolts==phase*2-1,"Wrong projectile count "+bolts);report.AppendLine("Phase "+phase+": "+bolts+" blood projectiles.");
            boss.ApplyStun(1);await Task.Delay(80);Check(UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length==0,"Stun leaves projectiles");
        }
        await Setup(2,12);await Until(()=>boss.State==BossState.Windup,"Interrupt windup missing");boss.ApplyStun(2);
        await Task.Delay(1200);Check(UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length==0,"Stunned attack fired later");
        await Setup(0,10);Field(boss,"attackTimer",1000f);Property(boss,"State",BossState.Recovery);
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary projectile obstruction";wall.transform.position=floor.Center(8)+new Vector3(0,1.6f,5);wall.transform.localScale=new Vector3(4,3,.5f);Physics.SyncTransforms();
        float healthBehindWall=p.State.Health+p.State.Armor;InquisitorBolt.Create(boss,floor.Center(8)+Vector3.up*1.6f,Vector3.forward);
        await Task.Delay(1000);Check(UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length==0&&p.State.Health+p.State.Armor==healthBehindWall,"Projectile crosses solid wall");UnityEngine.Object.Destroy(wall);boss.ApplyStun(.01f);await Task.Delay(80);Field(boss,"stunnedUntil",0f);Property(boss,"State",BossState.Recovery);Field(boss,"attackTimer",1000f);
        report.AppendLine("Blood projectile stopped at a solid collider without damaging the player behind it.");
        InquisitorBloodPool.Create(boss,p.transform.position,2,8);float hp=p.State.Health,armor=p.State.Armor;
        await Task.Delay(400);Check(p.State.Health==hp,"Pool warning damages player");await Task.Delay(1500);
        Check(p.State.Health<hp&&p.State.Armor==armor,"Poison should damage health, preserving armor");
        boss.SetPaused(true);hp=p.State.Health;await Task.Delay(1100);Check(p.State.Health==hp,"Poison advances during pause");boss.SetPaused(false);
        boss.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,ignoreGuard=true});await Task.Delay(150);
        Check(floor.Finished,"Victory callback failed");Check(UnityEngine.Object.FindObjectsByType<InquisitorBloodPool>().Length==0&&UnityEngine.Object.FindObjectsByType<InquisitorBolt>().Length==0,"Hazards persist after boss death");
        hp=p.State.Health;await Task.Delay(1100);Check(p.State.Health==hp,"Poison continues after boss death");
        report.AppendLine("Windup interruption, poison telegraph, armor bypass, pause, hazard cleanup and final victory passed.");
        p.enabled=true;System.IO.File.WriteAllText("Docs/Inquisitor_BloodValidation.txt",report.ToString());return report.ToString();
    }
}

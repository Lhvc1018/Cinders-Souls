from pathlib import Path
p=Path('Assets/02_Scripts/Bosses/BossCharacter.cs');s=p.read_text(encoding='utf-8-sig').replace('Bow,Charge','Bow,Charge,BloodNova');s=s.replace('if (bow&&!bloodInquisitor)', 'if(bloodInquisitor&&Phase>=2&&sequence%4==3)CurrentAttack=BossAttack.BloodNova;\n                if (bow&&!bloodInquisitor)');s=s.replace('windup=CurrentAttack==BossAttack.Charge?', 'windup=CurrentAttack==BossAttack.BloodNova?1.6f:CurrentAttack==BossAttack.Charge?');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Bosses/BossCharacter.Blood.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('if(CurrentAttack==BossAttack.Slam)bloodTelegraph=', 'if(CurrentAttack==BossAttack.BloodNova)bloodTelegraph=BloodVisual.Ring(transform.position+Vector3.up*.05f,5,new Color(.9f,.08f,.35f),.08f);\n            else if(CurrentAttack==BossAttack.Slam)bloodTelegraph=');s=s.replace('if(CurrentAttack==BossAttack.Bow)\n', '''if(CurrentAttack==BossAttack.BloodNova)
            {
                var origin=transform.position+Vector3.up*1.1f;
                for(int i=0;i<10;i++)InquisitorBolt.Create(this,origin,Quaternion.Euler(0,i*36,0)*Vector3.forward);
                var fx=Codice.Presentation.ImportedAttackVfx.Burst(origin,false,1.6f);if(fx)Track(fx);
            }
            else if(CurrentAttack==BossAttack.Bow)
''');s=s.replace('Codice.Mechanics.RelicPulse.Spawn(bloodImpact', 'var impactFx=Codice.Presentation.ImportedAttackVfx.Burst(bloodImpact+Vector3.up*.15f,true,slam?1.5f:.8f);if(impactFx)Track(impactFx);\n                Codice.Mechanics.RelicPulse.Spawn(bloodImpact');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Presentation/ImportedAttackVfx.cs');s=p.read_text(encoding='utf-8-sig').replace('public static void Attach(Transform parent,bool fire)','public static void Attach(Transform parent,bool fire)');s=s.replace('        public static void Attach', '''        public static GameObject Burst(Vector3 position,bool fire,float scale)
        {
            var prefab=Resources.Load<GameObject>(fire?"CindersVfx/Fire":"CindersVfx/Lightning");if(!prefab)return null;
            var fx=Object.Instantiate(prefab,position,Quaternion.identity);fx.transform.localScale=Vector3.one*scale;
            foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>()){var main=ps.main;main.loop=false;main.stopAction=ParticleSystemStopAction.None;}
            foreach(var light in fx.GetComponentsInChildren<Light>())light.enabled=false;
            foreach(var col in fx.GetComponentsInChildren<Collider>())col.enabled=false;
            Object.Destroy(fx,2.5f);return fx;
        }
        public static void Attach''');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Bosses/InquisitorBolt.cs');s=p.read_text(encoding='utf-8-sig').replace('owner.Track(root);','Codice.Presentation.ImportedAttackVfx.Attach(root.transform,false);\n            root.transform.localScale=Vector3.one*.45f;owner.Track(root);');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Enemies/BasicEnemy.Residents.cs');s=p.read_text(encoding='utf-8-sig').replace('int count=fire?1:2;', 'var castFx=Codice.Presentation.ImportedAttackVfx.Burst(origin,fire,fire?.4f:.55f);\n            int count=fire?1:2;');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.cs');s=p.read_text(encoding='utf-8').replace('case BossAttack.Charge:', 'case BossAttack.BloodNova:return "Nova de sangre · Busca un hueco";\n                case BossAttack.Charge:');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Multiplayer/LanProtocol.cs');s=p.read_text(encoding='utf-8-sig').replace('public bool encounter,flask;', 'public bool encounter,flask,secretRevealed;');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Multiplayer/LanSession.cs');s=p.read_text(encoding='utf-8-sig').replace('world.seed=context.Expedition.Seed;', 'world.secretRevealed=context.Expedition.SecretRevealed;world.seed=context.Expedition.Seed;').replace('if(context.Expedition)context.Expedition.ApplyCoop', 'if(context.Expedition&&msg.secretRevealed)context.Expedition.RevealSecret();\n            if(context.Expedition)context.Expedition.ApplyCoop');p.write_text(s,encoding='utf-8')

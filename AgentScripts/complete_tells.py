from pathlib import Path
p=Path('Assets/02_Scripts/Bosses/BossCharacter.Wardens.cs');s=p.read_text(encoding='utf-8-sig');a=s.index('                wardenWarning=CurrentAttack==');b=s.index('State=BossState.Windup',a);s=s[:a]+'                ShowWardenTelegraph();'+s[b:];s=s.replace('ResolveWardenAttack();wardenHitIndex++;','ResolveWardenAttack();wardenHitIndex++;\n                    if(wardenHitIndex<wardenHits.Length)ShowWardenTelegraph();');idx=s.index('        void ResolveWardenAttack()');s=s[:idx]+'''        void ShowWardenTelegraph()
        {
            if(wardenWarning)Destroy(wardenWarning);
            wardenWarning=CurrentAttack==BossAttack.Sweep?BloodVisual.Arc(transform.position+Vector3.up*.1f,kind==BossKind.SpearWarden?3.5f:3.2f,facing,wardenSpin?360:kind==BossKind.SpearWarden?98.9f:168.5f,wardenTint):CurrentAttack==BossAttack.Slam?BloodVisual.Ring(aim+Vector3.up*.1f,2,wardenTint,.06f):BloodVisual.Beam(transform.position+Vector3.up*1.4f,aim+Vector3.up,wardenTint,.05f);
            Track(wardenWarning);
        }
'''+s[idx:];p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Bosses/BossCharacter.Necromancer.cs');s=p.read_text(encoding='utf-8-sig').replace('BloodVisual.Beam(transform.position+Vector3.up*2,aim+Vector3.up,','BloodVisual.Beam(transform.position+Vector3.up*2,aim,');s=s.replace('Track(necroWarning);return;','Track(necroWarning);if(!necroRitual&&CurrentAttack==BossAttack.Bow){var origin=transform.position+Vector3.up*2;for(int i=-1;i<=1;i+=2){var ray=BloodVisual.Beam(origin,origin+Quaternion.Euler(0,i*12,0)*(aim-origin),new Color(.3f,.85f,.65f),.04f);ray.transform.SetParent(necroWarning.transform,true);}}return;');p.write_text(s,encoding='utf-8')

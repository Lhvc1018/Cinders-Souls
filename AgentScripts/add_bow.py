from pathlib import Path
p=Path('AgentScripts/BuildHumanPlayer.cs');s=p.read_text(encoding='utf-8').replace('   string controllerPath=', '''   var bow=Clip("Assets/07_Animacion/DoubleL/FBX Unity/Bow/Attack A/Bow_Attack_A_1_All.fbx","ShootBow");
   var bowIdle=Clip("Assets/07_Animacion/DoubleL/FBX Unity/Bow/Movement/Idle/Idle/Bow_Idle_B.fbx","BowIdle",true);
   string controllerPath=''').replace('state.motion=name=="Death"?', 'state.motion=name=="ShootBow"?bow:name=="BowIdle"?bowIdle:name=="Death"?');p.write_text(s,encoding='utf-8')

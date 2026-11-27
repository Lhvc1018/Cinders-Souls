using UnityEngine;
namespace Codice.Bosses {public sealed class BossWarningLifetime:MonoBehaviour {public BossCharacter owner;void LateUpdate(){if(!owner||!owner.IsAlive||owner.State!=BossState.Windup||!owner.Target||!owner.Target.State.IsAlive){gameObject.SetActive(false);Destroy(gameObject);}}}}

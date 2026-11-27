using UnityEngine;
namespace Codice.Mechanics
{
 public sealed class ChestLootReveal:MonoBehaviour
 {
  Vector3 from,to;float elapsed;Collider trigger;LootPickup pickup;
  public void Initialize(Vector3 origin,Vector3 destination){from=origin;to=destination;trigger=GetComponent<Collider>();pickup=GetComponent<LootPickup>();if(trigger)trigger.enabled=false;if(pickup)pickup.enabled=false;transform.position=from;}
  void Update(){elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.65f);transform.position=Vector3.Lerp(from,to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.65f;if(t<1)return;if(trigger)trigger.enabled=true;if(pickup)pickup.enabled=true;Destroy(this);}
 }
}

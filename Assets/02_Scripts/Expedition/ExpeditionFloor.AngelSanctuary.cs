using UnityEngine;

namespace Codice.Expedition
{
    public sealed partial class ExpeditionFloor
    {
        void BuildAngelSanctuary(Transform parent)
        {
            if(FloorNumber!=3)return;
            var center=Center(Layout.Boss);
            // Keep the central combat floor open and the entrance visible.
            for(int side=-1;side<=1;side+=2)
            for(int row=-1;row<=1;row++)
            {
                var position=center+new Vector3(side*14,0,row*11);
                var column=Place(pillarPrefab,position,0,parent);
                column.name="Pilar del juramento";column.transform.localScale=new Vector3(1.4f,2.2f,1.4f);
                Glow(position+Vector3.up*6,new Color(.6f,.72f,1),4,16,parent);
            }
            for(int row=-1;row<=1;row++)
            {
                var arch=Module("Arcade",center+new Vector3(0,0,row*11),0,parent);
                arch.name="Arcada mayor del coro";
                arch.transform.localScale=new Vector3(7.3f,1.95f,1.65f);
                // Only the side piers obstruct movement. The imported arch has an open span.
                arch.GetComponent<BoxCollider>().enabled=false;
                foreach(int sign in new[]{-1,1}){
                    Module("Monument",center+new Vector3(sign*16,0,row*11),-sign*90,parent);
                }
            }
            var halo=Codice.Bosses.BloodVisual.Ring(center+new Vector3(0,6,17),3.2f,new Color(.75f,.57f,.28f),.09f);
            halo.transform.SetParent(parent,true);halo.transform.Rotate(90,0,0,Space.World);
            Glow(center+new Vector3(0,7,14),new Color(1,.74f,.42f),5,22,parent);
        }
    }
}

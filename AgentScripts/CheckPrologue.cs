using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codice.Prologue;
using UnityEngine;
public static class CheckPrologue
{
    public static async Task<string> Run()
    {
        var d=UnityEngine.Object.FindAnyObjectByType<PrologueDirector>();
        if(!d || !d.IntroFinished || !d.DialogueOpen || !d.player.ControlsBlocked)throw new Exception("Initial dialogue not ready or input unlocked");
        int frame=Time.frameCount;
        await Task.Delay(200);
        if(Time.frameCount<=frame)throw new Exception("Frames not advancing");
        d.EnterThreshold();
        if(Codice.UI.RunNavigation.Loading)throw new Exception("Locked exit allowed travel");
        for(int i=0;i<8 && d.DialogueOpen;i++) d.AdvanceDialogue();
        if(!d.HasSpoken || d.DialogueOpen || d.player.ControlsBlocked)throw new Exception("Dialogue did not release controls");
        var cells=d.Layout.Cells;
        foreach(var c in cells)
        {
            var p=new Vector3(c.x*4,1,c.y*4);
            if(!Physics.Raycast(p,Vector3.down,2,~0,QueryTriggerInteraction.Ignore))throw new Exception("Missing floor "+c);
        }
        var queue=new Queue<Vector2Int>();var parent=new Dictionary<Vector2Int,Vector2Int>();
        queue.Enqueue(Vector2Int.zero);parent[Vector2Int.zero]=Vector2Int.zero;
        var sides=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        while(queue.Count>0){var c=queue.Dequeue();foreach(var side in sides){var next=c+side;if(cells.Contains(next)&&!parent.ContainsKey(next)){parent[next]=c;queue.Enqueue(next);}}}
        var route=new List<Vector2Int>();var current=d.Layout.Exit;
        while(current!=Vector2Int.zero){route.Add(current);current=parent[current];}route.Add(Vector2Int.zero);route.Reverse();
        // The NPC occupies the centre of the starting room; begin beside it.
        var cc=d.player.GetComponent<CharacterController>();
        cc.enabled=false;d.player.transform.position=new Vector3(1.3f,.1f,2.8f);cc.enabled=true;
        for(int i=1;i<route.Count;i++)
        {
            var target=new Vector3(route[i].x*4,.1f,route[i].y*4);
            for(int step=0;step<120;step++)
            {
                var delta=target-d.player.transform.position;delta.y=0;
                if(delta.magnitude<.15f)break;
                cc.Move(Vector3.ClampMagnitude(delta,.2f));
            }
            var difference=d.player.transform.position-target;difference.y=0;
            if(difference.magnitude>.3f)throw new Exception("Blocked corridor "+route[i]);
        }
        cc.enabled=false;d.player.transform.position=d.ExitPosition+Vector3.up*.1f;cc.enabled=true;
        d.EnterThreshold();
        for(int i=0;i<100 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso";i++)await Task.Delay(100);
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso")throw new Exception("Did not reach Descenso");
        Debug.Log("[Prologue QA] PASS: live frames, dialogue, exit lock, all floors, traversable route, expedition scene transition");
        return "PASS: live frames, dialogue releases controls, locked exit, floor colliders, traversable route, Descenso transition";
    }
}

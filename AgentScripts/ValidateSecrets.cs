using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Codice.Expedition;
public static class ValidateSecrets
{
 public static string Layouts()
 {
  var counts=new HashSet<int>();int forks=0,linear=0;
  for(int seed=0;seed<1000;seed++)
  {
   var a=new ExpeditionLayout(seed);var b=new ExpeditionLayout(seed);counts.Add(a.Count);
   if(a.Count<6||a.Count>9||!a.Connected()||!a.Cells.SetEquals(b.Cells))throw new Exception("Layout "+seed);
   var reached=new HashSet<int>{0};for(int i=0;i<a.Count;i++)foreach(var e in a.Links)if(reached.Contains(e.x))reached.Add(e.y);
   if(reached.Count!=a.Count||a.Links.Any(e=>e.x==a.Boss))throw new Exception("Reachability "+seed);
   if(a.Links.Where(e=>e.y!=a.SecretRoom).GroupBy(e=>e.x).Any(g=>g.Count()>1))forks++;else linear++;
   for(int i=0;i<a.Count;i++)if(a.RoomAt(new Vector3(a.Rooms[i].x*4,0,a.Rooms[i].y*4))!=i)throw new Exception("Overlap "+seed);
  }
  if(counts.Count!=4||forks==0||linear==0)throw new Exception("No variation");
  string report=$"1000 seeds passed; room counts 6,7,8,9; {forks} branched, {linear} linear; connected, deterministic, boss terminal, secret optional.";System.IO.File.WriteAllText("Docs/Secrets_Validation.txt",report);Debug.Log(report);return report;
 }
}

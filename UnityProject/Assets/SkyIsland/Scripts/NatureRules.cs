using System;
using System.Collections.Generic;
using UnityEngine;
namespace SkyIsland {
 [Serializable] public class NatureSpawn {
  public float harvestedSeconds; public int delivered; public int id,kind,health; public Hex cell; public float x,z,yaw;
  public Vector3 Position=>cell.World+new Vector3(x,0,z);
 }
 [Serializable] public class NatureWorldState {
  public int version=1,sequence; public bool initialized; public double nextSpawnAt;
  public List<NatureSpawn> nodes=new List<NatureSpawn>();
 }
 public static class NatureRules {
  public const int Target=50;
  public const float Scale=3;
  public static int Health(int kind)=>kind==0?3:kind==1?4:1;
  public static int Reward(int kind)=>kind==0?300:kind==1?500:0;
  public static string Material(int kind)=>kind==0?"wood":kind==1?"stone":"";
  public static float Height(int kind)=>(kind==0?1.3f:kind==1?.3f:.22f)*Scale;
  public static float Radius(int kind)=>kind==0?.65f:kind==1?.35f:.09f;
  static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
  public static bool Valid(NatureWorldState state){
   if(state==null||state.version!=1||state.nodes==null||state.nodes.Count>Target||state.sequence<0||(!state.initialized&&state.nodes.Count>0)||double.IsNaN(state.nextSpawnAt)||double.IsInfinity(state.nextSpawnAt)||state.nextSpawnAt<0)return false;
   var ids=new HashSet<int>();foreach(var n in state.nodes)if(n==null||n.id<=0||n.id>state.sequence||!ids.Add(n.id)||n.kind<0||n.kind>3||n.health<1||n.health>Health(n.kind)||!Finite(n.harvestedSeconds)||n.harvestedSeconds<0||n.harvestedSeconds>=30||n.delivered!=Mathf.FloorToInt(Reward(n.kind)*n.harvestedSeconds/30f)||!Finite(n.x)||!Finite(n.z)||!Finite(n.yaw)||Mathf.Abs(n.x)>1.1f||Mathf.Abs(n.z)>1.1f||Math.Abs((long)n.cell.q)>HexIsland.Limit||Math.Abs((long)n.cell.r)>HexIsland.Limit||!Hex.FromWorld(n.Position).Equals(n.cell))return false;
   return true;
  }
  public static bool EmptyCell(Hex cell,HexIsland island,List<PanelState> panels){
   if(!island.Cells.Contains(cell)||cell.Equals(new Hex(0,0))||cell.Equals(new Hex(0,1))||island.Workshops.Contains(cell))return false;
   foreach(var p in panels)if(Hex.FromWorld(new Vector3(p.x,0,p.z)).Equals(cell))return false;
   return true;
  }
  public static bool Add(NatureWorldState state,NatureSpawn node){
   if(state.nodes.Count>=Target||state.sequence==int.MaxValue)return false;
   foreach(var other in state.nodes){var delta=other.Position-node.Position;if(delta.sqrMagnitude<Mathf.Pow(Radius(other.kind)+Radius(node.kind),2))return false;}
   node.id=state.sequence+1;state.nodes.Add(node);state.sequence++;return true;
  }
  public const float HarvestSeconds=30;
  public static string Hit(NatureWorldState state,NatureSpawn node,SurvivalInventory inventory,HexIsland island,int damage,Func<bool> save)=>Harvest(state,node,inventory,island,damage,save);
  public static string Harvest(NatureWorldState state,NatureSpawn node,SurvivalInventory inventory,HexIsland island,float seconds,Func<bool> save){
   if(node==null||!state.nodes.Contains(node)||!Finite(seconds)||seconds<=0)return "missing";
   float before=node.harvestedSeconds,after=Mathf.Min(HarvestSeconds,before+seconds);int oldDelivered=node.delivered;
   int amount=Reward(node.kind),earned=Mathf.FloorToInt(amount*after/HarvestSeconds),delta=earned-oldDelivered;
   if(delta>0&&!inventory.CanCollect(Material(node.kind),delta))return "full";
   int wood=island.Wood,stone=island.Stone,index=state.nodes.IndexOf(node);bool complete=amount==0||after>=HarvestSeconds;
   node.harvestedSeconds=after;node.delivered=earned;if(delta>0)inventory.Collect(Material(node.kind),delta);if(complete)state.nodes.RemoveAt(index);
   if((complete||delta>0)&&!save()){if(complete)state.nodes.Insert(index,node);node.harvestedSeconds=before;node.delivered=oldDelivered;island.Wood=wood;island.Stone=stone;return "save-failed";}return complete?"complete":"hit";
  }
  public static bool Due(NatureWorldState state,double now)=>now>=state.nextSpawnAt;
  public static void Schedule(NatureWorldState state,double now,float random01){state.nextSpawnAt=now+25+Mathf.Clamp01(random01)*30;}
 }
}

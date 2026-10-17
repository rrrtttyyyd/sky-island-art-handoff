using System;using System.Collections.Generic;using UnityEngine;
namespace SkyIsland {
 [Serializable] public class DockState {public Hex cell;public int edge;public string Key=>cell.q+","+cell.r+":"+edge;public Vector3 Direction=>Hex.Directions[edge].World.normalized;public Vector3 Center=>cell.World+Direction*2.15f+Vector3.up*.43f;public Quaternion Rotation=>Quaternion.LookRotation(Direction);}
 [Serializable] public class HarborState {public int version=1;public bool initialized,hasParked;public Vector3 parked;public List<DockState> docks=new List<DockState>();}
 public static class DockRules {
  public const int Limit=8,WoodCost=12,StoneCost=4;
  public static bool Edge(DockState d,HexIsland island)=>d!=null&&d.edge>=0&&d.edge<6&&island.Cells.Contains(d.cell)&&!island.Cells.Contains(d.cell+Hex.Directions[d.edge]);
  public static bool Add(List<DockState> docks,DockState d,HexIsland island){if(docks==null||docks.Count>=Limit||!Edge(d,island))return false;foreach(var other in docks)if(other.Key==d.Key||Vector3.Distance(other.Center,d.Center)<2.8f)return false;return true;}
  public static bool Contains(DockState d,Vector3 p){var local=Quaternion.Inverse(d.Rotation)*(p-d.Center);return Mathf.Abs(local.x)<=1.2f&&Mathf.Abs(local.z)<=1.4f;}
  public static float? Floor(List<DockState> docks,Vector3 p){if(docks!=null)foreach(var d in docks)if(Contains(d,p)&&p.y>=d.Center.y+.45f)return d.Center.y+.65f;return null;}
  public static bool Blocks(List<DockState> docks,Hex cell){if(docks!=null)foreach(var d in docks)if(d.cell.Equals(cell)||(d.cell+Hex.Directions[d.edge]).Equals(cell))return true;return false;}
  public static bool Valid(HarborState s,HexIsland island){if(s==null||s.version!=1||s.docks==null||s.docks.Count>Limit)return false;var accepted=new List<DockState>();foreach(var d in s.docks){if(!Add(accepted,d,island))return false;accepted.Add(d);}return !s.hasParked||Finite(s.parked);}
  static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
  public static string Place(HarborState s,DockState d,HexIsland island,Func<bool> save){if(!Add(s.docks,d,island))return "invalid";if(island.Wood<WoodCost||island.Stone<StoneCost)return "materials";s.docks.Add(d);island.Wood-=WoodCost;island.Stone-=StoneCost;if(!save()){s.docks.Remove(d);island.Wood+=WoodCost;island.Stone+=StoneCost;return "save-failed";}return "complete";}
 }
 public class DockMarker:MonoBehaviour {public DockState State;}
}

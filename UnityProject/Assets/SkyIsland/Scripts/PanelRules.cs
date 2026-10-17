using System; using System.Collections.Generic; using UnityEngine;
namespace SkyIsland {
 [Serializable] public class PanelState {public const int CoordinateLimit=HexIsland.Limit*4;public int x,z,level,rotation;public bool vertical; public void RotateNext(){if(vertical){rotation=(rotation+1)%4;if(rotation==0)vertical=false;}else {rotation=(rotation+1)%2;if(rotation==0)vertical=true;}} public Quaternion Orientation=>Quaternion.Euler(0,rotation*90,0)*Quaternion.Euler(vertical?90:0,0,0); public Vector3 Center=>new Vector3(x,.45f+level,z)+(vertical?Quaternion.Euler(0,rotation*90,0)*new Vector3(0,.5f,.5f):Vector3.zero);public Vector3 Size=>vertical?(rotation%2==0?new Vector3(1,1,.1f):new Vector3(.1f,1,1)):new Vector3(1,.1f,1);public string Key=>vertical?$"w:{Mathf.RoundToInt(Center.x*2)}:{Mathf.RoundToInt(Center.z*2)}:{level}":$"f:{x}:{z}:{level}";public bool Valid=>Math.Abs((long)x)<=CoordinateLimit&&Math.Abs((long)z)<=CoordinateLimit&&level>=0&&level<=3&&rotation>=0&&rotation<4;}
 public static class PanelRules {
  public const int Limit=128;
  static bool Touch(PanelState wall,PanelState floor){var c=wall.Center;return wall.rotation%2==0?Mathf.Abs(c.x-floor.x)<.01f&&Mathf.Abs(Mathf.Abs(c.z-floor.z)-.5f)<.01f:Mathf.Abs(c.z-floor.z)<.01f&&Mathf.Abs(Mathf.Abs(c.x-floor.x)-.5f)<.01f;}
  public static bool Connected(PanelState a,PanelState b){
   if(!a.vertical&&!b.vertical)return a.level==b.level&&Math.Abs(a.x-b.x)+Math.Abs(a.z-b.z)==1;
   if(a.vertical!=b.vertical){var wall=a.vertical?a:b;var floor=a.vertical?b:a;return (floor.level==wall.level||floor.level==wall.level+1)&&Touch(wall,floor);}
   float dx=Mathf.Abs(a.Center.x-b.Center.x),dz=Mathf.Abs(a.Center.z-b.Center.z);
   if(a.rotation%2!=b.rotation%2)return a.level==b.level&&Mathf.Abs(dx-.5f)<.01f&&Mathf.Abs(dz-.5f)<.01f;
   if(a.level!=b.level)return Math.Abs(a.level-b.level)==1&&dx<.01f&&dz<.01f;
   return a.rotation%2==0?Mathf.Abs(dx-1)<.01f&&dz<.01f:dx<.01f&&Mathf.Abs(dz-1)<.01f;
  }
  static bool Supported(PanelState p,List<PanelState> done,HexIsland island){if(p.level==0&&island.Cells.Contains(Hex.FromWorld(new Vector3(p.x,0,p.z))))return true;foreach(var other in done)if(Connected(p,other))return true;return false;}
  public static bool Validate(List<PanelState> items,HexIsland island){if(items==null||items.Count>Limit)return false;var keys=new HashSet<string>();foreach(var p in items)if(p==null||!p.Valid||!keys.Add(p.Key))return false;var pending=new List<PanelState>(items);var done=new List<PanelState>();bool progress=true;while(pending.Count>0&&progress){progress=false;for(int i=pending.Count-1;i>=0;i--)if(Supported(pending[i],done,island)){done.Add(pending[i]);pending.RemoveAt(i);progress=true;}}return pending.Count==0;}
  public static bool Add(List<PanelState> items,PanelState p,HexIsland island){if(p==null||!p.Valid||items.Count>=Limit)return false;var proposed=new List<PanelState>(items){p};return Validate(proposed,island);}
  public static float? Floor(List<PanelState> items,Vector3 p){float? best=null;foreach(var panel in items)if(!panel.vertical&&Mathf.Abs(p.x-panel.x)<=.5f&&Mathf.Abs(p.z-panel.z)<=.5f){float y=panel.Center.y+.05f+.65f;if(y<=p.y+.2f&&(!best.HasValue||y>best.Value))best=y;}return best;}
  public static bool Blocked(List<PanelState> items,Vector3 position){var body=new Bounds(position,new Vector3(.46f,1.26f,.46f));foreach(var p in items)if((p.vertical||p.Center.y+.05f>position.y-.65f+.2f)&&new Bounds(p.Center,p.Size).Intersects(body))return true;return false;}
  public static float? Ceiling(List<PanelState> items,Vector3 position){float? best=null;foreach(var p in items)if(!p.vertical&&Mathf.Abs(position.x-p.x)<.72f&&Mathf.Abs(position.z-p.z)<.72f){float y=p.Center.y-.05f-.65f;if(y>=position.y-.01f&&(!best.HasValue||y<best.Value))best=y;}return best;}
 }

}

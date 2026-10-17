using System.Collections.Generic;using UnityEngine;
namespace SkyIsland {public partial class SkyIslandPrototype {
 readonly Dictionary<string,GameObject> dockObjects=new Dictionary<string,GameObject>();Transform dockGhost;int dockEdge;bool harborReady;
 void InitHarbor(){
  if(!DockRules.Valid(survival.harbor,island))survival.harbor=new HarborState();var h=survival.harbor;
  if(!h.initialized){h.initialized=true;var cells=new List<Hex>(island.Cells);cells.Sort((a,b)=>{int near=a.Distance(new Hex(0,1)).CompareTo(b.Distance(new Hex(0,1)));return near!=0?near:a.q!=b.q?a.q.CompareTo(b.q):a.r.CompareTo(b.r);});foreach(var cell in cells){if(cell.Equals(new Hex(0,0)))continue;bool found=false;for(int e=0;e<6;e++){var d=new DockState{cell=cell,edge=e};if(DockRules.Add(h.docks,d,island)){h.docks.Add(d);found=true;break;}}if(found)break;}}
  foreach(var d in h.docks)RenderDock(d);dockGhost=Primitive("Dock preview",PrimitiveType.Cube,Vector3.zero,new Vector3(2.4f,.12f,2.8f),Mat("dock preview",new Color(.5f,.9f,.7f))).transform;dockGhost.gameObject.SetActive(false);harborReady=true;
 }
 void RenderDock(DockState d){var g=new GameObject("船坞兜网 "+d.Key);g.transform.position=d.Center;g.transform.rotation=d.Rotation;var marker=g.AddComponent<DockMarker>();marker.State=d;var col=g.AddComponent<BoxCollider>();col.size=new Vector3(2.4f,.12f,2.8f);dockObjects[d.Key]=g;
  for(int i=-4;i<=4;i++){Primitive("Net across",PrimitiveType.Cube,new Vector3(0,0,i*.32f),new Vector3(2.4f,.035f,.025f),Mat("dock rope",new Color(.66f,.59f,.39f)),g.transform);Primitive("Net length",PrimitiveType.Cube,new Vector3(i*.28f,0,0),new Vector3(.025f,.035f,2.8f),Mat("dock rope",new Color(.66f,.59f,.39f)),g.transform);}
  for(int x=-1;x<=1;x+=2){Primitive("Dock frame",PrimitiveType.Cube,new Vector3(x*1.22f,.02f,0),new Vector3(.12f,.15f,2.9f),Mat("dock frame",new Color(.4f,.28f,.17f)),g.transform);for(int z=-1;z<=1;z+=2)Primitive("Dock post",PrimitiveType.Cylinder,new Vector3(x*1.2f,.3f,z*1.35f),new Vector3(.1f,.4f,.1f),Mat("dock frame",new Color(.4f,.28f,.17f)),g.transform);}
 }
 Vector3 DefaultBerth(){if(survival.harbor.docks.Count>0)return survival.harbor.docks[0].Center+Vector3.up*.25f;return FountainSpawn()+Vector3.up*1.9f;}
 bool LandingAt(Vector3 p,out Vector3 berth,out Vector3 exit){
  foreach(var d in survival.harbor.docks)if(DockRules.Contains(d,p)){berth=d.Center+Vector3.up*.25f;exit=d.cell.World+Vector3.up*(Height(d.cell)+.65f);return !PanelRules.Blocked(survival.placedPanels,exit);}
  var cell=Hex.FromWorld(p);berth=exit=Vector3.zero;if(!island.Cells.Contains(cell)||cell.Equals(new Hex(0,0))||island.Workshops.Contains(cell))return false;
  foreach(var panel in survival.placedPanels)if(Hex.FromWorld(panel.Center).Equals(cell))return false;
  if(survival.nature!=null)foreach(var n in survival.nature.nodes)if(n.kind<2&&Vector3.Distance(n.Position,cell.World)<1.4f)return false;
  berth=cell.World+Vector3.up*(Height(cell)+.25f);exit=cell.World+Vector3.up*(Height(cell)+.65f);return !PanelRules.Blocked(survival.placedPanels,exit);
 }
 void ToggleAirship(){
  if(!aboard){if(ShipNear()&&motion.Grounded){aboard=true;buildMode=false;message="小飞艇：WASD飞行，空格上升/Ctrl下降，F停靠下艇";}return;}
  if(!LandingAt(airship.position,out var berth,out var exit)){message="下方没有安全停靠点，请飞回兜网或岛上空地再按F";return;}
  if(airship.position.y>berth.y+4){message="请先下降到停靠面上方4米内，再按F停靠";return;}
  var h=survival.harbor;bool old=h.hasParked;var oldPark=h.parked;h.parked=berth;h.hasParked=true;if(!Save()){h.parked=oldPark;h.hasParked=old;return;}
  aboard=false;airship.position=berth;motion.Respawn(exit);avatar.position=motion.Position;cameraPan=Vector3.zero;message="飞艇已就地停靠，下艇完成";Save();
 }
 void DockInput(Ray ray,bool overUI,bool navigating){if(dockGhost)dockGhost.gameObject.SetActive(false);if(!buildMode||aboard||(mode!=5&&mode!=6)||overUI||navigating)return;
  if(mode==6){if(Input.GetMouseButtonDown(0)&&Physics.Raycast(ray,out var hit,100)){var marker=hit.collider.GetComponentInParent<DockMarker>();if(marker)RemoveDock(marker.State);}return;}
  if(Input.GetKeyDown(KeyCode.Q))dockEdge=(dockEdge+1)%6;var plane=new Plane(Vector3.up,Vector3.up*.35f);if(!plane.Raycast(ray,out float distance))return;var point=ray.GetPoint(distance);var cell=Hex.FromWorld(point);if(!island.Cells.Contains(cell)){float best=4;foreach(var c in island.Cells){float d=Vector3.Distance(c.World,point);if(d<best){best=d;cell=c;}}}
  var candidate=new DockState{cell=cell,edge=dockEdge};bool allowed=DockRules.Add(survival.harbor.docks,candidate,island)&&Vector3.Distance(avatar.position,candidate.Center)<8;
  dockGhost.gameObject.SetActive(true);dockGhost.position=candidate.Center;dockGhost.rotation=candidate.Rotation;dockGhost.GetComponent<Renderer>().sharedMaterial=allowed?Mat("dock good",new Color(.4f,.85f,.5f)):Mat("dock bad",new Color(.85f,.3f,.3f));
  if(Input.GetMouseButtonDown(0)){if(!allowed){message="船坞须在外露岛边、不重叠且靠近人物；Q换边";return;}string result=DockRules.Place(survival.harbor,candidate,island,Save);if(result=="complete"){RenderDock(candidate);message="船坞兜网建成：飞艇可在网面上停靠";}else message=result=="materials"?"船坞需要木材12、石料4":"船坞保存失败或位置无效";}
 }
 void RemoveDock(DockState d){if(Vector3.Distance(avatar.position,d.Center)>8){message="请靠近船坞再拆除";return;}if(DockRules.Contains(d,avatar.position)||DockRules.Contains(d,airship.position)){message="人物或飞艇仍在船坞上，先移开再拆除";return;}if(!inventory.CanCollectMany(new Dictionary<string,int>{{"wood",6},{"stone",2}})){message="库存已满，无法返还材料";return;}int index=survival.harbor.docks.IndexOf(d);if(index<0)return;survival.harbor.docks.RemoveAt(index);island.Wood+=6;island.Stone+=2;if(!Save()){survival.harbor.docks.Insert(index,d);island.Wood-=6;island.Stone-=2;return;}Destroy(dockObjects[d.Key]);dockObjects.Remove(d.Key);message="船坞已拆除，返还木材6/石料2";}
}}

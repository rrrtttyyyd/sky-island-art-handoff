using System;using System.Collections.Generic;using System.IO;using UnityEngine;using SkyIsland;
public static class ConstructionReview {
 public static void Validate(){
  int n=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("CONSTRUCTION FAIL "+name);n++;};
  var island=new HexIsland{Wood=1000,Stone=1000};bool expanded=true,oldSave=false;
  for(int x=2;x<=122;x++){expanded&=island.Place(new Hex(x,0),out _);if(island.Cells.Count==64)oldSave=HexIsland.TryRestore(island.Snapshot(Vector3.zero),out var old)&&old.Cells.Count==64;}
  check(expanded&&island.Cells.Count==128,"actual expansion reaches 128, beyond old 64 cap");check(oldSave,"legacy 64 cell saves remain readable");
  int wood=island.Wood,stone=island.Stone;check(!island.Place(new Hex(123,0),out _)&&island.Wood==wood&&island.Stone==stone,"129th cell rejected without debit");
  var save=island.Snapshot(Vector3.zero);check(HexIsland.TryRestore(JsonUtility.FromJson<IslandSave>(JsonUtility.ToJson(save)),out var loaded)&&loaded.Cells.Count==128&&loaded.Cells.Contains(new Hex(122,0)),"128 cell long island save roundtrip");
  island.Wood=1000000;check(!island.Remove(new Hex(122,0),new Hex(0,0),out _)&&island.Cells.Count==128,"full inventory does not overflow on foundation refund");island.Wood=100;check(island.Remove(new Hex(122,0),new Hex(0,0),out _)&&island.Wood==102,"normal foundation refund still works");
  var far=new NatureWorldState{initialized=true,sequence=1,nodes=new List<NatureSpawn>{new NatureSpawn{id=1,kind=2,health=1,cell=new Hex(100,0)}}};check(NatureRules.Valid(far)&&NatureRules.EmptyCell(new Hex(100,0),island,new List<PanelState>()),"renewable resources persist on expanded distant cells");
  check(PanelRules.Validate(new List<PanelState>{new PanelState{x=217}},island),"panel can be built on distant expanded foundation beyond old coordinate cap");
  check(!new PanelState{x=PanelState.CoordinateLimit+1}.Valid,"panel coordinate safety bound retained");
  var small=new HexIsland();var floor=new PanelState{x=3};
  check(PanelSnap.TrySnap(floor,floor.Center+Vector3.right*.48f,Vector3.up,false,0,out var east)&&east.x==4&&east.z==0&&!east.vertical,"floor snaps to right edge");
  check(PanelRules.Add(new List<PanelState>{floor},east,small),"floor extends beyond island edge with panel support");
  PanelSnap.TrySnap(east,east.Center+Vector3.right*.48f,Vector3.up,false,1,out var farther);check(PanelRules.Validate(new List<PanelState>{farther,east,floor},small),"multi panel extension roots regardless of save order");
  check(!PanelRules.Validate(new List<PanelState>{floor,farther},small),"removing connecting floor leaves unsupported extension rejected");
  check(PanelSnap.TrySnap(floor,floor.Center+Vector3.back*.49f,Vector3.up,false,1,out var south)&&south.z==-1&&south.rotation==1,"floor snaps backward with selected grain");
  check(PanelSnap.TrySnap(floor,floor.Center+Vector3.right*.49f,Vector3.up,true,1,out var edgeWall)&&Mathf.Abs(edgeWall.Center.x-3.5f)<.001f&&PanelRules.Connected(floor,edgeWall),"vertical wall joins floor edge");
  check(PanelSnap.TrySnap(floor,floor.Center+Vector3.back*.49f,Vector3.up,true,0,out var reverseWall)&&Mathf.Abs(reverseWall.Center.z+.5f)<.001f&&PanelRules.Connected(floor,reverseWall),"opposite edge retains selected wall orientation");
  var wall=new PanelState{x=3,vertical=true};
  check(PanelSnap.TrySnap(wall,wall.Center+Vector3.up*.48f,Vector3.forward,true,0,out var upper)&&upper.level==1&&Vector2.Distance(new Vector2(upper.Center.x,upper.Center.z),new Vector2(wall.Center.x,wall.Center.z))<.001f,"wall upper edge stacks vertically");
  check(PanelRules.Add(new List<PanelState>{wall},upper,small),"stacked wall has rooted support");
  check(PanelSnap.TrySnap(wall,wall.Center+Vector3.right*.48f,Vector3.forward,true,2,out var side)&&Mathf.Abs(side.Center.x-4)<.001f&&PanelRules.Connected(wall,side),"wall side extends horizontally");
  check(PanelRules.Add(new List<PanelState>{wall},side,small),"horizontal wall extension supported outside ground");
  check(PanelSnap.TrySnap(wall,wall.Center+Vector3.right*.48f,Vector3.forward,true,1,out var corner)&&PanelRules.Connected(wall,corner),"perpendicular corner walls share a vertical edge");
  check(PanelSnap.TrySnap(wall,wall.Center+Vector3.up*.48f,Vector3.forward,false,0,out var roof)&&roof.level==1&&roof.z==1&&PanelRules.Connected(wall,roof),"wall top joins horizontal floor on front face");
  check(PanelSnap.TrySnap(wall,wall.Center+Vector3.up*.48f,Vector3.back,false,1,out var backRoof)&&backRoof.z==0&&backRoof.level==1&&PanelRules.Connected(wall,backRoof),"reverse face chooses other side of wall");
  check(PanelSnap.TrySnap(wall,wall.Center+new Vector3(0,.5f,-.04f),Vector3.up,false,0,out var topBack)&&topBack.z==0,"top thin face uses pointer half for extension side");
  check(PanelRules.Validate(new List<PanelState>{roof,side,upper,corner,wall},small),"mixed wall and floor edge assembly saves with rooted graph");
  check(!PanelRules.Connected(floor,new PanelState{x=4,z=1}),"point-only corner contact is not structural support");check(!PanelRules.Connected(wall,new PanelState{x=3,z=1,vertical=true}),"parallel walls separated by gap do not support each other");
  check(!PanelSnap.TrySnap(new PanelState{x=3,vertical=true,level=3},new Vector3(3,4.4f,.5f),Vector3.forward,true,0,out _),"top layer bound still enforced");
  check(!PanelRules.Validate(new List<PanelState>{new PanelState{x=80,level=1},new PanelState{x=81,level=1}},small),"unrooted edge cycle rejected");
  var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);try{fixture.layer=31;var rootWall=new PanelState{x=70,vertical=true};fixture.transform.position=rootWall.Center;fixture.transform.rotation=rootWall.Orientation;fixture.transform.localScale=new Vector3(1,.1f,1);fixture.AddComponent<PanelMarker>().State=rootWall;Physics.SyncTransforms();var ray=new Ray(rootWall.Center+new Vector3(0,.4f,2),Vector3.back);bool hit=Physics.Raycast(ray,out var h,5,1<<31,QueryTriggerInteraction.Ignore);check(hit&&h.collider.GetComponent<PanelMarker>()!=null,"actual collider ray selects target wall");check(hit&&PanelSnap.TrySnap(rootWall,h.point,h.normal,false,0,out var actual)&&actual.level==1&&actual.z==1,"actual hit point and normal drive roof edge snap");}finally{UnityEngine.Object.DestroyImmediate(fixture);}
  Directory.CreateDirectory("../evidence/unity");File.WriteAllText("../evidence/unity/construction-tests.txt","PASS "+n+" foundation capacity/save/edge graph/raycast assertions; UTC "+DateTime.UtcNow.ToString("o")+"\nEditor collider and model checks, not browser mouse input or mobile performance.");Debug.Log("SKY_CONSTRUCTION_TESTS_PASS "+n);
 }
}

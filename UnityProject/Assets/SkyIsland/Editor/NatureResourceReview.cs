using System;using System.IO;using UnityEngine;using SkyIsland;
public static class NatureResourceReview {
 public static void Validate(){
  int n=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("NATURE FAIL "+name);n++;};
  var island=new HexIsland();var state=new NatureWorldState{initialized=true};var survival=new SurvivalState{nature=state};var inventory=new SurvivalInventory(island,survival);
  var tree=new NatureSpawn{kind=0,health=3,cell=new Hex(-1,0)};check(NatureRules.Add(state,tree),"add tree");
  check(NatureRules.Harvest(state,tree,inventory,island,1,()=>true)=="hit"&&island.Wood==10,"one second pays ten wood");
  check(NatureRules.Harvest(state,tree,inventory,island,1,()=>false)=="save-failed"&&tree.harvestedSeconds==1&&island.Wood==10,"failed payout rolls back time and inventory");
  for(int i=1;i<30;i++)NatureRules.Harvest(state,tree,inventory,island,1,()=>true);
  check(island.Wood==300&&!state.nodes.Contains(tree),"30 active seconds exactly 300 wood");check(NatureRules.Harvest(state,tree,inventory,island,1,()=>true)=="missing"&&island.Wood==300,"cannot pay twice");
  var rock=new NatureSpawn{kind=1,health=4,cell=new Hex(1,-1)};NatureRules.Add(state,rock);
  for(int i=0;i<30;i++){NatureRules.Harvest(state,rock,inventory,island,1,()=>true);check(island.Stone==Mathf.FloorToInt(500*(i+1)/30f),"stone uniform payout "+i);}
  check(island.Stone==500&&!state.nodes.Contains(rock),"stone completes at 30s");
  tree.harvestedSeconds=0;tree.delivered=0;NatureRules.Add(state,tree);island.Wood=1000000;
  check(NatureRules.Harvest(state,tree,inventory,island,1,()=>true)=="full"&&tree.harvestedSeconds==0,"full inventory pauses progress");
  island.Wood=999700;NatureRules.Harvest(state,tree,inventory,island,30,()=>true);check(island.Wood==1000000,"exact capacity full harvest");
  check(!inventory.Collect("wood",300)&&island.Wood==1000000,"bulk collect respects cap");check(!inventory.Collect("unknown",500)&&!inventory.Collect("wood",-300),"invalid collections rejected");
  check(NatureRules.EmptyCell(new Hex(-1,0),island,survival.placedPanels),"empty cell eligible");check(!NatureRules.EmptyCell(new Hex(0,0),island,survival.placedPanels)&&!NatureRules.EmptyCell(new Hex(0,1),island,survival.placedPanels),"core and spawn excluded");
  island.Workshops.Add(new Hex(-1,0));check(!NatureRules.EmptyCell(new Hex(-1,0),island,survival.placedPanels),"building excluded");island.Workshops.Clear();survival.placedPanels.Add(new PanelState{x=-2});check(!NatureRules.EmptyCell(new Hex(-1,0),island,survival.placedPanels),"panel footprint excluded");survival.placedPanels.Clear();
  NatureRules.Schedule(state,100,0);check(state.nextSpawnAt==125&&!NatureRules.Due(state,124)&&NatureRules.Due(state,125),"minimum random interval 25 seconds");NatureRules.Schedule(state,100,1);check(state.nextSpawnAt==155,"maximum random interval 55 seconds");
  NatureRules.Add(state,new NatureSpawn{kind=2,health=1,cell=new Hex(1,-1)});var restored=JsonUtility.FromJson<SurvivalState>(JsonUtility.ToJson(survival));check(restored.nature.initialized&&NatureRules.Valid(restored.nature)&&restored.nature.nextSpawnAt==155,"timer and remaining resources persist");
  check(!NatureRules.Valid(new NatureWorldState{initialized=true,nodes=new System.Collections.Generic.List<NatureSpawn>{new NatureSpawn{id=1,kind=8,health=1}}}),"corrupt resource type rejected");
  var capped=new NatureWorldState{initialized=true};for(int i=0;i<50;i++)check(NatureRules.Add(capped,new NatureSpawn{kind=2,health=1,cell=new Hex(i,0)}),"capacity fixture "+i);check(!NatureRules.Add(capped,new NatureSpawn{kind=2,health=1,cell=new Hex(60,0)}),"50 active resource cap");
  check(Mathf.Abs(NatureRules.Height(0)-3.9f)<.001f&&Mathf.Abs(NatureRules.Height(1)-.9f)<.001f&&Mathf.Abs(NatureRules.Height(2)-.66f)<.001f,"all three model scales tripled");
  Directory.CreateDirectory("../evidence/unity");File.WriteAllText("../evidence/unity/nature-resource-tests.txt","PASS "+n+" resource rules/atomic reward/save/refresh assertions; UTC "+DateTime.UtcNow.ToString("o")+"\nLocal offline model; browser input and mobile performance not validated.");Debug.Log("SKY_NATURE_RESOURCE_TESTS_PASS "+n);
 }
}

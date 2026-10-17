using System;using System.Collections.Generic;using UnityEngine;
namespace SkyIsland {
 public static class PanelDismantle {
  public static bool AxeEquipped(SurvivalState state)=>state!=null&&BackpackRules.Selected(state)=="axe";
  public static string Remove(SurvivalState state,PanelState panel,HexIsland island,Vector3 player,float reach,Func<bool> save){
   if(state==null||panel==null||state.placedPanels==null||!state.placedPanels.Contains(panel))return "missing";
   if(float.IsNaN(reach)||float.IsInfinity(reach)||reach<=0||Vector3.Distance(player,panel.Center)>reach)return "range";
   if(state.bag==null?state.panels>=999:!BackpackBridge.Fits(island,state,new Dictionary<string,int>{{"panel",1}}))return "full";
   var remaining=new List<PanelState>(state.placedPanels);remaining.Remove(panel);
   if(!PanelRules.Validate(remaining,island))return "support";
   if(!panel.vertical&&Mathf.Abs(player.x-panel.x)<.75f&&Mathf.Abs(player.z-panel.z)<.75f&&Mathf.Abs(player.y-(panel.Center.y+.7f))<.25f)return "standing";
   var original=state.placedPanels;int count=state.panels;state.placedPanels=remaining;state.panels++;
   bool committed;try{committed=save();}catch{committed=false;}
   if(!committed){state.placedPanels=original;state.panels=count;return "save-failed";}return "complete";
  }
 }
}

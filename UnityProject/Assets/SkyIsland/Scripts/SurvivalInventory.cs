using System; using System.Collections.Generic;
using UnityEngine;
namespace SkyIsland {
    [Serializable] public class SurvivalState {
        public List<GroundItemState> groundItems=new List<GroundItemState>(); public BackpackState bag; public HarborState harbor; public NatureWorldState nature; public SpringChestState springChest;
        public int benches,axes,pickaxes,selectedSlot,food,panels; public List<PanelState> placedPanels=new List<PanelState>();
        public bool Valid()=>panels>=0&&panels<=(bag==null?999:1000000)&&food>=0&&food<=1000000&&benches>=0&&benches<=(bag==null?99:1000000)&&axes>=0&&axes<=(bag==null?99:1000000)&&pickaxes>=0&&pickaxes<=(bag==null?99:1000000)&&selectedSlot>=0&&selectedSlot<8;
    }
    public class SurvivalInventory {
        readonly HexIsland island;
        public readonly SurvivalState State;
        public SurvivalInventory(HexIsland island,SurvivalState state){this.island=island;State=state!=null&&state.Valid()?state:new SurvivalState();}
        public bool NearBench(Hex player){foreach(var cell in island.Workshops)if(cell.Distance(player)<=1)return true;return false;}
        public bool Craft(string recipe,Hex player,out string message){
            int cost=recipe=="panel"?2:recipe=="bench"?6:recipe=="axe"?3:recipe=="pickaxe"?4:-1;
            if(cost<0){message="未知配方";return false;}
            if(recipe!="bench"&&!NearBench(player)){message="请先放置工作台，并靠近它制作工具";return false;}
            if(island.Wood<cost){message="木头不足";return false;}
            if(State.bag!=null&&!BackpackBridge.Fits(island,State,new Dictionary<string,int>{{"wood",-cost},{recipe,1}})){message="背包空间不足，无法制作";return false;}
            if(State.bag==null&&((recipe=="panel"&&State.panels>=999)||(recipe=="bench"&&State.benches>=99)||(recipe=="axe"&&State.axes>=99)||(recipe=="pickaxe"&&State.pickaxes>=99))){message="物品已达上限";return false;}

            island.Wood-=cost;
            if(recipe=="panel"){State.panels++;message="已制作木板，可从背包放到快捷栏建造";}else if(recipe=="bench"){State.benches++;message="已制作工作台，选择它后点击空闲地基放置";}
            else if(recipe=="axe"){State.axes++;message="已制作木斧，可在快捷栏装备";}
            else {State.pickaxes++;message="已制作木镐，可在快捷栏装备";}
            return true;
        }
        public bool PlaceBench(Hex cell,out string message){
            if(State.benches<=0){message="先按E，用6个木头制作工作台";return false;}
            if(!island.Cells.Contains(cell)||cell.Equals(new Hex(0,0))||island.Workshops.Contains(cell)){message="工作台需放在空闲地基，避开泉水";return false;}
            State.benches--;island.Workshops.Add(cell);message="工作台已放置，靠近后按E制作斧子和镐";return true;
        }
        public int HarvestAmount(string kind){
            if(kind=="food")return 1;
            if(kind=="wood")return BackpackRules.Selected(State)=="axe"?3:1;
            if(kind=="stone")return BackpackRules.Selected(State)=="pickaxe"?2:0;
            return 0;
        }
        public bool CanCollectMany(Dictionary<string,int> gains){foreach(var pair in gains)if(pair.Value<=0||pair.Value>1000000)return false;return BackpackBridge.Fits(island,State,gains);}
        public bool CanCollect(string kind,int amount)=>amount>0&&amount<=1000000&&BackpackBridge.Fits(island,State,new Dictionary<string,int>{{kind,amount}});
        public bool Collect(string kind,int amount){if(!CanCollect(kind,amount))return false;BackpackBridge.Set(island,State,kind,BackpackBridge.Get(island,State,kind)+amount);return true;}
    }
}

using System.Collections.Generic;
using UnityEngine;
namespace SkyIsland {
    public partial class SkyIslandPrototype {
        SurvivalState survival=new SurvivalState();
        SurvivalInventory inventory;
        bool craftOpen;
        readonly List<Transform> movingClouds=new List<Transform>();
        readonly List<ResourceNode> resourceNodes=new List<ResourceNode>();
        class Drop {public Transform visual;public string kind;public int amount;public float born;public GroundItemState saved;}
        readonly List<Drop> drops=new List<Drop>();
        GameObject heldTool;
        void InitSurvival(){inventory=new SurvivalInventory(island,survival);survival=inventory.State;BackpackBridge.Migrate(island,survival);bagOpen=survival.bag.cursor.count>0;SelectSlot(survival.selectedSlot);if(survival.groundItems==null)survival.groundItems=new List<GroundItemState>();survival.groundItems.RemoveAll(d=>d==null||!d.Valid());foreach(var item in survival.groundItems)RenderGroundItem(item,0);}
        void CreateClouds(){var random=new System.Random(417);for(int i=0;i<28;i++){float a=(float)random.NextDouble()*Mathf.PI*2,r=18+(float)random.NextDouble()*34,y=-8+(float)random.NextDouble()*12;var cloud=Primitive("Drifting cloud",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),new Vector3(6+(float)random.NextDouble()*5,1.2f+(float)random.NextDouble(),4+(float)random.NextDouble()*3),Mat("cloud",new Color(.87f,.94f,.96f)));movingClouds.Add(cloud.transform);}}
        void SurvivalTick(){
            foreach(var cloud in movingClouds){var p=cloud.position+new Vector3(-.65f,0,.17f)*Time.deltaTime;if(p.x< -62)p.x+=124;if(p.z>62)p.z-=124;cloud.position=p;}
            for(int i=drops.Count-1;i>=0;i--){var drop=drops[i];drop.visual.Rotate(0,75*Time.deltaTime,0);drop.saved.position=drop.visual.position;if(respawnAt==0&&!settingsOpen&&!craftOpen&&!chestOpen&&!aboard&&motion!=null&&motion.Grounded&&Time.time-drop.born>.35f&&Vector3.Distance(avatar.position,drop.visual.position)<2.3f){int old=BackpackBridge.Get(island,survival,drop.kind);if(!inventory.Collect(drop.kind,drop.amount)){message="背包已满，请腾出空间";continue;}int index=survival.groundItems.IndexOf(drop.saved);survival.groundItems.Remove(drop.saved);if(!Save()){BackpackBridge.Set(island,survival,drop.kind,old);survival.groundItems.Insert(index,drop.saved);continue;}message="拾取"+BackpackRules.Name(drop.kind)+" +"+drop.amount;Destroy(drop.visual.gameObject);drops.RemoveAt(i);}}
        }
        void RenderGroundItem(GroundItemState item,float delay){var visual=Primitive("Dropped "+item.item,PrimitiveType.Cube,item.position,new Vector3(.3f,.3f,.3f),Mat("drop "+item.item,item.item=="food"?new Color(.8f,.3f,.3f):item.item=="stone"?new Color(.6f,.65f,.67f):new Color(.65f,.4f,.2f)));drops.Add(new Drop{visual=visual.transform,kind=item.item,amount=item.count,born=Time.time+delay,saved=item});}
        void AddResourceNode(string name,string kind,Hex cell,Vector3 size){var g=new GameObject(name);g.transform.position=cell.World+Vector3.up*Height(cell);var box=g.AddComponent<BoxCollider>();box.center=new Vector3(0,size.y/2,0);box.size=size;var node=g.AddComponent<ResourceNode>();node.Kind=kind;resourceNodes.Add(node);}
        void Harvest(ResourceNode node){
            if(node==null||aboard||chestOpen||!motion.Grounded||(node.NatureId==0&&Time.time<gatherAt))return;
            var delta=avatar.position-node.transform.position;delta.y=0;if(delta.magnitude>2.7f){message="再靠近一点采集";return;}
            if(node.NatureId>0){HarvestNature(node);return;}
            int amount=inventory.HarvestAmount(node.Kind);if(amount==0){message="矿石需要木镐：靠近工作台按E制作，按3装备";gatherAt=Time.time+.5f;return;}
            gatherAt=Time.time+(BackpackRules.Selected(survival)=="axe"?.35f:.7f);
            if(survival.groundItems.Count>=128){message="地面物品太多，请先拾取";return;}var item=new GroundItemState{item=node.Kind,count=amount,position=node.transform.position+Vector3.up*.6f};survival.groundItems.Add(item);if(!Save()){survival.groundItems.Remove(item);return;}RenderGroundItem(item,0);message=BackpackRules.Name(node.Kind)+"掉落 +"+amount+"，靠近自动拾取";
        }
        void HarvestMouse(Ray ray){if(!Physics.Raycast(ray,out var hit,100))return;var panel=hit.collider.GetComponentInParent<PanelMarker>();if(panel&&PanelDismantle.AxeEquipped(survival)){if(Input.GetMouseButtonDown(0)&&motion.Grounded&&!aboard)RemovePanel(panel.State,2.7f);return;}Harvest(hit.collider.GetComponent<ResourceNode>());}
        void HarvestNearest(){ResourceNode nearest=null;float distance=2.7f;foreach(var node in resourceNodes){if(node==null)continue;var d=avatar.position-node.transform.position;d.y=0;if(d.magnitude<distance){nearest=node;distance=d.magnitude;}}if(nearest==null)message="靠近树木或矿石，鼠标左键采集";else Harvest(nearest);}
        string displayedHeld="";
        void SelectSlot(int slot){survival.selectedSlot=Mathf.Clamp(slot,0,7);string kind=BackpackRules.Selected(survival);displayedHeld=kind;buildMode=kind=="bench"||kind=="panel";if(kind=="panel")mode=3;if(kind=="bench")mode=1;if(heldTool)Destroy(heldTool);heldTool=null;
            bool axe=kind=="axe",pick=kind=="pickaxe";if(!axe&&!pick)return;
            heldTool=new GameObject(axe?"Held wooden axe":"Held wooden pickaxe");heldTool.transform.SetParent(avatar,false);heldTool.transform.localPosition=new Vector3(.65f,.1f,.4f);
            Primitive("Handle",PrimitiveType.Cube,Vector3.zero,new Vector3(.12f,1.1f,.12f),Mat("tool wood",new Color(.48f,.3f,.16f)),heldTool.transform);
            Primitive("Tool head",PrimitiveType.Cube,new Vector3(.16f,.46f,0),axe?new Vector3(.62f,.42f,.2f):new Vector3(1,.18f,.2f),Mat("tool head",new Color(.7f,.48f,.24f)),heldTool.transform);
        }
        void SelectItem(string item){BackpackBridge.Sync(island,survival);for(int i=0;i<8;i++)if(survival.bag.slots[27+i].item==item){SelectSlot(i);return;}}
        void DrawHotbar(float w,float h){float start=w/2-306;for(int i=0;i<8;i++){var stack=survival.bag.slots[BackpackRules.Main+i];GUI.backgroundColor=survival.selectedSlot==i?new Color(.55f,.85f,.75f):Color.white;if(GUI.Button(new Rect(start+i*77,h-92,72,72),(i+1)+"\n"+BackpackRules.Name(stack.item)+(stack.count>1?" ×"+stack.count:""),button)){SelectSlot(i);Save();}}GUI.backgroundColor=Color.white;}
        void DrawCrafting(float w,float h){float x=w/2-250,y=h/2-220;GUI.color=new Color(.95f,.97f,.93f,1);GUI.DrawTexture(new Rect(x,y,500,440),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(x+24,y+18,450,40),"制作 · 木头 "+island.Wood,title);
            GUI.Label(new Rect(x+24,y+65,450,62),inventory.NearBench(Hex.FromWorld(avatar.position))?"已靠近工作台，可制作木斧和木镐":"空手可制作工作台；工具需要靠近已放置的工作台",body);
            if(GUI.Button(new Rect(x+24,y+135,452,42),"制作工作台 · 木头6",button)){if(inventory.Craft("bench",Hex.FromWorld(avatar.position),out message))Save();}
            if(GUI.Button(new Rect(x+24,y+187,452,42),"制作木斧 · 木头3 · 需工作台",button)){if(inventory.Craft("axe",Hex.FromWorld(avatar.position),out message)){SelectItem("axe");Save();}}
            if(GUI.Button(new Rect(x+24,y+239,452,42),"制作木镐 · 木头4 · 需工作台",button)){if(inventory.Craft("pickaxe",Hex.FromWorld(avatar.position),out message)){SelectItem("pickaxe");Save();}}
            if(GUI.Button(new Rect(x+24,y+291,452,42),"制作木板 · 木头2 · 需工作台",button)){if(inventory.Craft("panel",Hex.FromWorld(avatar.position),out message))Save();}
            GUI.Label(new Rect(x+24,y+349,450,38),message,small);if(GUI.Button(new Rect(x+150,y+395,200,32),"返回游戏（E）",button))craftOpen=false;
        }
    }
}

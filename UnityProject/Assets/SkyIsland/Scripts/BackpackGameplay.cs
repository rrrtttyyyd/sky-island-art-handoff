using System;using System.Collections.Generic;using UnityEngine;
namespace SkyIsland {public partial class SkyIslandPrototype {
 bool bagOpen;int bagHover=-1,bagDragOrigin=-1;
 void ToggleBackpack(){if(bagOpen){CloseBackpack();return;}BackpackBridge.Sync(island,survival);bagOpen=true;buildMode=false;touchMove=Vector2.zero;}
 void CloseBackpack(){if(!EditBackpack(b=>BackpackRules.ReturnCursor(b))){message="请先放下手中物品，或检查保存是否成功";return;}bagOpen=false;bagDragOrigin=-1;SelectSlot(survival.selectedSlot);}
 bool EditBackpack(Func<BackpackState,bool> change)=>BackpackBridge.Edit(island,survival,change,Save);
 void DropSelected(){DropBackpackItem(BackpackRules.Main+survival.selectedSlot,false,false);}
 void DropBackpackItem(int slot,bool cursor,bool whole){
  if(aboard||respawnAt>0){message="请先停靠下艇再丢弃物品";return;}if(survival.groundItems.Count>=128){message="地面物品过多，请先拾取";return;}
  var stack=cursor?survival.bag.cursor:slot>=0&&slot<BackpackRules.Size?survival.bag.slots[slot]:null;if(stack==null||stack.count==0)return;
  int amount=whole?stack.count:1;var item=new GroundItemState{item=stack.item,count=amount,position=avatar.position+avatar.forward*.85f+Vector3.up*.15f};survival.groundItems.Add(item);
  bool done=EditBackpack(b=>{var source=cursor?b.cursor:b.slots[slot];if(source.item!=item.item||source.count<amount)return false;source.count-=amount;if(source.count==0)source.item=null;return true;});
  if(!done){survival.groundItems.Remove(item);return;}RenderGroundItem(item,1.5f);message="已丢下"+BackpackRules.Name(item.item)+" ×"+amount;
 }
 void DrawBackpack(float w,float h){
  float x=w/2-350,y=Mathf.Max(8,h/2-285);GUI.color=new Color(.88f,.89f,.86f,1);GUI.DrawTexture(new Rect(x,y,700,570),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.Label(new Rect(x+25,y+14,550,38),"背包",title);if(GUI.Button(new Rect(x+575,y+14,100,36),"关闭（I）",button))CloseBackpack();
  GUI.Box(new Rect(x+25,y+62,155,110),"");GUI.Label(new Rect(x+38,y+74,130,94),"旅人\n当前手持\n"+BackpackRules.Name(BackpackRules.Selected(survival)),body);
  GUI.Label(new Rect(x+205,y+65,460,65),"左键：拿起 / 放下 / 合并 / 交换\n右键：拿半组 / 放一个；Shift：快速转移\n拖到另一格可放下；悬停按1–8换快捷格",small);
  if(GUI.Button(new Rect(x+205,y+133,170,34),"制作配方（E）",button)){CloseBackpack();if(!bagOpen)craftOpen=true;}
  int pending=0;foreach(var stack in survival.bag.overflow)pending+=stack.count;
  if(pending>0&&GUI.Button(new Rect(x+390,y+133,280,34),"领取旧物资暂存 ×"+pending,button)){if(survival.bag.cursor.count>0)message="请先放下手中物品";else {int taken=0;bool claimed=EditBackpack(b=>{foreach(var stack in b.overflow){int before=stack.count;stack.count=BackpackRules.AddPartial(b,stack.item,stack.count);taken+=before-stack.count;}b.overflow.RemoveAll(s=>s.count==0);return taken>0;});message=claimed?"已领取 "+taken+" 件旧物资":"背包无空位，或保存失败未领取";}}
  var mouse=Event.current.mousePosition;bagHover=-1;for(int i=0;i<27;i++)DrawBackpackSlot(new Rect(x+90+i%9*58,y+195+i/9*58,54,54),i,mouse);
  GUI.Label(new Rect(x+90,y+374,520,24),"快捷栏 · 材料每组64，工具每格1",small);for(int i=0;i<8;i++)DrawBackpackSlot(new Rect(x+119+i*58,y+407,54,54),27+i,mouse);
  GUI.Label(new Rect(x+25,y+481,650,30),"Q：丢一个；Ctrl+Q：丢整组。制作沿用本游戏当前配方。",small);GUI.Label(new Rect(x+25,y+518,650,42),message,small);
  var e=Event.current;if(e.type==EventType.KeyDown){if(e.keyCode==KeyCode.Q){DropBackpackItem(bagHover>=0?bagHover:27+survival.selectedSlot,survival.bag.cursor.count>0,e.control);e.Use();}else if(e.keyCode>=KeyCode.Alpha1&&e.keyCode<=KeyCode.Alpha8&&bagHover>=0&&survival.bag.cursor.count==0){int target=27+(int)e.keyCode-(int)KeyCode.Alpha1;int source=bagHover;if(source!=target)EditBackpack(b=>{var swap=b.slots[source];b.slots[source]=b.slots[target];b.slots[target]=swap;return true;});e.Use();}else if(e.keyCode==KeyCode.E){CloseBackpack();if(!bagOpen)craftOpen=true;e.Use();}}
  if(e.type==EventType.MouseUp)bagDragOrigin=-1;
  if(survival.bag.cursor.count>0){GUI.Box(new Rect(mouse.x+12,mouse.y+10,105,48),BackpackRules.Name(survival.bag.cursor.item)+"\n×"+survival.bag.cursor.count,body);}
 }
 void DrawBackpackSlot(Rect rect,int index,Vector2 mouse){var stack=survival.bag.slots[index];bool hover=rect.Contains(mouse);if(hover)bagHover=index;GUI.color=hover?new Color(.98f,.98f,.9f):index==27+survival.selectedSlot?new Color(.67f,.86f,.76f):new Color(.68f,.7f,.67f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;GUI.Box(rect,"");if(stack.count>0){GUI.Label(new Rect(rect.x+3,rect.y+5,rect.width-6,27),BackpackRules.Name(stack.item),small);GUI.Label(new Rect(rect.x+27,rect.y+31,26,21),stack.count.ToString(),small);}var e=Event.current;
  if(hover&&e.type==EventType.MouseDown&&(e.button==0||e.button==1)){int target=index;if(e.shift){EditBackpack(b=>BackpackRules.QuickMove(b,target));bagDragOrigin=-1;}else{bool wasEmpty=survival.bag.cursor.count==0;bool changed=EditBackpack(b=>BackpackRules.Click(b,target,e.button==1));bagDragOrigin=e.button==0&&wasEmpty&&changed&&survival.bag.cursor.count>0?index:-1;}e.Use();}
  else if(hover&&e.type==EventType.MouseUp&&e.button==0&&bagDragOrigin>=0&&bagDragOrigin!=index){int target=index;EditBackpack(b=>BackpackRules.Click(b,target,false));bagDragOrigin=-1;e.Use();}
 }
}}

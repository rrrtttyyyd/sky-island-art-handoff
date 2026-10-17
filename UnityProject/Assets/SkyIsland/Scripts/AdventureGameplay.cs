using UnityEngine;
namespace SkyIsland {public partial class SkyIslandPrototype {
 bool chestOpen,aboard;Transform chestModel,airship;Vector3 dock;float chestCheckAt;
 static readonly Color[] ChestColors={new Color(.65f,.45f,.27f),new Color(.39f,.47f,.5f),new Color(.73f,.79f,.81f),new Color(.79f,.6f,.25f),new Color(.37f,.66f,.73f),new Color(.58f,.46f,.68f)};
 Vector3 ChestPosition=>new Vector3(-.75f,Height(new Hex(0,0))+.05f,-.5f);
 void InitAdventure(){
  if(!SpringChestRules.Valid(survival.springChest)){survival.springChest=new SpringChestState();SpringChestRules.Schedule(survival.springChest,NatureNow,Random.value);}
  if(survival.springChest.pending)RenderChest();
  InitHarbor();dock=DefaultBerth();airship=new GameObject("Little airship · limited local flight").transform;airship.position=dock;if(survival.harbor.hasParked&&LandingAt(survival.harbor.parked,out var restoredBerth,out _))airship.position=restoredBerth;
  Primitive("Airship hull",PrimitiveType.Cube,Vector3.zero,new Vector3(1.3f,.35f,2.2f),Mat("airship wood",new Color(.52f,.32f,.18f)),airship);
  Primitive("Airship balloon",PrimitiveType.Sphere,new Vector3(0,2,0),new Vector3(2.2f,1.5f,3.5f),Mat("airship cream",new Color(.9f,.76f,.47f)),airship);
  for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Primitive("Airship rope",PrimitiveType.Cylinder,new Vector3(x*.5f,1,z*.75f),new Vector3(.035f,1,.035f),Mat("airship rope",new Color(.28f,.24f,.18f)),airship);
  Primitive("Airship fin",PrimitiveType.Cube,new Vector3(0,1.8f,-1.75f),new Vector3(.12f,1,1.1f),Mat("airship fin",new Color(.32f,.55f,.59f)),airship);
 }
 void RenderChest(){if(chestModel)Destroy(chestModel.gameObject);chestModel=new GameObject("Spring chest · "+SpringChestRules.Names[survival.springChest.tier]).transform;chestModel.position=ChestPosition;var color=ChestColors[survival.springChest.tier];Primitive("Chest body",PrimitiveType.Cube,new Vector3(0,.28f,0),new Vector3(.8f,.55f,.65f),Mat("chest tier "+survival.springChest.tier,color),chestModel);Primitive("Chest lid",PrimitiveType.Cube,new Vector3(0,.62f,0),new Vector3(.85f,.16f,.7f),Mat("chest tier "+survival.springChest.tier,color),chestModel);Primitive("Chest lock",PrimitiveType.Cube,new Vector3(0,.42f,.34f),new Vector3(.14f,.2f,.07f),Mat("chest lock",new Color(.94f,.81f,.49f)),chestModel);}
 bool ChestNear()=>!aboard&&motion!=null&&motion.Grounded&&Vector3.Distance(avatar.position,ChestPosition+Vector3.up*.65f)<2.7f;
 bool ShipNear()=>Vector3.Distance(avatar.position,airship.position)<3.6f;
 void AdventureTick(){
  if(Time.time<chestCheckAt)return;chestCheckAt=Time.time+1;
  var s=survival.springChest;if(s==null||s.pending||NatureNow<s.nextAt)return;
  if(SpringChestRules.Spawn(s,NatureNow,Random.value,Random.Range(1,int.MaxValue))){if(!Save()){s.pending=false;return;}RenderChest();message="泉水旁出现了"+SpringChestRules.Names[s.tier]+"，靠近按G挑战消除";}
 }
 void AdventureInput(){
  if(Input.GetKeyDown(KeyCode.G)&&survival.springChest.pending&&ChestNear()){chestOpen=true;buildMode=false;touchMove=Vector2.zero;}
  if(Input.GetKeyDown(KeyCode.F))ToggleAirship();
 }
 void Fly(Vector3 direction){float up=(Input.GetKey(KeyCode.Space)?1:0)-(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)?1:0);airship.position=AirshipRules.Clamp(airship.position+(direction*AirshipRules.Speed+Vector3.up*up*3)*Mathf.Min(Time.deltaTime,.1f),island.Cells);if(direction.sqrMagnitude>.01f)airship.rotation=Quaternion.LookRotation(direction);avatar.position=airship.position+Vector3.up*.65f;avatar.rotation=airship.rotation;}
 void DrawAdventure(float w,float h){
  if(chestOpen){DrawChest(w,h);return;}
  if(settingsOpen||craftOpen||resetConfirm)return;
  string hint=aboard?"小飞艇 · WASD飞行 / 空格上升 / Ctrl下降 / F停靠下艇\n范围：地基外20米，高度3–16米":ShipNear()?"F：登上小飞艇":"";
  if(survival.springChest.pending&&ChestNear())hint+="\nG：挑战泉水宝箱 · "+SpringChestRules.Names[survival.springChest.tier];
  GUI.Label(new Rect(w/2-270,h-160,540,60),hint,body);
  if(Application.isMobilePlatform){if(GUI.Button(new Rect(w-160,130,140,40),aboard?"停靠下艇":"登上飞艇",button)){ToggleAirship();}if(survival.springChest.pending&&ChestNear()&&GUI.Button(new Rect(w-160,175,140,40),"泉水宝箱",button))chestOpen=true;if(aboard){if(GUI.RepeatButton(new Rect(w-160,220,65,45),"升",button))airship.position=AirshipRules.Clamp(airship.position+Vector3.up*3*Time.deltaTime,island.Cells);if(GUI.RepeatButton(new Rect(w-90,220,65,45),"降",button))airship.position=AirshipRules.Clamp(airship.position-Vector3.up*3*Time.deltaTime,island.Cells);}}
 }
 void DrawChest(float w,float h){
  var s=survival.springChest;float x=w/2-260,y=h/2-305;GUI.Box(new Rect(x,y,520,610),"");GUI.Label(new Rect(x+20,y+12,480,36),SpringChestRules.Names[s.tier]+" · 消除挑战",title);GUI.Label(new Rect(x+20,y+55,480,58),"点击两个及以上相邻同色块；18步内达标。失败免费重试。\n已消除 "+s.score+"/"+SpringChestRules.Goal(s)+" · 剩余 "+s.moves+"步",body);
  string[] symbols={"●","◆","▲","■","✦"};for(int i=0;i<36;i++){GUI.backgroundColor=ChestColors[(s.board[i]+1)%6];if(GUI.Button(new Rect(x+56+i%6*68,y+120+i/6*58,62,52),symbols[s.board[i]],button)){string before=JsonUtility.ToJson(s);if(SpringChestRules.Clear(s,i)&&!Save())survival.springChest=JsonUtility.FromJson<SpringChestState>(before);}}GUI.backgroundColor=Color.white;
  GUI.Label(new Rect(x+20,y+475,480,45),"奖励：木材 "+SpringChestRules.Wood(s)+" / 石材 "+SpringChestRules.Stone(s)+" / 食材 "+SpringChestRules.Food(s),small);
  if(s.score>=SpringChestRules.Goal(s)){if(GUI.Button(new Rect(x+25,y+520,225,42),"领取奖励",button)){string result=SpringChestRules.Claim(s,island,inventory,NatureNow,Random.value,Save);if(result=="complete"){chestOpen=false;Destroy(chestModel.gameObject);message="宝箱奖励已入包，下个宝箱3–8分钟后出现";}else message=result=="full"?"背包不足，请清理后再领取":"保存失败，请重试";}}
  else if(s.moves==0&&GUI.Button(new Rect(x+25,y+520,225,42),"免费重试",button)){string before=JsonUtility.ToJson(s);SpringChestRules.Puzzle(s);if(!Save())survival.springChest=JsonUtility.FromJson<SpringChestState>(before);}
  if(GUI.Button(new Rect(x+270,y+520,225,42),"暂时离开（保留进度）",button)){chestOpen=false;Save();}GUI.Label(new Rect(x+20,y+570,480,30),message,small);
 }
}}

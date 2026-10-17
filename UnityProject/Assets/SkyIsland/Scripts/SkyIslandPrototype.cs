using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkyIsland {
    public partial class SkyIslandPrototype : MonoBehaviour {
        public Material SharedPrototypeMaterial;
        public Font InterfaceFont;
        public GameObject[] NatureSamples;
        public Material NatureFoliageMaterial;
        HexIsland island;
        Camera view;
        Transform avatar,tiles,buildings,preview;
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        readonly Dictionary<Hex,GameObject> tileObjects=new Dictionary<Hex,GameObject>();
        float yaw=32,pitch=52,zoom=15,gatherAt;
        Vector3 cameraPan=Vector3.zero;
        int mode;
        bool resetConfirm,settingsOpen,toolsOpen=true,buildMode;
        ExplorerMotor motion; float respawnAt;
        string message="先走走看看，再将地基向云海扩展。";
        Hex hover; bool hasHover;
        Vector2 touchMove;
        GUIStyle title,body,small,button;
        Font font;
        string SavePath=>Path.Combine(Application.persistentDataPath,"sky-island-offline-v1.json");
        const string BrowserSaveKey="sky-island-offline-v1";
        Color ink=new Color(.13f,.22f,.26f);
        void Awake(){
#if !UNITY_WEBGL
            if(NetworkRouteProbe.TryRun()){enabled=false;return;}
#endif
            Application.targetFrameRate=60;
            QualitySettings.vSyncCount=0;
            island=new HexIsland();
            Vector3 savedPlayer=new Hex(0,1).World;
            if(!HasArg("-skyFresh"))try{string json=Application.platform==RuntimePlatform.WebGLPlayer?PlayerPrefs.GetString(BrowserSaveKey,""):(File.Exists(SavePath)?File.ReadAllText(SavePath):"");if(!string.IsNullOrEmpty(json)){var save=JsonUtility.FromJson<IslandSave>(json);if(HexIsland.TryRestore(save,out var restored)){island=restored;survival=save.survival??new SurvivalState();savedPlayer=save.player;message="已恢复本地灰盒存档";}else message="存档校验未通过，已使用新岛";}}catch(Exception){message="旧存档无法读取，已使用新岛";}
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.66f,.79f,.84f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.66f,.83f,.89f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=35;RenderSettings.fogEndDistance=90;
            var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.color=new Color(1,.92f,.77f);light.transform.rotation=Quaternion.Euler(45,-30,0);light.shadows=LightShadows.Soft;
            view=new GameObject("Island camera").AddComponent<Camera>();view.tag="MainCamera";view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.66f,.83f,.89f);view.fieldOfView=43;view.nearClipPlane=.1f;view.farClipPlane=180;
            view.gameObject.AddComponent<AudioListener>();
            tiles=new GameObject("Hex foundations").transform; buildings=new GameObject("Buildings").transform;
            foreach(var h in island.Cells)CreateTile(h);
            CreateCore();CreateResources();foreach(var h in island.Workshops)CreateWorkshop(h);
            CreateExplorer();InitSurvival();InitPanels();
            var start=Hex.FromWorld(savedPlayer);if(!island.Cells.Contains(start))savedPlayer=Vector3.zero;
            avatar.position=new Vector3(savedPlayer.x,FloorAt(savedPlayer)??(Height(Hex.FromWorld(savedPlayer))+.65f),savedPlayer.z);
            motion=new ExplorerMotor(avatar.position);InitPets();InitAdventure();CreateNatureSamples();
            preview=new GameObject("Placement preview").transform;
            for(int i=0;i<6;i++){var segment=Primitive("Edge",PrimitiveType.Cube,Vector3.zero,Vector3.one,Mat("preview",new Color(.97f,.83f,.4f)),preview);segment.transform.localScale=new Vector3(Hex.Radius,.035f,.045f);float a=(i*60+60)*Mathf.Deg2Rad;segment.transform.localPosition=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Hex.Radius*.866f;segment.transform.localRotation=Quaternion.Euler(0,-i*60,0);}
            preview.gameObject.SetActive(false);
            CreateClouds();
            for(int i=0;i<5;i++){float a=i*1.7f;var distant=Primitive("Distant island",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*44,-1+i*.6f,Mathf.Sin(a)*44),new Vector3(6,1.3f,5),Mat("far",new Color(.45f,.64f,.66f)));}
            UpdateCamera();
            if(HasArg("-skyScreenshot"))StartCoroutine(Capture());
        }
        bool HasArg(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0;
        IEnumerator Capture(){yield return new WaitForSeconds(4);yield return new WaitForEndOfFrame();var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-skyScreenshot");if(index>=0&&index+1<args.Length){ScreenCapture.CaptureScreenshot(args[index+1]);Debug.Log("SKY_SCREENSHOT_REQUESTED "+args[index+1]);}yield return new WaitForSeconds(2);Application.Quit();}
        Vector3 FountainSpawn()=>new Vector3(.45f,Height(new Hex(0,0))+.65f,0);
        float? FloorAt(Vector3 p){var cell=Hex.FromWorld(p);float? ground=island.Cells.Contains(cell)&&Height(cell)+.65f<=p.y+.2f?(float?)(Height(cell)+.65f):null;var panel=PanelRules.Floor(survival.placedPanels??new List<PanelState>(),p);var net=harborReady?DockRules.Floor(survival.harbor.docks,p):null;if(net.HasValue&&(!ground.HasValue||net>ground))ground=net;return panel.HasValue&&(!ground.HasValue||panel>ground)?panel:ground;}
        float Height(Hex h)=>.35f+(h.Equals(new Hex(0,0))?.12f:0);
        Material Mat(string name,Color color){if(materials.TryGetValue(name,out var m))return m;if(SharedPrototypeMaterial==null)throw new InvalidOperationException("Prototype material missing: run Sky Island/Prepare prototype scene");m=new Material(SharedPrototypeMaterial){color=color};m.SetFloat("_Glossiness",.05f);materials[name]=m;return m;}
        GameObject Primitive(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material,Transform parent=null){var g=GameObject.CreatePrimitive(type);g.name=name;if(parent){g.transform.SetParent(parent,false);g.transform.localPosition=pos;}else g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;var collider=g.GetComponent<Collider>();if(collider)Destroy(collider);return g;}
        void CreateTile(Hex h){
            var g=new GameObject($"Hex {h.q},{h.r}");g.transform.parent=tiles;g.transform.position=h.World;
            var v=new List<Vector3>();var top=new List<int>();var side=new List<int>();
            float y=Height(h),radius=Hex.Radius;
            v.Add(new Vector3(0,y,0));for(int i=0;i<6;i++){float a=(30+i*60)*Mathf.Deg2Rad;v.Add(new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius));}
            for(int i=0;i<6;i++){top.Add(0);top.Add((i+1)%6+1);top.Add(i+1);int k=v.Count;var a=v[i+1];var b=v[(i+1)%6+1];v.Add(a);v.Add(b);v.Add(new Vector3(a.x*.73f,-1.25f,a.z*.73f));v.Add(new Vector3(b.x*.73f,-1.25f,b.z*.73f));side.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            var mesh=new Mesh{name="Hex prism"};mesh.SetVertices(v);mesh.subMeshCount=2;mesh.SetTriangles(top,0);mesh.SetTriangles(side,1);mesh.RecalculateNormals();
            g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{Mat("grass"+(Math.Abs(h.q+h.r)%3),Color.Lerp(new Color(.43f,.65f,.43f),new Color(.59f,.75f,.47f),(Math.Abs(h.q+h.r)%3)*.35f)),Mat("rock",new Color(.35f,.42f,.44f))};
            g.AddComponent<MeshCollider>().sharedMesh=mesh;tileObjects[h]=g;
        }
        void CreateCore(){var p=new Vector3(0,Height(new Hex(0,0)),0);Primitive("Spring basin",PrimitiveType.Cylinder,p+Vector3.up*.12f,new Vector3(1.35f,.12f,1.35f),Mat("ivory",new Color(.87f,.86f,.72f)));Primitive("Spring water",PrimitiveType.Cylinder,p+Vector3.up*.25f,new Vector3(1.12f,.025f,1.12f),Mat("water",new Color(.23f,.72f,.86f)));var crystal=Primitive("Island heart",PrimitiveType.Cube,p+Vector3.up*1.15f,new Vector3(.25f,.42f,.25f),Mat("crystal",new Color(.28f,.79f,.82f)));crystal.transform.rotation=Quaternion.Euler(0,45,25);}
        void CreateResources(){var berries=new Hex(1,0);Primitive("Berry bush",PrimitiveType.Sphere,berries.World+Vector3.up*.75f,new Vector3(.65f,.7f,.65f),Mat("berries",new Color(.66f,.3f,.43f)));AddResourceNode("Berry hitbox","food",berries,new Vector3(.7f,1,.7f));}
        void CreateWorkshop(Hex h){var p=h.World+Vector3.up*Height(h);Primitive("Workbench legs",PrimitiveType.Cube,p+Vector3.up*.3f,new Vector3(.85f,.6f,.55f),Mat("workshop",new Color(.77f,.56f,.34f)),buildings);Primitive("Workbench top",PrimitiveType.Cube,p+Vector3.up*.78f,new Vector3(1.4f,.18f,1.05f),Mat("roof",new Color(.27f,.43f,.49f)),buildings);}
        void Update(){
            if(inventory!=null){BackpackBridge.Sync(island,survival);if(Input.GetKeyDown(KeyCode.I)&&!chestOpen&&!settingsOpen&&respawnAt==0){craftOpen=false;ToggleBackpack();}if(bagOpen){HidePanelPreview();if(dockGhost)dockGhost.gameObject.SetActive(false);if(Input.GetKeyDown(KeyCode.Escape))CloseBackpack();return;}if(displayedHeld!=BackpackRules.Selected(survival))SelectSlot(survival.selectedSlot);if(Input.GetKeyDown(KeyCode.Q)&&!buildMode&&!settingsOpen&&!craftOpen&&!chestOpen&&!aboard)DropSelected();}

            HidePanelPreview();if(dockGhost)dockGhost.gameObject.SetActive(false);SurvivalTick();NatureTick();AdventureTick();PetTick();
            if(respawnAt>0){preview.gameObject.SetActive(false);if(Time.time>=respawnAt){motion.Respawn(FountainSpawn());avatar.position=motion.Position;cameraPan=Vector3.zero;respawnAt=0;message="已在岛心泉水复活";Save();}UpdateCamera();return;}
            if(chestOpen){preview.gameObject.SetActive(false);if(Input.GetKeyDown(KeyCode.Escape)){chestOpen=false;Save();}return;}
            if(Input.GetKeyDown(KeyCode.Escape)){if(craftOpen)craftOpen=false;else settingsOpen=!settingsOpen;craftOpen=false;resetConfirm=false;}
            if(Input.GetKeyDown(KeyCode.E)){craftOpen=!craftOpen;settingsOpen=false;}
            if(settingsOpen||craftOpen){touchMove=Vector2.zero;preview.gameObject.SetActive(false);return;}
            AdventureInput();if(chestOpen){preview.gameObject.SetActive(false);return;}
            if(!aboard&&Input.GetKeyDown(KeyCode.B))buildMode=!buildMode;for(int i=0;i<8;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))){SelectSlot(i);Save();}
            float horizontal=((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0))+touchMove.x,vertical=((Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0))+touchMove.y;var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;var right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;var direction=Vector3.ClampMagnitude(forward*vertical+right*horizontal,1);
            if(aboard)Fly(direction);else {motion.Step(direction,Input.GetKeyDown(KeyCode.Space),Time.deltaTime,FloorAt,p=>PanelRules.Blocked(survival.placedPanels,p),p=>PanelRules.Ceiling(survival.placedPanels,p),Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));avatar.position=motion.Position;if(direction.sqrMagnitude>.01f)avatar.rotation=Quaternion.LookRotation(direction);}
            if(!aboard&&motion.IsDead){respawnAt=Time.time+.5f;buildMode=false;resetConfirm=false;message="坠落死亡，正在返回岛心泉水…";preview.gameObject.SetActive(false);UpdateCamera();return;}

            float uiScale=Mathf.Max(.7f,Mathf.Min(Screen.width/1280f,Screen.height/720f));var guiPoint=new Vector2(Input.mousePosition.x/uiScale,(Screen.height-Input.mousePosition.y)/uiScale);float guiWidth=Screen.width/uiScale,guiHeight=Screen.height/uiScale;
            bool overUI=resetConfirm||(Application.isMobilePlatform&&new Rect(guiWidth-170,120,160,160).Contains(guiPoint))||new Rect(20,18,430,44).Contains(guiPoint)||new Rect(20,70,430,toolsOpen?394:44).Contains(guiPoint)||new Rect(guiWidth-360,18,340,42).Contains(guiPoint)||new Rect(guiWidth/2-310,guiHeight-96,620,84).Contains(guiPoint)||(Application.isMobilePlatform&&((guiPoint.x<205&&guiPoint.y>guiHeight-280)||(guiPoint.x>guiWidth-160&&guiPoint.y>guiHeight-175)));
            if(!resetConfirm){
                if(Input.GetMouseButton(1)&&!overUI){yaw+=Input.GetAxisRaw("Mouse X")*3;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*3,12,85);}
                if(Input.GetMouseButton(2)&&!overUI){var panRight=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;var panForward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;cameraPan-= (panRight*Input.GetAxisRaw("Mouse X")+panForward*Input.GetAxisRaw("Mouse Y"))*zoom*.035f;cameraPan.x=Mathf.Clamp(cameraPan.x,-45,45);cameraPan.z=Mathf.Clamp(cameraPan.z,-45,45);}
                if(!overUI)zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y*Mathf.Max(.4f,zoom*.08f),4,40);
                if(Input.GetKeyDown(KeyCode.C))cameraPan=Vector3.zero;
                if(Input.GetKeyDown(KeyCode.R)){cameraPan=Vector3.zero;yaw=32;pitch=52;zoom=15;}
            }
            yaw=Mathf.Repeat(yaw,360);UpdateCamera();
            var ray=view.ScreenPointToRay(Input.mousePosition);var plane=new Plane(Vector3.up,Vector3.zero);hasHover=plane.Raycast(ray,out float distance);if(hasHover)hover=Hex.FromWorld(ray.GetPoint(distance));
            bool navigating=Input.GetMouseButton(1)||Input.GetMouseButton(2);
            preview.gameObject.SetActive(buildMode&&mode<3&&hasHover&&!overUI&&!navigating);if(hasHover){preview.position=hover.World+Vector3.up*(Height(hover)+.04f);}
            PanelInput(ray,overUI||aboard,navigating);DockInput(ray,overUI,navigating);
            if(!aboard&&buildMode&&mode<3&&Input.GetMouseButtonDown(0)&&!overUI&&hasHover&&!navigating){
                if(mode==0){if(DockRules.Blocks(survival.harbor.docks,hover)){message="船坞占用了这条岛边，请先拆除船坞";}else if(island.Place(hover,out message)){CreateTile(hover);Save();}}
                else if(mode==1){if(DockRules.Blocks(survival.harbor.docks,hover)||(!aboard&&Hex.FromWorld(airship.position).Equals(hover))){message="船坞或飞艇占用了这里，请先腾空";}else if(inventory.PlaceBench(hover,out message)){CreateWorkshop(hover);ClearNatureCell(hover);Save();}}
                else if(DockRules.Blocks(survival.harbor.docks,hover)||(!aboard&&Hex.FromWorld(airship.position).Equals(hover))){message="请先移走飞艇并拆除船坞";}else if(!CanRemovePanelGround(hover)){message="此地基支撑板件，请先拆除板件";}else if(!inventory.CanCollectMany(new Dictionary<string,int>{{"wood",2},{"stone",1}})){message="背包空间不足，无法拆除返还";}else if(island.Remove(hover,Hex.FromWorld(avatar.position),out message)){ClearNatureCell(hover);Destroy(tileObjects[hover]);tileObjects.Remove(hover);Save();}
            }
            if(!aboard&&!buildMode&&!overUI&&!navigating&&Input.GetMouseButton(0))HarvestMouse(ray);
            if(Input.GetKeyDown(KeyCode.F5))Save();
        }
        void UpdateCamera(){var rotation=Quaternion.Euler(pitch,yaw,0);view.transform.position=avatar.position+Vector3.up*.3f+cameraPan+rotation*new Vector3(0,0,-zoom);view.transform.rotation=rotation;}
        bool Save(){try{if(inventory!=null&&!BackpackBridge.Sync(island,survival)){message="背包容量不足或数据不一致，操作未保存";return false;}var snapshot=island.Snapshot(FloorAt(avatar.position).HasValue?avatar.position:FountainSpawn());snapshot.survival=survival;var json=JsonUtility.ToJson(snapshot,true);if(Application.platform==RuntimePlatform.WebGLPlayer){PlayerPrefs.SetString(BrowserSaveKey,json);PlayerPrefs.Save();return true;}var tmp=SavePath+".tmp";File.WriteAllText(tmp,json);if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);return true;}catch(Exception e){message="保存失败："+e.GetType().Name;return false;}}
        void OnApplicationFocus(bool focused){if(!focused&&avatar!=null)Save();}
        void OnApplicationQuit(){if(island!=null&&!HasArg("-skyFresh"))Save();}
        void SetupStyles(){if(title!=null)return;font=InterfaceFont!=null?InterfaceFont:Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);title=new GUIStyle(GUI.skin.label){font=font,fontSize=26,fontStyle=FontStyle.Bold};body=new GUIStyle(GUI.skin.label){font=font,fontSize=16,wordWrap=true};small=new GUIStyle(body){fontSize=13};button=new GUIStyle(GUI.skin.button){font=font,fontSize=16};title.normal.textColor=body.normal.textColor=small.normal.textColor=ink;}
        void OnGUI(){SetupStyles();float scale=Mathf.Max(.7f,Mathf.Min(Screen.width/1280f,Screen.height/720f));GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));float w=Screen.width/scale,h=Screen.height/scale;
            if(bagOpen){DrawBackpack(w,h);return;}
            if(chestOpen){DrawChest(w,h);return;}DrawAdventure(w,h);
            GUI.color=new Color(.95f,.97f,.93f,.95f);GUI.DrawTexture(new Rect(20,18,430,88),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(32,26,410,30),$"地基 {island.Cells.Count}/{HexIsland.Limit}    木材 {island.Wood}    石料 {island.Stone}",body);
            if(respawnAt>0){GUI.Label(new Rect(w/2-210,h/2-30,420,60),"坠落死亡，正在返回岛心泉水…",title);return;}
            if(!settingsOpen&&!craftOpen){DrawHotbar(w,h);DrawPetStatus(w,h);DrawPanelHint(w);}
            if(GUI.Button(new Rect(w-360,18,100,42),"背包（I）",button))ToggleBackpack();
            if(GUI.Button(new Rect(w-250,18,110,42),"制作（E）",button)){craftOpen=!craftOpen;settingsOpen=false;}
            if(craftOpen){DrawCrafting(w,h);return;}
            if(GUI.Button(new Rect(w-130,18,110,42),"设置",button)){settingsOpen=!settingsOpen;craftOpen=false;resetConfirm=false;}
            if(settingsOpen){
                float x=w/2-250,y=h/2-205;
                GUI.color=new Color(.95f,.97f,.93f,1);GUI.DrawTexture(new Rect(x,y,500,410),Texture2D.whiteTexture);GUI.color=Color.white;
                GUI.Label(new Rect(x+25,y+20,440,40),"设置 · 操作与玩法",title);
                GUI.Label(new Rect(x+25,y+75,450,265),"WASD / 方向键：移动    空格：跳跃 / Shift：加速跑\n右键拖动：自由环绕    中键拖动：平移镜头\n滚轮：缩放    镜头仅用鼠标右键拖动\nC：居中跟随    R：重置镜头\nB：进入 / 退出岛屿建造模式\n鼠标按住：树木/石头持续30秒，逐步入包\nI：背包；1–8：快捷栏；Q：丢一个物品；P：宠物状态\nE：制作；先制作并放置工作台，再造工具\nF5 或左上工具栏保存：保存当前浏览器进度\n\n选择木板建造：Q换朝向，PgUp/Dn层高。\n掉入虚空后0.5秒在泉水复活；G：泉水宝箱；F：就地停靠；Ctrl下降；船坞需建在岛边。",body);
                if(GUI.Button(new Rect(x+150,y+345,200,42),"返回游戏",button))settingsOpen=false;
                return;
            }
            if(GUI.Button(new Rect(20,70,430,42),toolsOpen?"岛屿工具栏  ▲ 收起":"岛屿工具栏  ▼ 展开",button))toolsOpen=!toolsOpen;
            if(toolsOpen){
                GUI.color=new Color(.95f,.97f,.93f,.95f);GUI.DrawTexture(new Rect(20,114,430,294),Texture2D.whiteTexture);GUI.color=Color.white;
                string[] tools={"扩建地基","放置工作台","拆除地基"};for(int i=0;i<3;i++){GUI.backgroundColor=buildMode&&mode==i?new Color(.6f,.83f,.75f):Color.white;if(GUI.Button(new Rect(32+i*138,126,130,36),tools[i],button)){mode=i;buildMode=true;}}GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(32,172,198,36),"拼接木板",button)){mode=3;buildMode=true;}if(GUI.Button(new Rect(240,172,198,36),"拆除木板",button)){mode=4;buildMode=true;}
                if(GUI.Button(new Rect(32,218,198,36),"船坞兜网 · Q换边",button)){mode=5;buildMode=true;}if(GUI.Button(new Rect(240,218,198,36),"拆除船坞",button)){mode=6;buildMode=true;}
                if(GUI.Button(new Rect(32,264,198,36),"保存进度",button)){if(Save())message="本地进度已保存";}if(GUI.Button(new Rect(240,264,198,36),"重建新岛",button))resetConfirm=!resetConfirm;
                if(GUI.Button(new Rect(32,310,406,36),buildMode?"退出岛屿建造模式（B）":"进入岛屿建造模式（B）",button))buildMode=!buildMode;
                GUI.Label(new Rect(32,356,400,42),message,small);
            }
            if(Application.isMobilePlatform){touchMove=Vector2.zero;if(GUI.RepeatButton(new Rect(76,h-270,64,54),"前",button))touchMove.y=1;if(GUI.RepeatButton(new Rect(76,h-158,64,44),"后",button))touchMove.y=-1;if(GUI.RepeatButton(new Rect(20,h-214,64,54),"左",button))touchMove.x=-1;if(GUI.RepeatButton(new Rect(132,h-214,64,54),"右",button))touchMove.x=1;if(GUI.RepeatButton(new Rect(w-150,h-170,116,52),"采集",button))HarvestNearest();}

            if(resetConfirm){GUI.color=new Color(.95f,.97f,.93f,1);GUI.DrawTexture(new Rect(w/2-210,h/2-80,420,160),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(new Rect(w/2-190,h/2-60,380,50),"重建灰盒小岛？当前本地进度将重置。",body);if(GUI.Button(new Rect(w/2-180,h/2+10,170,40),"取消",button))resetConfirm=false;if(GUI.Button(new Rect(w/2+10,h/2+10,170,40),"确认重建",button)){if(Application.platform==RuntimePlatform.WebGLPlayer){PlayerPrefs.DeleteKey(BrowserSaveKey);PlayerPrefs.Save();}else if(File.Exists(SavePath))File.Delete(SavePath);UnityEngine.SceneManagement.SceneManager.LoadScene(0);}}
        }
    }
}

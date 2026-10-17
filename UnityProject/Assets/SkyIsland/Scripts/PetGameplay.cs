using UnityEngine;
namespace SkyIsland {
    public partial class SkyIslandPrototype {
        Transform dog,mouse,bird,target;
        float mouseAt,birdAt,dogAt; bool gatherFood; int targetHealth=6; bool petPanel; string scout="笨鸟正在观察前方…",petNews="三位伙伴已加入（原型免费随行）";
        readonly string[] islandKinds={"林木岛","矿脉岛","果园岛","遗迹岛","云泉岛"};
        void InitPets(){
            dog=Pet("讷讷狗",new Color(.72f,.5f,.29f),new Vector3(.65f,.42f,.9f));
            Primitive("Dog muzzle",PrimitiveType.Cube,new Vector3(0,.17f,.48f),new Vector3(.4f,.25f,.32f),Mat("dog cream",new Color(.94f,.81f,.58f)),dog);
            for(int i=-1;i<=1;i+=2)Primitive("Dog ear",PrimitiveType.Cube,new Vector3(i*.26f,.43f,.2f),new Vector3(.18f,.35f,.26f),Mat("dog ears",new Color(.34f,.23f,.15f)),dog);
            mouse=Pet("鼠鼠",new Color(.61f,.65f,.73f),new Vector3(.42f,.3f,.56f));
            for(int i=-1;i<=1;i+=2)Primitive("Mouse ear",PrimitiveType.Sphere,new Vector3(i*.2f,.3f,.13f),new Vector3(.25f,.3f,.12f),Mat("pink",new Color(.89f,.62f,.68f)),mouse);
            bird=Pet("笨鸟",new Color(.88f,.72f,.36f),new Vector3(.43f,.4f,.55f));
            Primitive("Bird beak",PrimitiveType.Cube,new Vector3(0,.03f,.36f),new Vector3(.18f,.14f,.3f),Mat("beak",new Color(.94f,.43f,.2f)),bird);
            for(int i=-1;i<=1;i+=2)Primitive("Wing",PrimitiveType.Cube,new Vector3(i*.37f,0,0),new Vector3(.5f,.1f,.4f),Mat("wing",new Color(.56f,.42f,.26f)),bird);
            target=Primitive("训练靶（非敌人）",PrimitiveType.Cylinder,new Hex(0,-1).World+Vector3.up*.95f,new Vector3(.4f,.6f,.4f),Mat("training",new Color(.75f,.35f,.32f))).transform;
            mouseAt=Time.time+12;birdAt=Time.time+Random.Range(45,76);view.farClipPlane=220;
        }
        Transform Pet(string name,Color color,Vector3 size){var root=new GameObject(name).transform;root.position=avatar.position;Primitive(name+" body",PrimitiveType.Cube,Vector3.zero,size,Mat(name,color),root);for(int i=-1;i<=1;i+=2)Primitive("Eye",PrimitiveType.Sphere,new Vector3(i*size.x*.27f,size.y*.23f,size.z*.5f),Vector3.one*.07f,Mat("pet eye",Color.black),root);return root;}
        void FollowPet(Transform pet,Vector3 goal,float speed){pet.position=Vector3.MoveTowards(pet.position,goal,speed*Time.deltaTime);var delta=goal-pet.position;delta.y=0;if(delta.sqrMagnitude>.05f)pet.rotation=Quaternion.LookRotation(delta);}
        void PetTick(){
            if(Input.GetKeyDown(KeyCode.P))petPanel=!petPanel;
            if(dog==null||respawnAt>0||settingsOpen||craftOpen||chestOpen||aboard||!motion.Grounded)return;
            var home=avatar.position;var dogGoal=home-avatar.forward*.85f+avatar.right*.65f;
            Drop cargo=null;foreach(var drop in drops)if(Vector3.Distance(home,drop.visual.position)<8&&island.Cells.Contains(Hex.FromWorld(drop.visual.position))){cargo=drop;break;}
            if(cargo!=null){dogGoal=cargo.visual.position;if(Vector3.Distance(dog.position,cargo.visual.position)<.65f){cargo.visual.position=Vector3.MoveTowards(cargo.visual.position,home,6*Time.deltaTime);dogGoal=cargo.visual.position;}}
            else if(Vector3.Distance(home,target.position)<3.2f){dogGoal=target.position+Vector3.right*.55f;if(Vector3.Distance(dog.position,target.position)<.9f&&Time.time>dogAt){targetHealth--;dogAt=Time.time+1;petNews="讷讷狗攻击训练靶，剩余耐受 "+targetHealth+"/6";if(targetHealth<=0){targetHealth=6;petNews="训练靶击破，已重置；正式敌人后续接入";}}}
            FollowPet(dog,dogGoal,3.5f);FollowPet(mouse,home-avatar.right*.9f-avatar.forward*.45f,3);
            FollowPet(bird,home+avatar.forward*2+Vector3.up*(2+Mathf.Sin(Time.time*3)*.2f),5);
            foreach(var resource in resourceNodes)if(!Input.GetMouseButton(0)&&resource!=null&&resource.NatureId>0&&resource.Kind=="wood"&&Vector3.Distance(mouse.position,resource.transform.position)<2.7f){HarvestNature(resource,true);break;}
            if(Time.time>=mouseAt){mouseAt=Time.time+12;ResourceNode nearby=null;foreach(var node in resourceNodes)if(node!=null&&node.Kind==(gatherFood?"food":"wood")&&Vector3.Distance(home,node.transform.position)<5){nearby=node;break;}if(nearby!=null){if(nearby.NatureId>0){gatherFood=!gatherFood;}else {inventory.Collect(nearby.Kind,1);gatherFood=!gatherFood;petNews="鼠鼠收集了"+(nearby.Kind=="food"?"食材":"木头")+" +1";Save();}}else petNews="鼠鼠：附近没有可采集的树";}
            if(Time.time>=birdAt){birdAt=Time.time+Random.Range(45,76);string kind=Random.value<.5f?"food":"wood";inventory.Collect(kind,1);petNews="笨鸟带来小惊喜："+(kind=="food"?"食材":"木头")+" +1";Save();}
            float best=-2;int found=0;for(int i=0;i<5;i++){float a=i*1.7f;var pos=new Vector3(Mathf.Cos(a)*44,-1+i*.6f,Mathf.Sin(a)*44);float alignment=Vector3.Dot(avatar.forward,(pos-home).normalized);if(alignment>best){best=alignment;found=i;}}scout="前方侦察："+islandKinds[found]+" · 约 "+Mathf.RoundToInt(Vector3.Distance(home,new Vector3(Mathf.Cos(found*1.7f)*44,0,Mathf.Sin(found*1.7f)*44)))+"米（尚未开放登陆）";
            RenderSettings.fogEndDistance=110;
        }
        void DrawPetStatus(float w,float h){if(!petPanel)return;GUI.Box(new Rect(w-365,74,345,185),"");GUI.Label(new Rect(w-350,86,315,165),"伙伴状态（P收起）\n讷讷狗：随行、搬运、攻击训练靶\n鼠鼠：12秒采集附近木材/食材\n笨鸟：侦察、45–75秒惊喜\n"+scout+"\n"+petNews,small);}
    }
}

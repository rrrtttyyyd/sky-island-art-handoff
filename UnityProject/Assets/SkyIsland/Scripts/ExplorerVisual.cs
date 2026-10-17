using UnityEngine;
namespace SkyIsland { public partial class SkyIslandPrototype {
 Transform leftBoot,rightBoot;Vector3 previousExplorer;
 void CreateExplorer(){avatar=new GameObject("原创云海邮差").transform;
 Primitive("Coat",PrimitiveType.Cube,new Vector3(0,-.02f,0),new Vector3(.5f,.52f,.34f),Mat("coat",new Color(.96f,.56f,.24f)),avatar);
 Primitive("Head",PrimitiveType.Sphere,new Vector3(0,.4f,0),new Vector3(.39f,.4f,.37f),Mat("skin",new Color(.91f,.74f,.55f)),avatar);
 Primitive("Cap",PrimitiveType.Sphere,new Vector3(0,.58f,0),new Vector3(.45f,.19f,.44f),Mat("hat",new Color(.2f,.43f,.43f)),avatar);
 Primitive("Goggles",PrimitiveType.Cube,new Vector3(0,.43f,.18f),new Vector3(.32f,.1f,.08f),Mat("goggles",new Color(.16f,.28f,.31f)),avatar);
 Primitive("Scarf",PrimitiveType.Cube,new Vector3(0,.2f,.02f),new Vector3(.55f,.12f,.4f),Mat("scarf",new Color(.88f,.8f,.57f)),avatar);
 Primitive("Backpack",PrimitiveType.Cube,new Vector3(0,0,-.24f),new Vector3(.34f,.39f,.2f),Mat("pack",new Color(.4f,.29f,.2f)),avatar);
 for(int i=-1;i<=1;i+=2)Primitive("Arm",PrimitiveType.Capsule,new Vector3(i*.33f,-.02f,0),new Vector3(.14f,.24f,.16f),Mat("coat",new Color(.96f,.56f,.24f)),avatar);
 leftBoot=Primitive("Left boot",PrimitiveType.Cube,new Vector3(-.15f,-.4f,.02f),new Vector3(.19f,.3f,.25f),Mat("boot",new Color(.24f,.22f,.21f)),avatar).transform;
 rightBoot=Primitive("Right boot",PrimitiveType.Cube,new Vector3(.15f,-.4f,.02f),new Vector3(.19f,.3f,.25f),Mat("boot",new Color(.24f,.22f,.21f)),avatar).transform;
 }
 void LateUpdate(){if(leftBoot==null)return;float stride=Vector3.Distance(previousExplorer,avatar.position)>.001f&&motion.Grounded?Mathf.Sin(Time.time*13)*.09f:0;leftBoot.localPosition=new Vector3(-.15f,-.4f,.02f+stride);rightBoot.localPosition=new Vector3(.15f,-.4f,.02f-stride);previousExplorer=avatar.position;}
} }

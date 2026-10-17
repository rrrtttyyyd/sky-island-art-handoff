using UnityEngine;
namespace SkyIsland {
 public static class PanelSnap {
  static float Sign(float n)=>n<0?-1:1;
  static Vector3 Normal(PanelState p)=>p.rotation%2==0?Vector3.forward:Vector3.right;
  static Vector3 Facing(Vector3 normal,Vector3 face,Vector3 delta){float side=Vector3.Dot(face,normal);if(Mathf.Abs(side)<.25f)side=Vector3.Dot(delta,normal);return normal*Sign(side);}
  static PanelState At(Vector3 center,int level,bool vertical,int rotation){var offset=vertical?Quaternion.Euler(0,rotation*90,0)*new Vector3(0,0,.5f):Vector3.zero;return new PanelState{x=Mathf.RoundToInt(center.x-offset.x),z=Mathf.RoundToInt(center.z-offset.z),level=level,vertical=vertical,rotation=rotation};}
  public static bool TrySnap(PanelState source,Vector3 hit,Vector3 faceNormal,bool vertical,int rotation,out PanelState result){
   result=null;if(source==null||!source.Valid||rotation<0||rotation>3||float.IsNaN(hit.x)||float.IsNaN(hit.y)||float.IsNaN(hit.z))return false;
   var delta=hit-source.Center;Vector3 center;int level=source.level;
   if(!source.vertical){
    Vector3 edge=vertical?(rotation%2==0?Vector3.forward:Vector3.right):(Mathf.Abs(delta.x)>Mathf.Abs(delta.z)?Vector3.right:Vector3.forward);
    edge*=Sign(Vector3.Dot(delta,edge));center=source.Center+edge*(vertical?.5f:1);if(vertical)center.y+=.5f;
   }else {
    Vector3 width=source.rotation%2==0?Vector3.right:Vector3.forward,normal=Normal(source);float u=Vector3.Dot(delta,width);
    if(vertical){
     if(rotation%2==source.rotation%2){if(Mathf.Abs(delta.y)>=Mathf.Abs(u)){level+=delta.y>=0?1:-1;center=source.Center+Vector3.up*(level-source.level);}else center=source.Center+width*Sign(u);}
     else {normal=Facing(normal,faceNormal,delta);center=source.Center+width*Sign(u)*.5f+normal*.5f;}
    }else {level+=delta.y>=0?1:0;normal=Facing(normal,faceNormal,delta);center=source.Center+normal*.5f;center.y=.45f+level;}
   }
   result=At(center,level,vertical,rotation);return result.Valid;
  }
 }
}

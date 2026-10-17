using System;using System.Collections.Generic;using UnityEngine;
namespace SkyIsland.Authority {
 // Isolated scene projection. Does not mutate the offline backpack or pretend a transport is connected.
 public sealed class FoundationSceneView:MonoBehaviour {
  readonly Dictionary<Hex,GameObject> cells=new Dictionary<Hex,GameObject>();public int Revision{get;private set;}=-1;public int Count=>cells.Count;public Material TileMaterial;string islandId;
  public bool Apply(IslandView view){if(view==null||view.code!="OK"||view.revision<Revision||(islandId!=null&&view.islandId!=islandId)||!HexIsland.TryRestore(new IslandSave{cells=view.cells,wood=view.wood,stone=view.stone},out _))return false;
   var next=new HashSet<Hex>(view.cells);foreach(var old in new List<Hex>(cells.Keys))if(!next.Contains(old)){var obj=cells[old];cells.Remove(old);if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
   foreach(var cell in next)if(!cells.ContainsKey(cell)){var obj=new GameObject("Authoritative hex "+cell.q+","+cell.r);obj.transform.SetParent(transform,false);obj.transform.localPosition=cell.World;var filter=obj.AddComponent<MeshFilter>();var mesh=new Mesh();var vertices=new Vector3[7];for(int i=0;i<6;i++){float a=(60*i+30)*Mathf.Deg2Rad;vertices[i+1]=new Vector3(Mathf.Cos(a)*Hex.Radius,0,Mathf.Sin(a)*Hex.Radius);}var triangles=new int[18];for(int i=0;i<6;i++){triangles[i*3]=0;triangles[i*3+1]=(i+1)%6+1;triangles[i*3+2]=i+1;}mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();filter.sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=TileMaterial;cells.Add(cell,obj);}
   islandId=view.islandId;Revision=view.revision;return true;
  }
  void OnDestroy(){foreach(var child in cells.Values)if(child){var mesh=child.GetComponent<MeshFilter>().sharedMesh;if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
 }
}

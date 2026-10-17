using System;using System.Collections.Generic;using UnityEngine;
namespace SkyIsland {public partial class SkyIslandPrototype {
 readonly Dictionary<Material,Material> natureMaterials=new Dictionary<Material,Material>();
 Material NatureMaterial(Material source){if(natureMaterials.TryGetValue(source,out var cached))return cached;var material=new Material(NatureFoliageMaterial?NatureFoliageMaterial:source);material.CopyPropertiesFromMaterial(source);string texture=source.mainTexture?source.mainTexture.name.ToLowerInvariant():"";if(texture.Contains("leav")||texture.Contains("grass")||texture.Contains("flower")){material.SetFloat("_Mode",1);material.SetFloat("_Cutoff",.4f);material.SetInt("_SrcBlend",1);material.SetInt("_DstBlend",0);material.SetInt("_ZWrite",1);material.EnableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=2450;}natureMaterials[source]=material;return material;}
 readonly Dictionary<int,GameObject> natureObjects=new Dictionary<int,GameObject>();
 System.Random natureRandom;
 double NatureNow=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000d;
 void CreateNatureSamples(){
  if(NatureSamples==null||NatureSamples.Length!=4)return;
  if(!NatureRules.Valid(survival.nature))survival.nature=new NatureWorldState();
  var state=survival.nature;natureRandom=new System.Random(17017+state.sequence);
  state.nodes.RemoveAll(n=>!NatureRules.EmptyCell(n.cell,island,survival.placedPanels));
  if(!state.initialized){state.initialized=true;SpawnNature(NatureRules.Target);NatureRules.Schedule(state,NatureNow,(float)natureRandom.NextDouble());}
  foreach(var n in state.nodes)RenderNature(n);
  Debug.Log("SKY_NATURE_SAMPLES "+state.nodes.Count+" renewable instances at scale "+NatureRules.Scale);
  if(!HasArg("-skyFresh"))Save();
 }
 bool SpawnPositionFree(Vector3 p){
  if(airship&&!aboard&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(airship.position.x,airship.position.z))<1.4f)return false;
  if(avatar&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(avatar.position.x,avatar.position.z))<.65f)return false;
  if(target&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(target.position.x,target.position.z))<.6f)return false;
  foreach(var node in resourceNodes)if(node&&node.NatureId==0&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(node.transform.position.x,node.transform.position.z))<.65f)return false;
  return true;
 }
 int SpawnNature(int requested){
  var cells=new List<Hex>();foreach(var h in island.Cells)if(NatureRules.EmptyCell(h,island,survival.placedPanels))cells.Add(h);
  cells.Sort((a,b)=>a.q!=b.q?a.q.CompareTo(b.q):a.r.CompareTo(b.r));if(cells.Count==0)return 0;
  int added=0;for(int attempt=0;attempt<5000&&added<requested&&survival.nature.nodes.Count<NatureRules.Target;attempt++){
   var cell=cells[natureRandom.Next(cells.Count)];float a=(float)natureRandom.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)natureRandom.NextDouble())*.97f;
   int roll=natureRandom.Next(100),kind=survival.nature.sequence==0?0:survival.nature.sequence==1?1:roll<10?0:roll<25?1:roll<80?2:3;if(attempt>1000)kind=2;
   var node=new NatureSpawn{cell=cell,x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r,yaw=(float)natureRandom.NextDouble()*360,kind=kind,health=NatureRules.Health(kind)};
   if(!SpawnPositionFree(node.Position)||!Hex.FromWorld(node.Position).Equals(cell)||!NatureRules.Add(survival.nature,node))continue;added++;
  }return added;
 }
 void RenderNature(NatureSpawn n){
  Vector3 position=n.Position+Vector3.up*Height(n.cell);
  var root=new GameObject("Nature sample "+n.id+" "+NatureSamples[n.kind].name);root.transform.SetParent(tileObjects[n.cell].transform,true);root.transform.position=position;
  var visual=Instantiate(NatureSamples[n.kind],position,Quaternion.Euler(0,n.yaw,0),root.transform);
  foreach(var c in visual.GetComponentsInChildren<Collider>())c.enabled=false;
  var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new InvalidOperationException("Nature model has no renderer");
  foreach(var renderer in renderers){var slots=renderer.sharedMaterials;for(int k=0;k<slots.Length;k++)slots[k]=NatureMaterial(slots[k]);renderer.sharedMaterials=slots;}
  var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
  float factor=NatureRules.Height(n.kind)/Mathf.Max(.001f,bounds.size.y);visual.transform.localScale*=factor;
  visual.transform.position+=new Vector3((position.x-bounds.center.x)*factor,-(bounds.min.y-position.y)*factor,(position.z-bounds.center.z)*factor);
  var box=root.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up*NatureRules.Height(n.kind)*.5f;float width=n.kind==0?1.15f:n.kind==1?.85f:.35f;box.size=new Vector3(width,NatureRules.Height(n.kind),width);
  var resource=root.AddComponent<ResourceNode>();resource.NatureId=n.id;resource.Kind=NatureRules.Material(n.kind);resourceNodes.Add(resource);natureObjects.Add(n.id,root);
 }
 void NatureTick(){
  if(natureRandom==null||settingsOpen||craftOpen||!NatureRules.Due(survival.nature,NatureNow))return;
  int old=survival.nature.nodes.Count,added=SpawnNature(natureRandom.Next(1,4));NatureRules.Schedule(survival.nature,NatureNow,(float)natureRandom.NextDouble());
  for(int i=old;i<survival.nature.nodes.Count;i++)RenderNature(survival.nature.nodes[i]);
  if(added>0){message="空闲地基上长出了 "+added+" 处新资源";if(!HasArg("-skyFresh"))Save();}
 }
 void RemoveNatureVisual(int id){
  if(!natureObjects.TryGetValue(id,out var root))return;var resource=root.GetComponent<ResourceNode>();resourceNodes.Remove(resource);root.GetComponent<Collider>().enabled=false;Destroy(root);natureObjects.Remove(id);
 }
 void ClearNatureCell(Hex cell){if(survival.nature==null)return;for(int i=survival.nature.nodes.Count-1;i>=0;i--)if(survival.nature.nodes[i].cell.Equals(cell)){RemoveNatureVisual(survival.nature.nodes[i].id);survival.nature.nodes.RemoveAt(i);}}
 int harvestFrame=-1;
 void HarvestNature(ResourceNode resource,bool pet=false){
  var state=survival.nature;var node=state.nodes.Find(n=>n.id==resource.NatureId);if(node==null)return;
  if(node.kind==1&&inventory.HarvestAmount("stone")==0){message="石头需要木镐：靠近工作台按E制作，按3装备";return;}
  // One active-time contribution per frame. Pets share the same work budget rather than multiplying it.
  if(harvestFrame==Time.frameCount)return;harvestFrame=Time.frameCount;
  float seconds=pet?Mathf.Min(Time.deltaTime,.1f):Mathf.Min(Time.deltaTime,.1f);
  int before=node.delivered;string result=NatureRules.Harvest(state,node,inventory,island,seconds,Save);
  if(result=="full"){message="背包容量不足，采集已暂停";return;}if(result=="save-failed")return;
  if(result=="hit"){message=(node.kind==0?"砍伐":"开采")+" "+node.harvestedSeconds.ToString("F1")+"/30秒 · 已入包 "+node.delivered+"/"+NatureRules.Reward(node.kind);return;}
  if(result!="complete")return;RemoveNatureVisual(node.id);message=NatureRules.Reward(node.kind)>0?"采集完成 · 总计 "+NatureRules.Reward(node.kind)+(node.kind==0?" 木材":" 石材"):"已清理植被";if(pet)petNews="鼠鼠："+message;
 }
}}

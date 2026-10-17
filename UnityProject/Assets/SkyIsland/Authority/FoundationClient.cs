using System;using UnityEngine;
namespace SkyIsland.Authority {
 // Transport-independent client state. Never predicts economic writes; retry keeps the exact request.
 [Serializable] public class FoundationClientCheckpoint {public int version=1;public string islandId;public IslandView view;public FoundationCommand pending;}
 public sealed class FoundationClient {
  readonly string islandId;FoundationCommand pending;IslandView view;
  public bool Waiting=>pending!=null;public bool NeedsRefresh{get;private set;}=true;
  public IslandView View=>view==null?null:JsonUtility.FromJson<IslandView>(JsonUtility.ToJson(view));
  public FoundationClient(string islandId){this.islandId=islandId;}
  static FoundationCommand Clone(FoundationCommand c)=>c==null?null:JsonUtility.FromJson<FoundationCommand>(JsonUtility.ToJson(c));
  public bool AcceptSnapshot(IslandView snapshot){
   if(snapshot==null||snapshot.code!="OK"||snapshot.islandId!=islandId||snapshot.revision<0||snapshot.wood<0||snapshot.stone<0||!HexIsland.TryRestore(new IslandSave{cells=snapshot.cells,wood=snapshot.wood,stone=snapshot.stone},out _))return false;
   if(view!=null&&snapshot.revision<view.revision)return false;
   view=JsonUtility.FromJson<IslandView>(JsonUtility.ToJson(snapshot));NeedsRefresh=false;return true;
  }
  public FoundationCommand Begin(Hex cell){if(Waiting||NeedsRefresh||view==null)return null;pending=new FoundationCommand{islandId=islandId,requestId=Guid.NewGuid().ToString("D"),expectedRevision=view.revision,q=cell.q,r=cell.r};return Clone(pending);}
  public string Checkpoint()=>JsonUtility.ToJson(new FoundationClientCheckpoint{islandId=islandId,view=View,pending=Clone(pending)});
  public static FoundationClient Restore(string islandId,string json){var data=JsonUtility.FromJson<FoundationClientCheckpoint>(json);if(data==null||data.version!=1||data.islandId!=islandId)throw new InvalidOperationException("CLIENT_CHECKPOINT_INVALID");var client=new FoundationClient(islandId);if(data.view!=null&&!client.AcceptSnapshot(data.view))throw new InvalidOperationException("CLIENT_CHECKPOINT_INVALID");var c=data.pending;if(c!=null){if(data.view==null||c.islandId!=islandId||c.expectedRevision<0||c.expectedRevision>data.view.revision||c.foundationTypeId!="basic"||Math.Abs((long)c.q)>HexIsland.Limit||Math.Abs((long)c.r)>HexIsland.Limit||c.requestId==null||!System.Text.RegularExpressions.Regex.IsMatch(c.requestId,"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$"))throw new InvalidOperationException("CLIENT_CHECKPOINT_INVALID");client.pending=Clone(c);}client.NeedsRefresh=true;return client;}
  public FoundationCommand Retry()=>Clone(pending);
  public void Disconnected(){NeedsRefresh=true;} // Pending survives: ambiguous success must be resolved by retry.
  public bool Receive(CommandResult result){if(pending==null||result==null||result.requestId!=pending.requestId)return false;
   if(result.code=="STORAGE_UNAVAILABLE")return false;
   string[] terminal={"OK","VERSION_CONFLICT","CELL_EXISTS","ISLAND_CAPACITY","NOT_ADJACENT","INSUFFICIENT_MATERIALS","FORBIDDEN","REQUEST_EXPIRED","RECEIPT_CAPACITY","REVISION_CAPACITY","INVALID_REQUEST"};if(Array.IndexOf(terminal,result.code)<0)return false;
   pending=null;NeedsRefresh=true;return true;
  }
 }
}

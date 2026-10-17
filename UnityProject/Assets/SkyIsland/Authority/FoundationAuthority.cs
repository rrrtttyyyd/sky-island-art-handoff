// Server-side foundation command core. Transport/login integration is deliberately separate.
using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Security.Cryptography;using System.Globalization;using UnityEngine;
namespace SkyIsland.Authority {
 [Serializable] public class AccountState {public string id;public int wood,stone;}
 [Serializable] public class IslandState {public string id,owner;public int revision;public List<string> builders=new List<string>();public Hex[] cells;}
 [Serializable] public class FoundationCommand {public string requestId,islandId,foundationTypeId="basic";public int expectedRevision,q,r;}
 [Serializable] public class CommandResult {public string code,requestId;public int revision,wood,stone,cellCount;}
 [Serializable] public class IslandView {public string code,islandId;public int revision,wood,stone;public Hex[] cells;}
 [Serializable] public class Receipt {public string key,fingerprint;public long expiresAt;public CommandResult result;}
 [Serializable] public class AuthorityState {public int version=1;public List<AccountState> accounts=new List<AccountState>();public List<IslandState> islands=new List<IslandState>();public List<Receipt> receipts=new List<Receipt>();}
 public interface IAuthorityStore {string Load();void Commit(string json);}
 public sealed class FoundationAuthority {
  readonly object gate=new object();readonly IAuthorityStore store;readonly Func<string,string> resolveSession;readonly Func<long> clock;AuthorityState state;bool storageFaulted;
  public const int ReceiptLimit=10000;public const long RetryWindowMs=86400000;
  static T Copy<T>(T value)=>JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
  static bool Id(string id)=>id!=null&&System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-zA-Z0-9_-]{1,96}$");
  static string Fingerprint(FoundationCommand c){string data=c.islandId+"|"+c.expectedRevision.ToString(CultureInfo.InvariantCulture)+"|"+c.q.ToString(CultureInfo.InvariantCulture)+"|"+c.r.ToString(CultureInfo.InvariantCulture)+"|"+c.foundationTypeId;using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(data))).Replace("-","").ToLowerInvariant();}
  public FoundationAuthority(IAuthorityStore store,AuthorityState initial,Func<string,string> resolveSession,Func<long> clock=null){
   this.store=store??throw new ArgumentNullException(nameof(store));this.resolveSession=resolveSession??throw new ArgumentNullException(nameof(resolveSession));this.clock=clock??(()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
   var raw=store.Load();state=raw==null?Copy(initial):JsonUtility.FromJson<AuthorityState>(raw);if(!Valid(state))throw new InvalidOperationException("AUTHORITY_STATE_INVALID");if(raw==null)store.Commit(JsonUtility.ToJson(state));
  }
  static bool Valid(AuthorityState s){
   if(s==null||s.version!=1||s.accounts==null||s.islands==null||s.receipts==null||s.receipts.Count>ReceiptLimit)return false;
   var accounts=new HashSet<string>();foreach(var a in s.accounts)if(a==null||!Id(a.id)||!accounts.Add(a.id)||a.wood<0||a.wood>1000000||a.stone<0||a.stone>1000000)return false;
   var islands=new HashSet<string>();foreach(var i in s.islands){if(i==null||!Id(i.id)||!islands.Add(i.id)||!accounts.Contains(i.owner)||i.revision<0||i.builders==null||i.builders.Any(x=>!accounts.Contains(x))||i.builders.Distinct().Count()!=i.builders.Count||!HexIsland.TryRestore(new IslandSave{cells=i.cells,wood=0,stone=0},out _))return false;}
   var keys=new HashSet<string>();foreach(var r in s.receipts)if(r==null||r.key==null||!keys.Add(r.key)||r.fingerprint==null||!System.Text.RegularExpressions.Regex.IsMatch(r.fingerprint,"^[a-f0-9]{64}$")||r.expiresAt<0||r.result==null||string.IsNullOrEmpty(r.result.code))return false;return true;
  }
  string Account(string session){if(string.IsNullOrEmpty(session))return null;try{var id=resolveSession(session);return Id(id)?id:null;}catch{return null;}}
  static CommandResult Failure(string code,string request=null)=>new CommandResult{code=code,requestId=request};
  public CommandResult Place(string session,FoundationCommand raw){lock(gate){
   // Identity comes from a trusted server resolver, never from fields in the command.
   string actor=Account(session);if(actor==null)return Failure("UNAUTHENTICATED");if(storageFaulted)return Failure("STORAGE_UNAVAILABLE");
   if(raw==null||raw.requestId==null||!System.Text.RegularExpressions.Regex.IsMatch(raw.requestId,"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$")||!Guid.TryParseExact(raw.requestId,"D",out var request)||request==Guid.Empty||!Id(raw.islandId)||raw.expectedRevision<0||Math.Abs((long)raw.q)>HexIsland.Limit||Math.Abs((long)raw.r)>HexIsland.Limit||raw.foundationTypeId!="basic")return Failure("INVALID_REQUEST");
   var c=Copy(raw);c.requestId=request.ToString("D");var target=state.islands.Find(i=>i.id==c.islandId);var account=state.accounts.Find(a=>a.id==actor);
   if(target==null||account==null||(target.owner!=actor&&!target.builders.Contains(actor)))return Failure("FORBIDDEN",c.requestId);
   string key=actor+"|"+c.islandId+"|"+c.requestId,fp=Fingerprint(c);var prior=state.receipts.Find(r=>r.key==key);
   if(prior!=null){if(prior.fingerprint!=fp)return Failure("INVALID_REQUEST",c.requestId);if(clock()>=prior.expiresAt)return Failure("REQUEST_EXPIRED",c.requestId);return Copy(prior.result);}
   if(state.receipts.Count>=ReceiptLimit)return Failure("RECEIPT_CAPACITY",c.requestId);
   var next=Copy(state);var island=next.islands.Find(i=>i.id==c.islandId);var stock=next.accounts.Find(a=>a.id==actor);var result=new CommandResult{requestId=c.requestId,revision=island.revision,wood=stock.wood,stone=stock.stone,cellCount=island.cells.Length};
   var cell=new Hex(c.q,c.r);string code;
   if(c.expectedRevision!=island.revision)code="VERSION_CONFLICT";
   else if(island.revision==int.MaxValue)code="REVISION_CAPACITY";
   else if(island.cells.Contains(cell))code="CELL_EXISTS";
   else if(island.cells.Length>=HexIsland.Limit)code="ISLAND_CAPACITY";
   else if(!Hex.Directions.Any(d=>island.cells.Contains(cell+d)))code="NOT_ADJACENT";
   else {
    if(!HexIsland.TryRestore(new IslandSave{cells=island.cells,wood=stock.wood,stone=stock.stone},out var model))throw new InvalidOperationException("AUTHORITY_STATE_INVALID");
    if(!model.Place(cell,out _))code="INSUFFICIENT_MATERIALS";
    else {code="OK";stock.wood=model.Wood;stock.stone=model.Stone;island.cells=model.Snapshot(Vector3.zero).cells;island.revision++;result.wood=stock.wood;result.stone=stock.stone;result.cellCount=island.cells.Length;result.revision=island.revision;}
   }
   result.code=code;next.receipts.Add(new Receipt{key=key,fingerprint=fp,expiresAt=clock()+RetryWindowMs,result=Copy(result)});
   // State and receipt commit together. Storage failure cannot acknowledge success or mutate memory.
   try{store.Commit(JsonUtility.ToJson(next));}catch{storageFaulted=true;return Failure("STORAGE_UNAVAILABLE",c.requestId);}state=next;return Copy(result);
  }}
  public IslandView Snapshot(string session,string islandId){lock(gate){string actor=Account(session);if(actor==null)return new IslandView{code="UNAUTHENTICATED"};if(storageFaulted)return new IslandView{code="STORAGE_UNAVAILABLE"};var island=state.islands.Find(i=>i.id==islandId);var account=state.accounts.Find(a=>a.id==actor);if(island==null||account==null||(island.owner!=actor&&!island.builders.Contains(actor)))return new IslandView{code="FORBIDDEN"};return new IslandView{code="OK",islandId=island.id,revision=island.revision,wood=account.wood,stone=account.stone,cells=(Hex[])island.cells.Clone()};}}
  public CommandResult Read(string session,string islandId){lock(gate){string actor=Account(session);if(actor==null)return Failure("UNAUTHENTICATED");var island=state.islands.Find(i=>i.id==islandId);var account=state.accounts.Find(a=>a.id==actor);if(island==null||account==null||(island.owner!=actor&&!island.builders.Contains(actor)))return Failure("FORBIDDEN");return new CommandResult{code="OK",revision=island.revision,wood=account.wood,stone=account.stone,cellCount=island.cells.Length};}}
 }
}

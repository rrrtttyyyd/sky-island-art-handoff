using System;using System.Collections.Generic;using UnityEngine;
namespace SkyIsland {
 [Serializable] public class SpringChestState {public int version=1,tier,seed,opened,turn,moves=18,score;public double nextAt;public bool pending;public int[] board;}
 public static class SpringChestRules {
  public static readonly string[] Names={"木箱","铁箱","银箱","金箱","钻石箱","顶级宝箱"};
  public static int Tier(float roll){float n=Mathf.Clamp(roll,0,.999999f)*100;int[] weights={50,25,13,8,3,1};for(int i=0;i<6;i++){n-=weights[i];if(n<0)return i;}return 5;}
  public static void Schedule(SpringChestState s,double now,float roll){s.nextAt=now+180+Mathf.Clamp01(roll)*300;}
  public static int Goal(SpringChestState s)=>24+s.tier*5;
  public static bool Valid(SpringChestState s){if(s==null||s.version!=1||s.tier<0||s.tier>5||s.opened<0||s.turn<0||s.moves<0||s.moves>18||s.score<0||double.IsNaN(s.nextAt)||double.IsInfinity(s.nextAt)||s.nextAt<0)return false;if(!s.pending)return true;if(s.board==null||s.board.Length!=36)return false;foreach(int v in s.board)if(v<0||v>=(s.tier<3?4:5))return false;return true;}
  public static void Puzzle(SpringChestState s){var random=new System.Random(s.seed);s.board=new int[36];for(int i=0;i<36;i++)s.board[i]=random.Next(s.tier<3?4:5);s.board[1]=s.board[0];s.moves=18;s.turn=0;s.score=0;}
  public static bool Spawn(SpringChestState s,double now,float roll,int seed){if(s.pending||now<s.nextAt)return false;s.pending=true;s.tier=Tier(roll);s.seed=seed;Puzzle(s);return true;}
  public static List<int> Group(int[] board,int index){var list=new List<int>();if(board==null||board.Length!=36||index<0||index>=36)return list;list.Add(index);for(int k=0;k<list.Count;k++){int i=list[k];foreach(int j in new[]{i%6>0?i-1:-1,i%6<5?i+1:-1,i>=6?i-6:-1,i<30?i+6:-1})if(j>=0&&board[j]==board[index]&&!list.Contains(j))list.Add(j);}return list;}
  public static bool Clear(SpringChestState s,int index){if(!s.pending||s.moves<=0||s.score>=Goal(s))return false;var group=Group(s.board,index);if(group.Count<2)return false;foreach(int i in group)s.board[i]=-1;var random=new System.Random(unchecked(s.seed+(++s.turn)*7919));for(int x=0;x<6;x++){var column=new List<int>();for(int y=5;y>=0;y--)if(s.board[y*6+x]>=0)column.Add(s.board[y*6+x]);for(int y=5;y>=0;y--)s.board[y*6+x]=5-y<column.Count?column[5-y]:random.Next(s.tier<3?4:5);}s.moves--;s.score+=group.Count;bool move=false;for(int i=0;i<36;i++)if(Group(s.board,i).Count>=2){move=true;break;}if(!move)s.board[1]=s.board[0];return true;}
  public static int Wood(SpringChestState s)=>20*(1<<s.tier);
  public static int Stone(SpringChestState s)=>10*(1<<s.tier);
  public static int Food(SpringChestState s)=>2*(1<<s.tier);
  public static string Claim(SpringChestState s,HexIsland island,SurvivalInventory inventory,double now,float roll,Func<bool> save){
   if(!s.pending||s.score<Goal(s))return "locked";int w=Wood(s),r=Stone(s),f=Food(s);if(!inventory.CanCollectMany(new Dictionary<string,int>{{"wood",w},{"stone",r},{"food",f}}))return "full";
   int oldWood=island.Wood,oldStone=island.Stone,oldFood=inventory.State.food,oldOpened=s.opened;double oldNext=s.nextAt;
   inventory.Collect("wood",w);inventory.Collect("stone",r);inventory.Collect("food",f);s.pending=false;s.opened++;Schedule(s,now,roll);
   if(!save()){s.pending=true;s.opened=oldOpened;s.nextAt=oldNext;island.Wood=oldWood;island.Stone=oldStone;inventory.State.food=oldFood;return "save-failed";}return "complete";
  }
 }
 public static class AirshipRules {
  public const float Range=20,MinHeight=3,MaxHeight=16,Speed=6;
  public static Vector3 Clamp(Vector3 proposed,IEnumerable<Hex> cells){Vector3 nearest=Vector3.zero;float best=float.MaxValue;foreach(var cell in cells){var p=cell.World;float d=new Vector2(proposed.x-p.x,proposed.z-p.z).sqrMagnitude;if(d<best){best=d;nearest=p;}}var delta=new Vector3(proposed.x-nearest.x,0,proposed.z-nearest.z);delta=Vector3.ClampMagnitude(delta,Range+Hex.Radius);return new Vector3(nearest.x+delta.x,Mathf.Clamp(proposed.y,MinHeight,MaxHeight),nearest.z+delta.z);}
 }
}

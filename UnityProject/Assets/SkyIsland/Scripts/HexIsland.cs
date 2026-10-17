using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyIsland {
    [Serializable] public struct Hex : IEquatable<Hex> {
        public int q, r;
        public Hex(int q, int r) { this.q=q; this.r=r; }
        public bool Equals(Hex b) => q==b.q && r==b.r;
        public override bool Equals(object b) => b is Hex h && Equals(h);
        public override int GetHashCode() => unchecked(q*397 ^ r);
        public static Hex operator +(Hex a, Hex b) => new Hex(a.q+b.q,a.r+b.r);
        public static readonly Hex[] Directions = { new Hex(1,0),new Hex(1,-1),new Hex(0,-1),new Hex(-1,0),new Hex(-1,1),new Hex(0,1) };
        public const float Radius=1.25f;
        public Vector3 World => new Vector3(Mathf.Sqrt(3)*Radius*(q+r*.5f),0,1.5f*Radius*r);
        public static Hex FromWorld(Vector3 p) {
            float q=(Mathf.Sqrt(3)/3*p.x-p.z/3)/Radius, r=(2f/3*p.z)/Radius, s=-q-r;
            int qi=Mathf.RoundToInt(q),ri=Mathf.RoundToInt(r),si=Mathf.RoundToInt(s);
            float dq=Mathf.Abs(qi-q),dr=Mathf.Abs(ri-r),ds=Mathf.Abs(si-s);
            if(dq>dr && dq>ds) qi=-ri-si; else if(dr>ds) ri=-qi-si;
            return new Hex(qi,ri);
        }
        public int Distance(Hex b) => (Math.Abs(q-b.q)+Math.Abs(r-b.r)+Math.Abs(q+r-b.q-b.r))/2;
    }
    [Serializable] public class IslandSave {
        public int version=1, wood=24, stone=16;
        public Hex[] cells;
        public Hex[] workshops;
        public Vector3 player;
        public SurvivalState survival;
    }
    public class HexIsland {
        public readonly HashSet<Hex> Cells=new HashSet<Hex>();
        public readonly HashSet<Hex> Workshops=new HashSet<Hex>();
        public int Wood=0, Stone=0;
        public const int Limit=128;
        public HexIsland() { Cells.Add(new Hex(0,0)); foreach(var h in Hex.Directions) Cells.Add(h); }
        public bool CanPlace(Hex h,out string reason) {
            if(Cells.Contains(h)){reason="这里已有地基";return false;}
            if(Cells.Count>=Limit){reason=$"岛屿地基已达上限{Limit}格";return false;}
            bool adjacent=false; foreach(var d in Hex.Directions) if(Cells.Contains(h+d))adjacent=true;
            if(!adjacent){reason="新地基必须与岛屿相邻";return false;}
            if(Wood<3||Stone<2){reason="材料不足，用鼠标左键靠近资源点采集";return false;}
            reason="放置地基：木材3 / 石料2";return true;
        }
        public bool Place(Hex h,out string reason){if(!CanPlace(h,out reason))return false;Cells.Add(h);Wood-=3;Stone-=2;reason="地基已扩建";return true;}
        public bool Remove(Hex h,Hex player,out string reason) {
            if(!Cells.Contains(h)){reason="请选择已有地基";return false;}
            if(h.Equals(new Hex(0,0))||h.Equals(player)||Workshops.Contains(h)){reason="核心、角色脚下或有工坊的地基不可拆除";return false;}
            var visited=new HashSet<Hex>();var todo=new Queue<Hex>();todo.Enqueue(new Hex(0,0));visited.Add(new Hex(0,0));
            while(todo.Count>0){var c=todo.Dequeue();foreach(var d in Hex.Directions){var n=c+d;if(!n.Equals(h)&&Cells.Contains(n)&&visited.Add(n))todo.Enqueue(n);}}
            if(visited.Count!=Cells.Count-1){reason="拆除会切断岛屿连接";return false;}
            if(Wood>999998||Stone>999999){reason="材料库存已满，请先腾出空间再拆除";return false;}
            Cells.Remove(h);Wood+=2;Stone+=1;reason="拆除返还：木材2 / 石料1";return true;
        }
        public bool BuildWorkshop(Hex h,out string reason){
            if(!Cells.Contains(h)||h.Equals(new Hex(0,0))||Workshops.Contains(h)){reason="工坊需放在空闲地基，避开核心";return false;}
            if(Wood<6||Stone<4){reason="工坊需要木材6 / 石料4";return false;}
            Wood-=6;Stone-=4;Workshops.Add(h);reason="工坊建成（当前仅验证占格与摆放）";return true;
        }
        public IslandSave Snapshot(Vector3 player) => new IslandSave{wood=Wood,stone=Stone,cells=new List<Hex>(Cells).ToArray(),workshops=new List<Hex>(Workshops).ToArray(),player=player};
        public static bool TryRestore(IslandSave save,out HexIsland island){
            island=null;if(save==null||save.version!=1||save.cells==null||save.cells.Length<1||save.cells.Length>Limit||save.wood<0||save.stone<0||save.wood>1000000||save.stone>1000000)return false;
            var candidate=new HexIsland();candidate.Cells.Clear();
            foreach(var h in save.cells)if(Math.Abs((long)h.q)>Limit||Math.Abs((long)h.r)>Limit||!candidate.Cells.Add(h))return false;
            if(!candidate.Cells.Contains(new Hex(0,0)))return false;
            var visited=new HashSet<Hex>();var todo=new Queue<Hex>();todo.Enqueue(new Hex(0,0));visited.Add(new Hex(0,0));
            while(todo.Count>0){var h=todo.Dequeue();foreach(var d in Hex.Directions){var n=h+d;if(candidate.Cells.Contains(n)&&visited.Add(n))todo.Enqueue(n);}}
            if(visited.Count!=candidate.Cells.Count)return false;
            foreach(var h in save.workshops??new Hex[0])if(!candidate.Cells.Contains(h)||h.Equals(new Hex(0,0))||!candidate.Workshops.Add(h))return false;
            candidate.Wood=save.wood;candidate.Stone=save.stone;island=candidate;return true;
        }
    }
}

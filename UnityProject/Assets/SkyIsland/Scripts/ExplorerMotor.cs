using System;
using UnityEngine;
namespace SkyIsland {
    public class ExplorerMotor {
        public const float DeathY=-18f,WalkSpeed=3.2f,SprintSpeed=5.6f;
        public Vector3 Position {get;private set;}
        public float VelocityY {get;private set;}
        public bool Grounded {get;private set;}
        public bool IsDead=>Position.y<DeathY;
        public ExplorerMotor(Vector3 position){Respawn(position);}
        public void Respawn(Vector3 position){Position=position;VelocityY=0;Grounded=true;}
        public void Step(Vector3 direction,bool jump,float dt,Func<Vector3,float?> floorAt,Func<Vector3,bool> blocked=null,Func<Vector3,float?> ceilingAt=null,bool sprint=false){
            dt=Mathf.Clamp(dt,0,.05f);
            var next=Position+Vector3.ClampMagnitude(direction,1)*(sprint&&Grounded?SprintSpeed:WalkSpeed)*dt;
            if(blocked!=null&&blocked(next)){next.x=Position.x;next.z=Position.z;}
            var floor=floorAt(next);
            if(Grounded&&(!floor.HasValue||floor.Value<Position.y-.22f))Grounded=false;
            if(Grounded&&jump){VelocityY=6.2f;Grounded=false;}
            if(Grounded)next.y=floor.Value;
            else{
                VelocityY-=18*dt;next.y=Position.y+VelocityY*dt;
                var ceiling=ceilingAt?.Invoke(new Vector3(next.x,Position.y,next.z));if(VelocityY>0&&ceiling.HasValue&&next.y>ceiling.Value){next.y=ceiling.Value;VelocityY=0;}
                if(floor.HasValue&&VelocityY<=0&&Position.y>=floor.Value-.001f&&next.y<=floor.Value){next.y=floor.Value;VelocityY=0;Grounded=true;}
            }
            Position=next;
        }
    }
}

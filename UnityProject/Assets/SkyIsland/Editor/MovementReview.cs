using System;
using System.IO;
using UnityEngine;
using SkyIsland;
public static class MovementReview {
    public static void Validate(){
        int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL "+name);checks++;};
        Func<Vector3,float?> ledge=p=>p.x<1?(float?)1f:null;
        var m=new ExplorerMotor(new Vector3(0,1,0));
        m.Step(Vector3.zero,true,.02f,ledge);check(!m.Grounded&&m.Position.y>1,"jump leaves ground");
        float velocity=m.VelocityY;m.Step(Vector3.zero,true,.02f,ledge);check(m.VelocityY<velocity,"no midair double jump");
        for(int i=0;i<150;i++)m.Step(Vector3.zero,false,.02f,ledge);
        check(m.Grounded&&Mathf.Abs(m.Position.y-1)<.001f,"land on platform");
        for(int i=0;i<30;i++)m.Step(Vector3.right,false,.02f,ledge);
        check(m.Position.x>1&&!m.Grounded&&m.Position.y<1,"walk off edge and fall");
        for(int i=0;i<150;i++)m.Step(Vector3.zero,false,.02f,ledge);
        check(m.IsDead,"fall crosses death plane");
        m.Respawn(new Vector3(0,1,0));check(!m.IsDead&&m.Grounded&&m.VelocityY==0,"respawn clears falling state");
        var below=new ExplorerMotor(new Vector3(2,-2,0));below.Step(Vector3.zero,false,.02f,ledge);
        for(int i=0;i<30;i++)below.Step(Vector3.left,false,.02f,ledge);
        check(below.Position.x<1&&below.Position.y< -2&&!below.Grounded,"no teleport from island underside");
        Directory.CreateDirectory("../evidence/unity");File.WriteAllText("../evidence/unity/movement-tests.txt",$"PASS {checks} movement assertions; Unity {Application.unityVersion}; UTC {DateTime.UtcNow:o}\nLocal movement model only; not server authority or real-device input verification.\n");
        Debug.Log("SKY_MOVEMENT_PASS "+checks);
    }
}

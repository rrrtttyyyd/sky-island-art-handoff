using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SkyIsland {
    // Separate S0 diagnostic scene. Never treats a handshake as gameplay login.
    public class WebGLRouteProbe : MonoBehaviour {
        string endpoint="ws://127.0.0.1:18766/probe",credential="",status="输入本机试验端点与临时凭据。";
        bool connecting;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void SkyProbeConnect(string target,string endpoint,string token);
        [DllImport("__Internal")] static extern void SkyProbeClose();
        [DllImport("__Internal")] static extern void SkyProbeReport(string code);
#endif
        [Serializable] class Config {public string endpoint;public string token;}
        public void BeginLocalProbe(string json){
#if UNITY_WEBGL && !UNITY_EDITOR
            if(connecting)return;
            try{var config=JsonUtility.FromJson<Config>(json);connecting=true;SkyProbeConnect(gameObject.name,config.endpoint,config.token);}
            catch{connecting=false;SkyProbeReport("INVALID_CONFIG");}
#endif
        }
        void OnGUI(){
            GUI.Label(new Rect(20,20,800,35),"S0 WebGL传输试验 / 非游戏登录");
            GUI.Label(new Rect(20,60,130,25),"本机端点");endpoint=GUI.TextField(new Rect(155,60,600,28),endpoint);
            GUI.Label(new Rect(20,100,130,25),"临时凭据");credential=GUI.PasswordField(new Rect(155,100,600,28),credential,'*',128);
            GUI.enabled=!connecting;
            if(GUI.Button(new Rect(20,145,180,35),"验证握手")){
#if UNITY_WEBGL && !UNITY_EDITOR
                connecting=true;status="连接中…";SkyProbeConnect(gameObject.name,endpoint,credential);
#else
                status="此诊断入口仅用于WebGL构建。";
#endif
            }
            GUI.enabled=true;GUI.Label(new Rect(20,200,1000,80),status);
        }
        [Serializable] class Result {public string code;public string sessionId;}
        public void OnRouteProbeResult(string json){
            connecting=false;credential="";
            try{var result=JsonUtility.FromJson<Result>(json);if(result.code=="OK"&&Guid.TryParseExact(result.sessionId,"N",out _)){status="握手成功；仅传输试验，不代表游戏联机。";Debug.Log("SKY_WEBGL_ROUTE_PASS");}else status="握手未通过："+result.code;
#if UNITY_WEBGL && !UNITY_EDITOR
                SkyProbeReport(result.code=="OK"&&Guid.TryParseExact(result.sessionId,"N",out _)?"OK":"FAILED");
#endif
            }
            catch{status="无效的桥接响应";}
        }
        void OnDestroy(){
#if UNITY_WEBGL && !UNITY_EDITOR
            SkyProbeClose();
#endif
        }
    }
}

// S0 route experiment only. Native loopback TCP; not the production protocol.
#if !UNITY_WEBGL
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SkyIsland {
    public static class NetworkRouteProbe {
        [Serializable] public class Hello { public int version=1; public string token; public string nonce; }
        [Serializable] public class Reply { public int version=1; public string code; public string sessionId; public string nonce; }
        public static bool TryRun(){
            var args=Environment.GetCommandLineArgs();
            bool server=Array.IndexOf(args,"-skyProbeServer")>=0,client=Array.IndexOf(args,"-skyProbeClient")>=0,websocket=Array.IndexOf(args,"-skyWsProbe")>=0;
            if(!server&&!client&&!websocket)return false;
            try{
                string token=Environment.GetEnvironmentVariable("SKY_PROBE_TOKEN");
                if(string.IsNullOrEmpty(token)||token.Length<24)throw new Exception("Missing development credential");
                int index=Array.IndexOf(args,"-skyProbePort");
                int port=index>=0&&index+1<args.Length?int.Parse(args[index+1]):18765;
                if(websocket){
                    string nonce=Guid.NewGuid().ToString("N");
                    var reply=ParseWebSocketReply(ConnectWebSocket(token,nonce).GetAwaiter().GetResult(),nonce);
                    Debug.Log("SKY_WS_PROBE_CLIENT_PASS session="+reply.sessionId);
                }else if(server)Serve(port,token);else Connect(port,token,Array.IndexOf(args,"-skyProbeReject")>=0);
                Application.Quit(0);
            }catch(Exception e){Debug.LogError("SKY_PROBE_FAILED "+e.GetType().Name+": "+e.Message);Application.Quit(1);}
            return true;
        }
        static async Task<string> ConnectWebSocket(string token,string nonce){
            var uri=new Uri(Environment.GetEnvironmentVariable("SKY_PROBE_WS_URL")??"");
            if(uri.Scheme!="ws"||uri.Host!="127.0.0.1"||uri.AbsolutePath!="/probe")throw new Exception("Probe must use loopback WebSocket endpoint");
            using(var timeout=new CancellationTokenSource(6000))using(var socket=new ClientWebSocket()){
                await socket.ConnectAsync(uri,timeout.Token).ConfigureAwait(false);
                string json="{\"version\":1,\"token\":\""+token+"\",\"nonce\":\""+nonce+"\"}";
                // Probe tokens are generated as hex/base64, never user-provided text.
                if(token.IndexOfAny(new[]{'"','\\','\r','\n'})>=0)throw new Exception("Invalid probe credential encoding");
                byte[] payload=Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(new ArraySegment<byte>(payload),WebSocketMessageType.Text,true,timeout.Token).ConfigureAwait(false);
                var data=new byte[4096];int count=0;WebSocketReceiveResult result;
                do{
                    if(count==data.Length)throw new InvalidDataException("Frame exceeds probe limit");
                    result=await socket.ReceiveAsync(new ArraySegment<byte>(data,count,data.Length-count),timeout.Token).ConfigureAwait(false);
                    if(result.MessageType!=WebSocketMessageType.Text)throw new InvalidDataException("Expected text reply");
                    count+=result.Count;
                }while(!result.EndOfMessage);
                // Return JSON to be parsed on the caller's main thread.
                string response=Encoding.UTF8.GetString(data,0,count);
                return response;
            }
        }
        static Reply ParseWebSocketReply(string response,string nonce){
            var reply=JsonUtility.FromJson<Reply>(response);
            if(reply==null||reply.version!=1||reply.nonce!=nonce||reply.code!="OK"||!Guid.TryParseExact(reply.sessionId,"N",out _))throw new Exception("WebSocket response mismatch");
            return reply;
        }
        static void Serve(int port,string token){
            var listener=new TcpListener(IPAddress.Loopback,port);listener.Start();
            Debug.Log("SKY_PROBE_LISTENING loopback:"+port);
            int accepted=0,rejected=0;
            try{
                var deadline=DateTime.UtcNow.AddSeconds(40);
                while(accepted<2||rejected<1){
                    if(DateTime.UtcNow>deadline)throw new TimeoutException("Probe clients did not finish");
                    if(!listener.Pending()){System.Threading.Thread.Sleep(20);continue;}
                    using(var peer=listener.AcceptTcpClient()){
                        peer.ReceiveTimeout=5000;peer.SendTimeout=5000;
                        using(var stream=peer.GetStream()){
                            var request=JsonUtility.FromJson<Hello>(ReadLine(stream));
                            bool valid=request!=null&&request.version==1&&request.token==token&&!string.IsNullOrEmpty(request.nonce)&&request.nonce.Length<=64;
                            var reply=new Reply{code=valid?"OK":"AUTH_REJECTED",sessionId=valid?Guid.NewGuid().ToString("N"):null,nonce=request?.nonce};
                            WriteLine(stream,JsonUtility.ToJson(reply));
                            if(valid){accepted++;Debug.Log("SKY_PROBE_SESSION "+reply.sessionId);}else{rejected++;Debug.Log("SKY_PROBE_AUTH_REJECTED");}
                        }
                    }
                }
            }finally{listener.Stop();}
            Debug.Log("SKY_PROBE_SERVER_PASS accepted="+accepted+" rejected="+rejected);
        }
        static void Connect(int port,string token,bool expectReject){
            using(var peer=new TcpClient()){
                var pending=peer.ConnectAsync(IPAddress.Loopback,port);
                if(!pending.Wait(5000))throw new TimeoutException("Connect timeout");
                pending.GetAwaiter().GetResult();peer.ReceiveTimeout=5000;peer.SendTimeout=5000;
                using(var stream=peer.GetStream()){
                    string nonce=Guid.NewGuid().ToString("N");
                    WriteLine(stream,JsonUtility.ToJson(new Hello{token=expectReject?"invalid-development-token":token,nonce=nonce}));
                    var reply=JsonUtility.FromJson<Reply>(ReadLine(stream));
                    if(reply==null||reply.version!=1||reply.nonce!=nonce)throw new Exception("Response mismatch");
                    if(expectReject){if(reply.code!="AUTH_REJECTED"||!string.IsNullOrEmpty(reply.sessionId))throw new Exception("Bad credential accepted");Debug.Log("SKY_PROBE_REJECTION_PASS");}
                    else{if(reply.code!="OK"||!Guid.TryParseExact(reply.sessionId,"N",out _))throw new Exception("Handshake rejected");Debug.Log("SKY_PROBE_CLIENT_PASS session="+reply.sessionId);}
                }
            }
        }
        static string ReadLine(NetworkStream stream){
            var data=new MemoryStream();
            for(int i=0;i<4096;i++){int b=stream.ReadByte();if(b<0)throw new EndOfStreamException();if(b==10)return Encoding.UTF8.GetString(data.ToArray());data.WriteByte((byte)b);}
            throw new InvalidDataException("Frame exceeds probe limit");
        }
        static void WriteLine(NetworkStream stream,string json){var bytes=Encoding.UTF8.GetBytes(json+"\n");stream.Write(bytes,0,bytes.Length);stream.Flush();}
    }
}
#endif

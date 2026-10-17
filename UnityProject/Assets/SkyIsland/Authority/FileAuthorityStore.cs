#if UNITY_EDITOR || UNITY_SERVER || !UNITY_WEBGL
using System;using System.IO;using System.Text;
namespace SkyIsland.Authority {
 // Small local server prototype store, not a production transaction database.
 public sealed class FileAuthorityStore:IAuthorityStore,IDisposable {
  readonly string file;readonly FileStream lease;bool disposed;
  public FileAuthorityStore(string filename){file=Path.GetFullPath(filename);Directory.CreateDirectory(Path.GetDirectoryName(file));lease=new FileStream(file+".writer",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
  void Check(){if(disposed)throw new ObjectDisposedException(nameof(FileAuthorityStore));}
  public string Load(){Check();return File.Exists(file)?File.ReadAllText(file,Encoding.UTF8):null;}
  public void Commit(string json){Check();string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";try{var bytes=Encoding.UTF8.GetBytes(json);using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public void Dispose(){if(disposed)return;disposed=true;lease.Dispose();}
 }
}
#endif

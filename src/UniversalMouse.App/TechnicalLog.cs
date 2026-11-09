using System.IO;
namespace UniversalMouse.App;
/// <summary>Bounded failure codes only: no exception message, device identity or input history.</summary>
internal static class TechnicalLog
{
 private static readonly object Gate=new();
 public static void Record(string component,int code)
 {
  try
  {
   lock(Gate)
   {
    string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UniversalMouseStudio");Directory.CreateDirectory(directory);
    string path=Path.Combine(directory,"technical.log");if(File.Exists(path)&&new FileInfo(path).Length>65536)File.Move(path,path+".previous",true);
    File.AppendAllText(path,$"{DateTimeOffset.UtcNow:O} {component} 0x{code:X8}{Environment.NewLine}");
   }
  }
  catch(IOException){}catch(UnauthorizedAccessException){}
 }
}

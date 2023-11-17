using OpenTK;
using OpenTK.Graphics;
using OpenTK.Windowing.Desktop;
namespace Plot;

static class Program
{
   static void Main(string[] args)
   {
      using Window plotWindow = new Window(GameWindowSettings.Default, new NativeWindowSettings
      {
         APIVersion = new Version(4, 6),
         Title = "Plot",
         Size = (800,600)
      });

      plotWindow.CenterWindow();
      //var t1 = new Thread(plotWindow.Run);
      //t1.Start();
      plotWindow.Run();

   }

}
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Splat;

namespace StimmingSignalGenerator
{
   internal class Program
   {
      private static AppState appState;
      // Initialization code. Don't use any Avalonia, third-party APIs or any
      // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
      // yet and stuff might break.
      public static void Main(string[] args)
      {
         appState = new AppState();
         Locator.CurrentMutable.RegisterConstant(appState);

         BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
      }

      // Avalonia configuration, don't remove; also used by visual designer.
      public static AppBuilder BuildAvaloniaApp()
          => AppBuilder.Configure<App>()
               .UsePlatformDetect()
               .LogToDebug()
               .UseReactiveUI();
   }
}
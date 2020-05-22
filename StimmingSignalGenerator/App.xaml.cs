using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using Splat;
using StimmingSignalGenerator.MVVM.ViewModels;
using StimmingSignalGenerator.MVVM.Views;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace StimmingSignalGenerator
{
   public class App : Application
   {
      public override void Initialize()
      {
         AvaloniaXamlLoader.Load(this);
      }

      public override void OnFrameworkInitializationCompleted()
      {
         UnhandledExceptionLogging();

         if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
         {
            desktop.MainWindow = new MainWindow()
            {
               DataContext = new MainWindowViewModel().DisposeWith(Disposables)
            };
            Locator.CurrentMutable.RegisterConstant(desktop.MainWindow);
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;

            Observable.FromEventPattern<ControlledApplicationLifetimeExitEventArgs>
               (h => desktop.Exit += h, h => desktop.Exit -= h)
                  .Subscribe(_ =>
                  {
                     Disposables.Dispose();
                  })
                  .DisposeWith(Disposables);
         }

         base.OnFrameworkInitializationCompleted();
      }

      private void UnhandledExceptionLogging()
      {
         var exceptionLog = new LoggerConfiguration()
                   .WriteTo.File("ErrorLog.txt", rollingInterval: RollingInterval.Day)
                   .CreateLogger();

         Observable.FromEventPattern<UnobservedTaskExceptionEventArgs>
            (h => TaskScheduler.UnobservedTaskException += h,
            h => TaskScheduler.UnobservedTaskException -= h)
            .Subscribe(x =>
            {
               if (!x.EventArgs.Observed)
               {
                  exceptionLog.Error(
                           x.EventArgs.Exception,
                           $"Error:Unobserved Exception from TaskScheduler.");
                  x.EventArgs.SetObserved();
               }
            })
            .DisposeWith(Disposables);

         Observable.FromEventPattern<UnhandledExceptionEventHandler, UnhandledExceptionEventArgs>
            (h => AppDomain.CurrentDomain.UnhandledException += h,
            h => AppDomain.CurrentDomain.UnhandledException -= h)
            .Subscribe(x =>
            {
               if (x.EventArgs.IsTerminating)
               {
                  exceptionLog.Fatal(
                        (Exception)x.EventArgs.ExceptionObject,
                        $"Fetal:Unhandle Exception runtime terminating.");
               }
               else
               {
                  exceptionLog.Error(
                        (Exception)x.EventArgs.ExceptionObject,
                        $"Error:Unhandle Exception.");
               }
            })
            .DisposeWith(Disposables);
      }

      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
   }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Splat;
using StimmingSignalGenerator.MVVM.ViewModels;
using StimmingSignalGenerator.MVVM.Views;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
   }
}

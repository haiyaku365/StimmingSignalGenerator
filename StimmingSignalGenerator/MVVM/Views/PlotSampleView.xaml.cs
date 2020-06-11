using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.UiHelper;
using StimmingSignalGenerator.MVVM.ViewModels;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class PlotSampleView : ReactiveUserControlEx<PlotSampleViewModel>
   {
      private StopWatchRenderLoopTask stopWatchRenderLoopTask;
      private readonly IRenderLoop renderLoop;
      private CompositeDisposable plotEnableDisposables;
#if DEBUG
      private TextBlock FpsBlock => this.FindControl<TextBlock>("FpsBlock");
#endif
      public PlotSampleView()
      {
         renderLoop = AvaloniaLocator.Current.GetService<IRenderLoop>();
         this.WhenActivated(disposables =>
         {
            SafeViewModel(vm =>
            {
               stopWatchRenderLoopTask = new StopWatchRenderLoopTask().DisposeWith(disposables);
               renderLoop.Add(stopWatchRenderLoopTask);
               Disposable.Create(() => renderLoop.Remove(stopWatchRenderLoopTask)).DisposeWith(disposables);

               this.WhenAnyValue(x => x.ViewModel.IsPlotEnable)
                  .Subscribe(x =>
                  {
                     if (x)// plot enable, start monitor ui update time
                     {
                        plotEnableDisposables = new CompositeDisposable().DisposeWith(disposables);

                        this.WhenAnyValue(x => x.stopWatchRenderLoopTask.UiUpdateElapsedMilliseconds)
                           .ObserveOn(RxApp.MainThreadScheduler)
                           .Subscribe(_ =>
                           {
#if DEBUG
                              try
                              {
                                 FpsBlock.Text = (1000 / vm.InvalidatePlotPostedElapsedMilliseconds).ToString("D2");
                              }
                              catch (DivideByZeroException) { }
                              catch (Exception) { throw; }
#endif
                              //Set MinPlotUpdateInterval higher if ui take long time to update
                              vm.MinPlotUpdateIntervalMilliseconds = (int)(
                                 stopWatchRenderLoopTask.UiUpdateElapsedMilliseconds +
                                 (stopWatchRenderLoopTask.UiUpdateElapsedMilliseconds - vm.InvalidatePlotPostedElapsedMilliseconds) * 0.5);

                           })
                           .DisposeWith(plotEnableDisposables);

                        stopWatchRenderLoopTask.NeedsUpdate = true;
                     }
                     else
                     {
                        // plot disable, stop monitor ui update time
                        stopWatchRenderLoopTask.NeedsUpdate = false;
                        plotEnableDisposables?.Dispose();
                     }
                  }).DisposeWith(disposables);
            });
         });
         InitializeComponent();
      }

      /// <summary>
      /// this class measure update interval in render loop. 
      /// Add to IRenderLoop and set NeedsUpdate ture to start measuring.
      /// </summary>
      class StopWatchRenderLoopTask : ReactiveObject, IRenderLoopTask, IDisposable
      {
         public long UiUpdateElapsedMilliseconds { get => elapsedMilliseconds; private set => this.RaiseAndSetIfChanged(ref elapsedMilliseconds, value); }
         public bool NeedsUpdate { get => needsUpdate; set => this.RaiseAndSetIfChanged(ref needsUpdate, value); }

         private readonly Stopwatch stopwatch = Stopwatch.StartNew();
         private long elapsedMilliseconds;
         private bool needsUpdate;
         public StopWatchRenderLoopTask()
         {
            NeedsUpdate = false;
            this.WhenAnyValue(x => x.NeedsUpdate)
               .Where(x => !x)
               .Subscribe(_ => stopwatch.Reset())
               .DisposeWith(Disposables);
         }
         public void Render() { }

         public void Update(TimeSpan time)
         {
            UiUpdateElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
         }

         #region Dispose
         protected CompositeDisposable Disposables { get; } = new CompositeDisposable();
         private bool disposedValue;
         protected virtual void Dispose(bool disposing)
         {
            if (!disposedValue)
            {
               if (disposing)
               {
                  // dispose managed state (managed objects)
                  Disposables?.Dispose();
               }

               // free unmanaged resources (unmanaged objects) and override finalizer
               // set large fields to null
               disposedValue = true;
            }
         }

         // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
         // ~PlotSampleViewModel()
         // {
         //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         //     Dispose(disposing: false);
         // }

         public void Dispose()
         {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
         }
         #endregion
      }
      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}
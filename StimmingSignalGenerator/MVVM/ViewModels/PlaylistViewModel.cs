using DynamicData;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.MVVM.UiHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignPlaylistViewModel : DesignViewModelBase
   {
      public static PlaylistViewModel Data
      {
         get
         {
            PrepareAppState();
            var vm = new PlaylistViewModel();
            vm.AddNewTrack();
            vm.AddNewTrack();
            vm.AddNewTrack();

            vm.TrackVMs[0].TimeSpanSecond = 1;

            var count = Constants.DefaultSampleRate / 4;
            float[] buffer = Array.Empty<float>();
            buffer = BufferHelpers.Ensure(buffer, count);

            vm.FinalSample.Read(buffer, 0, count);

            return vm;
         }
      }
   }
   public class PlaylistViewModel : ViewModelBase, IDisposable
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }

      private readonly ReadOnlyObservableCollection<TrackViewModel> trackVMs;
      public ReadOnlyObservableCollection<TrackViewModel> TrackVMs => trackVMs;
      private SourceCache<TrackViewModel, int> TrackVMsSourceCache { get; }

      public TrackViewModel SelectedTrackVM
      {
         get => selectedTrackVM;
         set => this.RaiseAndSetIfChanged(ref selectedTrackVM, value);
      }
      public PlotSampleViewModel PlotSampleViewModel { get; }
      public ISampleProvider FinalSample => PlotSampleViewModel.SampleSignal;
      public AppState AppState { get; }

      private TrackViewModel selectedTrackVM;
      private string name;
      private readonly TimingSwitchSampleProvider timingSwitchSampleProvider;
      public PlaylistViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();

         TrackVMsSourceCache =
            new SourceCache<TrackViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         TrackVMsSourceCache.Connect()
            .OnItemAdded(vm =>
            {
               timingSwitchSampleProvider.AddSample(vm.FinalSample, TimeSpan.FromSeconds(vm.TimeSpanSecond));
               vm
               .WhenAnyValue(x => x.TimeSpanSecond)
               .Subscribe(x => timingSwitchSampleProvider.UpdateTimeSpan(vm.FinalSample, TimeSpan.FromSeconds(x)))
               .DisposeWith(Disposables);
            })
            .OnItemRemoved(vm =>
            {
               timingSwitchSampleProvider.RemoveSample(vm.FinalSample);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out trackVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         timingSwitchSampleProvider = new TimingSwitchSampleProvider();
         PlotSampleViewModel = 
            new PlotSampleViewModel(new PlotSampleProvider(timingSwitchSampleProvider))
            .DisposeWith(Disposables);

         AppState
            .WhenAnyValue(x => x.IsHDPlot)
            .Subscribe(x => PlotSampleViewModel.IsHighDefinition = x)
            .DisposeWith(Disposables);
         AppState
            .WhenAnyValue(x => x.IsPlotEnable)
            .Subscribe(x => PlotSampleViewModel.IsPlotEnable = x)
            .DisposeWith(Disposables);
      }

      public void AddNewTrack()
      {
         TrackVMsSourceCache.AddOrUpdate(
            new TrackViewModel()
            .SetNameAndId("Track", TrackVMsSourceCache)
            .DisposeWith(Disposables)
         );
      }

      public void RemoveTrack(TrackViewModel trackVM)
      {
         TrackVMsSourceCache.Remove(trackVM);
      }

      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
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
      // ~PlaylistViewModel()
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
   }
}

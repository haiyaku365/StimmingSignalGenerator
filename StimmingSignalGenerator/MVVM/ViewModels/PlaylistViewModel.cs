using DynamicData;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
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

      private TrackViewModel selectedTrackVM;
      private string name;
      public TimingSwitchSampleProvider FinalSample { get; }
      public PlaylistViewModel()
      {
         TrackVMsSourceCache =
            new SourceCache<TrackViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         TrackVMsSourceCache.Connect()
            .OnItemAdded(vm =>
            {
               FinalSample.AddSample(vm.FinalSample, TimeSpan.FromSeconds(vm.TimeSpanSecond));
               vm
               .WhenAnyValue(x => x.TimeSpanSecond)
               .Subscribe(x => FinalSample.UpdateTimeSpan(vm.FinalSample, TimeSpan.FromSeconds(x)))
               .DisposeWith(Disposables);
            })
            .OnItemRemoved(vm =>
            {
               FinalSample.RemoveSample(vm.FinalSample);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out trackVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         FinalSample = new TimingSwitchSampleProvider();
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

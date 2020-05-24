using Avalonia;
using NAudio.Wave;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.FileService;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StimmingSignalGenerator.Helper;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignTrackViewModel : DesignViewModelBase
   {
      public static TrackViewModel MonoData => CreateTrackViewModel(GeneratorModeType.Mono);
      public static TrackViewModel StereoData => CreateTrackViewModel(GeneratorModeType.Stereo);
      static TrackViewModel CreateTrackViewModel(GeneratorModeType generatorModeType)
      {
         PrepareAppState();
         return new TrackViewModel { Name = "Track1", GeneratorMode = generatorModeType };
      }
   }
   public class TrackViewModel : ViewModelBase, INamable, IDisposable
   {
      public AppState AppState { get; }
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public bool IsPlaying { get => isPlaying; set => this.RaiseAndSetIfChanged(ref isPlaying, value); }
      public bool IsSelected { get => isSelected; set => this.RaiseAndSetIfChanged(ref isSelected, value); }

      public double TimeSpanSecond { get => timeSpanSecond; set => this.RaiseAndSetIfChanged(ref timeSpanSecond, Math.Round(value, 2)); }
      public List<MultiSignalViewModel> MultiSignalVMs { get; }
      public List<ControlSliderViewModel> VolVMs { get; }
      public GeneratorModeType GeneratorMode { get => generatorMode; set => this.RaiseAndSetIfChanged(ref generatorMode, value); }

      public ISampleProvider FinalSample => sample;

      readonly SwitchingModeSampleProvider sample;
      private GeneratorModeType generatorMode;
      private string name;
      private bool isPlaying;
      private bool isSelected;
      private double timeSpanSecond = 0;

      public TrackViewModel() : this(
         new[] { new MultiSignalViewModel(), new MultiSignalViewModel(), new MultiSignalViewModel() },
         new[] { ControlSliderViewModel.BasicVol, ControlSliderViewModel.BasicVol, ControlSliderViewModel.BasicVol })
      { }
      public TrackViewModel(MultiSignalViewModel[] multiSignalVMs, ControlSliderViewModel[] controlSliderVMs)
      {
         AppState = Locator.Current.GetService<AppState>();

         sample = new SwitchingModeSampleProvider();

         VolVMs = new List<ControlSliderViewModel>();
         MultiSignalVMs = new List<MultiSignalViewModel>();
         SetupVolumeControlSlider(controlSliderVMs);
         SetupSwitchingModeSignal(multiSignalVMs);

         this.WhenAnyValue(x => x.GeneratorMode)
            .Subscribe(x =>
            {
               sample.GeneratorMode = x;
            })
            .DisposeWith(Disposables);
         VolVMs[0].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => sample.MonoLeftVolume = (float)m)
            .DisposeWith(Disposables);
         VolVMs[1].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => sample.MonoRightVolume = (float)m)
            .DisposeWith(Disposables);
         VolVMs[2].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => sample.StereoVolume = (float)m)
            .DisposeWith(Disposables);
      }

      private void SetupSwitchingModeSignal(MultiSignalViewModel[] multiSignalVMs)
      {
         var multiSignalVMsCount = multiSignalVMs.Count();
         switch (multiSignalVMsCount)
         {
            case 1:
               //mono
               GeneratorMode = GeneratorModeType.Mono;
               MultiSignalVMs.Clear();
               MultiSignalVMs.AddRange(new[]
               {
                  multiSignalVMs[0].DisposeWith(Disposables),
                  new MultiSignalViewModel().DisposeWith(Disposables),
                  new MultiSignalViewModel().DisposeWith(Disposables)
               });
               break;
            case 2:
               //stereo
               GeneratorMode = GeneratorModeType.Stereo;
               MultiSignalVMs.Clear();
               MultiSignalVMs.AddRange(new[]
               {
                  new MultiSignalViewModel().DisposeWith(Disposables),
                  multiSignalVMs[0].DisposeWith(Disposables),
                  multiSignalVMs[1].DisposeWith(Disposables),
               });
               break;
            case 3:
               //load all
               MultiSignalVMs.Clear();
               MultiSignalVMs.AddRange(new[]
               {
                  multiSignalVMs[0].DisposeWith(Disposables),
                  multiSignalVMs[1].DisposeWith(Disposables),
                  multiSignalVMs[2].DisposeWith(Disposables),
               });
               break;
            default:
               //somthing wrong
               throw new ApplicationException("somthing wrong in TrackViewModel.SetupMultiSignal(params MultiSignalViewModel[] multiSignalVMs)");
         }
         sample.MonoSampleProvider = MultiSignalVMs.Take(1).Single().SampleSignal;
         sample.StereoSampleProviders = MultiSignalVMs.Skip(1).Select(x => x.SampleSignal);
      }

      private void SetupVolumeControlSlider(ControlSliderViewModel[] controlSliderVMs)
      {
         if (controlSliderVMs == null) controlSliderVMs = Array.Empty<ControlSliderViewModel>();
         VolVMs.Clear();
         switch (controlSliderVMs.Length)
         {
            case 2://mono
               VolVMs.AddRange(
                  new[] {
                     controlSliderVMs[0],
                     controlSliderVMs[1],
                     ControlSliderViewModel.BasicVol
                  });
               break;
            case 1://stereo
               VolVMs.AddRange(
                  new[] {
                     controlSliderVMs[0],
                     ControlSliderViewModel.BasicVol,
                     ControlSliderViewModel.BasicVol
                  });
               break;
            case 3://load all
               VolVMs.AddRange(controlSliderVMs);
               break;
            case 0:
               VolVMs.AddRange(
                  new[] {
                     ControlSliderViewModel.BasicVol,
                     ControlSliderViewModel.BasicVol,
                     ControlSliderViewModel.BasicVol
                  });
               break;
            default:
               //somthing wrong
               throw new ApplicationException("somthing wrong in TrackViewModel.SetVolumesFromPOCOs(POCOs.ControlSlider[] pocoVols)");
         }
      }

      public POCOs.Track ToPOCO()
      {
         IEnumerable<POCOs.MultiSignal> signalPocos;
         IEnumerable<POCOs.ControlSlider> volPocos;
         switch (GeneratorMode)
         {
            case GeneratorModeType.Mono:
               signalPocos = MultiSignalVMs.Take(1).Select(vm => vm.ToPOCO());
               volPocos = VolVMs.Take(2).Select(vm => vm.ToPOCO());
               break;
            case GeneratorModeType.Stereo:
               signalPocos = MultiSignalVMs.Skip(1).Select(vm => vm.ToPOCO());
               volPocos = VolVMs.Skip(2).Take(1).Select(vm => vm.ToPOCO());
               break;
            default:
               throw new ApplicationException("Bad GeneratorMode");
         }
         return new POCOs.Track
         {
            Name = Name,
            MultiSignals = signalPocos.ToList(),
            Volumes = volPocos.ToList(),
            TimeSpanSecond = TimeSpanSecond
         };
      }
      public static TrackViewModel FromPOCO(POCOs.Track poco)
      {
         var vm = new TrackViewModel(
            poco.MultiSignals.Select(x => MultiSignalViewModel.FromPOCO(x)).ToArray(),
            poco.Volumes.Select(x => ControlSliderViewModel.FromPOCO(x)).ToArray()
            );
         vm.name = poco.Name;
         vm.TimeSpanSecond = poco.TimeSpanSecond;
         return vm;
      }
      public async Task CopyToClipboard()
      {
         var poco = this.ToPOCO();
         var json = JsonSerializer.Serialize(poco, new JsonSerializerOptions { WriteIndented = true });
         await Avalonia.Application.Current.Clipboard.SetTextAsync(json);
      }
      public static async Task<TrackViewModel> PasteFromClipboard()
      {
         var json = await Avalonia.Application.Current.Clipboard.GetTextAsync();
         if (string.IsNullOrWhiteSpace(json)) return null;
         try
         {
            var poco = JsonSerializer.Deserialize<POCOs.Track>(json);
            if (typeof(POCOs.Track).GetProperties().All(x => x.GetValue(poco).IsNullOrDefault())) return null;
            return TrackViewModel.FromPOCO(poco);
         }
         catch (JsonException)
         {
            return null;
         }
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
      // ~MainWindowViewModel()
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

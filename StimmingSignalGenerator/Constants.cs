using NAudio.Wave;

namespace StimmingSignalGenerator
{
   static class Constants
   {
      internal static class Wave
      {
         internal const int DefaultSampleRate = 44100;
         internal static readonly WaveFormat DefaultMonoWaveFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(DefaultSampleRate, 1);
         internal static readonly WaveFormat DefaultStereoWaveFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(DefaultSampleRate, 2);
      }

      internal static class ViewModelName
      {
         internal const string AMName = "AM";
         internal const string FMName = "FM";
         internal const string PMName = "PM";
         internal const string ZMName = "ZM";
         internal const string BasicSignalVMName = "Signal";
         internal const string MonoMultiSignalName = "Mono";
         internal const string LeftMultiSignalName = "Left";
         internal const string RightMultiSignalName = "Right";
         internal const string TrackVMName = "Track";
      }

      internal static class File
      {
         internal const string PlaylistNamePrefix = "Playlist";
         internal const string PlaylistDirectoryName = "Playlists";
      }

      internal static class ConfigKey
      {
         internal const string PlaylistVM = "Playlist";
         internal const string IsHideZeroModulation = "HideZeroModulation";
         internal const string WindowWidth = "WindowWidth";
         internal const string WindowHeight = "WindowHeight";
         internal const string IsTimingMode = "TimingMode";
         internal const string IsPlotEnable = "PlotEnable";
         internal const string IsHighDefinition = "HighDefinition";
         internal const string CurrentAudioPlayerType = "AudioPlayerType";
         internal const string MasterVolumeVM = "MasterVolume";
         internal const string Latency = "Latency";
      }
   }
}

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

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
   }
}

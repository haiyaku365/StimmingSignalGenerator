using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   static class Constants
   {
      internal const int DefaultSampleRate = 44100;
      internal static readonly WaveFormat DefaultMonoWaveFormat =
         WaveFormat.CreateIeeeFloatWaveFormat(DefaultSampleRate, 1);
      internal static readonly WaveFormat DefaultStereoWaveFormat =
         WaveFormat.CreateIeeeFloatWaveFormat(DefaultSampleRate, 2);
   }
}

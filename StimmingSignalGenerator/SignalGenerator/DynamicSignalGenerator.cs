using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   class DynamicSignalGenerator : ISampleProvider
   {
      public WaveFormat WaveFormat => SourceSignal.WaveFormat;
      public ISampleProvider SourceSignal { get; set; }

      public DynamicSignalGenerator(ISampleProvider sourceSignal)
      {
         SourceSignal = sourceSignal;
      }

      public int Read(float[] buffer, int offset, int count) => SourceSignal.Read(buffer, offset, count);
   }
}

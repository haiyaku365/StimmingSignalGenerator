using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   public class SwitchingSampleProvider : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }
      public ISampleProvider SampleProvider { get; set; }

      public SwitchingSampleProvider()
      {
         WaveFormat = Constants.Wave.DefaultStereoWaveFormat;
      }

      public int Read(float[] buffer, int offset, int count)
      {
         if (SampleProvider == null)
         {
            Array.Fill(buffer, 0, offset, count);
            return count;
         }
         int read;
         read = SampleProvider.Read(buffer, offset, count);
         return read;
      }
   }
}

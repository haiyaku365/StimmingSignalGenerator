using NAudio.Wave;
using System;

namespace StimmingSignalGenerator.NAudio
{
   public class SwitchingSampleProvider : ISampleProvider
   {

      public WaveFormat WaveFormat { get; }
      public double CrossfadeDuration { get; set; }
      public ISampleProvider SampleProvider
      {
         get => sampleProvider; set
         {
            OldSampleProvider = sampleProvider;
            sampleProvider = value;
            crossfadeSampleProvider.BeginCrossfade(OldSampleProvider, SampleProvider, CrossfadeDuration);
         }
      }

      private ISampleProvider sampleProvider;
      public ISampleProvider OldSampleProvider { get; private set; }
      private readonly CrossfadeSampleProvider crossfadeSampleProvider = new CrossfadeSampleProvider();
      public SwitchingSampleProvider()
      {
         WaveFormat = Constants.Wave.DefaultStereoWaveFormat;
      }
      public void ForceEndCrossfade()
      {
         crossfadeSampleProvider.ForceToEnd();
      }
      public int Read(float[] buffer, int offset, int count)
      {
         if (SampleProvider == null)
         {
            Array.Fill(buffer, 0, offset, count);
            return count;
         }
         int read;
         if (crossfadeSampleProvider.FadeSampleRemain > 0)
         {
            read = crossfadeSampleProvider.Read(buffer, offset, count);
         }
         else
         {
            read = SampleProvider.Read(buffer, offset, count);
         }

         return read;
      }
   }
}

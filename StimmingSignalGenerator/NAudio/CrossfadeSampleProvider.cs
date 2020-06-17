using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.NAudio
{
   class CrossfadeSampleProvider : ISampleProvider
   {
      public WaveFormat WaveFormat { get; } = Constants.Wave.DefaultStereoWaveFormat;
      public double CrossfadeDuration { get; private set; }
      public ISampleProvider SourceSampleFrom { get; private set; }
      public ISampleProvider SourceSampleTo { get; private set; }
      public int FadeSampleRemain => (fadeSampleEndPosition - fadeSamplePosition) * WaveFormat.Channels;

      private readonly object lockObject = new object();
      private int fadeSamplePosition;
      private int fadeSampleEndPosition;
      private float[] sourceSampleFromBuffer;
      private float[] sourceSampleToBuffer;
      public void BeginCrossfade(
         ISampleProvider sourceSampleFrom,
         ISampleProvider sourceSampleTo,
         double crossfadeDuration)
      {
         lock (lockObject)
         {
            SourceSampleFrom = sourceSampleFrom;
            SourceSampleTo = sourceSampleTo;
            CrossfadeDuration = crossfadeDuration;
            fadeSamplePosition = 0;
            fadeSampleEndPosition = (int)(CrossfadeDuration * WaveFormat.SampleRate);
         }
      }
      public void ForceToEnd()
      {
         lock (lockObject)
         {
            fadeSamplePosition = fadeSampleEndPosition;
         }
      }
      public int Read(float[] buffer, int offset, int count)
      {
         int sample = 0;
         int sourceSamplesRead = 0;
         lock (lockObject)
         {
            sourceSampleFromBuffer = BufferHelpers.Ensure(sourceSampleFromBuffer, count);
            sourceSampleToBuffer = BufferHelpers.Ensure(sourceSampleToBuffer, count);

            sourceSamplesRead = SourceSampleFrom.Read(sourceSampleFromBuffer, 0, count);
            SourceSampleTo.Read(sourceSampleToBuffer, 0, count);

            while (sample < sourceSamplesRead)
            {
               for (int ch = 0; ch < WaveFormat.Channels; ch++)
               {
                  var ratio = (float)fadeSamplePosition / fadeSampleEndPosition;
                  buffer[offset + sample] = ((1 - ratio) * sourceSampleFromBuffer[sample]) + (ratio * sourceSampleToBuffer[sample]);
                  sample++;
               }
               if (fadeSamplePosition <= fadeSampleEndPosition)
               {
                  fadeSamplePosition++;
               }
            }
         }
         return sourceSamplesRead;
      }
   }
}

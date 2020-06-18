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
      private float oldFadeInFactor;
      private float fadeInFactor = 1;
      public void BeginCrossfade(
         ISampleProvider sourceSampleFrom,
         ISampleProvider sourceSampleTo,
         double crossfadeDuration)
      {
         lock (lockObject)
         {
            if (sourceSampleFrom == sourceSampleTo)
            {
               //not crossfade same samples
               fadeSamplePosition = fadeSampleEndPosition = 0;
               return;
            }
            SourceSampleFrom = sourceSampleFrom;
            SourceSampleTo = sourceSampleTo;
            CrossfadeDuration = crossfadeDuration;
            oldFadeInFactor = fadeInFactor;
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

            if (SourceSampleFrom == null)
            {
               Array.Fill(sourceSampleFromBuffer, 0, 0, count);
               sourceSamplesRead = count;
            }
            else
            {
               sourceSamplesRead = SourceSampleFrom.Read(sourceSampleFromBuffer, 0, count);
            }
            if (SourceSampleTo == null)
            {
               Array.Fill(sourceSampleToBuffer, 0, 0, count);
            }
            else
            {
               SourceSampleTo.Read(sourceSampleToBuffer, 0, count);
            }

            while (sample < sourceSamplesRead)
            {
               for (int ch = 0; ch < WaveFormat.Channels; ch++)
               {
                  fadeInFactor = (float)fadeSamplePosition / fadeSampleEndPosition;
                  var fadeOutFactor = oldFadeInFactor - fadeInFactor;
                  fadeOutFactor = fadeOutFactor < 0 ? 0 : fadeOutFactor;
                  buffer[offset + sample] =
                     (fadeOutFactor * sourceSampleFromBuffer[sample]) +
                     (fadeInFactor * sourceSampleToBuffer[sample]);
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

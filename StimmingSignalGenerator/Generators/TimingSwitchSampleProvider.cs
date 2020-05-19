using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   /// <summary>
   /// Switch SampleProvider base on time
   /// </summary>
   public class TimingSwitchSampleProvider : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }

      public TimingSwitchSampleProvider()
      {
         WaveFormat = Constants.DefaultStereoWaveFormat;
         timeSpanSampleProviders = new List<TimeSpanSampleProvider>();
      }

      public void AddSample(ISampleProvider sampleProvider, TimeSpan timeSpan)
      {
         lock (timeSpanSampleProviders)
         {
            timeSpanSampleProviders.Add(new TimeSpanSampleProvider { SampleProvider = sampleProvider, TimeSpan = timeSpan });
            currentSamplePosition = 0;
         }
      }

      public void UpdateTimeSpan(ISampleProvider sampleProvider, TimeSpan newTimeSpan)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = timeSpanSampleProviders.SingleOrDefault(x => x.SampleProvider == sampleProvider);
            if (timeSpanSample != null) timeSpanSample.TimeSpan = newTimeSpan;
            currentSamplePosition = 0;
         }
      }

      public void RemoveSample(ISampleProvider sampleProvider)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = timeSpanSampleProviders.SingleOrDefault(x => x.SampleProvider == sampleProvider);
            if (timeSpanSample != null) timeSpanSampleProviders.Remove(timeSpanSample);
            currentSamplePosition = 0;
         }
      }

      public int Read(float[] buffer, int offset, int count)
      {
         if (timeSpanSampleProviders == null || timeSpanSampleProviders.Sum(x => x.SampleSpan) == 0)
         {
            Array.Fill(buffer, 0, offset, count);
            return count;
         }
         int read = 0;
         int sampleSpanEndPosition = 0, sampleIdx = 0;
         int sampleToReadRemain = count;
         lock (timeSpanSampleProviders)
         {
            while (sampleToReadRemain > 0)
            {
               sampleSpanEndPosition += timeSpanSampleProviders[sampleIdx].SampleSpan;
               if (sampleSpanEndPosition >= currentSamplePosition)// found position to read
               {
                  // calc read count
                  // read only until span end position
                  var sampleToRead = sampleSpanEndPosition - currentSamplePosition;
                  // if span end position sitll far away read until full
                  if (sampleToRead > sampleToReadRemain) sampleToRead = sampleToReadRemain;

                  // read sample
                  read += timeSpanSampleProviders[sampleIdx].SampleProvider.Read(buffer, count - sampleToReadRemain + offset, sampleToRead);
                  // update reading status
                  currentSamplePosition += sampleToRead;
                  sampleToReadRemain -= sampleToRead;

                  sampleIdx++; //go read next sample if still need to fill buffer

                  if (sampleIdx >= timeSpanSampleProviders.Count && // if sampleIdx overflow
                     currentSamplePosition >= sampleSpanEndPosition) // and already read to the end
                  {  // then loop to first sample
                     currentSamplePosition = 0;
                     sampleSpanEndPosition = 0;
                     sampleIdx = 0;
                  }
               }
               else
               {
                  sampleIdx++;// position not found. move to next sample.
               }
            }
         }
         return read;
      }
      private class TimeSpanSampleProvider
      {
         public ISampleProvider SampleProvider { get; set; }
         public TimeSpan TimeSpan { get; set; }
         public int SampleSpan => WaveHelper.TimeSpanToSamples(TimeSpan, SampleProvider.WaveFormat);
      }
      private List<TimeSpanSampleProvider> timeSpanSampleProviders;
      private int currentSamplePosition;
      private int TimeSpanToSamples(TimeSpan time) => WaveHelper.TimeSpanToSamples(time, WaveFormat);
      private TimeSpan SamplesToTimeSpan(int samples) => WaveHelper.SamplesToTimeSpan(samples, WaveFormat);
   }

   public static class WaveHelper
   {
      public static int TimeSpanToSamples(TimeSpan time, WaveFormat waveFormat)
      {
         var samples = (int)(time.TotalSeconds * waveFormat.SampleRate) * waveFormat.Channels;
         return samples;
      }

      public static TimeSpan SamplesToTimeSpan(int samples, WaveFormat waveFormat)
      {
         return TimeSpan.FromSeconds((samples / waveFormat.Channels) / (double)waveFormat.SampleRate);
      }
   }
}

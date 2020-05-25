using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   /// <summary>
   /// Switch SampleProvider base on time
   /// </summary>
   public class TimingSwitchSampleProvider : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }
      public event EventHandler<SampleProviderChangedEventArgs> OnSampleProviderChanged;
      public IObservable<EventPattern<SampleProviderChangedEventArgs>> ObservableOnSampleProviderChanged
         => Observable.FromEventPattern<SampleProviderChangedEventArgs>(
               h => OnSampleProviderChanged += h,
               h => OnSampleProviderChanged -= h);

      public event EventHandler<ProgressChangedEventArgs> OnProgressChanged;
      public IObservable<EventPattern<ProgressChangedEventArgs>> ObservableOnProgressChanged
         => Observable.FromEventPattern<ProgressChangedEventArgs>(
               h => OnProgressChanged += h,
               h => OnProgressChanged -= h);

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
            restartState();
         }
      }

      public void MoveSample(int oldIndex, int newIndex)
      {
         lock (timeSpanSampleProviders)
         {
            var item = timeSpanSampleProviders[oldIndex];
            timeSpanSampleProviders.RemoveAt(oldIndex);
            timeSpanSampleProviders.Insert(newIndex, item);
         }
      }

      public void UpdateTimeSpan(ISampleProvider sampleProvider, TimeSpan newTimeSpan)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = timeSpanSampleProviders.SingleOrDefault(x => x.SampleProvider == sampleProvider);
            if (timeSpanSample != null) timeSpanSample.TimeSpan = newTimeSpan;
            restartState();
         }
      }

      public void RemoveSample(ISampleProvider sampleProvider)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = timeSpanSampleProviders.SingleOrDefault(x => x.SampleProvider == sampleProvider);
            if (timeSpanSample != null) timeSpanSampleProviders.Remove(timeSpanSample);
            restartState();
         }
      }

      public int Read(float[] buffer, int offset, int count)
      {
         lock (timeSpanSampleProviders)
         {
            if (timeSpanSampleProviders == null || timeSpanSampleProviders.Sum(x => x.SampleSpan) == 0)
            {
               Array.Fill(buffer, 0, offset, count);
               return count;
            }
            int read = 0;
            int sampleToReadRemain = count;

            while (sampleToReadRemain > 0)
            {
               if (sampleSpanEndPosition > currentSamplePosition)// found position to read
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
                  OnProgressChanged?.Invoke(this,
                     ProgressChangedEventArgs.Create(
                        timeSpanSampleProviders[sampleIdx].SampleProvider,
                        (float)(currentSamplePosition - sampleSpanStartPosition) /
                        (sampleSpanEndPosition - sampleSpanStartPosition)
                     ));
                  InvokeSampleProviderChanged();
               }
               else
               {
                  var oldSampleIdx = sampleIdx;
                  sampleIdx++;// position not found. move to next sample.
                  if (sampleIdx >= timeSpanSampleProviders.Count && // if sampleIdx overflow
                                       currentSamplePosition >= sampleSpanEndPosition) // and already read to the end
                  {  // then loop to first sample
                     restartState();
                     sampleIdx++;
                  }
                  sampleSpanStartPosition = sampleSpanEndPosition;
                  sampleSpanEndPosition += timeSpanSampleProviders[sampleIdx].SampleSpan;
               }
            }
            return read;
         }
      }
      public class SampleProviderChangedEventArgs : EventArgs
      {
         public static SampleProviderChangedEventArgs Create(ISampleProvider sampleProvider)
            => new SampleProviderChangedEventArgs { SampleProvider = sampleProvider };
         public ISampleProvider SampleProvider { get; set; }
      }
      public class ProgressChangedEventArgs : EventArgs
      {
         public static ProgressChangedEventArgs Create(ISampleProvider sampleProvider, float progress)
            => new ProgressChangedEventArgs { SampleProvider = sampleProvider, Progress = progress };
         public ISampleProvider SampleProvider { get; set; }
         /// <summary>
         /// Progress 0 to 1
         /// </summary>
         public float Progress { get; set; }
      }

      private int lastInvokeSampleIdx = -1;
      private void InvokeSampleProviderChanged()
      {
         if (lastInvokeSampleIdx != sampleIdx) // only raise event once per sampleIdx change
         {
            OnSampleProviderChanged?.Invoke(this,
               SampleProviderChangedEventArgs.Create(timeSpanSampleProviders[sampleIdx].SampleProvider));
            lastInvokeSampleIdx = sampleIdx;
         }
      }
      private class TimeSpanSampleProvider
      {
         public ISampleProvider SampleProvider { get; set; }
         public TimeSpan TimeSpan { get; set; }
         public int SampleSpan => WaveHelper.TimeSpanToSamples(TimeSpan, SampleProvider.WaveFormat);
      }
      private List<TimeSpanSampleProvider> timeSpanSampleProviders;
      private int currentSamplePosition = 0;
      private int sampleSpanStartPosition = 0;
      private int sampleSpanEndPosition = 0;
      private int sampleIdx = -1;
      private void restartState()
      {
         sampleSpanEndPosition = currentSamplePosition = 0;
         sampleIdx = -1;
      }
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

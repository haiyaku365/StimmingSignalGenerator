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
            timeSpanSampleProviders.Add(new TimeSpanSampleProvider(sampleProvider, timeSpan));
         }
      }

      public void MoveSample(int oldIndex, int newIndex)
      {
         lock (timeSpanSampleProviders)
         {
            var item = timeSpanSampleProviders[oldIndex];
            timeSpanSampleProviders.RemoveAt(oldIndex);
            timeSpanSampleProviders.Insert(newIndex, item);
            // Move currentSampleIndex
            if (currentSampleIndex == oldIndex)
            {
               currentSampleIndex = newIndex;
            }
            else if (currentSampleIndex == newIndex)
            {
               currentSampleIndex = oldIndex;
            }
         }
      }

      public void UpdateTimeSpan(ISampleProvider sampleProvider, TimeSpan newTimeSpan)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = GetTimeSpanSampleProvider(sampleProvider);
            if (timeSpanSample == null) return;
            int oldSampleSpan = timeSpanSample.SampleSpan;
            timeSpanSample.TimeSpan = newTimeSpan;
            int newSampleSpan = timeSpanSample.SampleSpan;
            if (currentSampleIndex == GetTimeSpanSampleProviderIndex(sampleProvider))
            {
               // Update state if currently playing in this sample
               var sampleSpanDiff = newSampleSpan - oldSampleSpan;
               currentSampleSpanEndPosition += sampleSpanDiff;
            }
         }
      }

      public void RemoveSample(ISampleProvider sampleProvider)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = GetTimeSpanSampleProvider(sampleProvider);
            if (timeSpanSample == null) return;
            var removeIndex = GetTimeSpanSampleProviderIndex(sampleProvider);
            timeSpanSampleProviders.Remove(timeSpanSample);
            if (currentSampleIndex > removeIndex)
            {
               // shift current sample down if currently play above removed one
               currentSampleIndex--;
            }
            else if (currentSampleIndex == removeIndex && timeSpanSampleProviders.Count > 0)
            {
               //if remove last sample than set to 0
               if (currentSampleIndex >= timeSpanSampleProviders.Count)
                  currentSampleIndex = 0;
               // Current sample removed. Set new sampleSpan end position.
               currentSampleSpanEndPosition = timeSpanSampleProviders[currentSampleIndex].SampleSpan;
               // And reset to start position
               currentSampleSpanPosition = 0;
               // Avoid invoke event in lock block to prevent dead lock
               QueueInvokeSampleProviderChanged(
                  timeSpanSampleProviders[currentSampleIndex].SampleProvider);
            }
         }
         ProcessInvokeQueue();
      }

      private TimeSpanSampleProvider GetTimeSpanSampleProvider(ISampleProvider sampleProvider)
         => timeSpanSampleProviders.SingleOrDefault(x => x.SampleProvider == sampleProvider);
      private int GetTimeSpanSampleProviderIndex(ISampleProvider sampleProvider)
         => timeSpanSampleProviders.IndexOf(GetTimeSpanSampleProvider(sampleProvider));

      public int Read(float[] buffer, int offset, int count)
      {
         int read = 0;
         lock (timeSpanSampleProviders)
         {
            if (timeSpanSampleProviders == null || timeSpanSampleProviders.Sum(x => x.SampleSpan) == 0)
            {
               Array.Fill(buffer, 0, offset, count);
               return count;
            }
            int sampleToReadRemain = count;

            while (sampleToReadRemain > 0)
            {
               // continue read if this sample still not reach the end
               if (currentSampleSpanPosition < currentSampleSpanEndPosition)
               {
                  // calc read count
                  // read only until span end position
                  var sampleToRead = currentSampleSpanEndPosition - currentSampleSpanPosition;
                  // if span end position sitll far away read until full
                  if (sampleToRead > sampleToReadRemain) sampleToRead = sampleToReadRemain;

                  // read sample
                  read += timeSpanSampleProviders[currentSampleIndex].SampleProvider.Read(buffer, count - sampleToReadRemain + offset, sampleToRead);
                  // update reading status
                  currentSampleSpanPosition += sampleToRead;
                  sampleToReadRemain -= sampleToRead;

                  // Do event invoke
                  float progress = (float)currentSampleSpanPosition / currentSampleSpanEndPosition;
                  // Avoid invoke event in lock block to prevent dead lock
                  QueueInvokeProgressChanged(
                     timeSpanSampleProviders[currentSampleIndex].SampleProvider,
                     progress);
               }
               else // reach the end of sample. move to next sample.
               {
                  currentSampleIndex++;
                  if (currentSampleIndex >= timeSpanSampleProviders.Count)
                  {
                     // reach last sample go to first sample
                     currentSampleIndex = 0;
                  }
                  // Avoid invoke event in lock block to prevent dead lock
                  QueueInvokeSampleProviderChanged(
                     timeSpanSampleProviders[currentSampleIndex].SampleProvider);
                  //sample changed reset position
                  currentSampleSpanPosition = 0;
                  //set end position
                  currentSampleSpanEndPosition = timeSpanSampleProviders[currentSampleIndex].SampleSpan;
               }
            }
         }

         ProcessInvokeQueue();
         return read;
      }
      public class SampleProviderChangedEventArgs : EventArgs
      {
         public SampleProviderChangedEventArgs(ISampleProvider sampleProvider)
         {
            SampleProvider = sampleProvider;
         }
         public ISampleProvider SampleProvider { get; set; }
      }
      public class ProgressChangedEventArgs : EventArgs
      {
         public ProgressChangedEventArgs(ISampleProvider sampleProvider, float progress)
         {
            SampleProvider = sampleProvider;
            Progress = progress;
         }
         public ISampleProvider SampleProvider { get; set; }
         /// <summary>
         /// Progress 0 to 1
         /// </summary>
         public float Progress { get; set; }
      }

      private readonly Queue<Action> eventInvokeQueue = new Queue<Action>();
      private void QueueInvokeSampleProviderChanged(ISampleProvider sampleProvider)
      {
         eventInvokeQueue.Enqueue(() =>
            OnSampleProviderChanged?.Invoke(this,
               new SampleProviderChangedEventArgs(sampleProvider))
            );
      }
      private void QueueInvokeProgressChanged(ISampleProvider sampleProvider, float progress)
      {
         eventInvokeQueue.Enqueue(() =>
            OnProgressChanged?.Invoke(this,
               new ProgressChangedEventArgs(sampleProvider, progress))
         );
      }
      private void ProcessInvokeQueue()
      {
         while (eventInvokeQueue.Count > 0)
         {
            eventInvokeQueue.Dequeue().Invoke();
         }
      }

      private class TimeSpanSampleProvider
      {
         public TimeSpanSampleProvider(ISampleProvider sampleProvider, TimeSpan timeSpan)
         {
            SampleProvider = sampleProvider;
            TimeSpan = timeSpan;
         }
         public ISampleProvider SampleProvider { get; set; }
         public TimeSpan TimeSpan { get; set; }
         public int SampleSpan => WaveHelper.TimeSpanToSamples(TimeSpan, SampleProvider.WaveFormat);
      }
      private readonly List<TimeSpanSampleProvider> timeSpanSampleProviders;
      private int currentSampleSpanPosition = 0;
      private int currentSampleSpanEndPosition = 0;
      private int currentSampleIndex = -1;
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

using NAudio.Wave;
using StimmingSignalGenerator.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.NAudio
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
      public bool IsShuffleMode { get; set; }
      public double CrossfadeDuration { get; set; }
      public TimingSwitchSampleProvider()
      {
         WaveFormat = Constants.Wave.DefaultStereoWaveFormat;
         timeSpanSampleProviders = new List<TimeSpanSampleProvider>();
         deck = new List<TimeSpanSampleProvider>();
      }

      public void AddSample(ISampleProvider sampleProvider, TimeSpan timeSpan)
      {
         lock (timeSpanSampleProviders)
         {
            var timeSpanSample = new TimeSpanSampleProvider(sampleProvider, timeSpan);
            timeSpanSampleProviders.Add(timeSpanSample);
            deck.Add(timeSpanSample);
            if (timeSpanSampleProviders.Count == 1)
            {
               //first sample added update end position
               currentSampleSpanEndPosition = timeSpanSampleProviders[0].SampleSpan;
            }
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
            if (sampleProvider == crossfadeTo || sampleProvider == crossfadeFrom)
               crossfadeSampleProvider.ForceToEnd();

            var timeSpanSample = GetTimeSpanSampleProvider(sampleProvider);
            if (timeSpanSample == null) return;
            var removeIndex = GetTimeSpanSampleProviderIndex(sampleProvider);
            timeSpanSampleProviders.Remove(timeSpanSample);
            deck.Remove(timeSpanSample);
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

      public void ForceSwitch(ISampleProvider sampleProvider)
         => ForceSwitch(GetTimeSpanSampleProviderIndex(sampleProvider));
      public void ForceSwitch(int index)
      {
         lock (timeSpanSampleProviders)
         {
            if (0 > index || index >= timeSpanSampleProviders.Count) return;
            currentSampleIndex = index;
            currentSampleSpanEndPosition = timeSpanSampleProviders[index].SampleSpan;
            currentSampleSpanPosition = 0;
            QueueInvokeSampleProviderChanged(
               timeSpanSampleProviders[index].SampleProvider);
         }
         ProcessInvokeQueue();
      }

      private readonly CrossfadeSampleProvider crossfadeSampleProvider = new CrossfadeSampleProvider();
      ISampleProvider crossfadeFrom;
      ISampleProvider crossfadeTo;
      private TimeSpanSampleProvider GetTimeSpanSampleProvider(ISampleProvider sampleProvider)
         => timeSpanSampleProviders.SingleOrDefault(x => x.SampleProvider == sampleProvider);
      private int GetTimeSpanSampleProviderIndex(ISampleProvider sampleProvider)
         => timeSpanSampleProviders.IndexOf(GetTimeSpanSampleProvider(sampleProvider));

      public int Read(float[] buffer, int offset, int count)
      {
         int read = 0;
         lock (timeSpanSampleProviders)
         {
            int sampleToReadRemain = count;

            while (sampleToReadRemain > 0)
            {
               var sumSampleSpan = timeSpanSampleProviders.Sum(x => x.SampleSpan);
               // fill 0 when cannot read from source
               if (timeSpanSampleProviders == null ||
                  sumSampleSpan + CrossfadeDuration < 0.1 ||
                  0 > currentSampleIndex || currentSampleIndex >= timeSpanSampleProviders.Count)
               {
                  Array.Fill(buffer, 0, count - sampleToReadRemain + offset, sampleToReadRemain);
                  read += sampleToReadRemain;
                  sampleToReadRemain = 0;
                  break;
               }

               // crossfade
               var fadeSampleRemain = crossfadeSampleProvider.FadeSampleRemain;
               if (fadeSampleRemain > 0)
               {
                  var sampleToRead = fadeSampleRemain;
                  if (sampleToRead > sampleToReadRemain) sampleToRead = sampleToReadRemain;
                  read += crossfadeSampleProvider.Read(buffer, count - sampleToReadRemain + offset, sampleToRead);
                  sampleToReadRemain -= sampleToRead;
               }
               // continue read if this sample still not reach the end
               else if (currentSampleSpanPosition < currentSampleSpanEndPosition)
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
                  var crossfadeFromIndex = currentSampleIndex;
                  if (IsShuffleMode)
                  {
                     var randomTimeSpanSample = deck.GetRandom();
                     deck.Remove(randomTimeSpanSample);
                     currentSampleIndex = timeSpanSampleProviders.IndexOf(randomTimeSpanSample);
                     if (deck.Count == 0)
                     {
                        deck.AddRange(timeSpanSampleProviders);
                     }
                  }
                  else
                  {
                     currentSampleIndex++;
                     if (currentSampleIndex >= timeSpanSampleProviders.Count)
                     {
                        // reach last sample go to first sample
                        currentSampleIndex = 0;
                     }
                  }

                  var crossfadeToIndex = currentSampleIndex;
                  crossfadeFrom = timeSpanSampleProviders[crossfadeFromIndex].SampleProvider;
                  crossfadeTo = timeSpanSampleProviders[crossfadeToIndex].SampleProvider;
                  // incase init of 0 sum sample span
                  if (sumSampleSpan == 0 && crossfadeFrom == crossfadeTo)
                  {
                     crossfadeFrom = null;
                  }
                  crossfadeSampleProvider.BeginCrossfade(crossfadeFrom, crossfadeTo, CrossfadeDuration);

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
      private readonly List<TimeSpanSampleProvider> deck; //deck for shuffle
      private int currentSampleSpanPosition = 0;
      private int currentSampleSpanEndPosition = 0;
      private int currentSampleIndex = 0;
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

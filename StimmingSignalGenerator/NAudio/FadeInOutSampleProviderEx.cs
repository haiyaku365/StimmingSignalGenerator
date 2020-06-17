using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace StimmingSignalGenerator.NAudio
{
   /// <summary>
   /// Sample Provider to allow fading in and out
   /// </summary>
   public class FadeInOutSampleProviderEx : ISampleProvider
   {
      public event EventHandler OnFadeInCompleted;
      public event EventHandler OnFadeOutCompleted;

      enum FadeState
      {
         Silence,
         FadingIn,
         FullVolume,
         FadingOut,
      }

      private readonly object lockObject = new object();
      private readonly ISampleProvider source;
      private float fadePosition;
      private float fadeStep;
      private FadeState fadeState;
      private readonly Queue<Action> eventInvokeQueue = new Queue<Action>();

      /// <summary>
      /// Creates a new FadeInOutSampleProvider
      /// </summary>
      /// <param name="source">The source stream with the audio to be faded in or out</param>
      /// <param name="initiallySilent">If true, we start faded out</param>
      public FadeInOutSampleProviderEx(ISampleProvider source, bool initiallySilent = false)
      {
         this.source = source;
         fadeState = initiallySilent ? FadeState.Silence : FadeState.FullVolume;
         fadePosition = initiallySilent ? 0 : 1;
      }

      /// <summary>
      /// Requests that a fade-in begins (will start on the next call to Read)
      /// </summary>
      /// <param name="fadeDurationInSeconds">Duration of fade in seconds</param>
      public void BeginFadeIn(double fadeDurationInSeconds)
      {
         lock (lockObject)
         {
            fadeStep = (1 - fadePosition) / source.WaveFormat.SampleRate / (float)fadeDurationInSeconds;
            fadeState = FadeState.FadingIn;
         }
      }

      /// <summary>
      /// Requests that a fade-out begins (will start on the next call to Read)
      /// </summary>
      /// <param name="fadeDurationInSeconds">Duration of fade in seconds</param>
      public void BeginFadeOut(double fadeDurationInSeconds)
      {
         lock (lockObject)
         {
            fadeStep = fadePosition / source.WaveFormat.SampleRate / (float)fadeDurationInSeconds;
            fadeState = FadeState.FadingOut;
         }
      }

      /// <summary>
      /// Reads samples from this sample provider
      /// </summary>
      /// <param name="buffer">Buffer to read into</param>
      /// <param name="offset">Offset within buffer to write to</param>
      /// <param name="count">Number of samples desired</param>
      /// <returns>Number of samples read</returns>
      public int Read(float[] buffer, int offset, int count)
      {
         int sourceSamplesRead = source.Read(buffer, offset, count);
         lock (lockObject)
         {
            if (fadeState == FadeState.FadingIn)
            {
               FadeIn(buffer, offset, sourceSamplesRead);
            }
            else if (fadeState == FadeState.FadingOut)
            {
               FadeOut(buffer, offset, sourceSamplesRead);
            }
            else if (fadeState == FadeState.Silence)
            {
               ClearBuffer(buffer, offset, count);
            }
         }
         while (eventInvokeQueue.Count > 0)
         {
            eventInvokeQueue.Dequeue().Invoke();
         }
         return sourceSamplesRead;
      }

      private static void ClearBuffer(float[] buffer, int offset, int count)
      {
         for (int n = 0; n < count; n++)
         {
            buffer[n + offset] = 0;
         }
      }

      private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
      {
         int sample = 0;
         while (sample < sourceSamplesRead)
         {
            for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
            {
               buffer[offset + sample++] *= fadePosition;
            }
            fadePosition -= fadeStep;
            if (fadePosition <= 0)
            {
               fadePosition = 0;
               fadeState = FadeState.Silence;
               // clear out the end
               ClearBuffer(buffer, sample + offset, sourceSamplesRead - sample);
               eventInvokeQueue.Enqueue(() => OnFadeOutCompleted?.Invoke(this, EventArgs.Empty));
               break;
            }
         }
      }

      private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
      {
         int sample = 0;
         while (sample < sourceSamplesRead)
         {
            for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
            {
               buffer[offset + sample++] *= fadePosition;
            }
            fadePosition += fadeStep;
            if (fadePosition >= 1)
            {
               fadePosition = 1;
               fadeState = FadeState.FullVolume;
               // no need to multiply any more
               eventInvokeQueue.Enqueue(() => OnFadeInCompleted?.Invoke(this, EventArgs.Empty));
               break;
            }
         }
      }

      /// <summary>
      /// WaveFormat of this SampleProvider
      /// </summary>
      public WaveFormat WaveFormat
      {
         get { return source.WaveFormat; }
      }
   }
}

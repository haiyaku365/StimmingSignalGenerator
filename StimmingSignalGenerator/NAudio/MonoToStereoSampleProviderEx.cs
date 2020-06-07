using NAudio.Wave;
using System;

namespace StimmingSignalGenerator.NAudio
{
   /// <summary>
   /// Mono to stereo provider with ramp gain
   /// </summary>
   public class MonoToStereoSampleProviderEx : ISampleProvider
   {
      private readonly ISampleProvider source;
      private float[] sourceBuffer;
      private readonly RampGain leftRampGain;
      private readonly RampGain rightRampGain;
      /// <summary>
      /// Initializes a new instance of MonoToStereoSampleProviderEx
      /// </summary>
      /// <param name="source">Source sample provider</param>
      public MonoToStereoSampleProviderEx(ISampleProvider source,
         double initLeftGain = 1.0, double initRightGain = 1.0)
      {
         leftRampGain = new RampGain(initLeftGain);
         rightRampGain = new RampGain(initRightGain);
         if (source.WaveFormat.Channels != 1)
         {
            throw new ArgumentException("Source must be mono");
         }
         this.source = source;
         WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
      }

      public void ForceSetVolume(double leftVol,double rightVol)
      {
         leftRampGain.ForceSetGain(leftVol);
         rightRampGain.ForceSetGain(rightVol);
      }
      /// <summary>
      /// WaveFormat of this provider
      /// </summary>
      public WaveFormat WaveFormat { get; }

      /// <summary>
      /// Reads samples from this provider
      /// </summary>
      /// <param name="buffer">Sample buffer</param>
      /// <param name="offset">Offset into sample buffer</param>
      /// <param name="count">Number of samples required</param>
      /// <returns>Number of samples read</returns>
      public int Read(float[] buffer, int offset, int count)
      {
         var sourceSamplesRequired = count / 2;
         var outIndex = offset;
         EnsureSourceBuffer(sourceSamplesRequired);
         var sourceSamplesRead = source.Read(sourceBuffer, 0, sourceSamplesRequired);

         leftRampGain.CalculateGainStepDelta(sourceSamplesRequired);
         rightRampGain.CalculateGainStepDelta(sourceSamplesRequired);
         for (var n = 0; n < sourceSamplesRead; n++)
         {
            buffer[outIndex++] = sourceBuffer[n] * (float)leftRampGain.CurrentGain;
            buffer[outIndex++] = sourceBuffer[n] * (float)rightRampGain.CurrentGain;
            leftRampGain.CalculateNextGain();
            rightRampGain.CalculateNextGain();
         }

         return sourceSamplesRead * 2;
      }

      /// <summary>
      /// Multiplier for left channel (default is 1.0)
      /// </summary>
      public double LeftVolume { get => leftRampGain.Gain; set => leftRampGain.Gain = value; }

      /// <summary>
      /// Multiplier for right channel (default is 1.0)
      /// </summary>
      public double RightVolume { get => rightRampGain.Gain; set => rightRampGain.Gain = value; }

      private void EnsureSourceBuffer(int count)
      {
         if (sourceBuffer == null || sourceBuffer.Length < count)
         {
            sourceBuffer = new float[count];
         }
      }
   }
}

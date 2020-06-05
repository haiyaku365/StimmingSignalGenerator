using NAudio.Wave;

namespace StimmingSignalGenerator.Generators
{
   /// <summary>
   /// Sample provider supporting adjustable gain
   /// </summary>
   public class VolumeSampleProviderEx : ISampleProvider
   {
      private readonly ISampleProvider source;
      private readonly RampGain rampGain;

      /// <summary>
      /// Initializes a new instance of VolumeSampleProviderEx
      /// </summary>
      /// <param name="source">Source Sample Provider</param>
      public VolumeSampleProviderEx(ISampleProvider source)
      {
         this.source = source;
         rampGain = new RampGain();
      }

      /// <summary>
      /// WaveFormat
      /// </summary>
      public WaveFormat WaveFormat => source.WaveFormat;

      /// <summary>
      /// Reads samples from this sample provider
      /// </summary>
      /// <param name="buffer">Sample buffer</param>
      /// <param name="offset">Offset into sample buffer</param>
      /// <param name="sampleCount">Number of samples desired</param>
      /// <returns>Number of samples read</returns>
      public int Read(float[] buffer, int offset, int sampleCount)
      {
         int samplesRead = source.Read(buffer, offset, sampleCount);
         if (Volume != 1f)
         {
            rampGain.CalculateGainStepDelta(sampleCount);
            for (int n = 0; n < sampleCount; n++)
            {
               buffer[offset + n] *= (float)rampGain.CurrentGain;
               rampGain.CalculateNextGain();
            }
         }
         return samplesRead;
      }

      /// <summary>
      /// Allows adjusting the volume, 1.0f = full volume
      /// </summary>
      public double Volume { get => rampGain.Gain; set => rampGain.Gain = value; }
   }
}

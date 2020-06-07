using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.Linq;

namespace StimmingSignalGenerator.NAudio
{
   /// <summary>
   /// Generate signal from multiple BasicSignal
   /// </summary>
   public class MultiSignal : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }

      /// <summary>
      /// Gain for the Generator. (0.0 to 1.0)
      /// </summary>
      public double Gain { get => rampGain.Gain; set => rampGain.Gain = value; }
      private readonly RampGain rampGain;

      private readonly MixingSampleProvider mixingSampleProvider;
      private readonly List<BasicSignal> sources;

      public MultiSignal()
      {
         WaveFormat = Constants.Wave.DefaultMonoWaveFormat;

         rampGain = new RampGain(1);
         mixingSampleProvider = new MixingSampleProvider(WaveFormat);
         sources = new List<BasicSignal>();
      }

      public void AddSignal(BasicSignal basicSignal)
      {
         lock (sources)
         {
            sources.Add(basicSignal);
            mixingSampleProvider.AddMixerInput(basicSignal);
         }
      }

      public void RemoveSignal(BasicSignal basicSignal)
      {
         lock (sources)
         {
            sources.Remove(basicSignal);
            mixingSampleProvider.RemoveMixerInput(basicSignal);
         }
      }

      public int Read(float[] buffer, int offset, int count)
      {
         int read;
         int outIndex = offset;
         double sumCurrentGain, sumGainStepDelta;


         lock (sources)
         {
            var enableSource = sources.Where(s => s.CurrentGain != 0).ToList();
            if (enableSource.Any(s => s.Gain != s.CurrentGain))
            {
               sumCurrentGain = enableSource.Sum(s => s.CurrentGain);
               read = mixingSampleProvider.Read(buffer, offset, count);
               sumGainStepDelta = enableSource.Sum(s => s.GainStepDelta);
            }
            else
            {
               sumCurrentGain = enableSource.Sum(s => s.Gain);
               read = mixingSampleProvider.Read(buffer, offset, count);
               sumGainStepDelta = 0;
            }
         }

         int countPerChannel = count / WaveFormat.Channels;
         rampGain.CalculateGainStepDelta(countPerChannel);
         var sumGain = sumCurrentGain;
         for (int sampleCount = 0; sampleCount < countPerChannel; sampleCount++)
         {
            for (int i = 0; i < WaveFormat.Channels; i++)
            {
               // mixingSampleProvider cause gain overdrive
               // need to correct to 0-1 level
               buffer[outIndex++] *= (sumGain == 0) ? 0 : (float)(rampGain.CurrentGain / sumGain);
            }
            sumGain += sumGainStepDelta;
            rampGain.CalculateNextGain();
         }
         return read;
      }
   }
}

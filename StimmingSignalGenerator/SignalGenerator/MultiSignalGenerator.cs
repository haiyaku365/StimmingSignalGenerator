using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   /// <summary>
   /// Generate signal from multiple BasicSignalGenerator
   /// </summary>
   class MultiSignalGenerator : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }

      /// <summary>
      /// Gain for the Generator. (0.0 to 1.0)
      /// </summary>
      public double Gain { get; set; }

      private readonly MixingSampleProvider mixingSampleProvider;
      private readonly List<BasicSignalGenerator> sources;

      public MultiSignalGenerator(WaveFormat waveFormat)
      {
         WaveFormat = waveFormat;

         mixingSampleProvider = new MixingSampleProvider(waveFormat);
         sources = new List<BasicSignalGenerator>();
      }

      public void AddMixerInput(BasicSignalGenerator basicSignalGenerator)
      {
         lock (sources)
         {
            sources.Add(basicSignalGenerator);
            mixingSampleProvider.AddMixerInput(basicSignalGenerator);
         }
      }

      public void RemoveMixerInput(BasicSignalGenerator basicSignalGenerator)
      {
         lock (sources)
         {
            sources.Remove(basicSignalGenerator);
            mixingSampleProvider.RemoveMixerInput(basicSignalGenerator);
         }
      }

      public int Read(float[] buffer, int offset, int count)
      {
         int read;
         int outIndex = offset;
         float sumCurrentGain, sumGainStepDelta;

         lock (sources)
         {
            var enableSource = sources.Where(s => s.Gain != 0).ToList();

            if (enableSource.Any(s => s.Gain != s.CurrentGain))
            {
               sumCurrentGain = (float)enableSource.Sum(s => s.CurrentGain);
               read = mixingSampleProvider.Read(buffer, offset, count);
               sumGainStepDelta = (float)enableSource.Sum(s => s.GainStepDelta);
            }
            else
            {
               sumCurrentGain = (float)enableSource.Sum(s => s.Gain);
               read = mixingSampleProvider.Read(buffer, offset, count);
               sumGainStepDelta = 0;
            }
         }

         int countPerChannel = count / WaveFormat.Channels;

         var sumGain = sumCurrentGain;
         for (int sampleCount = 0; sampleCount < countPerChannel; sampleCount++)
         {
            for (int i = 0; i < WaveFormat.Channels; i++)
            {
               // mixingSampleProvider cause gain overdrive
               // need to correct to 0-1 level
               buffer[outIndex++] *= (sumGain == 0) ? 0 : (float)Gain / sumGain;
            }
            sumGain += sumGainStepDelta;
         }
         return read;
      }
   }
}

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
      private readonly List<BasicSignalGenerator> source;

      public MultiSignalGenerator(WaveFormat waveFormat)
      {
         WaveFormat = waveFormat;

         mixingSampleProvider = new MixingSampleProvider(waveFormat);
         source = new List<BasicSignalGenerator>();
      }

      public void AddMixerInput(BasicSignalGenerator basicSignalGenerator)
      {
         source.Add(basicSignalGenerator);
         mixingSampleProvider.AddMixerInput(basicSignalGenerator);
      }

      public void RemoveMixerInput(BasicSignalGenerator basicSignalGenerator)
      {
         source.Remove(basicSignalGenerator);
         mixingSampleProvider.RemoveMixerInput(basicSignalGenerator);
      }

      public int Read(float[] buffer, int offset, int count)
      {
         var sumGain = (float)source.Sum(s => s.Gain);
         if (sumGain == 0)
         {
            for (int i = 0; i < count; i++)
            {
               buffer[i] = 0;
            }
            return count;
         }

         var read = mixingSampleProvider.Read(buffer, offset, count);
         // mixingSampleProvider cause gain overdrive
         // need to correct to 0-1 level
         for (int i = 0; i < count; i++)
         {
            buffer[i] *= (float)(Gain / source.Sum(s => s.Gain));
         }
         return read;
      }
   }
}

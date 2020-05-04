using NAudio.Wave;
using StimmingSignalGenerator.Generators.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   class GainControlSampleProvider : ISingleSignalInputSampleProvider
   {
      public WaveFormat WaveFormat => InputSample.WaveFormat;

      public ISampleProvider InputSample { get; set; }
      public Func<float, float> GainFunction { get; set; }

      public GainControlSampleProvider(
         ISampleProvider inputSample,
         Func<float, float> gainFunction)
      {
         InputSample = inputSample;
         GainFunction = gainFunction;
      }

      public int Read(float[] buffer, int offset, int count)
      {
         int samplesRead = InputSample.Read(buffer, offset, count);
         for (int n = 0; n < count; n++)
         {
            buffer[offset + n] = GainFunction(buffer[offset + n]);
         }
         return samplesRead;
      }
   }
}

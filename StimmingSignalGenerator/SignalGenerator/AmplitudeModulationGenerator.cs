using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.SignalGenerator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   class AmplitudeModulationGenerator : IDoubleSignalInputSampleProvider
   {
      public WaveFormat WaveFormat => InputSampleA.WaveFormat;

      /// <summary>
      /// Carrier Signal
      /// </summary>
      public ISampleProvider InputSampleA { get; set; }
      /// <summary>
      /// Information Signal
      /// </summary>
      public ISampleProvider InputSampleB { get; set; }

      public AmplitudeModulationGenerator(
         ISampleProvider inputSampleA,
         ISampleProvider inputSampleB
         )
      {
         InputSampleA = inputSampleA;
         InputSampleB = inputSampleB;
      }

      public int Read(float[] buffer, int offset, int count)
      {
         int sampleARead = InputSampleA.Read(buffer, offset, count);

         float[] sampleBBuffer = new float[buffer.Length];
         InputSampleB.Read(sampleBBuffer, offset, count);

         for (int n = 0; n < count; n++)
         {
            buffer[offset + n] *= (sampleBBuffer[offset + n] + 1) / 2;
         }
         return sampleARead;
      }
   }
}

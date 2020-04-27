using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.SignalGenerator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   class AmplitudeModulationProvider : IDoubleSignalInputSampleProvider
   {
      public WaveFormat WaveFormat => SourceA.WaveFormat;

      /// <summary>
      /// Carrier Signal [-1, 1]
      /// </summary>
      public ISampleProvider SourceA { get; set; }
      /// <summary>
      /// Information Signal [-1, 1]
      /// </summary>
      public ISampleProvider SourceB { get; set; }

      private float[] sourceBBuffer;
      /// <summary>
      /// Amplitude Modulation
      /// </summary>
      /// <param name="sourceA">Carrier Signal [-1, 1]</param>
      /// <param name="sourceB">Information Signal [-1, 1]</param>
      public AmplitudeModulationProvider(
         ISampleProvider sourceA,
         ISampleProvider sourceB
         )
      {
         SourceA = sourceA;
         SourceB = sourceB;
      }

      public int Read(float[] buffer, int offset, int count)
      {
         /*
         https://www.desmos.com/calculator/gtghlhuqut
         f_{1}=1
         g_{0}=0.5
         y_{0}=g_{0}\sin\left(f_{1}\cdot2\pi x\right)
         y_{1}=\frac{\left(y_{0}+1\right)}{2}
         y=\sin\left(20\cdot2\pi x\right)\cdot y_{1}\left\{-1<y<1\right\}
          */
         int sampleARead = SourceA.Read(buffer, offset, count);
         sourceBBuffer = BufferHelpers.Ensure(sourceBBuffer, count);
         SourceB.Read(sourceBBuffer, offset, count);

         for (int n = 0; n < count; n++)
         {
            buffer[offset + n] *= (sourceBBuffer[offset + n] + 1) / 2;
         }
         return sampleARead;
      }
   }
}

using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.SignalGenerator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   class FrequencyModulationProvider : IDoubleSignalInputSampleProvider
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
      public float PitchOctaveUpDown { get; set; }

      private float[] sourceBBuffer;
      /// <summary>
      /// Frequency Modulation
      /// </summary>
      /// <param name="sourceA">Carrier Signal [-1, 1]</param>
      /// <param name="sourceB">Information Signal [-1, 1]</param>
      public FrequencyModulationProvider(
         ISampleProvider sourceA,
         ISampleProvider sourceB,
         float pitchOctaveUpDown = 1
         )
      {
         SourceA = sourceA;
         SourceB = sourceB;
         PitchOctaveUpDown = pitchOctaveUpDown;

         smbPitchShiftingSampleProvider = new SmbPitchShiftingSampleProvider(SourceA);
      }
      private readonly SmbPitchShiftingSampleProvider smbPitchShiftingSampleProvider;

      public int Read(float[] buffer, int offset, int count)
      {
         sourceBBuffer = BufferHelpers.Ensure(sourceBBuffer, count);
         SourceB.Read(sourceBBuffer, offset, count);

         smbPitchShiftingSampleProvider.PitchFactor = MathF.Pow(PitchOctaveUpDown + 1, sourceBBuffer[offset]);
         return smbPitchShiftingSampleProvider.Read(buffer, offset, count);
      }
   }
}

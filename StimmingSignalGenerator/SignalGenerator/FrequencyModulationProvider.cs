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
      public WaveFormat WaveFormat => InputSampleA.WaveFormat;

      /// <summary>
      /// Carrier Signal [-1, 1]
      /// </summary>
      public ISampleProvider InputSampleA { get; set; }
      /// <summary>
      /// Information Signal [-1, 1]
      /// </summary>
      public ISampleProvider InputSampleB { get; set; }
      public float PitchOctaveUpDown { get; set; }

      /// <summary>
      /// Frequency Modulation
      /// </summary>
      /// <param name="inputSampleA">Carrier Signal [-1, 1]</param>
      /// <param name="inputSampleB">Information Signal [-1, 1]</param>
      public FrequencyModulationProvider(
         ISampleProvider inputSampleA,
         ISampleProvider inputSampleB,
         float pitchOctaveUpDown = 1
         )
      {
         InputSampleA = inputSampleA;
         InputSampleB = inputSampleB;
         PitchOctaveUpDown = pitchOctaveUpDown;

         smbPitchShiftingSampleProvider = new SmbPitchShiftingSampleProvider(InputSampleA);
      }
      private readonly SmbPitchShiftingSampleProvider smbPitchShiftingSampleProvider;

      public int Read(float[] buffer, int offset, int count)
      {
         float[] sampleBBuffer = new float[buffer.Length];
         InputSampleB.Read(sampleBBuffer, offset, count);

         smbPitchShiftingSampleProvider.PitchFactor = MathF.Pow(PitchOctaveUpDown + 1, sampleBBuffer[offset]);
         return smbPitchShiftingSampleProvider.Read(buffer, offset, count);
      }
   }
}

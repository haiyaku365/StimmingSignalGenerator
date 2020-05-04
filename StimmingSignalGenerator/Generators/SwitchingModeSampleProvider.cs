using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   class SwitchingModeSampleProvider : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }
      public GeneratorModeType GeneratorMode { get; set; }
      public float MonoRightVolume
      {
         get
         {
            return monoSample.RightVolume;
         }
         set
         {
            monoSample.RightVolume = value;
         }
      }
      public float MonoLeftVolume
      {
         get
         {
            return monoSample.LeftVolume;
         }
         set
         {
            monoSample.LeftVolume = value;
         }
      }

      private readonly MultiplexingSampleProvider stereoSample;
      private readonly MonoToStereoSampleProvider monoSample;
      public SwitchingModeSampleProvider(
         ISampleProvider monoSampleProvider,
         IEnumerable<ISampleProvider> stereoSampleProviders)
      {
         WaveFormat =
            WaveFormat.CreateIeeeFloatWaveFormat(
               monoSampleProvider.WaveFormat.SampleRate, 2
            );

         monoSample =
            new MonoToStereoSampleProvider(monoSampleProvider);
         stereoSample =
            new MultiplexingSampleProvider(stereoSampleProviders, 2);
      }

      public int Read(float[] buffer, int offset, int count)
      {
         int read;
         switch (GeneratorMode)
         {
            case GeneratorModeType.Mono:
               read = monoSample.Read(buffer, offset, count);
               break;
            case GeneratorModeType.Stereo:
               read = stereoSample.Read(buffer, offset, count);
               break;
            default:
               read = 0;
               break;
         }
         return read;
      }
   }

   public enum GeneratorModeType
   {
      Mono = 0,
      Stereo
   }
}

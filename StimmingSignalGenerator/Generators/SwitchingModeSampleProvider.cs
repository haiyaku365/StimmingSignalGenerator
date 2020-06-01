using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   public class SwitchingModeSampleProvider : ISampleProvider
   {
      public WaveFormat WaveFormat { get; }
      public GeneratorModeType GeneratorMode { get; set; }
      /// <summary>
      /// Multiplier for mono right channel (default is 1.0)
      /// </summary>
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
      /// <summary>
      /// Multiplier for mono left channel (default is 1.0)
      /// </summary>
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
      /// <summary>
      /// Multiplier for stereo channels (default is 1.0)
      /// </summary>
      public float StereoVolume { get; set; } = 1.0f;
      public ISampleProvider MonoSampleProvider
      {
         get => monoSampleProvider;
         set
         {
            monoSampleProvider = value;
            monoSample = new MonoToStereoSampleProvider(monoSampleProvider);
         }
      }
      public IEnumerable<ISampleProvider> StereoSampleProviders
      {
         get => stereoSampleProviders;
         set
         {
            stereoSampleProviders = value;
            stereoSample = new MultiplexingSampleProvider(stereoSampleProviders, 2);
         }
      }

      private MultiplexingSampleProvider stereoSample;
      private MonoToStereoSampleProvider monoSample;
      private IEnumerable<ISampleProvider> stereoSampleProviders;
      private ISampleProvider monoSampleProvider;

      public SwitchingModeSampleProvider()
      {
         WaveFormat = Constants.Wave.DefaultStereoWaveFormat;
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
               for (int i = 0; i < count; i++)
               {
                  buffer[i] *= StereoVolume;
               }
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

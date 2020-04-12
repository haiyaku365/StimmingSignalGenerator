using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator.Effect
{
   class AmplitudeModulation : ISampleProvider
   {
      public WaveFormat WaveFormat => CarrierProvider.WaveFormat;
      public ISampleProvider InformationProvider { get; }
      public ISampleProvider CarrierProvider { get; }

      public AmplitudeModulation(
         ISampleProvider informationProvider,
         ISampleProvider carrierProvider
         )
      {
         throw new NotImplementedException();
         InformationProvider = informationProvider;
         CarrierProvider = carrierProvider;
      }

      public int Read(float[] buffer, int offset, int count)
      {
         float[] carrierBuffer = new float[buffer.Length];
         int carrierRead = CarrierProvider.Read(carrierBuffer, offset, count);

         int informationRead = InformationProvider.Read(buffer, offset, count);
         for (int n = 0; n < count; n++)
         {
            if (true)
            {
               buffer[offset + n] *= carrierBuffer[offset + n];
            }
         }
         return informationRead;
      }
   }
}

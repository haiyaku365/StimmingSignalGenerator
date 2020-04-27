using NAudio.Wave;
using StimmingSignalGenerator.SignalGenerator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   // https://raw.githubusercontent.com/naudio/NAudio/master/NAudio/Wave/SampleProviders/SignalGenerator.cs
   /// <summary>
   /// Basic Signal Generator
   /// Sin, SawTooth, Triangle, Square, White Noise, Pink Noise.
   /// </summary>
   /// <remarks>
   /// Posibility to change ISampleProvider
   /// Example :
   /// ---------
   /// WaveOut _waveOutGene = new WaveOut();
   /// WaveGenerator wg = new BasicSignalGenerator();
   /// wg.Type = ...
   /// wg.Frequency = ...
   /// wg ...
   /// _waveOutGene.Init(wg);
   /// _waveOutGene.Play();
   /// </remarks>
   public class BasicSignalGenerator : ISampleProvider
   {
      // Wave format
      private readonly WaveFormat waveFormat;

      // Random Number for the White Noise & Pink Noise Generator
      private readonly Random random = new Random();

      private readonly double[] pinkNoiseBuffer = new double[7];

      /// <summary>
      /// Initializes a new instance for the Generator (Default :: 44.1Khz, 2 channels, Sinus, Frequency = 440, Gain = 1)
      /// </summary>
      public BasicSignalGenerator()
          : this(44100, 2)
      {

      }

      /// <summary>
      /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
      /// </summary>
      /// <param name="sampleRate">Desired sample rate</param>
      /// <param name="channel">Number of channels</param>
      public BasicSignalGenerator(int sampleRate, int channel)
      {
         waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);

         // Default
         Type = BasicSignalGeneratorType.Sin;
         Frequency = 440.0;
         ZeroCrossingPosition = 0.5;
         Gain = 1;
         ChannelGain = Enumerable.Repeat(1.0, channel).ToArray();
      }

      /// <summary>
      /// The waveformat of this WaveProvider (same as the source)
      /// </summary>
      public WaveFormat WaveFormat => waveFormat;

      /// <summary>
      /// Frequency for the Generator. (20.0 - 20000.0 Hz)
      /// Noise ignore this
      /// </summary>
      public double Frequency
      {
         get => targetPhaseStep;
         set
         {
            targetPhaseStep = value;
            seekFrequency = true;
         }
      }

      private bool seekFrequency;
      private double targetPhaseStep;
      private double currentPhaseStep;
      private double phaseStepDelta;
      private double phase;
      private double Period => WaveFormat.SampleRate;
      /// <summary>
      /// Position when signal cross zero default 0.5 (0.0 to 1.0)
      /// Noise ignore this
      /// </summary>
      public double ZeroCrossingPosition { get; set; }

      /// <summary>
      /// Return Log of Frequency Start (Read only)
      /// </summary>
      public double FrequencyLog => Math.Log(Frequency);

      /// <summary>
      /// Gain for the Generator. (0.0 to 1.0)
      /// </summary>
      public double Gain
      {
         get => targetGain;
         set
         {
            targetGain = value;
            seekGain = true;
         }
      }

      private bool seekGain;
      private double targetGain;
      private double currentGain;
      private double gainStepDelta;

      /// <summary>
      /// Gain for each channel.
      /// Negative gain will reverse phase.
      /// default are 1.0 for all channel.
      /// (-1.0 to 1.0)
      /// </summary>
      public double[] ChannelGain { get; }

      /// <summary>
      /// Type of Generator.
      /// </summary>
      public BasicSignalGeneratorType Type { get; set; }

      /// <summary>
      /// Reads from this provider.
      /// </summary>
      public int Read(float[] buffer, int offset, int count)
      {
         int outIndex = offset;
         int countPerChannel = count / waveFormat.Channels;

         // Generator current value
         double sampleValue;

         // Once per Read variable
         double zeroCrossingPoint = ZeroCrossingPosition * Period;
         double beforeZCFrequencyFactor = 1 / ZeroCrossingPosition;
         double beforeZCShift = 0;
         double afterZCFrequencyFactor = 1 / (1 - ZeroCrossingPosition);
         double afterZCShift = -Period;
         double x, frequencyFactor, shift;

         // Calc gainStepDelta
         if (seekGain) // process Gain change only once per call to Read
         {
            gainStepDelta = (targetGain - currentGain) / countPerChannel;
            seekGain = false;
         }
         // Calc frequencyStepDelta
         if (seekFrequency) // process frequency change only once per call to Read
         {
            phaseStepDelta = (targetPhaseStep - currentPhaseStep) / countPerChannel;
            seekFrequency = false;
         }

         // Complete Buffer
         for (int sampleCount = 0; sampleCount < countPerChannel; sampleCount++)
         {
            //calculate common variable
            x = phase % Period;

            bool isBeforeCrossingZero = 0 <= x && x < zeroCrossingPoint;
            //bool isAfterCrossingZero = zeroCrossingPoint <= x && x < period;
            if (isBeforeCrossingZero)
            {
               frequencyFactor = beforeZCFrequencyFactor;
               shift = beforeZCShift;
            }
            else //if (isAfterCrossingZero)
            {
               frequencyFactor = afterZCFrequencyFactor;
               shift = afterZCShift;
            }

            switch (Type)
            {
               case BasicSignalGeneratorType.Sin:

                  // Sinus Generator
                  sampleValue = currentGain * SampleSin(x, frequencyFactor, shift);
                  CalculateNextPhase();
                  break;

               case BasicSignalGeneratorType.SawTooth:

                  // SawTooth Generator

                  sampleValue = currentGain * SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero);
                  CalculateNextPhase();
                  break;

               case BasicSignalGeneratorType.Triangle:

                  // Triangle Generator

                  sampleValue = 2 * SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero);
                  if (sampleValue > 1)
                     sampleValue = 2 - sampleValue;
                  if (sampleValue < -1)
                     sampleValue = -2 - sampleValue;

                  sampleValue *= currentGain;

                  CalculateNextPhase();
                  break;

               case BasicSignalGeneratorType.Square:

                  // Square Generator

                  sampleValue =
                     SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero) < 0 ?
                        currentGain : -currentGain;

                  CalculateNextPhase();
                  break;

               case BasicSignalGeneratorType.Pink:

                  // Pink Noise Generator

                  double white = NextRandomTwo();
                  pinkNoiseBuffer[0] = 0.99886 * pinkNoiseBuffer[0] + white * 0.0555179;
                  pinkNoiseBuffer[1] = 0.99332 * pinkNoiseBuffer[1] + white * 0.0750759;
                  pinkNoiseBuffer[2] = 0.96900 * pinkNoiseBuffer[2] + white * 0.1538520;
                  pinkNoiseBuffer[3] = 0.86650 * pinkNoiseBuffer[3] + white * 0.3104856;
                  pinkNoiseBuffer[4] = 0.55000 * pinkNoiseBuffer[4] + white * 0.5329522;
                  pinkNoiseBuffer[5] = -0.7616 * pinkNoiseBuffer[5] - white * 0.0168980;
                  double pink = pinkNoiseBuffer[0] + pinkNoiseBuffer[1] + pinkNoiseBuffer[2] + pinkNoiseBuffer[3] + pinkNoiseBuffer[4] + pinkNoiseBuffer[5] + pinkNoiseBuffer[6] + white * 0.5362;
                  pinkNoiseBuffer[6] = white * 0.115926;
                  sampleValue = (currentGain * (pink / 5));
                  break;

               case BasicSignalGeneratorType.White:

                  // White Noise Generator
                  sampleValue = (currentGain * NextRandomTwo());
                  break;

               default:
                  sampleValue = 0.0;
                  break;
            }
            CalculateNextGain();
            // Phase Reverse and Gain Per Channel
            for (int i = 0; i < waveFormat.Channels; i++)
            {
               buffer[outIndex++] = (float)(sampleValue * ChannelGain[i]);
            }
         }
         return count;
      }

      private void CalculateNextGain()
      {
         //calculate currentGain
         currentGain += gainStepDelta;
         //correct if value exceed target
         if (gainStepDelta > 0 && currentGain > targetGain ||
             gainStepDelta < 0 && currentGain < targetGain)
            currentGain = targetGain;
      }

      private void CalculateNextPhase()
      {
         phase += currentPhaseStep;
         if (phase > Period) phase -= Period;
         if (currentPhaseStep != targetPhaseStep)
         {
            //calculate currentPhaseStep
            currentPhaseStep += phaseStepDelta;
            //correct if value exceed target
            if (phaseStepDelta > 0 && currentPhaseStep > targetPhaseStep ||
                phaseStepDelta < 0 && currentPhaseStep < targetPhaseStep)
               currentPhaseStep = targetPhaseStep;
         }
      }

      private double SampleSin(double x, double frequencyFactor, double shift)
      {
         /*
         https://www.desmos.com/calculator/0de76phnur
         f_{1}=1
         f_{2}=0.3
         p=\frac{1}{f_{1}}
         z=\frac{f_{2}}{f_{1}}
         y=\sin\left(f_{1}\cdot2\pi x\right)\left\{0\le x<p\right\}
         y_{1}=\sin\left(\frac{f_{1}}{f_{2}}\cdot\pi x\right)\left\{0\le x<z\right\}
         y_{2}=\sin\left(\frac{f_{1}}{\left(1-f_{2}\right)}\cdot\pi\left(x-p\right)\right)\left\{z\le x<p\right\}
         \left(0,0\right),\left(z,0\right),\left(p,0\right)
         */
         return Math.Sin(frequencyFactor * Math.PI / WaveFormat.SampleRate * (x + shift));
      }

      private double SampleSaw(double x, double frequencyFactor, double shift, bool isBeforeCrossingZero)
      {
         /*
         https://www.desmos.com/calculator/kb4nj3hurl
         f_{1}=1
         f_{2}=0.7
         p=\frac{1}{f_{1}}
         z=\frac{f_{2}}{f_{1}}
         y=\left(\operatorname{mod}\left(2f_{1}x,2\right)\right)-1\left\{0\le x<p\right\}
         y_{1}=\left(\frac{f_{1}}{f_{2}}\operatorname{mod}\left(x,p\right)\right)-1\left\{0\le x<z\right\}
         y_{2}=\left(\frac{f_{1}}{1-f_{2}}\left(\operatorname{mod}\left(x,p\right)-p\right)\right)+1\left\{z\le x<p\right\}
         \left(0,0\right),\left(z,0\right),\left(p,0\right)
         */
         double lift = isBeforeCrossingZero ? -1 : 1;
         return (frequencyFactor / WaveFormat.SampleRate * (x + shift)) + lift;
      }

      /// <summary>
      /// Private :: Random for WhiteNoise &amp; Pink Noise (Value form -1 to 1)
      /// </summary>
      /// <returns>Random value from -1 to +1</returns>
      private double NextRandomTwo()
      {
         return 2 * random.NextDouble() - 1;
      }


   }

   /// <summary>
   /// Basic Signal Generator type
   /// </summary>
   public enum BasicSignalGeneratorType
   {
      /// <summary>
      /// Sine wave
      /// </summary>
      Sin,
      /// <summary>
      /// Sawtooth wave
      /// </summary>
      SawTooth,
      /// <summary>
      /// Triangle Wave
      /// </summary>
      Triangle,
      /// <summary>
      /// Square wave
      /// </summary>
      Square,
      /// <summary>
      /// Pink noise
      /// </summary>
      Pink,
      /// <summary>
      /// White noise
      /// </summary>
      White
   }
}

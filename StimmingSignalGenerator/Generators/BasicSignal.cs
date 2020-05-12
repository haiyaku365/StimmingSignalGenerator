using NAudio.Utils;
using NAudio.Wave;
using StimmingSignalGenerator.Generators.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   // https://raw.githubusercontent.com/naudio/NAudio/master/NAudio/Wave/SampleProviders/SignalGenerator.cs
   /// <summary>
   /// Basic Signal
   /// Sin, SawTooth, Triangle, Square, White Noise, Pink Noise.
   /// </summary>
   /// <remarks>
   /// Posibility to change ISampleProvider
   /// Example :
   /// ---------
   /// WaveOut _waveOutGene = new WaveOut();
   /// WaveGenerator wg = new BasicSignal();
   /// wg.Type = ...
   /// wg.Frequency = ...
   /// wg ...
   /// _waveOutGene.Init(wg);
   /// _waveOutGene.Play();
   /// </remarks>
   class BasicSignal : ISampleProvider
   {

      // Random Number for the White Noise & Pink Noise Generator
      private readonly Random random = new Random();

      private readonly double[] pinkNoiseBuffer = new double[7];

      /// <summary>
      /// Initializes a new instance for the Generator
      /// </summary>
      /// <param name="sampleRate">Desired sample rate</param>
      /// <param name="channel">Number of channels</param>
      public BasicSignal()
      {
         WaveFormat = Constants.DefaultMonoWaveFormat;

         // Default
         Type = BasicSignalType.Sin;
         Frequency = 440.0;
         ZeroCrossingPosition = 0.5;
         Gain = 1;

         AMSignals = new List<BasicSignal>();
         FMSignals = new List<BasicSignal>();
      }

      /// <summary>
      /// The waveformat of this WaveProvider (same as the source)
      /// </summary>
      public WaveFormat WaveFormat { get; }

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

      List<BasicSignal> FMSignals;
      float[] fmBuffer;
      float[] aggregateFMBuffer;

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
      /// Gain before seek to target gain.
      /// </summary>
      public double CurrentGain { get => currentGain; }
      /// <summary>
      /// Gain step delta of latest read.
      /// </summary>
      public double GainStepDelta { get => gainStepDelta; }

      List<BasicSignal> AMSignals;
      float[] amBuffer;
      float[] aggregateAMBuffer;

      /// <summary>
      /// Type of Generator.
      /// </summary>
      public BasicSignalType Type { get; set; }

      /// <summary>
      /// 1 Channel Signal for amplitude modulation.
      /// </summary>
      /// <param name="signal">1 Channel Signal</param>
      public void AddAMSignal(BasicSignal signal)
      {
         lock (AMSignals)
         {
            AMSignals.Add(signal);
         }
      }
      public void RemoveAMSignal(BasicSignal signal)
      {
         lock (AMSignals)
         {
            AMSignals.Remove(signal);
         }
      }

      /// <summary>
      /// Add 1 Channel Signal for frequency modulation. Gain of signal indicate how much frequency change.
      /// </summary>
      /// <param name="signal">1 Channel Signal</param>
      public void AddFMSignal(BasicSignal signal)
      {
         lock (FMSignals)
         {
            FMSignals.Add(signal);
         }
      }
      public void RemoveFMSignal(BasicSignal signal)
      {
         lock (FMSignals)
         {
            FMSignals.Remove(signal);
         }
      }

      /// <summary>
      /// Reads from this provider.
      /// </summary>
      public int Read(float[] buffer, int offset, int count)
      {
         int outIndex = offset;
         int countPerChannel = count / WaveFormat.Channels;

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

         aggregateAMBuffer = BufferHelpers.Ensure(aggregateAMBuffer, count);
         Array.Fill(aggregateAMBuffer, 1);
         // read AM signal
         lock (AMSignals)
         {
            foreach (var signal in AMSignals)
            {
               amBuffer = BufferHelpers.Ensure(amBuffer, count);
               signal.Read(amBuffer, offset, count);
               /*
               AM Signal with gain bump
               https://www.desmos.com/calculator/ya9ayr9ylc
               f_{1}=1
               g_{0}=0.25
               y_{0}=g_{0}\sin\left(f_{1}\cdot2\pi x\right)
               y_{1}=y_{0}+1-g_{0}
               y_{2}=\frac{\left(y_{1}+1\right)}{2}
               y=\sin\left(20\cdot2\pi x\right)\cdot y_{2}\left\{-1<y<1\right\}
               */
               for (int i = 0; i < amBuffer.Length; i++)
               {
                  aggregateAMBuffer[i] *= (amBuffer[i] + 2 - (float)signal.Gain) / 2;
               }
            }
         }


         aggregateFMBuffer = BufferHelpers.Ensure(aggregateFMBuffer, count);
         Array.Fill(aggregateFMBuffer, 0);
         // read FM signal
         lock (FMSignals)
         {
            foreach (var signal in FMSignals)
            {
               fmBuffer = BufferHelpers.Ensure(fmBuffer, count);
               signal.Read(fmBuffer, offset, count);
               for (int i = 0; i < fmBuffer.Length; i++)
               {
                  aggregateFMBuffer[i] += fmBuffer[i];
               }
            }
         }

         //skip calc if gain is 0
         if (Gain == 0)
         {
            for (int i = 0; i < count; i++)
               buffer[i] = 0;

            //prevent out of phase when mixing multi signal
            for (int i = 0; i < countPerChannel; i++)
               CalculateNextPhase(aggregateAMBuffer[i]);

            return count;
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
               case BasicSignalType.Sin:

                  // Sinus Generator
                  sampleValue = currentGain * SampleSin(x, frequencyFactor, shift);
                  CalculateNextPhase(aggregateFMBuffer[sampleCount]);
                  break;

               case BasicSignalType.SawTooth:

                  // SawTooth Generator

                  sampleValue = currentGain * SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero);
                  CalculateNextPhase(aggregateFMBuffer[sampleCount]);
                  break;

               case BasicSignalType.Triangle:

                  // Triangle Generator

                  sampleValue = 2 * SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero);
                  if (sampleValue > 1)
                     sampleValue = 2 - sampleValue;
                  if (sampleValue < -1)
                     sampleValue = -2 - sampleValue;

                  sampleValue *= currentGain;

                  CalculateNextPhase(aggregateFMBuffer[sampleCount]);
                  break;

               case BasicSignalType.Square:

                  // Square Generator

                  sampleValue =
                     SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero) < 0 ?
                        currentGain : -currentGain;

                  CalculateNextPhase(aggregateFMBuffer[sampleCount]);
                  break;

               case BasicSignalType.Pink:

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

               case BasicSignalType.White:

                  // White Noise Generator
                  sampleValue = (currentGain * NextRandomTwo());
                  break;

               default:
                  sampleValue = 0.0;
                  break;
            }
            CalculateNextGain();
            // Phase Reverse, Gain Per Channel and AM signal
            for (int i = 0; i < WaveFormat.Channels; i++)
            {
               buffer[outIndex++] = (float)sampleValue * aggregateAMBuffer[sampleCount];
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

      private void CalculateNextPhase(float fmValue)
      {
         // move to next phase and apply FM
         phase += currentPhaseStep + fmValue;
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
   /// Basic Signal type
   /// </summary>
   public enum BasicSignalType
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

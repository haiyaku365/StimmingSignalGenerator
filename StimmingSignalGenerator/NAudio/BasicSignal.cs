using NAudio.Utils;
using NAudio.Wave;
using System;

namespace StimmingSignalGenerator.NAudio
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
   public class BasicSignal : ISampleProvider
   {
      /// <summary>
      /// Initializes a new instance for the Generator
      /// </summary>
      public BasicSignal(double initGain = 1, double initFrequency = 440.0)
      {
         WaveFormat = Constants.Wave.DefaultMonoWaveFormat;

         // Default
         Type = BasicSignalType.Sin;
         ZeroCrossingPosition = 0.5;

         rampGain = new RampGain(initGain);

         CurrentPhaseStep = initFrequency;
         TargetPhaseStep = initFrequency;
         PhaseStepDelta = 0;
         SeekFrequency = false;

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
         AMSignals = new AggregableSignals(1, (current, next, signal) => current * (next + 2 - (float)signal.Gain) / 2);
         FMSignals = new AggregableSignals(0, (current, next, _) => current + next);
         PMSignals = new AggregableSignals(0, (current, next, _) => current + next);

         //init noise
         for (int i = 0; i < noiseValue.Length; i++)
         {
            noiseValue[i] = SampleWhite();
         }
      }

      /// <summary>
      /// The waveformat of this WaveProvider (same as the source)
      /// </summary>
      public WaveFormat WaveFormat { get; }

      /// <summary>
      /// Type of Generator.
      /// </summary>
      public BasicSignalType Type { get; set; }

      #region Frequency and Phase field, prop
      /// <summary>
      /// Frequency for the Generator. (Hz)
      /// </summary>
      public double Frequency
      {
         get => TargetPhaseStep;
         set
         {
            TargetPhaseStep = value;
            SeekFrequency = true;
         }
      }
      /// <summary>
      /// Phase shift (0.0 to 1.0)
      /// </summary>
      public double PhaseShift { get; set; }
      public void SetFrequencyAndPhaseTo(BasicSignal basicSignal)
      {
         SeekFrequency = basicSignal.SeekFrequency;
         TargetPhaseStep = basicSignal.TargetPhaseStep;
         CurrentPhaseStep = basicSignal.CurrentPhaseStep;
         PhaseStepDelta = basicSignal.PhaseStepDelta;
         Phase = basicSignal.Phase;
      }

      public bool SeekFrequency { get; private set; }
      public double TargetPhaseStep { get; private set; }
      public double CurrentPhaseStep { get; private set; }
      public double PhaseStepDelta { get; private set; }
      public double Phase { get; private set; }
      private double Period => WaveFormat.SampleRate;
      private bool isPeriodCycle;
      /// <summary>
      /// Position when signal cross zero default 0.5 (0.0 to 1.0)
      /// Noise ignore this
      /// </summary>
      public double ZeroCrossingPosition { get; set; }
      #endregion

      #region Amplitude field, Prop, Method
      private readonly RampGain rampGain;
      /// <summary>
      /// Gain for the Generator. (0.0 to 1.0)
      /// </summary>
      public double Gain { get => rampGain.Gain; set => rampGain.Gain = value; }
      /// <summary>
      /// Gain before seek to target gain.
      /// </summary>
      public double CurrentGain => rampGain.CurrentGain;
      /// <summary>
      /// Gain step delta of latest read.
      /// </summary>
      public double GainStepDelta => rampGain.GainStepDelta;
      #endregion

      #region Modulation field, Prop, Add, Remove method
      readonly AggregableSignals AMSignals;
      readonly AggregableSignals FMSignals;
      readonly AggregableSignals PMSignals;

      float[] aggregateAMBuffer;
      float[] aggregateFMBuffer;
      float[] aggregatePMBuffer;
      /// <summary>
      /// 1 Channel Signal for amplitude modulation.
      /// </summary>
      /// <param name="signal">1 Channel Signal</param>
      public void AddAMSignal(BasicSignal signal) => AMSignals.Add(signal);
      public void RemoveAMSignal(BasicSignal signal) => AMSignals.Remove(signal);
      /// <summary>
      /// Add 1 Channel Signal for frequency modulation. Gain of signal indicate how much frequency change.
      /// </summary>
      /// <param name="signal">1 Channel Signal</param>
      public void AddFMSignal(BasicSignal signal) => FMSignals.Add(signal);
      public void RemoveFMSignal(BasicSignal signal) => FMSignals.Remove(signal);
      /// <summary>
      /// Add 1 Channel Signal for phase modulation.
      /// </summary>
      /// <param name="signal">1 Channel Signal</param>
      public void AddPMSignal(BasicSignal signal) => PMSignals.Add(signal);
      public void RemovePMSignal(BasicSignal signal) => PMSignals.Remove(signal);
      #endregion

      #region Noise generator field, prop, method
      // Random Number for the White Noise & Pink Noise Generator
      private readonly Random random = new Random();
      private readonly double[] pinkNoiseBuffer = new double[7];
      private readonly double[] noiseValue = new double[4];
      #endregion

      /// <summary>
      /// Reads from this provider.
      /// </summary>
      public int Read(float[] buffer, int offset, int count)
      {
         // Generator current value
         double sampleValue;

         // Once per Read variable
         double zeroCrossingPoint = ZeroCrossingPosition * Period;
         double beforeZCFrequencyFactor = 1 / ZeroCrossingPosition;
         double beforeZCShift = 0;
         double afterZCFrequencyFactor = 1 / (1 - ZeroCrossingPosition);
         double afterZCShift = -Period;
         double x = 0, frequencyFactor = 0, shift = 0;
         bool isBeforeCrossingZero = true;
         double noisePre, noisePost;

         // Calc gainStepDelta
         rampGain.CalculateGainStepDelta(count);

         // Calc frequencyStepDelta
         if (SeekFrequency) // process frequency change only once per call to Read
         {
            PhaseStepDelta = (TargetPhaseStep - CurrentPhaseStep) / count;
            SeekFrequency = false;
         }

         // read modulation signal
         aggregateAMBuffer = BufferHelpers.Ensure(aggregateAMBuffer, count);
         AMSignals.Read(aggregateAMBuffer, offset, count);

         aggregateFMBuffer = BufferHelpers.Ensure(aggregateFMBuffer, count);
         FMSignals.Read(aggregateFMBuffer, offset, count);
         
         aggregatePMBuffer = BufferHelpers.Ensure(aggregatePMBuffer, count);
         PMSignals.Read(aggregatePMBuffer, offset, count);


         //skip calc if gain is 0
         if (CurrentGain == 0)
         {
            Array.Fill(buffer, 0, offset, count);

            //prevent out of phase when mixing multi signal
            for (int i = offset; i < count; i++)
            {
               CalculateNextPhase(aggregateFMBuffer[i]);
               rampGain.CalculateNextGain();
            }
            return count;
         }

         // Complete Buffer
         for (int sampleCount = offset; sampleCount < count; sampleCount++)
         {
            //calculate common variable
            x = Phase + ((PhaseShift + aggregatePMBuffer[sampleCount]) * Period);
            switch (Type)
            {
               case BasicSignalType.Sin:
               case BasicSignalType.SawTooth:
               case BasicSignalType.Triangle:
               case BasicSignalType.Square:
                  //Canot use % here x < 0 will cause SampleSaw calc error
                  if (x < 0) x += Period; //phase shift to the past
                  else if (x > Period) x -= Period; // phase shift to the future

                  isBeforeCrossingZero = 0 <= x && x < zeroCrossingPoint;
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
                  break;
               case BasicSignalType.Pink:
               case BasicSignalType.White:
               default:
                  break;
            }

            switch (Type)
            {
               case BasicSignalType.Sin:
                  // Sinus Generator
                  sampleValue = CurrentGain * SampleSin(x, frequencyFactor, shift);
                  break;

               case BasicSignalType.SawTooth:
                  // SawTooth Generator
                  sampleValue = CurrentGain * SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero);
                  break;

               case BasicSignalType.Triangle:
                  // Triangle Generator
                  sampleValue = -2 * SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero);
                  if (sampleValue > 1)
                     sampleValue = 2 - sampleValue;
                  if (sampleValue < -1)
                     sampleValue = -2 - sampleValue;

                  sampleValue *= CurrentGain;
                  break;

               case BasicSignalType.Square:
                  // Square Generator
                  sampleValue =
                     SampleSaw(x, frequencyFactor, shift, isBeforeCrossingZero) < 0 ?
                        CurrentGain : -CurrentGain;

                  break;

               case BasicSignalType.Pink:
               case BasicSignalType.White:
                  // Pink Noise Generator
                  // White Noise Generator
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

                  // only get new random point when reach new period
                  if (isPeriodCycle)
                  {
                     //keep history for phase shift beyond period
                     noiseValue[0] = noiseValue[1];
                     noiseValue[1] = noiseValue[2];
                     noiseValue[2] = noiseValue[3];
                     noiseValue[3] = Type switch
                     {
                        BasicSignalType.White => SampleWhite(),
                        BasicSignalType.Pink => SamplePink()
                     };
                  }

#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

                  if (x < 0) //phase shift to the past
                  {
                     x += Period;
                     noisePre = noiseValue[0];
                     noisePost = noiseValue[1];
                  }
                  else if (x > Period) // phase shift to the future
                  {
                     x -= Period;
                     noisePre = noiseValue[2];
                     noisePost = noiseValue[3];
                  }
                  else
                  {
                     noisePre = noiseValue[1];
                     noisePost = noiseValue[2];
                  }
                  // Interpolate between random point
                  sampleValue = CurrentGain * ((noisePost - noisePre) / Period * x + noisePre);
                  break;

               default:
                  sampleValue = 0.0;
                  break;
            }
            // also CalculateNextPhase when do noise to avoid out of phase when sync with another signal
            CalculateNextPhase(aggregateFMBuffer[sampleCount]);
            rampGain.CalculateNextGain();
            // apply AM signal
            buffer[sampleCount] = (float)sampleValue * aggregateAMBuffer[sampleCount];
         }
         return count;
      }

      private void CalculateNextPhase(float fmValue)
      {
         // move to next phase and apply FM
         var nextPhaseStep = CurrentPhaseStep + fmValue;
         switch (Type)
         {
            case BasicSignalType.Sin:
            case BasicSignalType.SawTooth:
            case BasicSignalType.Triangle:
            case BasicSignalType.Square:
               Phase += nextPhaseStep;
               break;
            case BasicSignalType.Pink:
            case BasicSignalType.White:
               // Noise Phase only go forward if it go backward then freeze
               Phase += nextPhaseStep > 0 ? nextPhaseStep : 0;
               break;
            default:
               break;
         }
         isPeriodCycle = Phase >= Period;
         if (isPeriodCycle) Phase -= Period;
         if (CurrentPhaseStep != TargetPhaseStep)
         {
            //calculate currentPhaseStep
            CurrentPhaseStep += PhaseStepDelta;
            //correct if value exceed target
            if (PhaseStepDelta > 0 && CurrentPhaseStep > TargetPhaseStep ||
                PhaseStepDelta < 0 && CurrentPhaseStep < TargetPhaseStep)
               CurrentPhaseStep = TargetPhaseStep;
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
      /// White noise (Value form -1 to 1)
      /// </summary>
      /// <returns>Random value from -1 to +1</returns>
      private double SampleWhite()
      {
         return 2 * random.NextDouble() - 1;
      }
      private double SamplePink()
      {
         double white = SampleWhite();
         pinkNoiseBuffer[0] = 0.99886 * pinkNoiseBuffer[0] + white * 0.0555179;
         pinkNoiseBuffer[1] = 0.99332 * pinkNoiseBuffer[1] + white * 0.0750759;
         pinkNoiseBuffer[2] = 0.96900 * pinkNoiseBuffer[2] + white * 0.1538520;
         pinkNoiseBuffer[3] = 0.86650 * pinkNoiseBuffer[3] + white * 0.3104856;
         pinkNoiseBuffer[4] = 0.55000 * pinkNoiseBuffer[4] + white * 0.5329522;
         pinkNoiseBuffer[5] = -0.7616 * pinkNoiseBuffer[5] - white * 0.0168980;
         double pink = pinkNoiseBuffer[0] + pinkNoiseBuffer[1] + pinkNoiseBuffer[2] + pinkNoiseBuffer[3] + pinkNoiseBuffer[4] + pinkNoiseBuffer[5] + pinkNoiseBuffer[6] + white * 0.5362;
         pinkNoiseBuffer[6] = white * 0.115926;
         return pink / 5;
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

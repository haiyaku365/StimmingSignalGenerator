using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   public class RampGain
   {
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
      /// <summary>
      /// Gain before seek to target gain.
      /// </summary>
      public double CurrentGain { get; private set; }
      /// <summary>
      /// Gain step delta of latest read.
      /// </summary>
      public double GainStepDelta { get; private set; }

      private bool seekGain;
      private double targetGain;
      private readonly TimeSpan rampTimeSpan;

      public RampGain(double initGain = 1)
      {
         this.rampTimeSpan = TimeSpan.FromMilliseconds(100);
         CurrentGain = initGain;
         targetGain = initGain;
         GainStepDelta = 0;
         seekGain = false;
      }

      /// <summary>
      /// Call this each read to update GainStepDelta
      /// </summary>
      /// <param name="stepCount">Step take to ramp to target.</param>
      public void CalculateGainStepDelta(int stepCount)
      {
         if (seekGain) // process Gain change only once per call to Read
         {
            GainStepDelta = (targetGain - CurrentGain) / stepCount;
            seekGain = false;
         }
      }

      /// <summary>
      /// Call this after each step to update CurrentGain
      /// </summary>
      public void CalculateNextGain()
      {
         //calculate currentGain
         CurrentGain += GainStepDelta;
         //correct if value exceed target
         if (GainStepDelta > 0 && CurrentGain > targetGain ||
             GainStepDelta < 0 && CurrentGain < targetGain)
            CurrentGain = targetGain;
      }
   }
}

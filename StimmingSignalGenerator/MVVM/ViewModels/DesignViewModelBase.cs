using Splat;
using StimmingSignalGenerator.NAudio;
using StimmingSignalGenerator.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public abstract class DesignViewModelBase
   {
      protected static readonly Random random = new Random();
      protected static bool RandomBool(int percentChange) => RandomHelper.RandomBool(percentChange);
      protected static T GetRandomEnum<T>() where T : Enum => RandomHelper.GetRandomEnum<T>();
      protected static void PrepareAppState()
      {
         Locator.CurrentMutable.RegisterConstant(
            new AppState { });
      }
   }
}

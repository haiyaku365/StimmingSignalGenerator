using System;
using System.Collections.Generic;

namespace StimmingSignalGenerator.Helper
{
   public static class RandomHelper
   {
      static readonly Random rand = new Random();
      public static bool RandomBool(int percentChange) => rand.Next(0, 100) < percentChange;
      public static T GetRandomEnum<T>() where T : Enum => GetEnumValues<T>().GetRandom();
      public static T GetRandom<T>(this T[] array) => array[rand.Next(0, array.Length)];
      public static T GetRandom<T>(this List<T> items) => items[rand.Next(0, items.Count)];
      static T[] GetEnumValues<T>() where T : Enum => (T[])Enum.GetValues(typeof(T));
   }
}

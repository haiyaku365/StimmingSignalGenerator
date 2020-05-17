using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   internal static class SourceCacheViewModelHelper
   {
      public static void AddTo<T>(this T vm, SourceCache<T, int> sourceCache)
         where T : ISourceCacheViewModel
         => sourceCache.AddOrUpdate(vm);
      internal static T SetNameAndId<T>(this T vm,
         string namePrefix, SourceCache<T, int> sourceCache)
         where T : ISourceCacheViewModel
      {
         vm.Id = GetNextId(sourceCache);
         vm.Name = $"{namePrefix}{vm.Id}";
         return vm;
      }
      private static int GetNextId<T>(this SourceCache<T, int> sourceCache)
         where T : ISourceCacheViewModel =>
         sourceCache.Count == 0 ? 0 : sourceCache.Keys.Max() + 1;
   }
}

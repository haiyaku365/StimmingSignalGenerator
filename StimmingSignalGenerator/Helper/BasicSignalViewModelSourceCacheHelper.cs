using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.Helper
{
   static class BasicSignalViewModelSourceCacheHelper
   {
      public static void AddTo(this BasicSignalViewModel vm, SourceCache<BasicSignalViewModel, int> sourceCache)
         => sourceCache.AddOrUpdate(vm);
      public static async Task AddFromClipboard(this SourceCache<BasicSignalViewModel, int> sourceCache, string namePrefix)
      {
         var vm = await BasicSignalViewModel.PasteFromClipboard();
         if (vm == null) return;
         vm.SetNameAndId(namePrefix, sourceCache).AddTo(sourceCache);
      }
      public static BasicSignalViewModel SetNameAndId(this BasicSignalViewModel vm,
         string namePrefix, SourceCache<BasicSignalViewModel, int> sourceCache)
      {
         vm.Id = GetNextId(sourceCache);
         vm.Name = $"{namePrefix}{vm.Id}";
         return vm;
      }
      private static int GetNextId(this SourceCache<BasicSignalViewModel, int> sourceCache) =>
         sourceCache.Count == 0 ? 0 : sourceCache.Keys.Max() + 1;
   }
}

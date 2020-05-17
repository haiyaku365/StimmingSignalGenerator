using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   static class BasicSignalViewModelHelper
   {
      public static async Task AddFromClipboard(this SourceCache<BasicSignalViewModel, int> sourceCache, string namePrefix)
      {
         var vm = await BasicSignalViewModel.PasteFromClipboard();
         if (vm == null) return;
         vm.SetNameAndId(namePrefix, sourceCache).AddTo(sourceCache);
      }
   }
}

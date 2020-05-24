using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   static class BasicSignalViewModelHelper
   {
      public static async Task AddFromClipboard(this SourceList<BasicSignalViewModel> sourceList, string namePrefix)
      {
         var vm = await BasicSignalViewModel.PasteFromClipboard();
         if (vm == null) return;
         sourceList.Add(vm.SetName(namePrefix, sourceList));
      }
   }
}

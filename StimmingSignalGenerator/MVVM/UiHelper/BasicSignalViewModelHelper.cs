using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   static class BasicSignalViewModelHelper
   {
      public static async Task AddFromClipboard(
         this SourceList<BasicSignalViewModel> sourceList, 
         ISignalTree parent, 
         string namePrefix)
      {
         var vm = await BasicSignalViewModel.PasteFromClipboard(parent);
         if (vm == null) return;
         sourceList.Add(vm.SetName(namePrefix, sourceList));
      }
   }
}

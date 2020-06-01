using DynamicData;
using StimmingSignalGenerator.MVVM.ViewModels;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.UiHelper
{
   static class ClipboardItemHelper
   {
      public static async Task AddFromClipboard(
         this SourceList<BasicSignalViewModel> sourceList, 
         ISignalTree parent, 
         string namePrefix,
         CompositeDisposable disposable)
      {
         var vm = await BasicSignalViewModel.PasteFromClipboard(parent);
         vm.DisposeWith(disposable);
         if (vm == null) return;
         vm.AddAndSetName(namePrefix, sourceList);
      }

      public static async Task AddFromClipboard(
         this SourceList<TrackViewModel> sourceList,
         string name,
         CompositeDisposable disposable
         )
      {
         var vm = await TrackViewModel.PasteFromClipboard();
         vm.DisposeWith(disposable);
         if (vm == null) return;
         vm.AddAndSetName(name, sourceList);
      }
   }
}

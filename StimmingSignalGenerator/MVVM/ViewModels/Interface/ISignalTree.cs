using System;

namespace StimmingSignalGenerator.MVVM.ViewModels.Interface
{
   public interface ISignalTree : INamable
   {
      ISignalTree Parent { get; }
      String FullName { get; }
      IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded { get; }
      IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved { get; }
   }
}

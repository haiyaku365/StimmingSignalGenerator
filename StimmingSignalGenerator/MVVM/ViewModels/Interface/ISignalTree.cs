using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels.Interface
{
   public interface ISignalTree
   {
      ISignalTree Parent { get; }
      IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded { get; }
      IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved { get; }
   }
}

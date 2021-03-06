using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.UiHelper;
using StimmingSignalGenerator.MVVM.ViewModels;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class ControlSliderView : ReactiveUserControlEx<ControlSliderViewModel>
   {
      readonly string[] NumericUpDownNames = {
         "MinValueNumericUpDown",
         "ValueNumericUpDown",
         "MaxValueNumericUpDown" };
      NumericUpDown[] NumericUpDowns =>
         NumericUpDownNames.Select(x => this.FindControl<NumericUpDown>(x)).ToArray();

      public ControlSliderView()
      {
         this.WhenActivated(disposables =>
         {
            (NumericUpDown numericUpDown, Action<double> setVmValue, Func<double> getVmValue)[]
               NumericUpDownToVmBinder = new (NumericUpDown, Action<double>, Func<double>)[]
            {
               (NumericUpDowns[0], x => ViewModel.MinValue = x , () => ViewModel.MinValue),
               (NumericUpDowns[1], x => ViewModel.Value    = x , () => ViewModel.Value),
               (NumericUpDowns[2], x => ViewModel.MaxValue = x , () => ViewModel.MaxValue),
            };
            // bind VM to V
            SafeViewModel(vm =>
               vm.WhenAnyValue(x => x.MinValue, x => x.Value, x => x.MaxValue)
                  .Subscribe(_ =>
                  {
                     NumericUpDowns[0].Value = vm.MinValue;
                     NumericUpDowns[1].Value = vm.Value;
                     NumericUpDowns[2].Value = vm.MaxValue;
                  })
                  .DisposeWith(disposables)
            );

            // bind V to VM
            foreach (var (numericUpDown, setVmValue, getVmValue) in NumericUpDownToVmBinder)
            {
               //set value when spin number
               numericUpDown.ObservableSpinned()
                  .Subscribe(_ => setVmValue(numericUpDown.Value))
                  .DisposeWith(disposables);
               //set value when hit enter cancel when hit esc
               numericUpDown.ObservableKeyDown()
                  .Subscribe(x =>
                  {
                     if (x.EventArgs.Key == Key.Enter)
                     {
                        setVmValue(numericUpDown.Value);
                     }
                     else if (x.EventArgs.Key == Key.Escape)
                     {
                        numericUpDown.Value = getVmValue();
                     }
                  })
                  .DisposeWith(disposables);
               //set value when lost focus
               numericUpDown.ObservableLostFocus()
                  .Subscribe(_ => setVmValue(numericUpDown.Value))
                  .DisposeWith(disposables);
            }
         });
         InitializeComponent();
      }


      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}
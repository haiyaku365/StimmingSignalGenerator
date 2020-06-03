using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.ViewModels;
using System.Collections;
using System.Linq;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Interactivity;
using System.Reactive;
using Avalonia.Input;
using StimmingSignalGenerator.MVVM.UiHelper;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class ControlSliderView : ReactiveUserControl<ControlSliderViewModel>
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
            // In design time ViewModel can be null
            var cancelSub = new CancellationDisposable().DisposeWith(disposables);
            this.WhenAnyValue(x => x.ViewModel)
            .Subscribe(_ =>
            {
               if (ViewModel == null) return; 
               ViewModel.WhenAnyValue(x => x.MinValue, x => x.Value, x => x.MaxValue)
                  .Subscribe(_ =>
                  {
                     NumericUpDowns[0].Value = ViewModel.MinValue;
                     NumericUpDowns[1].Value = ViewModel.Value;
                     NumericUpDowns[2].Value = ViewModel.MaxValue;
                  })
                  .DisposeWith(disposables);
               //cancel WhenAnyValue(x => x.ViewModel) after success binding
               cancelSub.Dispose();
            }, cancelSub.Token);

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
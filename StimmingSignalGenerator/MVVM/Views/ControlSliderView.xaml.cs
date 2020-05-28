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
            // TODO fixes design time ViewModel == null
            ViewModel?.WhenAnyValue(x => x.MinValue, x => x.Value, x => x.MaxValue)
               .Subscribe(_ =>
               {
                  NumericUpDowns[0].Value = ViewModel.MinValue;
                  NumericUpDowns[1].Value = ViewModel.Value;
                  NumericUpDowns[2].Value = ViewModel.MaxValue;
               })
               .DisposeWith(disposables);

            // bind V to VM
            foreach (var (numericUpDown, setVmValue, getVmValue) in NumericUpDownToVmBinder)
            {
               //set value when spin number
               ObserveSpinned(numericUpDown)
                  .Subscribe(_ => setVmValue(numericUpDown.Value))
                  .DisposeWith(disposables);
               //set value when hit enter cancel when hit esc
               ObserveKeyDown(numericUpDown)
                  .Subscribe(x =>
                  {
                     if (x.EventArgs.Key == Key.Enter)
                     {
                        setVmValue(numericUpDown.Value);
                     }
                     if (x.EventArgs.Key == Key.Escape)
                     {
                        numericUpDown.Value = getVmValue();
                     }
                  })
                  .DisposeWith(disposables);
               //set value when lost focus
               ObserveLostFocus(numericUpDown)
                  .Subscribe(_ => setVmValue(numericUpDown.Value))
                  .DisposeWith(disposables);
            }
         });
         InitializeComponent();
      }
      IObservable<EventPattern<SpinEventArgs>> ObserveSpinned(NumericUpDown numericUpDown)
         => Observable.FromEventPattern<SpinEventArgs>(
            h => numericUpDown.Spinned += h,
            h => numericUpDown.Spinned -= h);
      IObservable<EventPattern<KeyEventArgs>> ObserveKeyDown(IControl control)
         => Observable.FromEventPattern<KeyEventArgs>(
            h => control.KeyDown += h,
            h => control.KeyDown -= h);
      IObservable<EventPattern<RoutedEventArgs>> ObserveLostFocus(IControl control)
         => Observable.FromEventPattern<RoutedEventArgs>(
            h => control.LostFocus += h,
            h => control.LostFocus -= h);

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}
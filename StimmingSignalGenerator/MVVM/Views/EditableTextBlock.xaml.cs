using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StimmingSignalGenerator.MVVM.UiHelper;
using System.Reactive.Disposables;
using System;
using System.Reactive.Linq;
using ReactiveUI;
using Avalonia.Input;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class EditableTextBlock : UserControl
   {
      public static readonly DirectProperty<EditableTextBlock, string> TextProperty =
        TextBox.TextProperty.AddOwner<EditableTextBlock>(
           o => o.Text,
            (o, v) => o.Text = v);
      public string Text { get { return text; } set { SetAndRaise(TextProperty, ref text, value); } }

      private string text;
      private Panel Panel => this.FindControl<Panel>("Panel");
      private TextBox TextBox => this.FindControl<TextBox>("TextBox");
      private TextBlock TextBlock => this.FindControl<TextBlock>("TextBlock");
      private bool IsPointerOverOrFocused => Panel.IsPointerOver || TextBox.IsFocused;

      readonly CompositeDisposable disposables = new CompositeDisposable();
      public EditableTextBlock()
      {
         this.ObservableAttachedToVisualTree()
            .Subscribe(_ =>
            {
               #region Binding
               TextBlock
                  .Bind(TextBlock.TextProperty, this.WhenAnyValue(x => x.Text))
                  .DisposeWith(disposables);
               TextBox
                  .Bind(TextBox.TextProperty, this.WhenAnyValue(x => x.Text))
                  .DisposeWith(disposables);

               TextBlock.GetObservable(TextBlock.TextProperty)
                  .Subscribe(x => Text = x)
                  .DisposeWith(disposables);
               //set value when hit enter cancel when hit esc
               TextBox.ObservableKeyDown()
                  .Subscribe(x =>
                  {
                     if (x.EventArgs.Key == Key.Enter)
                     {
                        Text = TextBox.Text;
                        Panel.Focus();
                     }
                     else if (x.EventArgs.Key == Key.Escape)
                     {
                        TextBox.Text = Text;
                        Panel.Focus();
                     }
                  })
                  .DisposeWith(disposables);
               //set value when lost focus
               TextBox.ObservableLostFocus()
                  .Subscribe(_ => Text = TextBox.Text)
                  .DisposeWith(disposables);
               #endregion

               this.WhenAnyValue(
                  property1: x => x.Panel.IsPointerOver,
                  property2: x => x.TextBox.IsFocused)
               .Subscribe(_ =>
               {
                  TextBox.IsVisible = IsPointerOverOrFocused;
                  TextBlock.IsVisible = !IsPointerOverOrFocused;
               })
               .DisposeWith(disposables);
            })
            .DisposeWith(disposables);

         //cleanup
         this.ObservableDetachedFromVisualTree()
            .Subscribe(_ => disposables.Dispose())
            .DisposeWith(disposables);

         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}
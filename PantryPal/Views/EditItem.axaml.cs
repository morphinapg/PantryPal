using Avalonia.Controls;
using Avalonia.Threading;
using PantryPal.ViewModels;
using System;
using System.Threading.Tasks;

namespace PantryPal.Views
{
    public partial class EditItem : UserControl
    {
        public EditItem()
        {
            InitializeComponent();
        }

        TextBox? CurrentFocused;

        private async void TextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
        {
            if (sender is TextBox textbox && textbox != CurrentFocused)
            {
                CurrentFocused = textbox;
                if (textbox.Parent is not null && textbox.Parent.Parent is not null)
                {
                    if (textbox.Parent.Parent.Parent is ScrollViewer scrollViewer)
                    {
                        if (OperatingSystem.IsAndroid() && textbox.Tag is not null && textbox.Tag.ToString()=="Scroll")
                            scrollViewer.ScrollToEnd();

                        await Task.Delay(100);

                        textbox.SelectAll();
                    }
                }
            }
        }

        private void Suggested_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is FoodItem item)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    item.SuggestedServing = null;
                }
            }
        }

        private async void Servings_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is FoodItem item)
            {
                if (string.IsNullOrEmpty(textBox.Text) || textBox.Text == "0")
                {
                    item.Quantity = 0;

                    await Task.Delay(100);

                    textBox.SelectAll();
                }
            }
        }

        private void TextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is FoodItem item)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    item.Quantity = 0;
                }
            }
        }
    }
}

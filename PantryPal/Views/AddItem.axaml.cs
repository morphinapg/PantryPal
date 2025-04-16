using Avalonia.Controls;
using PantryPal.ViewModels;
using System;
using System.Threading.Tasks;

namespace PantryPal.Views
{
    public partial class AddItem : UserControl
    {
        public AddItem()
        {
            InitializeComponent();

        }

        TextBox? CurrentFocused;

        public async void Textbox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
        {
            if (sender is TextBox textbox && textbox != CurrentFocused)
            {
                CurrentFocused = textbox;
                if (textbox.Parent is not null)
                {
                    if (textbox.Parent.Parent is ScrollViewer scrollViewer)
                    {
                        if (OperatingSystem.IsAndroid())
                        scrollViewer.ScrollToEnd();

                        await Task.Delay(100);
                        textbox.SelectAll();
                    }
                }
            }
        }

        private void TextBox_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            if (sender is TextBox textbox && DataContext is FoodItem item)
            {
                if (string.IsNullOrEmpty(textbox.Text) && textbox.Tag is string tag)
                {
                    switch (tag)
                    {
                        case "Calories":
                            item.Calories = null;
                            break;
                        case "PartsPerServing":
                            item.PartsPerServing = null; 
                            break;
                        case "Quantity":
                            item.MaxQuantity = null; 
                            break;
                        case "MinServings":
                            item.MinServings = null;
                            break;
                        case "MaxServings":
                            item.MaxServings = null;
                            break;
                    }
                }
            }
        }
    }
}

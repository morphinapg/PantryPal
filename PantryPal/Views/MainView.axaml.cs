using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using PantryPal.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PantryPal.Views;

public partial class MainView : UserControl
{
    IInsetsManager? InsetsManager;
    IInputPane? InputPane;
    TopLevel? toplevel;

    public MainView()
    {
        DataContextChanged += MainView_DataContextChanged;

        InitializeComponent();

        if (OperatingSystem.IsAndroid())
        {
            AddItemBox.Loaded += AddItemBox_Loaded;

            
        }        
    }

    private void MainView_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel model)
        {           
            model.BackupClicked += Model_BackupClicked;
            model.RestoreClicked += Model_RestoreClicked;
        }
    }

    

    private void AddItemBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        toplevel = TopLevel.GetTopLevel(AddItemBox);

        if (toplevel is not null)
        {
            InsetsManager = toplevel.InsetsManager;
            
            if (InsetsManager is not null)
                InsetsManager.DisplayEdgeToEdge = true;

            InputPane = toplevel.InputPane;

            if (InputPane is not null) 
                InputPane.StateChanged += InputPane_StateChanged;


            //if (InsetsManager is not null)
            //{
            //    InsetsManager.SafeAreaChanged += InsetsManager_SafeAreaChanged;
            //}

            toplevel.BackRequested += TopLevel_BackRequested;
        }

        
    }

    private void InputPane_StateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (DataContext is MainViewModel model && InputPane is not null && InsetsManager is not null)
        {
            var OccludedArea = InputPane.OccludedRect;            

            model.SafeArea = new Avalonia.Thickness(0, 0, 0, OccludedArea.Height);

            if (!model.Filterclearing)
            {
                model.FilterFocused = FilterBox.IsFocused;
            }
        }
    }

    [Serializable]
    record BackupContainer(ObservableCollection<FoodItem> AllFoods, ObservableCollection<FoodItem> BackupFoods);

    private async void Model_BackupClicked(object? sender, EventArgs e)
    {
        if (toplevel is null)
            toplevel = TopLevel.GetTopLevel(AddItemBox);

        if (toplevel is not null && toplevel.StorageProvider is not null && DataContext is MainViewModel model)
        {
            var file = await toplevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Backup",
                SuggestedFileName = "DatabaseBackup-" + DateTime.Now.ToString("yyyyMMddHHmm") + ".xml",
            });

            if (file is not null)
            {

                await Task.Run(async () =>
                {
                    using (var stream = await file.OpenWriteAsync())
                    {
                        var BackupData = new BackupContainer(model.AllFoods, model.BackupFoods);

                        new DataContractSerializer(typeof(BackupContainer)).WriteObject(stream, BackupData);
                    }
                });
            }
        }
    }

    private async void Model_RestoreClicked(object? sender, EventArgs e)
    {
        if (toplevel is null)
            toplevel = TopLevel.GetTopLevel(AddItemBox);

        if (toplevel is not null && toplevel.StorageProvider is not null && DataContext is MainViewModel model)
        {
            var files = await toplevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Backup",
                AllowMultiple = false
            });

            if (files.Any())
            {
                try
                {
                    var item = await Task.Run(async () =>
                    {
                        using (var stream = await files[0].OpenReadAsync())
                        {
                            return new DataContractSerializer(typeof(BackupContainer)).ReadObject(stream);
                        }
                    });

                    if (item is BackupContainer data)
                    {
                        model.AllFoods = data.AllFoods;
                        model.Filter_Foods();

                        model.BackupFoods = new ObservableCollection<FoodItem>(data.BackupFoods.OrderBy(x => x.Name).ThenByDescending(x => x.Calories));

                        model.Save();
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        var item = await Task.Run(async () =>
                        {
                            using (var stream = await files[0].OpenReadAsync())
                            {
                                return new DataContractSerializer(typeof(ObservableCollection<FoodItem>)).ReadObject(stream);
                            }
                        });

                        if (item is ObservableCollection<FoodItem> data)
                        {
                            model.AllFoods = data;
                            model.Filter_Foods();

                            model.Save();
                        }
                    }

                    catch (Exception)
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Error", "Error reading file. The file may not be a PantryPal backup file.", ButtonEnum.Ok, Icon.Error).ShowAsync();
                    }                    
                }   
                
            }
        }
    }

    //bool DialogOpen = false;

    private void TopLevel_BackRequested(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel model && model.CurrentItem is not null)
        {
            e.Handled = true;

            model.CurrentItem_Cancel_Clicked(sender, e);

            //if (!DialogOpen)
            //{
            //    DialogOpen = true;

            //    var response = await MessageBoxManager.GetMessageBoxStandard("Are you sure?", "Are you sure you want to cancel?", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question).ShowAsync();

            //    if (response == ButtonResult.Yes)
            //    {
            //        model.CurrentItem_Cancel_Clicked(sender, e);
            //    }

            //    DialogOpen = false;
            //}            
        }
    }

    private void InsetsManager_SafeAreaChanged(object? sender, Avalonia.Controls.Platform.SafeAreaChangedArgs e)
    {
        if (DataContext is MainViewModel model && InsetsManager is not null)
        {
            model.SafeArea = InsetsManager.SafeAreaPadding;

            if (!model.Filterclearing)
            {
                model.FilterFocused = FilterBox.IsFocused;
            }
        }
    }

    private void ListBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel model)
        {
            model.EditItem();
        }
    }

    private void TextBox_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && DataContext is MainViewModel model)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                model.Filter = null;
            }
        }
    }

    private void FilterBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel model)
        {
            model.FilterCleared += Model_FilterCleared;
        }
    }

    private void Model_FilterCleared(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel model)
        {
            if (!model.FilterFocused)
                AddItemBox.Focus();

            model.Filterclearing = false;
        }
        
    }

    private void TextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        if (DataContext is MainViewModel model && model.Filterclearing)
        {
            if (!model.FilterFocused)
                AddItemBox.Focus();
        }
    }
}

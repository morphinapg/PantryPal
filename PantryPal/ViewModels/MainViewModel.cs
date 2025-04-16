using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.VisualBasic.FileIO;
using PantryPal.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Collections.Generic;
using MsBox.Avalonia;

namespace PantryPal.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public CommandHandler Add_Item => new CommandHandler(AddItem);

    FoodItem? _currentItem;
    public FoodItem? CurrentItem
    {
        get => _currentItem;
        set
        {
            _currentItem = value;
            OnPropertyChanged(nameof(CurrentItem));
        }
    }

    bool _addItemVisible;
    public bool AddItemVisible
    {
        get => _addItemVisible;
        set
        {
            _addItemVisible = value;
            OnPropertyChanged(nameof(AddItemVisible));
            OnPropertyChanged(nameof(HideList));
        }
    }

    public bool HideList => !AddItemVisible;

    UserControl? _addItemPanel;
    public UserControl? AddItemPanel
    {
        get => _addItemPanel;
        set
        {
            _addItemPanel = value;
            OnPropertyChanged(nameof(AddItemPanel));
        }
    }

    public enum SortModes : int
    {
        Default,
        CaloriesDescending,
        CaloriesAscending,
        ExpirationDescending,
        ExpirationAscending
    }

    int _sortOrder = (int)SortModes.Default;
    public int SortOrder
    {
        get => _sortOrder;
        set
        {
            _sortOrder = value;
            OnPropertyChanged(nameof(SortOrder));
            OnPropertyChanged(nameof(FilteredFoods));
        }
    }

    Thickness _safeArea = new Thickness(0);
    public Thickness SafeArea
    {
        get => _safeArea;
        set
        {
            _safeArea = value;
            OnPropertyChanged(nameof(SafeArea));
        }
    }

    FoodItem? _selectedItem;
    public FoodItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged(nameof(SelectedItem));
        }
    }

    FoodItem? BackupItem;

    double? _filter;
    public double? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            OnPropertyChanged(nameof(Filter));
            OnPropertyChanged(nameof(FilteredFoods));
        }
    }


    public ObservableCollection<FoodItem> AllFoods = new();

    ObservableCollection<FoodItem> _backupFoods = new();
    public ObservableCollection<FoodItem> BackupFoods
    {
        get => _backupFoods;
        set
        {
            _backupFoods = value;
            OnPropertyChanged(nameof(BackupFoods));
        }
    }

    ObservableCollection<FoodItem> _filteredFoods = new();

    System.Timers.Timer PendingSave;

    bool _isSnack, _isFood;
    public bool IsSnack
    {
        get => _isSnack;
        set
        {
            _isSnack = value;
            OnPropertyChanged(nameof(IsSnack));
            OnPropertyChanged(nameof(FilteredFoods));

            if (value)
                IsFood = false;
        }
    }

    public bool IsFood
    {
        get => _isFood;
        set
        {
            _isFood = value;
            OnPropertyChanged(nameof(IsFood));
            OnPropertyChanged(nameof(FilteredFoods));

            if (value)
                IsSnack = false;
        }
    }

    public bool FilterFocused = false, Filterclearing = false;

    public ObservableCollection<FoodItem> FilteredFoods
    {
        get
        {
            Parallel.ForEach(AllFoods, x =>
            {
                x.Filter = Filter.HasValue ? Filter.Value : null;
                if (x.Quantity is null)
                {
                    x.Quantity = 1;
                    x.MaxQuantity = 1;
                }
            });

            var BaseFilter = IsFood || IsSnack ? AllFoods.Where(x => x.IsSnack == IsSnack) : AllFoods;

            if (Filter.HasValue)
                BaseFilter = BaseFilter.Where(x => x.FilteredCalories <= Filter.Value && x.FilteredCalories > 0);

            //BaseFilter = BaseFilter.OrderByDescending(x => x.Opacity).ThenBy(x => x.ExpireSort);

            switch (SortOrder)
            {
                case (int)SortModes.Default:
                    {
                        double? CaloriesAvg = BaseFilter.Average(x => x.FilteredCalories);
                        double? ExpirationAvg = BaseFilter.Average(x => x.ExpirationDate.HasValue ? x.ExpirationDate.Value.Ticks : default(double?));
                        double? LastTimeAvg = BaseFilter.Average(x => x.LastTime.HasValue ? x.LastTime.Value.Ticks : default(double?));
                        double? CaloriesDev = BaseFilter.Select(x => x.FilteredCalories - CaloriesAvg).Average(x => x * x);
                        double? ExpirationDev = BaseFilter.Select(x => (x.ExpirationDate.HasValue ? x.ExpirationDate.Value.Ticks : default(double?)) - ExpirationAvg).Average(x => x * x);
                        double? LastTimeDev = BaseFilter.Select(x => (x.LastTime.HasValue ? x.LastTime.Value.Ticks : default(double?)) - LastTimeAvg).Average(x => x * x);

                        if (CaloriesDev is not null)
                            CaloriesDev = Math.Sqrt(CaloriesDev.Value);

                        if (ExpirationDev is not null)
                            ExpirationDev = Math.Sqrt(ExpirationDev.Value);

                        if (LastTimeDev is not null)
                            LastTimeDev = Math.Sqrt(LastTimeDev.Value);

                        double MaxBounds = 1;
                        if (CaloriesAvg is not null && CaloriesDev is not null)
                        {
                            double
                                Min = BaseFilter.Where(x => x.FilteredCalories is not null).Min(x => x.FilteredCalories!.Value),
                                Max = BaseFilter.Where(x => x.FilteredCalories is not null).Max(x => x.FilteredCalories!.Value),
                                Bounds = Math.Max(Max - CaloriesAvg.Value, CaloriesAvg.Value - Min);

                            MaxBounds = Bounds / CaloriesDev.Value;
                        }

                        if (ExpirationAvg is not null && ExpirationDev is not null)
                        {
                            double
                                Min = BaseFilter.Where(x => x.ExpirationDate is not null).Min(x => x.ExpirationDate!.Value.Ticks),
                                Max = BaseFilter.Where(x => x.ExpirationDate is not null).Max(x => x.ExpirationDate!.Value.Ticks),
                                Bounds = Math.Max(Max - ExpirationAvg.Value, ExpirationAvg.Value - Min);

                            MaxBounds = Math.Max(MaxBounds, Bounds / ExpirationDev.Value);
                        }

                        if (LastTimeAvg is not null && LastTimeDev is not null)
                        {
                            double
                                Min = BaseFilter.Where(x => x.LastTime is not null).Min(x => x.LastTime!.Value.Ticks),
                                Max = BaseFilter.Where(x => x.LastTime is not null).Max(x => x.LastTime!.Value.Ticks),
                                Bounds = Math.Max(Max - LastTimeAvg.Value, LastTimeAvg.Value - Min);

                            MaxBounds = Math.Max(MaxBounds, Bounds / LastTimeDev.Value);
                        }

                        Parallel.ForEach(BaseFilter, x =>
                        {
                            double?
                                CaloriesZ = (x.FilteredCalories - CaloriesAvg) / CaloriesDev,
                                ExpirationValue = x.ExpirationDate.HasValue ? x.ExpirationDate.Value.Ticks : default(double?),
                                LastTimeValue = x.LastTime.HasValue ? x.LastTime.Value.Ticks : default(double?),
                                ExpirationZ = (ExpirationAvg - ExpirationValue) / ExpirationDev,
                                LastTimeZ = (LastTimeAvg - LastTimeValue) / LastTimeDev;

                            if (CaloriesZ is not null && double.IsNaN(CaloriesZ.Value))
                                CaloriesZ = 0;

                            if (ExpirationZ is not null && double.IsNaN(ExpirationZ.Value))
                                ExpirationZ = 0;

                            if (LastTimeZ is not null && double.IsNaN(LastTimeZ.Value))
                                LastTimeZ = 0;

                            if (LastTimeZ is null)
                                LastTimeZ = MaxBounds;

                            x.Score = ExpirationZ is not null ? 
                                CaloriesZ * 0.5 + ExpirationZ * 0.3 + LastTimeZ * 0.2 : 
                                CaloriesZ * 0.6 + LastTimeZ * 0.4;

                            
                        });

                        _filteredFoods.Clear();
                        //var NewList = BaseFilter.OrderByDescending(x => x.Opacity).ThenByDescending(x => x.Score);
                        var NewList = BaseFilter.OrderByDescending(x => x.Opacity).ThenByDescending(x => x.ExpireSort).ThenByDescending(x => x.Score);
                        foreach (var item in NewList)
                            _filteredFoods.Add(item);

                        break;
                    }                    
                case (int)SortModes.CaloriesDescending:
                    {
                        _filteredFoods.Clear();
                        var NewList = BaseFilter.OrderByDescending(x => x.Opacity).ThenByDescending(x => x.ExpireSort).ThenByDescending(x => x.FilteredCalories).ThenBy(x => x.ExpirationDate is null ? DateTime.MaxValue : x.ExpirationDate);
                        foreach (var item in NewList)
                        {
                            _filteredFoods.Add(item);
                        }
                        break;
                    }
                case (int)SortModes.CaloriesAscending:
                    {
                        _filteredFoods.Clear();
                        var NewList = BaseFilter.OrderByDescending(x => x.Opacity).ThenByDescending(x => x.ExpireSort).ThenBy(x => x.FilteredCalories).ThenBy(x => x.ExpirationDate is null ? DateTime.MaxValue : x.ExpirationDate);
                        foreach (var item in NewList)
                        {
                            _filteredFoods.Add(item);
                        }
                        break;
                    }
                case (int)SortModes.ExpirationDescending:
                    {
                        _filteredFoods.Clear();
                        var NewList = BaseFilter.OrderByDescending(x => x.Opacity).ThenByDescending(x => x.ExpireSort).ThenByDescending(x => x.ExpirationDate is null ? DateTime.MaxValue : x.ExpirationDate).ThenByDescending(x => x.FilteredCalories);
                        foreach (var item in NewList)
                        {
                            _filteredFoods.Add(item);
                        }
                        break;
                    }
                case (int)SortModes.ExpirationAscending:
                    {
                        _filteredFoods.Clear();
                        var NewList = BaseFilter.OrderByDescending(x => x.Opacity).ThenByDescending(x => x.ExpireSort).ThenBy(x => x.ExpirationDate is null ? DateTime.MaxValue : x.ExpirationDate).ThenByDescending(x => x.FilteredCalories);
                        foreach (var item in NewList)
                        {
                            _filteredFoods.Add(item);
                        }
                        break;
                    }
                default:
                    {
                        _filteredFoods.Clear();
                        var NewList = BaseFilter;
                        foreach (var item in NewList)
                        {
                            _filteredFoods.Add(item);
                        }
                        break;
                    }
            }

            return _filteredFoods;
        }
        set
        {
            _filteredFoods = value;
            OnPropertyChanged(nameof(FilteredFoods));
        }
    }

    public CommandHandler Clear_Filter => new CommandHandler(ClearFilter);

    void AddItem()
    {
        CurrentItem = new FoodItem();
        AddItemPanel = new AddItem();
        AddItemPanel.DataContext = CurrentItem;
        CurrentItem.Add_Clicked += CurrentItem_Add_Clicked;
        CurrentItem.Cancel_Clicked += CurrentItem_Cancel_Clicked;
        CurrentItem.Import_Clicked += CurrentItem_Import_Clicked;


        AddItemVisible = true;
    }

    private void CurrentItem_Import_Clicked(object? sender, EventArgs e)
    {
        ImportPanelVisible= true;
    }

    public void CurrentItem_Cancel_Clicked(object? sender, System.EventArgs e)
    {
        if (ImportPanelVisible)
        {
            ImportPanelVisible = false;
            SelectedBackup = null;
        }
            
        else if (CurrentItem is not null)
        {
            
            CurrentItem.Cancel_Clicked -= CurrentItem_Cancel_Clicked;

            if (BackupItem is not null)
            {
                AllFoods.Remove(CurrentItem);

                if (BackupItem.Quantity is null)
                {
                    BackupItem.Quantity = 1;
                    BackupItem.MaxQuantity = 1;
                }

                AllFoods.Add(BackupItem);
                BackupItem = null;

                CurrentItem.Save_Clicked -= CurrentItem_Save_Clicked;
                CurrentItem.Delete_Clicked -= CurrentItem_Delete_Clicked;
                CurrentItem.Edit_Clicked -= CurrentItem_Edit_Clicked;

                OnPropertyChanged(nameof(FilteredFoods));

                Save();
            }
            else
            {
                CurrentItem.Add_Clicked -= CurrentItem_Add_Clicked;
                CurrentItem.Import_Clicked -= CurrentItem_Import_Clicked;
            }

            CurrentItem = null;

            SelectedItem = null;

            AddItemVisible = false;
        }
        
        
    }

    private void CurrentItem_Add_Clicked(object? sender, System.EventArgs e)
    {
        if (CurrentItem is not null)
        {
            CurrentItem.Add_Clicked -= CurrentItem_Add_Clicked;
            CurrentItem.Cancel_Clicked -= CurrentItem_Cancel_Clicked;
            CurrentItem.Import_Clicked -= CurrentItem_Import_Clicked;

            
            if (CurrentItem.Name is not null)
                CurrentItem.Name = CurrentItem.Name.Trim();

            if (CurrentItem.Location is not null)
                CurrentItem.Location = CurrentItem.Location.Trim();

            if (!CurrentItem.UseNumberOfServings)
            {
                CurrentItem.Quantity = 1;
                CurrentItem.MaxQuantity = 1;
            }
            else if (CurrentItem.Quantity > CurrentItem.MaxQuantity)
                CurrentItem.Quantity = CurrentItem.MaxQuantity;
            else if (CurrentItem.Quantity is null)
                CurrentItem.Quantity = CurrentItem.MaxQuantity;
            else if (BackupItem is not null && BackupItem.Quantity == BackupItem.MaxQuantity && CurrentItem.MaxQuantity > BackupItem.MaxQuantity)
                CurrentItem.Quantity = CurrentItem.MaxQuantity;

            AllFoods.Add(CurrentItem);
            OnPropertyChanged(nameof(FilteredFoods));

            var MatchedGUID = BackupFoods.Where(x => x.GUID == CurrentItem.GUID).ToList();

            if (MatchedGUID.Any())
                foreach(var food in MatchedGUID)
                    BackupFoods.Remove(food);

            CurrentItem = null;
            BackupItem = null;

            Save();
        }

        AddItemVisible = false;
    }

    public void EditItem()
    {
        if (SelectedItem is not null)
        {
            CurrentItem = SelectedItem;
            BackupItem = new FoodItem(CurrentItem);
            AddItemPanel = new EditItem();
            AddItemPanel.DataContext = CurrentItem;

            CurrentItem.Save_Clicked += CurrentItem_Save_Clicked;
            CurrentItem.Delete_Clicked += CurrentItem_Delete_Clicked;
            CurrentItem.Cancel_Clicked += CurrentItem_Cancel_Clicked;
            CurrentItem.Edit_Clicked += CurrentItem_Edit_Clicked;

            CurrentItem.SuggestedServing = CurrentItem.FilteredCalories / CurrentItem.Calories;

            AddItemVisible = true;
        }        
    }

    private void CurrentItem_Edit_Clicked(object? sender, EventArgs e)
    {
        if (CurrentItem is not null)
        {
            CurrentItem.Save_Clicked-= CurrentItem_Save_Clicked;
            CurrentItem.Delete_Clicked-= CurrentItem_Delete_Clicked;
            CurrentItem.Edit_Clicked-= CurrentItem_Edit_Clicked;
            CurrentItem.Cancel_Clicked-= CurrentItem_Cancel_Clicked;

            AllFoods.Remove(CurrentItem);

            if (AddItemPanel is not null)
                AddItemPanel.DataContext = null;

            AddItemPanel = new AddItem();

            AddItemPanel.DataContext = CurrentItem;

            CurrentItem.Add_Clicked += CurrentItem_Add_Clicked;
            CurrentItem.Cancel_Clicked += CurrentItem_Cancel_Clicked;
        }
    }

    private void CurrentItem_Delete_Clicked(object? sender, EventArgs e)
    {
        if (CurrentItem is not null)
        {
            CurrentItem.Save_Clicked -= CurrentItem_Save_Clicked;
            CurrentItem.Delete_Clicked -= CurrentItem_Delete_Clicked;
            CurrentItem.Cancel_Clicked -= CurrentItem_Cancel_Clicked;

            var BackupFood = new FoodItem()
            {
                Name = CurrentItem.Name,
                Calories = CurrentItem.Calories,
                Emoji = CurrentItem.Emoji,
                PartsPerServing = CurrentItem.PartsPerServing,
                UsePartsPerServing = CurrentItem.UsePartsPerServing,
                MinServings = CurrentItem.MinServings,
                UseMinServings = CurrentItem.UseMinServings,
                IsSnack = CurrentItem.IsSnack,
                Location = CurrentItem.Location,
                MaxServings = CurrentItem.MaxServings,
                UseMaxServings = CurrentItem.UseMaxServings,
                LastTime = CurrentItem.LastTime,
                GUID = CurrentItem.GUID,
            };
            BackupFoods.Add(BackupFood);

            BackupFoods = new ObservableCollection<FoodItem>(BackupFoods.OrderBy(x => x.Name).ThenByDescending(x => x.Calories));

            AllFoods.Remove(CurrentItem);

            CurrentItem = null;
            BackupItem = null;
            
            OnPropertyChanged(nameof(FilteredFoods));

            SelectedItem = null;

            Save();
        }   

        AddItemVisible= false;
    }

    private void CurrentItem_Save_Clicked(object? sender, EventArgs e)
    {
        if (CurrentItem is not null)
        {
            CurrentItem.Save_Clicked -= CurrentItem_Save_Clicked;
            CurrentItem.Delete_Clicked -= CurrentItem_Delete_Clicked;
            CurrentItem.Cancel_Clicked -= CurrentItem_Cancel_Clicked;
            CurrentItem.Edit_Clicked -= CurrentItem_Edit_Clicked;

            //if (CurrentItem.UsePartsPerServing && CurrentItem.PartsPerServing.HasValue)
                //CurrentItem.Quantity = Convert.ToInt32( CurrentItem.Quantity * CurrentItem.PartsPerServing) / CurrentItem.PartsPerServing;

            CurrentItem = null;
            BackupItem = null;

            OnPropertyChanged(nameof(FilteredFoods));

            SelectedItem = null;

            Save();
        }

        AddItemVisible = false;
    }

    public MainViewModel()
    {
        PendingSave = new System.Timers.Timer(1000);
        PendingSave.AutoReset = false;
        PendingSave.Elapsed += PendingSave_Elapsed;

        LoadData();
    }

    async void LoadData()
    {
        string?
            Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PantryPal"),
            DatabasePath = Path.Combine(Folder, "database"),
            //Folder2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            BackupPath = Path.Combine(Folder, "backup");

        ObservableCollection<FoodItem>? LoadedData = null;

        try
        {
            if (File.Exists(DatabasePath))
                LoadedData = await ReadObjectAsync<ObservableCollection<FoodItem>>(DatabasePath);
        }
        catch (Exception)
        {
            if (File.Exists(DatabasePath + "-corrupted"))
                File.Delete(DatabasePath + "-corrupted");

            File.Move(DatabasePath, DatabasePath + "-corrupted");

            LoadedData = null;
        }

        try
        {
            if (LoadedData is null && File.Exists(DatabasePath + ".bak"))
                LoadedData = await ReadObjectAsync<ObservableCollection<FoodItem>>(DatabasePath + ".bak");
        }
        catch (Exception)
        {
            if (File.Exists(DatabasePath + "-corrupted.bak"))
                File.Delete(DatabasePath + "-corrupted.bak");

            File.Move(DatabasePath + ".bak", DatabasePath + "-corrupted.bak");

            LoadedData = null;
        }

        if (LoadedData is not null)
            AllFoods = LoadedData;

        LoadedData = null;

        try
        {
            if (File.Exists(BackupPath))
                LoadedData = await ReadObjectAsync<ObservableCollection<FoodItem>>(BackupPath);
        }
        catch (Exception)
        {
            if (File.Exists(BackupPath + "-corrupted"))
                File.Delete(BackupPath + "-corrupted");

            File.Move(BackupPath, BackupPath + "-corrupted");

            LoadedData = null;
        }

        try
        {
            if (LoadedData is null && File.Exists(BackupPath + ".bak"))
                LoadedData = await ReadObjectAsync<ObservableCollection<FoodItem>>(BackupPath + ".bak");
        }
        catch (Exception)
        {
            if (File.Exists(BackupPath + "-corrupted.bak"))
                File.Delete(BackupPath + "-corrupted.bak");

            File.Move(BackupPath + ".bak", BackupPath + "-corrupted.bak");

            LoadedData = null;
        }

        if (LoadedData is not null)
            BackupFoods = new ObservableCollection<FoodItem>(LoadedData.OrderBy(x => x.Name).ThenByDescending(x => x.Calories));


        Parallel.ForEach(AllFoods, x =>
        {
            if (x.Quantity is null)
            {
                x.Quantity = 1;
                x.MaxQuantity = 1;
            }

            if (x.GUID is null)
                x.GUID = Guid.NewGuid();
        });

        Parallel.ForEach(BackupFoods, x =>
        {
            if (x.GUID is null)
                x.GUID = Guid.NewGuid();
        });

        OnPropertyChanged(nameof(FilteredFoods));
    }

    public void Save()
    {
        PendingSave.Stop();
        PendingSave.Start();
    }

    private async void PendingSave_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        string?
            Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PantryPal"),
            DatabasePath = Path.Combine(Folder, "database"),
            BackupPath = Path.Combine(Folder, "backup");

        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);

        if (File.Exists(DatabasePath))
        {
            if (File.Exists(DatabasePath + ".bak"))
                File.Delete(DatabasePath + ".bak");

            File.Copy(DatabasePath, DatabasePath + ".bak");
        }    

        await WriteObjectAsync<ObservableCollection<FoodItem>>(DatabasePath, AllFoods);

        if (File.Exists(BackupPath))
        {
            if (File.Exists(BackupPath + ".bak"))
                File.Delete(BackupPath + ".bak");

            File.Copy(BackupPath, BackupPath + ".bak");
        }

        await WriteObjectAsync<ObservableCollection<FoodItem>>(BackupPath, BackupFoods);

    }


    async Task WriteObjectAsync<T>(string FileName, T item)
    {
        await Task.Run(() =>
        {
            using (var writer = new FileStream(FileName, FileMode.Create))
            {
                new DataContractSerializer(typeof(T)).WriteObject(writer, item);
            }
        });
    }

    async Task<T?> ReadObjectAsync<T>(string FileName)
    {
        var item = await Task.Run(() =>
        {
            using (var fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            {
                return new DataContractSerializer(typeof(T)).ReadObject(fs);
            }
        });

        if (item is null) return default;

        return (T)item;
    }

    public event EventHandler? FilterCleared;

    void ClearFilter()
    {
        Filterclearing = true;
        FilterCleared?.Invoke(this, EventArgs.Empty);
        Filter = null;        
    }

    public event EventHandler? BackupClicked;
    public CommandHandler Backup_Clicked => new CommandHandler(Backup);
    void Backup()
    {
        BackupClicked?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? RestoreClicked;
    public CommandHandler Restore_Clicked => new CommandHandler(Restore);
    void Restore()
    {
        RestoreClicked?.Invoke(this, EventArgs.Empty);
    }

    public void Filter_Foods()
    {
        OnPropertyChanged(nameof(FilteredFoods));
    }

    ImportItem _importPanel = new ImportItem();
    public ImportItem ImportPanel => _importPanel;

    bool _importPanelVisible = false;
    public bool ImportPanelVisible
    {
        get => _importPanelVisible;
        set
        {
            _importPanelVisible = value;
            OnPropertyChanged(nameof(ImportPanelVisible));
        }
    }

    public CommandHandler Cancel_Click => new CommandHandler(CancelImport);
    public CommandHandler Delete_Click => new CommandHandler(DeleteBackupFood);
    public CommandHandler Import_Click => new CommandHandler(ImportBackupFood);

    FoodItem? _selectedBackup = null;
    public FoodItem? SelectedBackup
    {
        get => _selectedBackup;
        set
        {
            _selectedBackup = value; 
            OnPropertyChanged(nameof(SelectedBackup));
        }
    }

    void CancelImport()
    {
        ImportPanelVisible = false;
        SelectedBackup = null;
    }

    async void DeleteBackupFood()
    {
        if (SelectedBackup is not null)
        {
            var result = await MessageBoxManager.GetMessageBoxStandard("Are you sure?", "Are you sure you want to delete this item from memory? You can't undo this!", MsBox.Avalonia.Enums.ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();

            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
                BackupFoods.Remove(SelectedBackup);

            Save();
        }        
    }

    void ImportBackupFood()
    {
        if ( SelectedBackup is not null)
        {
            CurrentItem = new FoodItem(SelectedBackup);
            if (AddItemPanel is not null)
            {
                CurrentItem.Add_Clicked -= CurrentItem_Add_Clicked;
                CurrentItem.Cancel_Clicked -= CurrentItem_Cancel_Clicked;
                CurrentItem.Import_Clicked -= CurrentItem_Import_Clicked;

                AddItemPanel.DataContext = CurrentItem;
                CurrentItem.Add_Clicked += CurrentItem_Add_Clicked;
                CurrentItem.Cancel_Clicked += CurrentItem_Cancel_Clicked;
                CurrentItem.Import_Clicked += CurrentItem_Import_Clicked;
            }
                
            ImportPanelVisible = false;
            SelectedBackup = null;
        }        
    }
}

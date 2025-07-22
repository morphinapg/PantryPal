using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PantryPal.ViewModels
{
    public partial class FoodItem : ViewModelBase
    {
        string? _name;
        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        string? _emoji;
        public string? Emoji
        {
            get => _emoji;
            set
            {
                _emoji = value;
                OnPropertyChanged(nameof(Emoji));
            }
        }

        double? _calories;
        public double? Calories
        {
            get => _calories;
            set
            {
                _calories = value;
                OnPropertyChanged(nameof(Calories));
            }
        }

        bool _usePartsPerServing;
        public bool UsePartsPerServing
        {
            get => _usePartsPerServing;
            set
            {
                _usePartsPerServing = value;
                OnPropertyChanged(nameof(UsePartsPerServing));
            }
        }

        double? _partsPerServing;
        public double? PartsPerServing
        {
            get
            {
                return _partsPerServing;
            }
            set
            {
                _partsPerServing = value;
                OnPropertyChanged(nameof(PartsPerServing));
            }
        }

        bool _useMinServings;
        public bool UseMinServings
        {
            get => _useMinServings;
            set
            {
                _useMinServings = value;
                OnPropertyChanged(nameof(UseMinServings));
            }
        }

        double? _minServings;
        public double? MinServings
        {
            get => _minServings;
            set
            {
                _minServings = value;
                OnPropertyChanged(nameof(MinServings));
            }
        }

        public double SliderStep
        {
            get
            {
                if (UsePartsPerServing && PartsPerServing.HasValue)
                {
                    return 1 / PartsPerServing.Value;
                }
                //else if (UseMinCalories && MinCalories.HasValue && Calories.HasValue)
                //    return MinCalories.Value / Calories.Value;
                else
                    return 1;
            }
        }

        DateTime? _expirationDate;
        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set
            {
                _expirationDate = value;
                OnPropertyChanged(nameof(ExpirationDate));
            }
        }

        public DateTime StartingDate => DateTime.Now;

        bool _isSnack;
        public bool IsSnack
        {
            get => _isSnack;
            set
            {
                _isSnack = value;
                OnPropertyChanged(nameof(IsSnack));
                OnPropertyChanged(nameof(IsFood));
            }
        }

        public bool IsFood => !IsSnack;

        string? _location;
        public string? Location
        {
            get => _location;
            set
            {
                _location = value;
                OnPropertyChanged(nameof(Location));
            }
        }

        double? _quantity;
        public double? Quantity
        {
            get
            {
                if (_quantity is null)
                    return null;

                //Allow for partial servings

                var MaxRounded = UsePartsPerServing ? Convert.ToInt32(MaxQuantity * PartsPerServing) / PartsPerServing : Convert.ToInt32(MaxQuantity);

                if (MaxQuantity > MaxRounded && _quantity > MaxRounded)
                {
                    double?
                        range = MaxQuantity - MaxRounded,
                        difference = _quantity - MaxRounded,
                        percent = difference / range;

                    if (percent > 0.5)
                        return MaxQuantity;
                    else
                        return MaxRounded;
                }

                double? servings = UsePartsPerServing ? Convert.ToInt32(_quantity * PartsPerServing) / PartsPerServing : Convert.ToInt32(_quantity);

                var max = MaxQuantity.HasValue ? MaxQuantity.Value : 1;

                return servings > max ? max : servings;
            }
            set
            {
                _quantity = value;
                
                OnPropertyChanged(nameof(Quantity));
            }
        }

        double? _maxQuantity;
        public double? MaxQuantity
        {
            get => _maxQuantity;
            set
            {
                _maxQuantity = value;

                if (value is not null)
                    UseNumberOfServings = true;
                OnPropertyChanged(nameof(MaxQuantity));
            }
        }

        double? _maxServings;
        public double? MaxServings
        {
            get => _maxServings;
            set
            {
                _maxServings = value;
                OnPropertyChanged(nameof(MaxServings));
            }
        }

        bool _useMaxServings;
        public bool UseMaxServings
        {
            get => _useMaxServings;
            set
            {
                _useMaxServings = value;
                OnPropertyChanged(nameof(UseMaxServings));
            }
        }

        bool _useNumberOfServings;
        public bool UseNumberOfServings
        {
            get => _useNumberOfServings;
            set
            {
                _useNumberOfServings = value;
                if (!value)
                {
                    Quantity = null;
                    MaxQuantity = null;
                }                    
                OnPropertyChanged(nameof(UseNumberOfServings));
                OnPropertyChanged(nameof(SliderEnabled));
            }
        }

        DateTime? _lastTime;
        public DateTime? LastTime
        {
            get => _lastTime;
            set
            {
                _lastTime = value;
                OnPropertyChanged(nameof(LastTime));
                OnPropertyChanged(nameof(Opacity));
            }
        }

        public double Opacity
        {
            get
            {
                if (LastTime is null || (DateTime.Now - LastTime.Value).TotalHours > 36)
                    return 1;

                return 0.5;
            }
        }

        public bool SliderEnabled => UseNumberOfServings || UsePartsPerServing;

        public event EventHandler? Add_Clicked;
        public event EventHandler? Cancel_Clicked;
        public event EventHandler? Delete_Clicked;
        public event EventHandler? Save_Clicked;
        public event EventHandler? Edit_Clicked;
        public event EventHandler? Import_Clicked;

        public CommandHandler Add_Click => new CommandHandler(Add);
        public CommandHandler Cancel_Click => new CommandHandler(Cancel);
        public CommandHandler Delete_Click => new CommandHandler(Delete);
        public CommandHandler Save_Click => new CommandHandler(Save);

        public CommandHandler Eat_One => new CommandHandler(EatOne);

        public CommandHandler Edit_Item => new CommandHandler(Edit);

        
        double? _filter;
        public double? Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                OnPropertyChanged(nameof(Filter));
                OnPropertyChanged(nameof(FilteredCalories));
                OnPropertyChanged(nameof(FilteredServings));
            }
        }

        public double? FilteredCalories
        {
            get
            {
                if (Filter.HasValue && Calories.HasValue)
                {
                    var part = UsePartsPerServing && PartsPerServing.HasValue ? PartsPerServing : 1;

                    var FilteredServings = Filter / Calories;

                    var servings = (int)(FilteredServings * part) / part;

                    var MaxRounded = UsePartsPerServing ? Convert.ToInt32(MaxQuantity * PartsPerServing) / PartsPerServing : Convert.ToInt32(MaxQuantity);

                    if (MaxQuantity > MaxRounded && FilteredServings >= MaxQuantity)
                    {
                        double?
                            range = MaxQuantity - MaxRounded,
                            difference = FilteredServings - MaxRounded,
                            percent = difference / range;

                        if (percent > 0.5)
                            servings = MaxQuantity;
                        else
                            servings = MaxRounded;
                    }

                    if (UseMaxServings && MaxServings < servings)
                        servings = MaxServings;

                    if (MaxQuantity.HasValue && servings > MaxQuantity)
                        servings = MaxQuantity;

                    if (Quantity.HasValue && servings > Quantity)
                        servings = Quantity;

                    if (UseMinServings && MinServings > servings)
                        servings = 0;  

                    var calories = servings * Calories;

                    SuggestedServing = servings;
                    OnPropertyChanged(nameof(SuggestedServing));
                    OnPropertyChanged(nameof(SuggestedCalories));
                    OnPropertyChanged(nameof(FilteredServings));

                    return Convert.ToInt32(calories);
                }
                else
                {
                    var Maximum = Quantity * Calories;

                    return Maximum < Calories ? Convert.ToInt32(Maximum) : Convert.ToInt32(Calories);
                }
                    
            }
        }

        double? _suggestedServing;
        public double? SuggestedServing
        {
            get
            {
                var serving = Filter.HasValue ? _suggestedServing : 1;
                return serving < Quantity ? serving : Quantity;
            }
            set
            {
                _suggestedServing = value;
                OnPropertyChanged(nameof(SuggestedServing));
                OnPropertyChanged(nameof(SuggestedCalories));
            }
        }

        public double? SuggestedCalories => Convert.ToInt32(SuggestedServing * Calories);

        public string? FilteredServings
        {
            get
            {
                if (SuggestedServing.HasValue && Quantity.HasValue)
                {
                    var servings = Filter.HasValue ? SuggestedServing.Value : Quantity.Value;


                    if (Filter.HasValue || servings == MaxQuantity)
                        return servings != 1 ? servings.ToString("#,##0.##") + " servings - " : "1 serving - ";
                    else
                        return servings != 1 ? servings.ToString("#,##0.##") + " servings left - " : "1 serving left - ";
                }
                else
                    return null;                
            }
        }

        public IBrush Color
        {
            get
            {
                if (ExpirationDate < DateTime.Now)
                    return new ImmutableSolidColorBrush(Colors.DarkRed, 0.25);
                else if (ExpirationDate.HasValue && (ExpirationDate.Value - DateTime.Now).Days < 7)
                    return new ImmutableSolidColorBrush(Colors.DarkGoldenrod, 0.25);
                else
                    return Brushes.Transparent;
            }
        }

        public int ExpireSort
        {
            get
            {
                if (ExpirationDate < DateTime.Now)
                    return -1;
                else if (ExpirationDate.HasValue && (ExpirationDate.Value - DateTime.Now).Days < 7)
                    return 1;
                else
                    return 0;
            }
        }

        async void Add()
        {
            if (string.IsNullOrEmpty(Name) || Calories is null)
                await MessageBoxManager.GetMessageBoxStandard("Required data missing", "The form must contain the name of the item and number of calories per serving.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
            else
                Add_Clicked?.Invoke(this, EventArgs.Empty);
        }

        void Cancel() => Cancel_Clicked?.Invoke(this, EventArgs.Empty);

        void EatOne()
        {
            LastTime = DateTime.Now;

            Quantity -= 1;
            if (Quantity < 0)
                Quantity = 0;
        }

        async void Delete()
        {
            var msgParams = new MessageBoxCustomParams()
            {
                ButtonDefinitions = new List<ButtonDefinition>
                {
                    new ButtonDefinition {Name = "Eat and Delete"},
                    new ButtonDefinition {Name = "Delete Only"},
                    new ButtonDefinition {Name = "Cancel"}
                },
                ContentTitle = "Are you sure?",
                ContentMessage = "Are you sure you want to delete this item?",
                Icon = MsBox.Avalonia.Enums.Icon.Warning
            };

            var result = await MessageBoxManager.GetMessageBoxCustom(msgParams).ShowAsync();

            if (result == "Eat and Delete")
                LastTime = DateTime.Now;

            if (result is not null && result != "Cancel")
                Delete_Clicked?.Invoke(this, EventArgs.Empty);
        }

        void Save()
        {
            Save_Clicked?.Invoke(this, EventArgs.Empty);
        }

        public FoodItem(FoodItem other)
        {
            Name = other.Name;
            Emoji = other.Emoji;
            Calories = other.Calories;
            PartsPerServing = other.PartsPerServing;
            UsePartsPerServing = other.UsePartsPerServing;
            MinServings = other.MinServings;
            UseMinServings = other.UseMinServings;
            ExpirationDate = other.ExpirationDate;
            IsSnack = other.IsSnack;
            Location = other.Location;
            MaxQuantity = other.MaxQuantity;
            Quantity = other.Quantity;            
            UseNumberOfServings = other.UseNumberOfServings;
            MaxServings = other.MaxServings;
            UseMaxServings = other.UseMaxServings;
            LastTime = other.LastTime;
            GUID = other.GUID;
        }

        public FoodItem()
        {
            GUID = Guid.NewGuid();
        }

        void Edit()
        {
            Edit_Clicked?.Invoke(this, EventArgs.Empty);
        }

        public CommandHandler Eat_Suggested => new CommandHandler(EatSuggested);
        void EatSuggested()
        {
            if (SuggestedServing >= 0.5)
                LastTime = DateTime.Now;

            Quantity -= SuggestedServing;   
        }

        //Will represent a recommendation score for Default sorting
        public double? Score;

        public Guid? GUID;


        public CommandHandler Import_Click => new CommandHandler(Import);
        void Import()
        {
            Import_Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}

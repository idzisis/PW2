using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using PotatoWarehouse.Data;
using PotatoWarehouse.Models;

namespace PotatoWarehouse;

public partial class MainWindow : Window
{
    private int _activeSeasonId;
    private string _activeSeasonName = "Nav izvēlēta";

    public MainWindow()
    {
        InitializeComponent();
        LoadActiveSeason();
        NavigateHome(this, new RoutedEventArgs());
    }

    private void IncomingWeight_Changed(object sender, TextChangedEventArgs e)
    {
        CalculateIncomingTotal();
    }

    private void IncomingCount_Changed(object sender, TextChangedEventArgs e)
    {
        CalculateIncomingTotal();
    }

    private void CalculateIncomingTotal()
    {
        if (double.TryParse(IncomingContainerWeight.Text, out double weight) && 
            int.TryParse(IncomingContainerCount.Text, out int count))
        {
            double total = weight * count;
            IncomingTotalWeight.Text = total.ToString("N3");
        }
        else
        {
            IncomingTotalWeight.Text = "";
        }
    }

    private void LoadActiveSeason()
    {
        using var context = new WarehouseDbContext();
        var settings = context.AppSettings.FirstOrDefault();
        
        if (settings?.ActiveSeasonId != null)
        {
            var season = context.Seasons.Find(settings.ActiveSeasonId);
            if (season != null)
            {
                _activeSeasonId = season.Id;
                _activeSeasonName = season.Name;
                SeasonLabel.Text = season.Name;
            }
        }
        else
        {
            var firstSeason = context.Seasons.FirstOrDefault();
            if (firstSeason != null)
            {
                settings!.ActiveSeasonId = firstSeason.Id;
                context.SaveChanges();
                _activeSeasonId = firstSeason.Id;
                _activeSeasonName = firstSeason.Name;
                SeasonLabel.Text = firstSeason.Name;
            }
        }

        SeasonLabel.Text = _activeSeasonName;
    }

    private void NavigateHome(object sender, RoutedEventArgs e)
    {
        HideAllPages();
        HomePage.Visibility = Visibility.Visible;
        LoadInventory();
    }

    private void NavigateIncoming(object sender, RoutedEventArgs e)
    {
        HideAllPages();
        IncomingPage.Visibility = Visibility.Visible;
        LoadIncomingData();
        LoadComboBoxes();
    }

    private void NavigateOutgoing(object sender, RoutedEventArgs e)
    {
        HideAllPages();
        OutgoingPage.Visibility = Visibility.Visible;
        LoadOutgoingData();
        LoadComboBoxes();
    }

    private void NavigateSettings(object sender, RoutedEventArgs e)
    {
        HideAllPages();
        SettingsPage.Visibility = Visibility.Visible;
        LoadSettingsData();
    }

    private void HideAllPages()
    {
        HomePage.Visibility = Visibility.Collapsed;
        IncomingPage.Visibility = Visibility.Collapsed;
        OutgoingPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
    }

    private void LoadInventory()
    {
        using var context = new WarehouseDbContext();

        var incomingRaw = (from i in context.IncomingPotatoes
                          join v in context.Varieties on i.VarietyId equals v.Id
                          join c in context.Calibers on i.CaliberId equals c.Id
                          where i.SeasonId == _activeSeasonId
                          select new { i.VarietyId, i.CaliberId, VarietyName = v.Name, CaliberName = c.Name, i.ContainerWeight, i.ContainerCount })
                          .ToList();

        var incoming = incomingRaw
            .GroupBy(i => new { i.VarietyId, i.CaliberId, i.VarietyName, i.CaliberName })
            .Select(g => new
            {
                g.Key.VarietyId,
                g.Key.CaliberId,
                g.Key.VarietyName,
                g.Key.CaliberName,
                Weight = g.Sum(x => x.ContainerWeight * x.ContainerCount) / 1000.0
            })
            .ToList();

        var outgoingRaw = context.OutgoingPotatoes
            .Where(o => o.SeasonId == _activeSeasonId)
            .Select(o => new { o.VarietyId, o.CaliberId, o.ContainerWeight, o.ContainerCount })
            .ToList();

        var outgoing = outgoingRaw
            .GroupBy(o => new { o.VarietyId, o.CaliberId })
            .Select(g => new
            {
                g.Key.VarietyId,
                g.Key.CaliberId,
                Weight = g.Sum(x => x.ContainerWeight * x.ContainerCount) / 1000.0
            })
            .ToList();

        var inventory = incoming.Select(i => new
        {
            i.VarietyId,
            i.CaliberId,
            i.VarietyName,
            i.CaliberName,
            Weight = i.Weight - (outgoing.FirstOrDefault(o => o.VarietyId == i.VarietyId && o.CaliberId == i.CaliberId)?.Weight ?? 0)
        }).Where(x => x.Weight > 0).ToList();

        // Group by variety for TreeView
        var groupedByVariety = inventory
            .GroupBy(i => i.VarietyName)
            .ToList();

        InventoryTree.Items.Clear();
        
        double totalWeight = 0;

        foreach (var varietyGroup in groupedByVariety)
        {
            var varietyWeight = varietyGroup.Sum(x => x.Weight);
            totalWeight += varietyWeight;

            var varietyItem = new TreeViewItem
            {
                Header = CreateVarietyHeader(varietyGroup.Key, varietyWeight),
                IsExpanded = true
            };

            foreach (var item in varietyGroup)
            {
                var caliberItem = new TreeViewItem
                {
                    Header = CreateCaliberHeader(item.CaliberName, item.Weight),
                    IsExpanded = true,
                    Padding = new Thickness(0, 4, 0, 4)
                };
                varietyItem.Items.Add(caliberItem);
            }

            InventoryTree.Items.Add(varietyItem);
        }

        TotalWeightText.Text = $"{totalWeight:N2} t";

        LoadHomeProgress();
    }

    private void LoadHomeProgress()
    {
        using var context = new WarehouseDbContext();

        var season = context.Seasons.Find(_activeSeasonId);
        if (season == null) return;

        var incomingTotal = context.IncomingPotatoes
            .Where(i => i.SeasonId == _activeSeasonId)
            .Sum(i => i.ContainerWeight * i.ContainerCount);

        var target = season.TargetWeight;
        var actual = incomingTotal;

        HomeTargetText.Text = $"{target / 1000:N1} t";
        HomeActualText.Text = $"{actual / 1000:N1} t";

        if (target <= 0)
        {
            HomeProgressPercentText.Text = "0%";
            HomeProgressArc.StrokeDashArray = new DoubleCollection { 0, 100 };
            return;
        }

        double percent = Math.Min(100, (actual / target) * 100);
        HomeProgressPercentText.Text = $"{percent:F0}%";

        double dashLength = percent;
        double dashGap = 100 - percent;
        HomeProgressArc.StrokeDashArray = new DoubleCollection { dashLength, dashGap };

        if (percent >= 100)
            HomeProgressArc.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
        else if (percent >= 75)
            HomeProgressArc.Stroke = (System.Windows.Media.Brush)FindResource("AccentBrush");
        else if (percent >= 50)
            HomeProgressArc.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 115, 22));
        else
            HomeProgressArc.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
    }

    private StackPanel CreateVarietyHeader(string name, double weight)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        
        var nameText = new TextBlock 
        { 
            Text = name, 
            FontWeight = FontWeights.SemiBold, 
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39)),
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var weightText = new TextBlock 
        { 
            Text = $"{weight:N2} t", 
            FontWeight = FontWeights.Medium,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
            FontSize = 13,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        sp.Children.Add(nameText);
        sp.Children.Add(weightText);
        return sp;
    }

    private StackPanel CreateCaliberHeader(string name, double weight)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 0, 0, 0) };
        
        var nameText = new TextBlock 
        { 
            Text = name, 
            FontWeight = FontWeights.Normal, 
            FontSize = 13,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 100
        };
        
        var weightText = new TextBlock 
        { 
            Text = $"{weight:N2} t", 
            FontWeight = FontWeights.Medium,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };

        sp.Children.Add(nameText);
        sp.Children.Add(weightText);
        return sp;
    }

    private void LoadIncomingData()
    {
        using var context = new WarehouseDbContext();
        var data = context.IncomingPotatoes
            .Include(i => i.Variety)
            .Include(i => i.Caliber)
            .Where(i => i.SeasonId == _activeSeasonId)
            .OrderByDescending(i => i.Date)
            .ToList();
        IncomingGrid.ItemsSource = data;
    }

    private void LoadOutgoingData()
    {
        using var context = new WarehouseDbContext();
        var data = context.OutgoingPotatoes
            .Include(o => o.Variety)
            .Include(o => o.Caliber)
            .Where(o => o.SeasonId == _activeSeasonId)
            .OrderByDescending(o => o.Date)
            .ToList();
        OutgoingGrid.ItemsSource = data;
    }

    private void LoadComboBoxes()
    {
        using var context = new WarehouseDbContext();

        var varieties = context.Varieties
            .Where(v => v.SeasonId == _activeSeasonId)
            .OrderBy(v => v.Name)
            .ToList();

        var calibers = context.Calibers
            .Where(c => c.SeasonId == _activeSeasonId)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        IncomingVarietyCombo.ItemsSource = varieties;
        IncomingVarietyCombo.DisplayMemberPath = "Name";
        IncomingVarietyCombo.SelectedValuePath = "Id";

        IncomingCaliberCombo.ItemsSource = calibers;
        IncomingCaliberCombo.DisplayMemberPath = "Name";
        IncomingCaliberCombo.SelectedValuePath = "Id";

        var varietiesWithIncoming = context.IncomingPotatoes
            .Where(i => i.SeasonId == _activeSeasonId)
            .Select(i => i.VarietyId)
            .Distinct()
            .ToList();

        var outgoingVarieties = varieties.Where(v => varietiesWithIncoming.Contains(v.Id)).ToList();

        OutgoingVarietyCombo.ItemsSource = outgoingVarieties;
        OutgoingVarietyCombo.DisplayMemberPath = "Name";
        OutgoingVarietyCombo.SelectedValuePath = "Id";

        OutgoingCaliberCombo.ItemsSource = calibers;
        OutgoingCaliberCombo.DisplayMemberPath = "Name";
        OutgoingCaliberCombo.SelectedValuePath = "Id";
    }

    private void LoadSettingsData()
    {
        using var context = new WarehouseDbContext();

        var seasons = context.Seasons.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name).ToList();
        SeasonCombo.ItemsSource = seasons;
        SeasonCombo.DisplayMemberPath = "Name";
        
        var activeSeason = seasons.FirstOrDefault(s => s.IsActive);
        if (activeSeason != null)
        {
            _isLoadingSeason = true;
            SeasonCombo.SelectedItem = activeSeason;
            _isLoadingSeason = false;
            
            SeasonTargetWeight.Text = activeSeason.TargetWeight > 0 ? (activeSeason.TargetWeight / 1000).ToString("N3") : "";
            
            var incomingTotal = context.IncomingPotatoes
                .Where(i => i.SeasonId == _activeSeasonId)
                .Sum(i => i.ContainerWeight * i.ContainerCount);
            
            var outgoingTotal = context.OutgoingPotatoes
                .Where(o => o.SeasonId == _activeSeasonId)
                .Sum(o => o.ContainerWeight * o.ContainerCount);
            
            var actualTotal = incomingTotal - outgoingTotal;
            var remaining = activeSeason.TargetWeight - actualTotal;
            
            SeasonIncomingTotal.Text = (actualTotal / 1000).ToString("N3");
            SeasonRemaining.Text = (remaining / 1000).ToString("N3");

            UpdateProgressIndicator(actualTotal, activeSeason.TargetWeight);
        }

        var varieties = context.Varieties
            .Where(v => v.SeasonId == _activeSeasonId)
            .OrderBy(v => v.Name)
            .ToList();
        VarietiesList.ItemsSource = varieties;

        var calibers = context.Calibers
            .Where(c => c.SeasonId == _activeSeasonId)
            .OrderBy(c => c.DisplayOrder)
            .ToList();
        CalibersList.ItemsSource = calibers;
    }

    private void UpdateProgressIndicator(double actual, double target)
    {
        if (target <= 0)
        {
            ProgressPercentText.Text = "0%";
            ProgressArc.StrokeDashArray = new DoubleCollection { 0, 100 };
            return;
        }

        double percent = Math.Min(100, (actual / target) * 100);
        ProgressPercentText.Text = $"{percent:F0}%";

        double dashLength = percent;
        double dashGap = 100 - percent;
        ProgressArc.StrokeDashArray = new DoubleCollection { dashLength, dashGap };

        if (percent >= 100)
            ProgressArc.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
        else if (percent >= 75)
            ProgressArc.Stroke = (System.Windows.Media.Brush)FindResource("AccentBrush");
        else if (percent >= 50)
            ProgressArc.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 115, 22));
        else
            ProgressArc.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
    }

    private void SaveSeasonTarget(object sender, RoutedEventArgs e)
    {
        if (SeasonCombo.SelectedItem is not Season season)
        {
            MessageBox.Show("Lūdzu, izvēlieties sezonu!");
            return;
        }

        if (!double.TryParse(SeasonTargetWeight.Text, out double targetTons) || targetTons < 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu mērķa svaru!");
            return;
        }

        using var context = new WarehouseDbContext();
        var dbSeason = context.Seasons.Find(season.Id);
        if (dbSeason != null)
        {
            dbSeason.TargetWeight = targetTons * 1000;
            context.SaveChanges();
            LoadSettingsData();
            MessageBox.Show("Mērķis saglabāts!");
        }
    }

    private void EditIncoming(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var editWindow = new EditWindow(id, "Incoming", _activeSeasonId);
            editWindow.Owner = this;
            editWindow.ShowDialog();

            if (editWindow.SaveSuccess)
            {
                LoadIncomingData();
            }
        }
    }

    private void EditOutgoing(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var editWindow = new EditWindow(id, "Outgoing", _activeSeasonId);
            editWindow.Owner = this;
            editWindow.ShowDialog();

            if (editWindow.SaveSuccess)
            {
                LoadOutgoingData();
            }
        }
    }

    private void AddIncoming(object sender, RoutedEventArgs e)
    {
        if (IncomingVarietyCombo.SelectedValue == null || IncomingCaliberCombo.SelectedValue == null)
        {
            MessageBox.Show("Lūdzu, izvēlieties šķirni un kalibru!");
            return;
        }

        if (!double.TryParse(IncomingContainerWeight.Text, out double weight) || weight <= 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu konteinera svaru!");
            return;
        }

        if (!int.TryParse(IncomingContainerCount.Text, out int count) || count <= 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu konteineru skaitu!");
            return;
        }

        using var context = new WarehouseDbContext();
        var incoming = new IncomingPotato
        {
            Date = IncomingDatePicker.SelectedDate ?? DateTime.Today,
            VarietyId = (int)IncomingVarietyCombo.SelectedValue,
            CaliberId = (int)IncomingCaliberCombo.SelectedValue,
            ContainerWeight = weight * 1000,
            ContainerCount = count,
            SeasonId = _activeSeasonId
        };

        context.IncomingPotatoes.Add(incoming);
        context.SaveChanges();

        IncomingContainerWeight.Text = "";
        IncomingContainerCount.Text = "";
        LoadIncomingData();
        MessageBox.Show("Ieraksts veiksmīgi pievienots!");
    }

    private void DeleteIncoming(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var result = MessageBox.Show("Vai tiešām vēlaties dzēst šo ierakstu?", 
                "Apstiprinājums", MessageBoxButton.YesNo);
            
            if (result == MessageBoxResult.Yes)
            {
                using var context = new WarehouseDbContext();
                var item = context.IncomingPotatoes.Find(id);
                if (item != null)
                {
                    context.IncomingPotatoes.Remove(item);
                    context.SaveChanges();
                    LoadIncomingData();
                }
            }
        }
    }

    private void AddOutgoing(object sender, RoutedEventArgs e)
    {
        if (OutgoingVarietyCombo.SelectedValue == null || OutgoingCaliberCombo.SelectedValue == null)
        {
            MessageBox.Show("Lūdzu, izvēlieties šķirni un kalibru!");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutgoingBuyer.Text))
        {
            MessageBox.Show("Lūdzu, ievadiet pircēju!");
            return;
        }

        if (!double.TryParse(OutgoingWeight.Text, out double totalWeightTons) || totalWeightTons <= 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu svaru!");
            return;
        }

        double totalWeightKg = totalWeightTons * 1000;

        using var context = new WarehouseDbContext();
        
        var availableWeight = context.IncomingPotatoes
            .Where(i => i.SeasonId == _activeSeasonId && 
                        i.VarietyId == (int)OutgoingVarietyCombo.SelectedValue && 
                        i.CaliberId == (int)OutgoingCaliberCombo.SelectedValue)
            .Sum(i => i.ContainerWeight * i.ContainerCount);

        var usedWeight = context.OutgoingPotatoes
            .Where(o => o.SeasonId == _activeSeasonId && 
                        o.VarietyId == (int)OutgoingVarietyCombo.SelectedValue && 
                        o.CaliberId == (int)OutgoingCaliberCombo.SelectedValue)
            .Sum(o => o.ContainerWeight * o.ContainerCount);

        var remainingWeight = availableWeight - usedWeight;

        if (totalWeightKg > remainingWeight)
        {
            MessageBox.Show($"Ne pietiek atlikuma! Pieejams: {remainingWeight/1000:N2} t");
            return;
        }

        var outgoing = new OutgoingPotato
        {
            Date = OutgoingDatePicker.SelectedDate ?? DateTime.Today,
            VarietyId = (int)OutgoingVarietyCombo.SelectedValue,
            CaliberId = (int)OutgoingCaliberCombo.SelectedValue,
            ContainerWeight = totalWeightKg,
            ContainerCount = 1,
            Buyer = OutgoingBuyer.Text,
            SeasonId = _activeSeasonId
        };

        context.OutgoingPotatoes.Add(outgoing);
        context.SaveChanges();

        OutgoingWeight.Text = "";
        OutgoingBuyer.Text = "";
        LoadOutgoingData();
        MessageBox.Show("Ieraksts veiksmīgi pievienots!");
    }

    private void DeleteOutgoing(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var result = MessageBox.Show("Vai tiešām vēlaties dzēst šo ierakstu?", 
                "Apstiprinājums", MessageBoxButton.YesNo);
            
            if (result == MessageBoxResult.Yes)
            {
                using var context = new WarehouseDbContext();
                var item = context.OutgoingPotatoes.Find(id);
                if (item != null)
                {
                    context.OutgoingPotatoes.Remove(item);
                    context.SaveChanges();
                    LoadOutgoingData();
                }
            }
        }
    }

    private void AddSeason(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewSeasonName.Text))
        {
            MessageBox.Show("Lūdzu, ievadiet sezonas nosaukumu!");
            return;
        }

        using var context = new WarehouseDbContext();
        
        if (context.Seasons.Any(s => s.Name == NewSeasonName.Text))
        {
            MessageBox.Show("Sezona ar šādu nosaukumu jau pastāv!");
            return;
        }

        var season = new Season
        {
            Name = NewSeasonName.Text,
            IsActive = !context.Seasons.Any()
        };

        context.Seasons.Add(season);
        context.SaveChanges();

        var defaultCalibers = new[]
        {
            new Caliber { Name = "Lielie", SeasonId = season.Id, DisplayOrder = 1 },
            new Caliber { Name = "Vidējie", SeasonId = season.Id, DisplayOrder = 2 },
            new Caliber { Name = "Puveņi", SeasonId = season.Id, DisplayOrder = 3 },
            new Caliber { Name = "Brāķi", SeasonId = season.Id, DisplayOrder = 4 }
        };

        context.Calibers.AddRange(defaultCalibers);
        context.SaveChanges();

        NewSeasonName.Text = "";
        LoadSettingsData();
        
        if (season.IsActive)
        {
            _activeSeasonId = season.Id;
            _activeSeasonName = season.Name;
            SeasonLabel.Text = season.Name;
            
            var settings = context.AppSettings.First();
            settings.ActiveSeasonId = season.Id;
            context.SaveChanges();
        }

        MessageBox.Show("Sezona veiksmīgi pievienota!");
    }

    private bool _isLoadingSeason = false;
    
    private void SeasonSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSeason || SeasonCombo.SelectedItem is not Season season)
            return;
        
        _isLoadingSeason = true;
        
        _activeSeasonId = season.Id;
        _activeSeasonName = season.Name;
        SeasonLabel.Text = season.Name;
        
        using var context = new WarehouseDbContext();
        SeasonTargetWeight.Text = season.TargetWeight > 0 ? (season.TargetWeight / 1000).ToString("N3") : "";
        
        var incomingTotal = context.IncomingPotatoes
            .Where(i => i.SeasonId == season.Id)
            .Sum(i => i.ContainerWeight * i.ContainerCount);
        
        var outgoingTotal = context.OutgoingPotatoes
            .Where(o => o.SeasonId == season.Id)
            .Sum(o => o.ContainerWeight * o.ContainerCount);
        
        var actualTotal = incomingTotal - outgoingTotal;
        var remaining = season.TargetWeight - actualTotal;
        
        SeasonIncomingTotal.Text = (actualTotal / 1000).ToString("N3");
        SeasonRemaining.Text = (remaining / 1000).ToString("N3");
        
        LoadSettingsData();
        
        _isLoadingSeason = false;
    }

    private void SetActiveSeason(object sender, RoutedEventArgs e)
    {
        if (SeasonCombo.SelectedItem is Season season)
        {
            using var context = new WarehouseDbContext();
            
            var allSeasons = context.Seasons.ToList();
            foreach (var s in allSeasons)
            {
                s.IsActive = false;
            }

            season.IsActive = true;
            context.SaveChanges();

            var settings = context.AppSettings.First();
            settings.ActiveSeasonId = season.Id;
            context.SaveChanges();

            _activeSeasonId = season.Id;
            _activeSeasonName = season.Name;
            SeasonLabel.Text = season.Name;

            LoadSettingsData();
            MessageBox.Show($"Sezona '{season.Name}' iestatīta kā aktīvā!");
        }
    }

    private void AddVariety(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewVarietyName.Text))
        {
            MessageBox.Show("Lūdzu, ievadiet šķirnes nosaukumu!");
            return;
        }

        using var context = new WarehouseDbContext();
        
        if (context.Varieties.Any(v => v.SeasonId == _activeSeasonId && v.Name == NewVarietyName.Text))
        {
            MessageBox.Show("Šķirne ar šādu nosaukumu jau pastāv šajā sezonā!");
            return;
        }

        var variety = new Variety
        {
            Name = NewVarietyName.Text,
            SeasonId = _activeSeasonId
        };

        context.Varieties.Add(variety);
        context.SaveChanges();

        NewVarietyName.Text = "";
        LoadSettingsData();
        MessageBox.Show("Šķirne veiksmīgi pievienota!");
    }

    private void DeleteVariety(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var result = MessageBox.Show("Vai tiešām vēlaties dzēst šo šķirni?", 
                "Apstiprinājums", MessageBoxButton.YesNo);
            
            if (result == MessageBoxResult.Yes)
            {
                using var context = new WarehouseDbContext();
                var item = context.Varieties.Find(id);
                if (item != null)
                {
                    context.Varieties.Remove(item);
                    context.SaveChanges();
                    LoadSettingsData();
                }
            }
        }
    }

    private void AddCaliber(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewCaliberName.Text))
        {
            MessageBox.Show("Lūdzu, ievadiet kalibra nosaukumu!");
            return;
        }

        using var context = new WarehouseDbContext();
        
        if (context.Calibers.Any(c => c.SeasonId == _activeSeasonId && c.Name == NewCaliberName.Text))
        {
            MessageBox.Show("Kalibrs ar šādu nosaukumu jau pastāv šajā sezonā!");
            return;
        }

        var maxOrder = context.Calibers
            .Where(c => c.SeasonId == _activeSeasonId)
            .Max(c => (int?)c.DisplayOrder) ?? 0;

        var caliber = new Caliber
        {
            Name = NewCaliberName.Text,
            SeasonId = _activeSeasonId,
            DisplayOrder = maxOrder + 1
        };

        context.Calibers.Add(caliber);
        context.SaveChanges();

        NewCaliberName.Text = "";
        LoadSettingsData();
        MessageBox.Show("Kalibrs veiksmīgi pievienots!");
    }

    private void DeleteCaliber(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int id)
        {
            var result = MessageBox.Show("Vai tiešām vēlaties dzēst šo kalibru?", 
                "Apstiprinājums", MessageBoxButton.YesNo);
            
            if (result == MessageBoxResult.Yes)
            {
                using var context = new WarehouseDbContext();
                var item = context.Calibers.Find(id);
                if (item != null)
                {
                    context.Calibers.Remove(item);
                    context.SaveChanges();
                    LoadSettingsData();
                }
            }
        }
    }
}

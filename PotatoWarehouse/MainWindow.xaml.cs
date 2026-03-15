using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        var incoming = context.IncomingPotatoes
            .Where(i => i.SeasonId == _activeSeasonId)
            .GroupBy(i => new { i.VarietyId, i.CaliberId, VarietyName = i.Variety.Name, CaliberName = i.Caliber.Name })
            .Select(g => new
            {
                VarietyId = g.Key.VarietyId,
                CaliberId = g.Key.CaliberId,
                VarietyName = g.Key.VarietyName,
                CaliberName = g.Key.CaliberName,
                Weight = g.Sum(x => x.ContainerWeight * x.ContainerCount)
            })
            .ToList();

        var outgoing = context.OutgoingPotatoes
            .Where(o => o.SeasonId == _activeSeasonId)
            .GroupBy(o => new { o.VarietyId, o.CaliberId })
            .Select(g => new
            {
                VarietyId = g.Key.VarietyId,
                CaliberId = g.Key.CaliberId,
                Weight = g.Sum(x => x.ContainerWeight * x.ContainerCount)
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

        InventoryGrid.ItemsSource = inventory;

        var totalWeight = inventory.Sum(x => x.Weight);
        TotalWeightText.Text = $"{totalWeight:N2} kg";
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

        OutgoingVarietyCombo.ItemsSource = varieties;
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
            SeasonCombo.SelectedItem = activeSeason;
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
            ContainerWeight = weight,
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

        if (!double.TryParse(OutgoingContainerWeight.Text, out double weight) || weight <= 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu konteinera svaru!");
            return;
        }

        if (!int.TryParse(OutgoingContainerCount.Text, out int count) || count <= 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu konteineru skaitu!");
            return;
        }

        double totalWeight = weight * count;

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

        if (totalWeight > remainingWeight)
        {
            MessageBox.Show($"N pietiek atlikuma! Pieejams: {remainingWeight:N2} kg");
            return;
        }

        var outgoing = new OutgoingPotato
        {
            Date = OutgoingDatePicker.SelectedDate ?? DateTime.Today,
            VarietyId = (int)OutgoingVarietyCombo.SelectedValue,
            CaliberId = (int)OutgoingCaliberCombo.SelectedValue,
            ContainerWeight = weight,
            ContainerCount = count,
            Buyer = OutgoingBuyer.Text,
            SeasonId = _activeSeasonId
        };

        context.OutgoingPotatoes.Add(outgoing);
        context.SaveChanges();

        OutgoingContainerWeight.Text = "";
        OutgoingContainerCount.Text = "";
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

    private void SeasonSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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

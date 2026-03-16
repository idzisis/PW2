using System;
using System.Linq;
using System.Windows;
using PotatoWarehouse.Data;
using PotatoWarehouse.Models;

namespace PotatoWarehouse;

public partial class EditWindow : Window
{
    private readonly int _recordId;
    private readonly string _recordType;
    private readonly int _seasonId;

    public bool SaveSuccess { get; private set; }

    public EditWindow(int id, string recordType, int seasonId)
    {
        InitializeComponent();
        _recordId = id;
        _recordType = recordType;
        _seasonId = seasonId;

        LoadData();

        HeaderText.Text = _recordType == "Incoming" ? "Rediģēt ienākošo" : "Rediģēt izejošo";
    }

    private void LoadData()
    {
        using var context = new WarehouseDbContext();

        var varieties = context.Varieties.Where(v => v.SeasonId == _seasonId).OrderBy(v => v.Name).ToList();
        var calibers = context.Calibers.Where(c => c.SeasonId == _seasonId).OrderBy(c => c.DisplayOrder).ToList();

        EditVarietyCombo.ItemsSource = varieties;
        EditCaliberCombo.ItemsSource = calibers;

        if (_recordType == "Incoming")
        {
            CountPanel.Visibility = Visibility.Visible;
            BuyerPanel.Visibility = Visibility.Collapsed;
            Title = "Rediģēt ienākošo";

            var incoming = context.IncomingPotatoes.Find(_recordId);
            if (incoming != null)
            {
                EditDatePicker.SelectedDate = incoming.Date;
                EditVarietyCombo.SelectedValue = incoming.VarietyId;
                EditCaliberCombo.SelectedValue = incoming.CaliberId;
                EditWeight.Text = (incoming.ContainerWeight / 1000).ToString("N3");
                EditCount.Text = incoming.ContainerCount.ToString();
            }
        }
        else
        {
            CountPanel.Visibility = Visibility.Collapsed;
            BuyerPanel.Visibility = Visibility.Visible;
            Title = "Rediģēt izejošo";

            var outgoing = context.OutgoingPotatoes.Find(_recordId);
            if (outgoing != null)
            {
                EditDatePicker.SelectedDate = outgoing.Date;
                EditVarietyCombo.SelectedValue = outgoing.VarietyId;
                EditCaliberCombo.SelectedValue = outgoing.CaliberId;
                EditWeight.Text = (outgoing.ContainerWeight / 1000).ToString("N3");
                EditBuyer.Text = outgoing.Buyer;
            }
        }
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (EditVarietyCombo.SelectedValue == null || EditCaliberCombo.SelectedValue == null)
        {
            MessageBox.Show("Lūdzu, izvēlieties šķirni un kalibru!");
            return;
        }

        if (!double.TryParse(EditWeight.Text, out double weight) || weight <= 0)
        {
            MessageBox.Show("Lūdzu, ievadiet derīgu svaru!");
            return;
        }

        using var context = new WarehouseDbContext();

        if (_recordType == "Incoming")
        {
            if (!int.TryParse(EditCount.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Lūdzu, ievadiet derīgu daudzumu!");
                return;
            }

            var incoming = context.IncomingPotatoes.Find(_recordId);
            if (incoming != null)
            {
                incoming.Date = EditDatePicker.SelectedDate ?? DateTime.Today;
                incoming.VarietyId = (int)EditVarietyCombo.SelectedValue;
                incoming.CaliberId = (int)EditCaliberCombo.SelectedValue;
                incoming.ContainerWeight = weight * 1000;
                incoming.ContainerCount = count;
                context.SaveChanges();
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(EditBuyer.Text))
            {
                MessageBox.Show("Lūdzu, ievadiet pircēju!");
                return;
            }

            var outgoing = context.OutgoingPotatoes.Find(_recordId);
            if (outgoing != null)
            {
                outgoing.Date = EditDatePicker.SelectedDate ?? DateTime.Today;
                outgoing.VarietyId = (int)EditVarietyCombo.SelectedValue;
                outgoing.CaliberId = (int)EditCaliberCombo.SelectedValue;
                outgoing.ContainerWeight = weight * 1000;
                outgoing.Buyer = EditBuyer.Text.Trim();
                context.SaveChanges();
            }
        }

        SaveSuccess = true;
        Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        SaveSuccess = false;
        Close();
    }
}

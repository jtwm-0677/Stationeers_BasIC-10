using System.Windows;
using System.Windows.Controls;
using BasicToMips.Data;

namespace BasicToMips.UI;

public partial class DeviceDatabaseWindow : Window
{
    public string? SelectedHash { get; private set; }
    public string? SelectedName { get; private set; }

    public DeviceDatabaseWindow()
    {
        InitializeComponent();
        LoadData();
        UpdateStatus();
    }

    private void LoadData()
    {
        DevicesGrid.ItemsSource = DeviceDatabase.Devices;
        LogicTypesGrid.ItemsSource = DeviceDatabase.LogicTypes;
        SlotLogicTypesGrid.ItemsSource = DeviceDatabase.SlotLogicTypes;
        BatchModesGrid.ItemsSource = DeviceDatabase.BatchModes;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;

        DevicesGrid.ItemsSource = DeviceDatabase.SearchDevices(query);
        LogicTypesGrid.ItemsSource = DeviceDatabase.SearchLogicTypes(query);

        if (string.IsNullOrWhiteSpace(query))
        {
            SlotLogicTypesGrid.ItemsSource = DeviceDatabase.SlotLogicTypes;
        }
        else
        {
            var lower = query.ToLowerInvariant();
            SlotLogicTypesGrid.ItemsSource = DeviceDatabase.SlotLogicTypes
                .Where(s => s.Name.ToLowerInvariant().Contains(lower) ||
                           s.DisplayName.ToLowerInvariant().Contains(lower) ||
                           s.Hash.ToString().Contains(query))
                .ToList();
        }

        UpdateStatus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
    }

    private void HashInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        var input = HashInputBox.Text;
        if (string.IsNullOrEmpty(input))
        {
            HashResultDecimal.Text = "";
            HashResultHex.Text = "";
            return;
        }

        int hash = DeviceDatabase.CalculateHash(input);
        HashResultDecimal.Text = hash.ToString();
        HashResultHex.Text = $"0x{hash:X8}";
    }

    private void CopyHash_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(HashResultDecimal.Text))
        {
            Clipboard.SetText(HashResultDecimal.Text);
            StatusText.Text = "Hash copied to clipboard";
        }
    }

    private void CopySelectedHash_Click(object sender, RoutedEventArgs e)
    {
        var hash = GetSelectedHash();
        if (hash != null)
        {
            Clipboard.SetText(hash);
            StatusText.Text = $"Hash {hash} copied to clipboard";
        }
        else
        {
            StatusText.Text = "No item selected";
        }
    }

    private void InsertToEditor_Click(object sender, RoutedEventArgs e)
    {
        var hash = GetSelectedHash();
        var name = GetSelectedName();

        if (hash != null)
        {
            SelectedHash = hash;
            SelectedName = name;
            DialogResult = true;
            Close();
        }
        else
        {
            StatusText.Text = "No item selected";
        }
    }

    private string? GetSelectedHash()
    {
        var tabIndex = MainTabs.SelectedIndex;

        return tabIndex switch
        {
            0 => (DevicesGrid.SelectedItem as DeviceInfo)?.Hash.ToString(),
            1 => (LogicTypesGrid.SelectedItem as LogicType)?.Hash.ToString(),
            2 => (SlotLogicTypesGrid.SelectedItem as SlotLogicType)?.Hash.ToString(),
            3 => (BatchModesGrid.SelectedItem as BatchMode)?.Value.ToString(),
            4 => !string.IsNullOrEmpty(HashResultDecimal.Text) ? HashResultDecimal.Text : null,
            _ => null
        };
    }

    private string? GetSelectedName()
    {
        var tabIndex = MainTabs.SelectedIndex;

        return tabIndex switch
        {
            0 => (DevicesGrid.SelectedItem as DeviceInfo)?.PrefabName,
            1 => (LogicTypesGrid.SelectedItem as LogicType)?.Name,
            2 => (SlotLogicTypesGrid.SelectedItem as SlotLogicType)?.Name,
            3 => (BatchModesGrid.SelectedItem as BatchMode)?.Name,
            4 => HashInputBox.Text,
            _ => null
        };
    }

    private void UpdateStatus()
    {
        var deviceCount = (DevicesGrid.ItemsSource as IEnumerable<DeviceInfo>)?.Count() ?? 0;
        var logicCount = (LogicTypesGrid.ItemsSource as IEnumerable<LogicType>)?.Count() ?? 0;
        var slotCount = (SlotLogicTypesGrid.ItemsSource as IEnumerable<SlotLogicType>)?.Count() ?? 0;

        StatusText.Text = $"Showing: {deviceCount} devices, {logicCount} logic types, {slotCount} slot types";
    }
}

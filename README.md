using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace TailorShop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Customer> _customers = new();
    private readonly ObservableCollection<Order> _orders = new();
    private readonly ObservableCollection<Measurement> _measurements = new();
    private readonly ObservableCollection<Payment> _payments = new();
    private readonly DatabaseService _database = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DatabaseService.Initialize();
        OrderStatusCombo.ItemsSource = new[] { "New", "Doing", "Done", "Delivered" };
        OrderStatusCombo.SelectedIndex = 0;
        LoadAll();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var tag = button.Tag?.ToString();

        DashboardPage.Visibility = tag == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        CustomersPage.Visibility = tag == "Customers" ? Visibility.Visible : Visibility.Collapsed;
        OrdersPage.Visibility = tag == "Orders" ? Visibility.Visible : Visibility.Collapsed;
        MeasurementsPage.Visibility = tag == "Measurements" ? Visibility.Visible : Visibility.Collapsed;
        PaymentsPage.Visibility = tag == "Payments" ? Visibility.Visible : Visibility.Collapsed;

        if (tag == "Customers") LoadCustomers();
        if (tag == "Orders") LoadOrders();
        if (tag == "Measurements") LoadMeasurements();
        if (tag == "Payments") LoadPayments();
        if (tag == "Dashboard") LoadDashboard();
    }

    private void LoadAll()
    {
        LoadDashboard();
        LoadCustomers();
        LoadOrders();
        LoadMeasurements();
        LoadPayments();
    }

    private void LoadDashboard()
    {
        var summary = _database.GetDashboardSummary();
        DashboardCustomersCount.Text = summary.CustomersCount.ToString();
        DashboardActiveOrders.Text = summary.ActiveOrders.ToString();
        DashboardTodayIncome.Text = summary.TodayIncome.ToString("C0", CultureInfo.CurrentCulture);
        DashboardOutstanding.Text = summary.Outstanding.ToString("C0", CultureInfo.CurrentCulture);

        var orders = _database.GetRecentOrders(8);
        DashboardOrdersGrid.ItemsSource = orders;
    }

    private void LoadCustomers()
    {
        _customers.Clear();
        foreach (var customer in _database.GetCustomers())
            _customers.Add(customer);

        CustomersGrid.ItemsSource = _customers;
        OrderCustomerCombo.ItemsSource = _customers;
        OrderCustomerCombo.DisplayMemberPath = "Name";
        MeasurementCustomerCombo.ItemsSource = _customers;
        MeasurementCustomerCombo.DisplayMemberPath = "Name";
        PaymentOrderCombo.ItemsSource = _orders;
        PaymentOrderCombo.DisplayMemberPath = "DisplayName";
    }

    private void LoadOrders()
    {
        _orders.Clear();
        foreach (var order in _database.GetOrders())
            _orders.Add(order);

        OrderGrid.ItemsSource = _orders;
        PaymentOrderCombo.ItemsSource = _orders;
        PaymentOrderCombo.DisplayMemberPath = "DisplayName";
    }

    private void LoadMeasurements()
    {
        _measurements.Clear();
        foreach (var measurement in _database.GetMeasurements())
            _measurements.Add(measurement);

        MeasurementsGrid.ItemsSource = _measurements;
    }

    private void LoadPayments()
    {
        _payments.Clear();
        foreach (var payment in _database.GetPayments())
            _payments.Add(payment);

        PaymentsGrid.ItemsSource = _payments;
    }

    private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomersGrid.SelectedItem is not Customer customer) return;
        CustomerIdText.Text = customer.Id.ToString();
        CustomerNameText.Text = customer.Name;
        CustomerPhoneText.Text = customer.Phone;
        CustomerNoteText.Text = customer.Note;
    }

    private void SaveCustomer_Click(object sender, RoutedEventArgs e)
    {
        var name = CustomerNameText.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Customer name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var customer = new Customer
        {
            Id = int.TryParse(CustomerIdText.Text, out var id) ? id : 0,
            Name = name,
            Phone = CustomerPhoneText.Text.Trim(),
            Note = CustomerNoteText.Text.Trim()
        };

        _database.SaveCustomer(customer);
        ClearCustomerForm();
        LoadCustomers();
        LoadDashboard();
    }

    private void ClearCustomer_Click(object sender, RoutedEventArgs e) => ClearCustomerForm();

    private void ClearCustomerForm()
    {
        CustomerIdText.Text = string.Empty;
        CustomerNameText.Text = string.Empty;
        CustomerPhoneText.Text = string.Empty;
        CustomerNoteText.Text = string.Empty;
        CustomersGrid.SelectedItem = null;
    }

    private void OrderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrderGrid.SelectedItem is not Order order) return;

        OrderIdText.Text = order.Id.ToString();
        OrderItemText.Text = order.Item;
        OrderPriceText.Text = order.Price.ToString();
        OrderNoteText.Text = order.Note;
        OrderStatusCombo.SelectedItem = order.Status;
        OrderDueDatePicker.SelectedDate = DateTime.TryParse(order.DueDate, out var due) ? due : DateTime.Today.AddDays(7);

        foreach (Customer customer in _customers)
        {
            if (customer.Id == order.CustomerId)
            {
                OrderCustomerCombo.SelectedItem = customer;
                break;
            }
        }
    }

    private void SaveOrder_Click(object sender, RoutedEventArgs e)
    {
        if (OrderCustomerCombo.SelectedItem is not Customer customer)
        {
            MessageBox.Show("Please select a customer for the order.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(OrderPriceText.Text.Trim(), out var price) || price < 0)
        {
            MessageBox.Show("Please enter a valid price.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var order = new Order
        {
            Id = int.TryParse(OrderIdText.Text, out var id) ? id : 0,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Item = OrderItemText.Text.Trim(),
            Price = price,
            DueDate = OrderDueDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
            Status = OrderStatusCombo.SelectedItem?.ToString() ?? "New",
            Note = OrderNoteText.Text.Trim()
        };

        _database.SaveOrder(order);
        ClearOrderForm();
        LoadOrders();
        LoadDashboard();
        LoadPayments();
    }

    private void ClearOrder_Click(object sender, RoutedEventArgs e) => ClearOrderForm();

    private void ClearOrderForm()
    {
        OrderIdText.Text = string.Empty;
        OrderItemText.Text = string.Empty;
        OrderPriceText.Text = string.Empty;
        OrderNoteText.Text = string.Empty;
        OrderDueDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        OrderStatusCombo.SelectedIndex = 0;
        OrderCustomerCombo.SelectedIndex = -1;
        OrderGrid.SelectedItem = null;
    }

    private void MeasurementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MeasurementsGrid.SelectedItem is not Measurement measurement) return;

        MeasurementIdText.Text = measurement.Id.ToString();
        ShirtLengthText.Text = measurement.ShirtLength;
        SleeveText.Text = measurement.Sleeve;
        ShoulderText.Text = measurement.Shoulder;
        NeckText.Text = measurement.Neck;
        ChatiText.Text = measurement.Chati;
        BagalText.Text = measurement.Bagal;
        DamanText.Text = measurement.Daman;
        PantsLengthText.Text = measurement.PantsLength;
        PantsHemText.Text = measurement.PantsHem;
        MeasurementNotesText.Text = measurement.Notes;

        foreach (Customer customer in _customers)
        {
            if (customer.Id == measurement.CustomerId)
            {
                MeasurementCustomerCombo.SelectedItem = customer;
                break;
            }
        }
    }

    private void SaveMeasurement_Click(object sender, RoutedEventArgs e)
    {
        if (MeasurementCustomerCombo.SelectedItem is not Customer customer)
        {
            MessageBox.Show("Please select a customer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var measurement = new Measurement
        {
            Id = int.TryParse(MeasurementIdText.Text, out var id) ? id : 0,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            ShirtLength = ShirtLengthText.Text.Trim(),
            Sleeve = SleeveText.Text.Trim(),
            Shoulder = ShoulderText.Text.Trim(),
            Neck = NeckText.Text.Trim(),
            Chati = ChatiText.Text.Trim(),
            Bagal = BagalText.Text.Trim(),
            Daman = DamanText.Text.Trim(),
            PantsLength = PantsLengthText.Text.Trim(),
            PantsHem = PantsHemText.Text.Trim(),
            Notes = MeasurementNotesText.Text.Trim()
        };

        _database.SaveMeasurement(measurement);
        ClearMeasurementForm();
        LoadMeasurements();
    }

    private void ClearMeasurement_Click(object sender, RoutedEventArgs e) => ClearMeasurementForm();

    private void ClearMeasurementForm()
    {
        MeasurementIdText.Text = string.Empty;
        MeasurementCustomerCombo.SelectedIndex = -1;
        ShirtLengthText.Text = string.Empty;
        SleeveText.Text = string.Empty;
        ShoulderText.Text = string.Empty;
        NeckText.Text = string.Empty;
        ChatiText.Text = string.Empty;
        BagalText.Text = string.Empty;
        DamanText.Text = string.Empty;
        PantsLengthText.Text = string.Empty;
        PantsHemText.Text = string.Empty;
        MeasurementNotesText.Text = string.Empty;
        MeasurementsGrid.SelectedItem = null;
    }

    private void SavePayment_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentOrderCombo.SelectedItem is not Order order)
        {
            MessageBox.Show("Please select an order.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(PaymentAmountText.Text.Trim(), out var amount) || amount <= 0)
        {
            MessageBox.Show("Please enter a valid payment amount.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _database.SavePayment(order.Id, amount);
        ClearPaymentForm();
        LoadPayments();
        LoadDashboard();
        LoadOrders();
    }

    private void ClearPayment_Click(object sender, RoutedEventArgs e) => ClearPaymentForm();

    private void ClearPaymentForm()
    {
        PaymentOrderCombo.SelectedIndex = -1;
        PaymentAmountText.Text = string.Empty;
        PaymentDatePicker.SelectedDate = DateTime.Today;
    }

    private void NewCustomer_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Customers");
        ClearCustomerForm();
    }

    private void NewOrder_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Orders");
        ClearOrderForm();
    }

    private void NewMeasurement_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Measurements");
        ClearMeasurementForm();
    }

    private void NewPayment_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Payments");
        ClearPaymentForm();
    }

    private void Backup_Click(object sender, RoutedEventArgs e) => Export_Click(sender, e);

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite Backup (*.db)|*.db",
                FileName = $"AqibShahBackup-{DateTime.Today:yyyy-MM-dd}.db"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using var source = new SqliteConnection(DatabaseService.ConnectionString);
                using var destination = new SqliteConnection($"Data Source={saveFileDialog.FileName}");
                source.Open();
                destination.Open();
                source.BackupDatabase(destination, "main", "main", -1, null, 0);
                MessageBox.Show("Backup created successfully.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Backup failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite Backup (*.db)|*.db"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                using var backup = new SqliteConnection($"Data Source={openFileDialog.FileName}");
                using var target = new SqliteConnection(DatabaseService.ConnectionString);
                backup.Open();
                target.Open();
                backup.BackupDatabase(target, "main", "main", -1, null, 0);
                MessageBox.Show("Backup imported successfully.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadAll();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowPage(string pageName)
    {
        DashboardPage.Visibility = pageName == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        CustomersPage.Visibility = pageName == "Customers" ? Visibility.Visible : Visibility.Collapsed;
        OrdersPage.Visibility = pageName == "Orders" ? Visibility.Visible : Visibility.Collapsed;
        MeasurementsPage.Visibility = pageName == "Measurements" ? Visibility.Visible : Visibility.Collapsed;
        PaymentsPage.Visibility = pageName == "Payments" ? Visibility.Visible : Visibility.Collapsed;
    }
}

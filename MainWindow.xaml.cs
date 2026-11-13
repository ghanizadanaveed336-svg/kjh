using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace TailorShop;

public partial class MainWindow : Window
{
    private readonly DatabaseService _database = new();
    private bool _ready;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            DatabaseService.Initialize();
            OrderStatusCombo.ItemsSource = new[] { "New", "Doing", "Done", "Delivered" };
            OrderStatusCombo.SelectedIndex = 0;
            OrderDueDatePicker.SelectedDate = DateTime.Today.AddDays(7);
            PaymentDatePicker.SelectedDate = DateTime.Today;
            _ready = true;
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not initialize the local database.\n\n{ex.Message}", "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadAll()
    {
        LoadCustomers();
        LoadOrders();
        LoadPayments();
        LoadDashboard();
    }

    private void LoadCustomers()
    {
        var customers = _database.GetCustomers();
        CustomersGrid.ItemsSource = customers;
        OrderCustomerCombo.ItemsSource = customers;
    }

    private void LoadOrders()
    {
        var orders = _database.GetOrders();
        OrderGrid.ItemsSource = orders;
        PaymentOrderCombo.ItemsSource = orders;
    }

    private void LoadPayments()
    {
        PaymentsGrid.ItemsSource = _database.GetPayments();
    }

    private void LoadDashboard()
    {
        var summary = _database.GetDashboardSummary();
        DashboardCustomersCount.Text = summary.CustomersCount.ToString(CultureInfo.CurrentCulture);
        DashboardActiveOrders.Text = summary.ActiveOrders.ToString(CultureInfo.CurrentCulture);
        DashboardTodayIncome.Text = $"{summary.TodayIncome:N0} AFN";
        DashboardOutstanding.Text = $"{summary.Outstanding:N0} AFN";
        DashboardOrdersGrid.ItemsSource = _database.GetRecentOrders(8);
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        ShowPage(button.Tag?.ToString() ?? "Dashboard");
    }

    private void ShowPage(string page)
    {
        DashboardPage.Visibility = page == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        CustomersPage.Visibility = page == "Customers" ? Visibility.Visible : Visibility.Collapsed;
        OrdersPage.Visibility = page == "Orders" ? Visibility.Visible : Visibility.Collapsed;
        PaymentsPage.Visibility = page == "Payments" ? Visibility.Visible : Visibility.Collapsed;

        foreach (var button in new[] { navDashboard, navCustomers, navOrders, navPayments })
        {
            var active = string.Equals(button.Tag?.ToString(), page, StringComparison.OrdinalIgnoreCase);
            button.Background = active ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 25, 25)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 23, 23));
            button.Foreground = active ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 213, 122)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 241, 235));
        }
    }

    private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomersGrid.SelectedItem is not Customer customer) return;
        CustomerIdText.Text = customer.Id.ToString(CultureInfo.InvariantCulture);
        CustomerNameText.Text = customer.Name;
        CustomerPhoneText.Text = customer.Phone;
        CustomerNoteText.Text = customer.Note;
    }

    private void SaveCustomer_Click(object sender, RoutedEventArgs e)
    {
        var name = CustomerNameText.Text.Trim();
        if (name.Length == 0)
        {
            Warn("Customer name is required.");
            CustomerNameText.Focus();
            return;
        }

        try
        {
            _database.SaveCustomer(new Customer
            {
                Id = int.TryParse(CustomerIdText.Text, out var id) ? id : 0,
                Name = name,
                Phone = CustomerPhoneText.Text.Trim(),
                Note = CustomerNoteText.Text.Trim()
            });
            ClearCustomerForm();
            LoadCustomers();
            LoadOrders();
            LoadPayments();
            LoadDashboard();
        }
        catch (Exception ex) { ShowError("Could not save customer", ex); }
    }

    private void ClearCustomer_Click(object sender, RoutedEventArgs e) => ClearCustomerForm();

    private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (CustomersGrid.SelectedItem is not Customer customer)
        {
            Warn("Select a customer to delete.");
            return;
        }

        if (MessageBox.Show($"Delete customer '{customer.Name}' and all related records?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            _database.DeleteCustomer(customer.Id);
            ClearCustomerForm();
            LoadAll();
        }
        catch (Exception ex) { ShowError("Could not delete customer", ex); }
    }

    private void ClearCustomerForm()
    {
        CustomerIdText.Clear();
        CustomerNameText.Clear();
        CustomerPhoneText.Clear();
        CustomerNoteText.Clear();
        CustomersGrid.SelectedItem = null;
    }

    private void NewCustomer_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Customers");
        ClearCustomerForm();
        CustomerNameText.Focus();
    }

    private void OrderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrderGrid.SelectedItem is not Order order) return;
        OrderIdText.Text = order.Id.ToString(CultureInfo.InvariantCulture);
        OrderCustomerCombo.SelectedValue = order.CustomerId;
        OrderItemText.Text = order.Item;
        OrderPriceText.Text = order.Price.ToString(CultureInfo.CurrentCulture);
        OrderDueDatePicker.SelectedDate = DateTime.TryParseExact(order.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var due) ? due : DateTime.Today.AddDays(7);
        OrderStatusCombo.SelectedItem = order.Status;
        OrderNoteText.Text = order.Note;
    }

    private void SaveOrder_Click(object sender, RoutedEventArgs e)
    {
        if (OrderCustomerCombo.SelectedItem is not Customer customer)
        {
            Warn("Select a customer for this order.");
            return;
        }
        if (string.IsNullOrWhiteSpace(OrderItemText.Text))
        {
            Warn("Enter an item or work description.");
            OrderItemText.Focus();
            return;
        }
        if (!decimal.TryParse(OrderPriceText.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price < 0)
        {
            Warn("Enter a valid non-negative price.");
            OrderPriceText.Focus();
            return;
        }

        try
        {
            _database.SaveOrder(new Order
            {
                Id = int.TryParse(OrderIdText.Text, out var id) ? id : 0,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Item = OrderItemText.Text.Trim(),
                Price = price,
                DueDate = OrderDueDatePicker.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? DateTime.Today.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Status = OrderStatusCombo.SelectedItem?.ToString() ?? "New",
                Note = OrderNoteText.Text.Trim()
            });
            ClearOrderForm();
            LoadOrders();
            LoadPayments();
            LoadDashboard();
        }
        catch (Exception ex) { ShowError("Could not save order", ex); }
    }

    private void ClearOrder_Click(object sender, RoutedEventArgs e) => ClearOrderForm();

    private void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (OrderGrid.SelectedItem is not Order order)
        {
            Warn("Select an order to delete.");
            return;
        }

        if (MessageBox.Show($"Delete order #{order.Id} for {order.CustomerName}?", "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            _database.DeleteOrder(order.Id);
            ClearOrderForm();
            LoadAll();
        }
        catch (Exception ex) { ShowError("Could not delete order", ex); }
    }

    private void ClearOrderForm()
    {
        OrderIdText.Clear();
        OrderCustomerCombo.SelectedItem = null;
        OrderItemText.Clear();
        OrderPriceText.Clear();
        OrderDueDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        OrderStatusCombo.SelectedIndex = 0;
        OrderNoteText.Clear();
        OrderGrid.SelectedItem = null;
    }

    private void NewOrder_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Orders");
        ClearOrderForm();
        if (OrderCustomerCombo.Items.Count == 0)
        {
            Warn("Add a customer before creating an order.");
            ShowPage("Customers");
            return;
        }
        OrderCustomerCombo.Focus();
    }

    private void SavePayment_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentOrderCombo.SelectedItem is not Order order)
        {
            Warn("Select an order to receive a payment.");
            return;
        }
        if (!decimal.TryParse(PaymentAmountText.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount <= 0)
        {
            Warn("Enter a valid payment amount greater than zero.");
            PaymentAmountText.Focus();
            return;
        }

        try
        {
            var date = PaymentDatePicker.SelectedDate ?? DateTime.Today;
            _database.SavePayment(order.Id, amount, date);
            ClearPaymentForm();
            LoadOrders();
            LoadPayments();
            LoadDashboard();
        }
        catch (Exception ex) { ShowError("Could not record payment", ex); }
    }

    private void ClearPayment_Click(object sender, RoutedEventArgs e) => ClearPaymentForm();

    private void ClearPaymentForm()
    {
        PaymentOrderCombo.SelectedItem = null;
        PaymentAmountText.Clear();
        PaymentDatePicker.SelectedDate = DateTime.Today;
    }

    private void NewPayment_Click(object sender, RoutedEventArgs e)
    {
        ShowPage("Payments");
        ClearPaymentForm();
        if (PaymentOrderCombo.Items.Count == 0)
            Warn("Create an order before recording a payment.");
    }

    private void Backup_Click(object sender, RoutedEventArgs e) => Export_Click(sender, e);

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "SQLite database backup (*.db)|*.db",
            FileName = $"AqibShah-{DateTime.Today:yyyy-MM-dd}.db",
            Title = "Export database backup"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            _database.ExportBackup(dialog.FileName);
            MessageBox.Show("Backup exported successfully.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Backup export failed", ex); }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "SQLite database backup (*.db)|*.db",
            Title = "Import database backup"
        };
        if (dialog.ShowDialog(this) != true) return;
        if (MessageBox.Show("Importing replaces the current database. Continue?", "Confirm restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            _database.ImportBackup(dialog.FileName);
            LoadAll();
            MessageBox.Show("Backup imported successfully.", "Restore", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { ShowError("Backup import failed", ex); }
    }

    private void Warn(string message) => MessageBox.Show(message, "Check input", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void ShowError(string action, Exception ex) =>
        MessageBox.Show($"{action}.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}

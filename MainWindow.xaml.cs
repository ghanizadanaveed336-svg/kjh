using System.Collections.ObjectModel;
using System.Windows;

namespace TailorShop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new OrderViewModel();
        }
    }

    public class OrderModel
    {
        public string No { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Due { get; set; } = string.Empty;
    }

    public class OrderViewModel
    {
        public ObservableCollection<OrderModel> Orders { get; } = new()
        {
            new OrderModel { No = "A-104", Customer = "Ali Rahimi", Item = "Shalwar Kameez", Price = "$320", Due = "2026-11-22" },
            new OrderModel { No = "A-112", Customer = "Naseebullah", Item = "Waistcoat", Price = "$220", Due = "2026-11-25" },
            new OrderModel { No = "A-118", Customer = "Amina Khan", Item = "Suit Set", Price = "$540", Due = "2026-11-30" },
            new OrderModel { No = "A-126", Customer = "Hamid Zada", Item = "Formal Coat", Price = "$410", Due = "2026-12-02" }
        };
    }
}

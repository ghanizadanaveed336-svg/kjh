namespace TailorShop;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = "New";
    public string DueDate { get; set; } = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
    public string Note { get; set; } = string.Empty;
    public decimal Paid { get; set; }
    public string DisplayName => $"{CustomerName} - {Item}";
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaidOn { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
}

public class DashboardSummary
{
    public int CustomersCount { get; set; }
    public int ActiveOrders { get; set; }
    public decimal TodayIncome { get; set; }
    public decimal Outstanding { get; set; }
}

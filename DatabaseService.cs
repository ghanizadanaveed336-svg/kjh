using Microsoft.Data.Sqlite;

namespace TailorShop;

public sealed class DatabaseService
{
    public static string DbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "AqibShahTailor.db");

    public static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Customers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phone TEXT NOT NULL DEFAULT '',
                Note TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                Item TEXT NOT NULL DEFAULT '',
                Price REAL NOT NULL DEFAULT 0,
                DueDate TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'New',
                Note TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
            );
            CREATE TABLE IF NOT EXISTS Payments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId INTEGER NOT NULL,
                Amount REAL NOT NULL CHECK (Amount > 0),
                PaidOn TEXT NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id)
            );
            CREATE TABLE IF NOT EXISTS Measurements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                ShirtLength TEXT NOT NULL DEFAULT '', Sleeve TEXT NOT NULL DEFAULT '',
                Shoulder TEXT NOT NULL DEFAULT '', Neck TEXT NOT NULL DEFAULT '',
                Chati TEXT NOT NULL DEFAULT '', Bagal TEXT NOT NULL DEFAULT '',
                Daman TEXT NOT NULL DEFAULT '', PantsLength TEXT NOT NULL DEFAULT '',
                PantsHem TEXT NOT NULL DEFAULT '', Notes TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
            );";
        command.ExecuteNonQuery();
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    public List<Customer> GetCustomers()
    {
        var items = new List<Customer>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Phone, Note FROM Customers ORDER BY Name COLLATE NOCASE;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(new Customer { Id = reader.GetInt32(0), Name = Text(reader, 1), Phone = Text(reader, 2), Note = Text(reader, 3) });
        return items;
    }

    public void SaveCustomer(Customer customer)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = customer.Id > 0
            ? "UPDATE Customers SET Name=@Name, Phone=@Phone, Note=@Note WHERE Id=@Id;"
            : "INSERT INTO Customers (Name, Phone, Note) VALUES (@Name, @Phone, @Note);";
        Add(command, "@Name", customer.Name);
        Add(command, "@Phone", customer.Phone);
        Add(command, "@Note", customer.Note);
        if (customer.Id > 0) Add(command, "@Id", customer.Id);
        command.ExecuteNonQuery();
    }

    public void DeleteCustomer(int customerId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            DELETE FROM Payments WHERE OrderId IN (SELECT Id FROM Orders WHERE CustomerId=@Id);
            DELETE FROM Orders WHERE CustomerId=@Id;
            DELETE FROM Measurements WHERE CustomerId=@Id;
            DELETE FROM Customers WHERE Id=@Id;";
        Add(command, "@Id", customerId);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public List<Order> GetOrders()
    {
        var items = new List<Order>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT o.Id, o.CustomerId, c.Name, o.Item, o.Price, o.Status, o.DueDate, o.Note,
                   COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.OrderId=o.Id), 0)
            FROM Orders o JOIN Customers c ON c.Id=o.CustomerId
            ORDER BY o.Id DESC;";
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(ReadOrder(reader));
        return items;
    }

    public List<Order> GetRecentOrders(int count = 10)
    {
        var items = new List<Order>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT o.Id, o.CustomerId, c.Name, o.Item, o.Price, o.Status, o.DueDate, o.Note,
                   COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.OrderId=o.Id), 0)
            FROM Orders o JOIN Customers c ON c.Id=o.CustomerId
            ORDER BY o.Id DESC LIMIT @Limit;";
        Add(command, "@Limit", count);
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(ReadOrder(reader));
        return items;
    }

    private static Order ReadOrder(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0), CustomerId = reader.GetInt32(1), CustomerName = Text(reader, 2),
        Item = Text(reader, 3), Price = reader.GetDecimal(4), Status = Text(reader, 5),
        DueDate = Text(reader, 6), Note = Text(reader, 7), Paid = reader.GetDecimal(8)
    };

    public void SaveOrder(Order order)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = order.Id > 0
            ? @"UPDATE Orders SET CustomerId=@CustomerId, Item=@Item, Price=@Price, DueDate=@DueDate, Status=@Status, Note=@Note WHERE Id=@Id;"
            : @"INSERT INTO Orders (CustomerId, Item, Price, DueDate, Status, Note) VALUES (@CustomerId, @Item, @Price, @DueDate, @Status, @Note);";
        Add(command, "@CustomerId", order.CustomerId);
        Add(command, "@Item", order.Item);
        Add(command, "@Price", order.Price);
        Add(command, "@DueDate", order.DueDate);
        Add(command, "@Status", order.Status);
        Add(command, "@Note", order.Note);
        if (order.Id > 0) Add(command, "@Id", order.Id);
        command.ExecuteNonQuery();
    }

    public void DeleteOrder(int orderId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Payments WHERE OrderId=@Id; DELETE FROM Orders WHERE Id=@Id;";
        Add(command, "@Id", orderId);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public List<Payment> GetPayments()
    {
        var items = new List<Payment>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.Id, p.OrderId, c.Name, p.Amount, p.PaidOn
            FROM Payments p JOIN Orders o ON o.Id=p.OrderId JOIN Customers c ON c.Id=o.CustomerId
            ORDER BY p.Id DESC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            items.Add(new Payment { Id = reader.GetInt32(0), OrderId = reader.GetInt32(1), CustomerName = Text(reader, 2), Amount = reader.GetDecimal(3), PaidOn = Text(reader, 4) });
        return items;
    }

    public void SavePayment(int orderId, decimal amount, DateTime paidOn)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment must be greater than zero.");
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var check = connection.CreateCommand();
        check.Transaction = transaction;
        check.CommandText = @"
            SELECT o.Price - COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.OrderId=o.Id), 0)
            FROM Orders o WHERE o.Id=@Id;";
        Add(check, "@Id", orderId);
        var scalar = check.ExecuteScalar();
        if (scalar is null || scalar is DBNull) throw new InvalidOperationException("The selected order no longer exists.");
        var remaining = Convert.ToDecimal(scalar);
        if (amount > remaining) throw new InvalidOperationException($"Payment exceeds the remaining balance ({remaining:N0} AFN).");

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO Payments (OrderId, Amount, PaidOn) VALUES (@OrderId, @Amount, @PaidOn);";
        Add(insert, "@OrderId", orderId);
        Add(insert, "@Amount", amount);
        Add(insert, "@PaidOn", paidOn.ToString("yyyy-MM-dd"));
        insert.ExecuteNonQuery();
        transaction.Commit();
    }

    public DashboardSummary GetDashboardSummary()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                (SELECT COUNT(*) FROM Customers),
                (SELECT COUNT(*) FROM Orders WHERE Status IN ('New','Doing')),
                (SELECT COALESCE(SUM(Amount),0) FROM Payments WHERE PaidOn=@Today),
                (SELECT COALESCE(SUM(o.Price-COALESCE((SELECT SUM(p.Amount) FROM Payments p WHERE p.OrderId=o.Id),0)),0) FROM Orders o);";
        Add(command, "@Today", DateTime.Today.ToString("yyyy-MM-dd"));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return new DashboardSummary();
        return new DashboardSummary
        {
            CustomersCount = reader.GetInt32(0), ActiveOrders = reader.GetInt32(1),
            TodayIncome = reader.GetDecimal(2), Outstanding = reader.GetDecimal(3)
        };
    }

    public void ExportBackup(string destinationPath)
    {
        using var source = OpenConnection();
        using var destination = new SqliteConnection($"Data Source={destinationPath}");
        destination.Open();
        source.BackupDatabase(destination);
    }

    public void ImportBackup(string backupPath)
    {
        if (!File.Exists(backupPath)) throw new FileNotFoundException("Backup file was not found.", backupPath);
        using (var validation = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly"))
        {
            validation.Open();
            using var check = validation.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Customers','Orders','Payments');";
            if (Convert.ToInt32(check.ExecuteScalar()) != 3)
                throw new InvalidDataException("This file is not a Tailor Shop database backup.");
        }

        var safetyCopy = DbPath + ".before-restore";
        if (File.Exists(DbPath)) File.Copy(DbPath, safetyCopy, overwrite: true);
        try
        {
            using var source = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly");
            using var target = OpenConnection();
            source.Open();
            source.BackupDatabase(target);
            Initialize();
        }
        catch
        {
            if (File.Exists(safetyCopy)) File.Copy(safetyCopy, DbPath, overwrite: true);
            throw;
        }
    }

    private static string Text(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

    private static void Add(SqliteCommand command, string name, object value) =>
        command.Parameters.AddWithValue(name, value ?? string.Empty);
}

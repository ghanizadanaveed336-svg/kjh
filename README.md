# Aqib Shah Tailoring Manager

A Windows WPF desktop application for customer, order, and payment management. The UI uses a black-and-gold tailor-shop theme and stores records locally in SQLite.

## Requirements
- Windows 10/11
- .NET 8 SDK or Visual Studio 2022 with the .NET desktop development workload

## Run and build
From the repository folder:

```powershell
dotnet restore .\TailorShop.csproj
dotnet build .\TailorShop.csproj --configuration Release
dotnet run --project .\TailorShop.csproj
```

The SQLite database is created at `%USERPROFILE%\Documents\AqibShahTailor.db`.

## Working functionality
- Customer create, edit (select a row, edit the form, save), and delete
- Order create, edit, status/date updates, and delete
- Payment recording with balance validation
- Dashboard metrics and recent-order list loaded from SQLite
- SQLite backup export and restore (restore first makes a `.before-restore` safety copy)

Backup restore replaces the current database. Confirm the restore prompt and keep your own backup copy.

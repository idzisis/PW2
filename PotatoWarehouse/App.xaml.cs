using System;
using System.IO;
using System.Windows;
using PotatoWarehouse.Data;

namespace PotatoWarehouse;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var dbPath = "warehouse.db";
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
        
        using var context = new WarehouseDbContext();
        context.Database.EnsureCreated();
    }
}

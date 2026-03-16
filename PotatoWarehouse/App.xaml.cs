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
        
        using var context = new WarehouseDbContext();
        context.Database.EnsureCreated();
    }
}

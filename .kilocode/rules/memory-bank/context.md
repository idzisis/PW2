# Active Context: PotatoWarehouse - C# WPF Application

## Current State

**Project Status**: ✅ Complete

A C# WPF desktop application for potato warehouse inventory management has been created. The application uses SQLite for data persistence and features a modern green-themed UI design.

## Recently Completed

- [x] Created C# WPF project structure
- [x] Implemented database models (Season, Variety, Caliber, IncomingPotato, OutgoingPotato)
- [x] Created SQLite database with Entity Framework Core
- [x] Built 4 UI sections: Sākumlapa, Ienākošie, Izejošie, Iestatījumi
- [x] Implemented season-based data filtering
- [x] Added inventory calculation (incoming - outgoing)
- [x] Created modern green-themed UI with card-based design
- [x] Added TargetWeight field to Season model
- [x] Added TotalWeightTons property to IncomingPotato and OutgoingPotato
- [x] Added edit buttons (✏️) to both Incoming and Outgoing tables
- [x] Fixed form field sizes to be equal width
- [x] Added auto-calculation for Total weight (weight * count)
- [x] Added bold formatting to weight columns in DataGrids
- [x] Added circular progress indicator in Settings for target vs actual

## Project Structure

| File/Directory | Purpose |
|----------------|---------|
| `PotatoWarehouse/` | Main project folder |
| `PotatoWarehouse/Models/Models.cs` | Database models |
| `PotatoWarehouse/Data/WarehouseDbContext.cs` | EF Core DbContext |
| `PotatoWarehouse/MainWindow.xaml` | Main UI |
| `PotatoWarehouse/MainWindow.xaml.cs` | Code-behind logic |
| `publish/PotatoWarehouse.exe` | Windows executable |

## Features Implemented

1. **Sākumlapa (Homepage)**: Shows total potato inventory by variety and caliber
2. **Ienākošie (Incoming)**: Add/view incoming potato shipments with auto-calculated totals
3. **Izejošie (Outgoing)**: Add/view outgoing shipments with buyer info
4. **Iestatījumi (Settings)**: Manage seasons, varieties, calibers, and target weights
5. **Edit functionality**: Click ✏️ to edit records (populates form, removes old record, user saves new)
6. **Target tracking**: Circular progress shows % of target weight achieved with color coding (green=100%, blue>=75%, orange>=50%, red<50%)

## How to Use

1. Open `PotatoWarehouse/PotatoWarehouse.csproj` in Visual Studio
2. Run the application (F5)
3. In Settings, create a season (e.g., "2026. sezona")
4. Add varieties (Vineta, Bellarosa, etc.)
5. Use default or custom calibers
6. Add incoming and outgoing potato records

## Session History

| Date | Changes |
|------|---------|
| 2026-03-15 | Created PotatoWarehouse C# WPF application |
| 2026-03-15 | Added target/usage section, edit buttons, equal field sizes, tons display, bold weight columns, circular progress indicator |

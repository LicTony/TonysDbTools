# GEMINI.md - Context & Instructions for Gemini CLI

## Project Overview
**TonysDbTools** is a desktop application built with **WPF** and **.NET 10.0**. Its stated purpose is to serve as a helper for database-related testing.

### Key Technologies
- **Framework:** .NET 10.0 (Windows target).
- **UI:** Windows Presentation Foundation (WPF).
- **Pattern:** Model-View-ViewModel (MVVM).
- **Library:** `CommunityToolkit.Mvvm` (v8.4.0) for observable properties and commands.

### Architecture
The project follows a standard MVVM structure:
- **Models/**: Data structures and core business logic (currently minimal).
- **ViewModels/**: Logic for the UI, using `CommunityToolkit.Mvvm`. `MainViewModel` inherits from `ViewModelBase`.
- **Views/**: XAML definitions and code-behind for the UI. `MainView` is the primary window.
- **App.xaml / App.xaml.cs**: Application entry point and initialization logic.

## Building and Running
As a standard .NET 10 project, use the following commands:

- **Restore dependencies:** `dotnet restore`
- **Build the project:** `dotnet build`
- **Run the application:** `dotnet run`
- **Test (if applicable):** `dotnet test` (No test project currently detected).

## Development Conventions
- **MVVM Integration:** Use `[ObservableProperty]` from `CommunityToolkit.Mvvm` for properties that need to notify the UI.
- **Naming Conventions:**
    - Classes/Methods: `PascalCase`.
    - Private fields (backing properties): `_camelCase`.
- **Initialization:** Views are initialized in `App.xaml.cs`, with their `DataContext` manually assigned to the corresponding ViewModel.

## Key Files
- `TonysDbTools.csproj`: Project configuration and dependencies.
- `Views/MainView.xaml`: Main UI layout.
- `ViewModels/MainViewModel.cs`: Core UI logic and properties.
- `App.xaml.cs`: Entry point where the main window is created and shown.
- `todo.md`: Pending tasks and features to implement.

## Pending Tasks
See [`todo.md`](todo.md) for the full list. Current main task:

- Implementar pantalla moderna con **iNKORE.UI.WPF.Modern** (`NavigationView` con 7 ítems + `AutoSuggestBox`) al estilo Windows 11/Fluent, combinando con **CommunityToolkit.Mvvm**.
  - Menú: Conexiones, Rel. 2 tablas, Rel. 3 tablas, Buscar en SPs, Buscar texto, Buscar número, Acerca de.

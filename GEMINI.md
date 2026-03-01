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
See [`todo.md`](todo.md) for the full list.

## Lessons Learned & Best Practices (iNKORE.UI.WPF.Modern)
Para evitar errores de compilación y visualización en este proyecto, seguir estas reglas:

### 1. Namespaces y URIs
- Usar siempre `http://schemas.inkore.net/lib/ui/wpf/modern` con el prefijo `ui:`. (El protocolo `https` suele fallar en el compilador XAML).

### 2. Declaración de Ventana
- No usar la etiqueta `<ui:ModernWindow>`.
- Usar la etiqueta estándar de WPF `<Window>` y aplicar el estilo moderno mediante propiedades adjuntas:
  ```xml
  ui:WindowHelper.UseModernWindowStyle="True"
  ui:WindowHelper.SystemBackdropType="Mica"
  ui:TitleBar.ExtendViewIntoTitleBar="True"
  ```

### 3. Uso de Controles
- **Controles Estándar (Sin prefijo):** `Button`, `TextBox`, `ComboBox`, `PasswordBox`, `DataGrid`, `CheckBox`, `TextBlock`, `Frame`, `Border`. La librería los estiliza automáticamente.
- **Controles Especiales (Con prefijo `ui:`):** `NavigationView`, `FontIcon`, `AutoSuggestBox`, `SimpleStackPanel`, `WindowHelper`.

### 4. Colores y Recursos
- **Color de Acento:** Usar `{ui:ThemeResource SystemControlHighlightAccentBrush}` para propiedades de tipo `Brush` (como `Foreground` o `Background`). Evitar `SystemAccentColor` ya que es de tipo `Color` y causa errores de conversión.
- **Estilos:** Si un estilo como `TitleTextBlockStyle` falla con `StaticResource`, verificar si existe en el diccionario global o simplemente omitirlo y usar propiedades locales (`FontSize`, `FontWeight`).


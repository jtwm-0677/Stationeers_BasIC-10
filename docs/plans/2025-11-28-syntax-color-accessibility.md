# Syntax Color Accessibility Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add colorblind-friendly presets and user-customizable syntax highlighting colors with a dedicated settings UI.

**Architecture:** Create a SyntaxColorSettings model class that stores RGB values for each syntax element. Extend SettingsService to persist these colors. Create SyntaxColorsWindow with preset dropdown and individual color pickers. Modify BasicHighlighting to read colors from settings instead of hardcoded values.

**Tech Stack:** WPF, System.Text.Json, AvalonEdit highlighting, Windows Color Picker

---

## Task 1: Create SyntaxColorSettings Model

**Files:**
- Create: `Editor/Highlighting/SyntaxColorSettings.cs`

**Step 1: Create the model class with all color properties and presets**

```csharp
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace BasicToMips.Editor.Highlighting;

public class SyntaxColorSettings
{
    // Color properties stored as hex strings for JSON serialization
    public string Keywords { get; set; } = "#569CD6";           // IF, THEN, WHILE
    public string Declarations { get; set; } = "#4EC9B0";       // VAR, LET, ALIAS
    public string DeviceRefs { get; set; } = "#9CDCFE";         // d0, d1, db
    public string Properties { get; set; } = "#9CDCFE";         // .Temperature, .On
    public string Functions { get; set; } = "#DCDCAA";          // ABS, SIN, MAX
    public string Labels { get; set; } = "#C586C0";             // main:
    public string Strings { get; set; } = "#CE9178";            // "text"
    public string Numbers { get; set; } = "#B5CEA8";            // 123, 3.14
    public string Comments { get; set; } = "#6A9955";           // ' comment
    public string Booleans { get; set; } = "#569CD6";           // TRUE, FALSE
    public string Operators { get; set; } = "#D4D4D4";          // +, -, *, /

    public string PresetName { get; set; } = "Default";

    // Convert hex string to Color
    public static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromRgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
        return Colors.White;
    }

    // Convert Color to hex string
    public static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // Get Color objects for use in highlighting
    public Color GetKeywordsColor() => HexToColor(Keywords);
    public Color GetDeclarationsColor() => HexToColor(Declarations);
    public Color GetDeviceRefsColor() => HexToColor(DeviceRefs);
    public Color GetPropertiesColor() => HexToColor(Properties);
    public Color GetFunctionsColor() => HexToColor(Functions);
    public Color GetLabelsColor() => HexToColor(Labels);
    public Color GetStringsColor() => HexToColor(Strings);
    public Color GetNumbersColor() => HexToColor(Numbers);
    public Color GetCommentsColor() => HexToColor(Comments);
    public Color GetBooleansColor() => HexToColor(Booleans);
    public Color GetOperatorsColor() => HexToColor(Operators);

    // Create a deep copy
    public SyntaxColorSettings Clone()
    {
        return new SyntaxColorSettings
        {
            Keywords = Keywords,
            Declarations = Declarations,
            DeviceRefs = DeviceRefs,
            Properties = Properties,
            Functions = Functions,
            Labels = Labels,
            Strings = Strings,
            Numbers = Numbers,
            Comments = Comments,
            Booleans = Booleans,
            Operators = Operators,
            PresetName = PresetName
        };
    }

    // Preset definitions
    public static SyntaxColorSettings GetPreset(string name)
    {
        return name switch
        {
            "Default" => GetDefaultPreset(),
            "Protanopia" => GetProtanopiaPreset(),
            "Deuteranopia" => GetDeuteranopiaPreset(),
            "Tritanopia" => GetTritanopiaPreset(),
            "High Contrast" => GetHighContrastPreset(),
            "Monochrome" => GetMonochromePreset(),
            _ => GetDefaultPreset()
        };
    }

    public static string[] GetPresetNames() => new[]
    {
        "Default",
        "Protanopia",
        "Deuteranopia",
        "Tritanopia",
        "High Contrast",
        "Monochrome",
        "Custom"
    };

    private static SyntaxColorSettings GetDefaultPreset() => new()
    {
        PresetName = "Default",
        Keywords = "#569CD6",
        Declarations = "#4EC9B0",
        DeviceRefs = "#9CDCFE",
        Properties = "#9CDCFE",
        Functions = "#DCDCAA",
        Labels = "#C586C0",
        Strings = "#CE9178",
        Numbers = "#B5CEA8",
        Comments = "#6A9955",
        Booleans = "#569CD6",
        Operators = "#D4D4D4"
    };

    // Protanopia (red-blind): Avoid red, use blue/yellow distinction
    private static SyntaxColorSettings GetProtanopiaPreset() => new()
    {
        PresetName = "Protanopia",
        Keywords = "#6699FF",       // Bright blue
        Declarations = "#00CCCC",   // Cyan
        DeviceRefs = "#FFCC00",     // Yellow
        Properties = "#FFCC00",     // Yellow
        Functions = "#99CCFF",      // Light blue
        Labels = "#FF99FF",         // Pink (safe)
        Strings = "#FFFF66",        // Bright yellow
        Numbers = "#66FFCC",        // Cyan-green
        Comments = "#999999",       // Gray
        Booleans = "#6699FF",       // Bright blue
        Operators = "#FFFFFF"       // White
    };

    // Deuteranopia (green-blind): Avoid green, use blue/orange distinction
    private static SyntaxColorSettings GetDeuteranopiaPreset() => new()
    {
        PresetName = "Deuteranopia",
        Keywords = "#6699FF",       // Bright blue
        Declarations = "#FF9933",   // Orange
        DeviceRefs = "#FFCC00",     // Yellow
        Properties = "#FFCC00",     // Yellow
        Functions = "#CC99FF",      // Lavender
        Labels = "#FF66CC",         // Pink
        Strings = "#FFFF66",        // Bright yellow
        Numbers = "#66CCFF",        // Sky blue
        Comments = "#999999",       // Gray
        Booleans = "#6699FF",       // Bright blue
        Operators = "#FFFFFF"       // White
    };

    // Tritanopia (blue-blind): Avoid blue, use red/green distinction
    private static SyntaxColorSettings GetTritanopiaPreset() => new()
    {
        PresetName = "Tritanopia",
        Keywords = "#FF6666",       // Coral red
        Declarations = "#66FF66",   // Bright green
        DeviceRefs = "#FFCC66",     // Gold
        Properties = "#FFCC66",     // Gold
        Functions = "#FF99CC",      // Pink
        Labels = "#CC66FF",         // Violet
        Strings = "#FFFF99",        // Pale yellow
        Numbers = "#99FF99",        // Light green
        Comments = "#AAAAAA",       // Gray
        Booleans = "#FF6666",       // Coral red
        Operators = "#FFFFFF"       // White
    };

    // High Contrast: Maximum distinction with bold primary colors
    private static SyntaxColorSettings GetHighContrastPreset() => new()
    {
        PresetName = "High Contrast",
        Keywords = "#FF0000",       // Pure red
        Declarations = "#00FF00",   // Pure green
        DeviceRefs = "#FFFF00",     // Pure yellow
        Properties = "#00FFFF",     // Pure cyan
        Functions = "#FF00FF",      // Pure magenta
        Labels = "#FFA500",         // Orange
        Strings = "#FFD700",        // Gold
        Numbers = "#00FF7F",        // Spring green
        Comments = "#808080",       // Gray
        Booleans = "#FF0000",       // Pure red
        Operators = "#FFFFFF"       // White
    };

    // Monochrome: Grayscale with brightness distinction
    private static SyntaxColorSettings GetMonochromePreset() => new()
    {
        PresetName = "Monochrome",
        Keywords = "#FFFFFF",       // White (brightest)
        Declarations = "#E0E0E0",   // Very light gray
        DeviceRefs = "#C0C0C0",     // Light gray
        Properties = "#C0C0C0",     // Light gray
        Functions = "#A0A0A0",      // Medium-light gray
        Labels = "#FFFFFF",         // White
        Strings = "#808080",        // Medium gray
        Numbers = "#B0B0B0",        // Light-medium gray
        Comments = "#606060",       // Dark gray
        Booleans = "#FFFFFF",       // White
        Operators = "#D0D0D0"       // Near-white
    };
}
```

**Step 2: Build to verify no syntax errors**

Run: `dotnet build`
Expected: Build succeeded

---

## Task 2: Extend SettingsService for Syntax Colors

**Files:**
- Modify: `UI/Services/SettingsService.cs`

**Step 1: Add SyntaxColors property and update SettingsData class**

Add these using statements at the top:
```csharp
using BasicToMips.Editor.Highlighting;
```

Add this property after line 19 (after AutoSaveIntervalSeconds):
```csharp
    public SyntaxColorSettings SyntaxColors { get; set; } = new();
```

Update the Load() method to load syntax colors after line 48:
```csharp
                    SyntaxColors = settings.SyntaxColors ?? new SyntaxColorSettings();
```

Update the Save() method - add to settings object creation around line 73:
```csharp
                SyntaxColors = SyntaxColors
```

Update the private SettingsData class to include:
```csharp
        public SyntaxColorSettings? SyntaxColors { get; set; }
```

**Full modified SettingsService.cs:**

```csharp
using System.IO;
using System.Text.Json;
using BasicToMips.Editor.Highlighting;

namespace BasicToMips.UI.Services;

public class SettingsService
{
    private readonly string _settingsPath;

    public string? StationeersPath { get; set; }
    public bool ShowDocumentation { get; set; } = true;
    public bool AutoCompile { get; set; } = true;
    public List<string> RecentFiles { get; set; } = new();
    public double FontSize { get; set; } = 14;
    public bool WordWrap { get; set; } = false;
    public int OptimizationLevel { get; set; } = 1;
    public string Theme { get; set; } = "Dark";
    public bool AutoSaveEnabled { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 60;
    public SyntaxColorSettings SyntaxColors { get; set; } = new();

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(appData, "BasicToMips");
        Directory.CreateDirectory(settingsDir);
        _settingsPath = Path.Combine(settingsDir, "settings.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                if (settings != null)
                {
                    StationeersPath = settings.StationeersPath;
                    ShowDocumentation = settings.ShowDocumentation;
                    AutoCompile = settings.AutoCompile;
                    RecentFiles = settings.RecentFiles ?? new List<string>();
                    FontSize = settings.FontSize > 0 ? settings.FontSize : 14;
                    WordWrap = settings.WordWrap;
                    OptimizationLevel = settings.OptimizationLevel;
                    Theme = settings.Theme ?? "Dark";
                    AutoSaveEnabled = settings.AutoSaveEnabled;
                    AutoSaveIntervalSeconds = settings.AutoSaveIntervalSeconds > 0 ? settings.AutoSaveIntervalSeconds : 60;
                    SyntaxColors = settings.SyntaxColors ?? new SyntaxColorSettings();
                }
            }
        }
        catch
        {
            // Use defaults if settings can't be loaded
        }
    }

    public void Save()
    {
        try
        {
            var settings = new SettingsData
            {
                StationeersPath = StationeersPath,
                ShowDocumentation = ShowDocumentation,
                AutoCompile = AutoCompile,
                RecentFiles = RecentFiles,
                FontSize = FontSize,
                WordWrap = WordWrap,
                OptimizationLevel = OptimizationLevel,
                Theme = Theme,
                AutoSaveEnabled = AutoSaveEnabled,
                AutoSaveIntervalSeconds = AutoSaveIntervalSeconds,
                SyntaxColors = SyntaxColors
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public void AddRecentFile(string path)
    {
        RecentFiles.Remove(path);
        RecentFiles.Insert(0, path);
        if (RecentFiles.Count > 10)
        {
            RecentFiles = RecentFiles.Take(10).ToList();
        }
        Save();
    }

    private class SettingsData
    {
        public string? StationeersPath { get; set; }
        public bool ShowDocumentation { get; set; }
        public bool AutoCompile { get; set; }
        public List<string>? RecentFiles { get; set; }
        public double FontSize { get; set; }
        public bool WordWrap { get; set; }
        public int OptimizationLevel { get; set; }
        public string? Theme { get; set; }
        public bool AutoSaveEnabled { get; set; } = true;
        public int AutoSaveIntervalSeconds { get; set; } = 60;
        public SyntaxColorSettings? SyntaxColors { get; set; }
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

---

## Task 3: Create SyntaxColorsWindow XAML

**Files:**
- Create: `UI/SyntaxColorsWindow.xaml`

**Step 1: Create the XAML file with preset dropdown and color pickers**

```xml
<Window x:Class="BasicToMips.UI.SyntaxColorsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Syntax Highlighting Colors"
        Height="650" Width="750"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        Style="{StaticResource MainWindowStyle}">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="{StaticResource TertiaryBackgroundBrush}" Padding="16,12">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="&#xE790;" FontFamily="Segoe MDL2 Assets" FontSize="20"
                           VerticalAlignment="Center" Foreground="{StaticResource AccentBrush}"/>
                <TextBlock Text=" Syntax Highlighting Colors" FontSize="18" FontWeight="SemiBold"
                           VerticalAlignment="Center" Margin="8,0,0,0"/>
            </StackPanel>
        </Border>

        <!-- Content -->
        <Grid Grid.Row="1" Margin="16">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="280"/>
            </Grid.ColumnDefinitions>

            <!-- Color Settings -->
            <ScrollViewer Grid.Column="0" VerticalScrollBarVisibility="Auto" Margin="0,0,16,0">
                <StackPanel>
                    <!-- Preset Selection -->
                    <TextBlock Text="Color Preset" FontWeight="SemiBold" FontSize="14"
                               Foreground="{StaticResource AccentBrush}" Margin="0,0,0,8"/>
                    <ComboBox x:Name="PresetCombo" Width="250" HorizontalAlignment="Left" Margin="0,0,0,16"
                              SelectionChanged="PresetCombo_SelectionChanged"/>

                    <TextBlock Text="Presets include colorblind-friendly options:"
                               Foreground="{StaticResource SecondaryTextBrush}" FontSize="11" Margin="0,0,0,4"/>
                    <TextBlock Text="• Protanopia - Red-blind friendly"
                               Foreground="{StaticResource SecondaryTextBrush}" FontSize="11"/>
                    <TextBlock Text="• Deuteranopia - Green-blind friendly"
                               Foreground="{StaticResource SecondaryTextBrush}" FontSize="11"/>
                    <TextBlock Text="• Tritanopia - Blue-blind friendly"
                               Foreground="{StaticResource SecondaryTextBrush}" FontSize="11"/>
                    <TextBlock Text="• High Contrast - Maximum distinction"
                               Foreground="{StaticResource SecondaryTextBrush}" FontSize="11"/>
                    <TextBlock Text="• Monochrome - Grayscale"
                               Foreground="{StaticResource SecondaryTextBrush}" FontSize="11" Margin="0,0,0,16"/>

                    <!-- Individual Color Pickers -->
                    <TextBlock Text="Individual Colors" FontWeight="SemiBold" FontSize="14"
                               Foreground="{StaticResource AccentBrush}" Margin="0,8,0,12"/>

                    <!-- Keywords -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Keywords:" VerticalAlignment="Center" ToolTip="IF, THEN, WHILE, FOR, etc."/>
                        <Border x:Name="KeywordColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Keywords"/>
                        <TextBlock Grid.Column="2" Text="IF, THEN, WHILE, FOR" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Declarations -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Declarations:" VerticalAlignment="Center"/>
                        <Border x:Name="DeclarationColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Declarations"/>
                        <TextBlock Grid.Column="2" Text="VAR, LET, ALIAS, DEFINE" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Device References -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Device Refs:" VerticalAlignment="Center"/>
                        <Border x:Name="DeviceRefColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="DeviceRefs"/>
                        <TextBlock Grid.Column="2" Text="d0, d1, d2, db" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Properties -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Properties:" VerticalAlignment="Center"/>
                        <Border x:Name="PropertyColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Properties"/>
                        <TextBlock Grid.Column="2" Text=".Temperature, .On, .Setting" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Functions -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Functions:" VerticalAlignment="Center"/>
                        <Border x:Name="FunctionColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Functions"/>
                        <TextBlock Grid.Column="2" Text="ABS, SIN, MAX, SQRT" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Labels -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Labels:" VerticalAlignment="Center"/>
                        <Border x:Name="LabelColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Labels"/>
                        <TextBlock Grid.Column="2" Text="main:, loop:, subroutine:" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Strings -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Strings:" VerticalAlignment="Center"/>
                        <Border x:Name="StringColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Strings"/>
                        <TextBlock Grid.Column="2" Text="&quot;Hello World&quot;" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Numbers -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Numbers:" VerticalAlignment="Center"/>
                        <Border x:Name="NumberColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Numbers"/>
                        <TextBlock Grid.Column="2" Text="123, 3.14159, -273.15" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Comments -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Comments:" VerticalAlignment="Center"/>
                        <Border x:Name="CommentColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Comments"/>
                        <TextBlock Grid.Column="2" Text="' This is a comment" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Booleans -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Booleans:" VerticalAlignment="Center"/>
                        <Border x:Name="BooleanColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Booleans"/>
                        <TextBlock Grid.Column="2" Text="TRUE, FALSE" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>

                    <!-- Operators -->
                    <Grid Margin="0,0,0,8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="140"/>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Text="Operators:" VerticalAlignment="Center"/>
                        <Border x:Name="OperatorColorBox" Grid.Column="1" Width="40" Height="24"
                                BorderBrush="#555" BorderThickness="1" CornerRadius="3" Cursor="Hand"
                                MouseLeftButtonUp="ColorBox_Click" Tag="Operators"/>
                        <TextBlock Grid.Column="2" Text="+ - * / = &lt; &gt;" Foreground="{StaticResource SecondaryTextBrush}"
                                   FontSize="11" VerticalAlignment="Center" Margin="8,0,0,0"/>
                    </Grid>
                </StackPanel>
            </ScrollViewer>

            <!-- Live Preview -->
            <Border Grid.Column="1" Background="#1E1E1E" BorderBrush="#333" BorderThickness="1" CornerRadius="4">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <Border Background="#333" Padding="8,4">
                        <TextBlock Text="Preview" FontWeight="SemiBold" FontSize="12"/>
                    </Border>
                    <TextBlock x:Name="PreviewText" Grid.Row="1" Padding="12" FontFamily="Consolas" FontSize="12"
                               xml:space="preserve"/>
                </Grid>
            </Border>
        </Grid>

        <!-- Footer -->
        <Border Grid.Row="2" Background="{StaticResource SecondaryBackgroundBrush}" Padding="16,12">
            <Grid>
                <Button Content="Reset to Default" Style="{StaticResource ModernButtonStyle}"
                        HorizontalAlignment="Left" Padding="12,6" Click="Reset_Click"/>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <Button Content="Cancel" Style="{StaticResource ModernButtonStyle}"
                            Width="100" Margin="0,0,8,0" Click="Cancel_Click"/>
                    <Button Content="Apply" Style="{StaticResource PrimaryButtonStyle}"
                            Width="100" Click="Apply_Click"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
```

---

## Task 4: Create SyntaxColorsWindow Code-Behind

**Files:**
- Create: `UI/SyntaxColorsWindow.xaml.cs`

**Step 1: Create the code-behind with color picker logic**

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BasicToMips.Editor.Highlighting;
using BasicToMips.UI.Services;

namespace BasicToMips.UI;

public partial class SyntaxColorsWindow : Window
{
    private readonly SettingsService _settings;
    private SyntaxColorSettings _currentColors;
    private bool _suppressPresetChange = false;

    public bool ColorsChanged { get; private set; } = false;

    public SyntaxColorsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _currentColors = settings.SyntaxColors.Clone();

        LoadPresets();
        UpdateColorBoxes();
        UpdatePreview();
    }

    private void LoadPresets()
    {
        PresetCombo.Items.Clear();
        foreach (var preset in SyntaxColorSettings.GetPresetNames())
        {
            PresetCombo.Items.Add(preset);
        }

        // Select current preset
        var currentPreset = _currentColors.PresetName;
        if (PresetCombo.Items.Contains(currentPreset))
        {
            _suppressPresetChange = true;
            PresetCombo.SelectedItem = currentPreset;
            _suppressPresetChange = false;
        }
        else
        {
            _suppressPresetChange = true;
            PresetCombo.SelectedItem = "Custom";
            _suppressPresetChange = false;
        }
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPresetChange) return;

        var selectedPreset = PresetCombo.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selectedPreset) || selectedPreset == "Custom") return;

        _currentColors = SyntaxColorSettings.GetPreset(selectedPreset);
        UpdateColorBoxes();
        UpdatePreview();
    }

    private void UpdateColorBoxes()
    {
        KeywordColorBox.Background = new SolidColorBrush(_currentColors.GetKeywordsColor());
        DeclarationColorBox.Background = new SolidColorBrush(_currentColors.GetDeclarationsColor());
        DeviceRefColorBox.Background = new SolidColorBrush(_currentColors.GetDeviceRefsColor());
        PropertyColorBox.Background = new SolidColorBrush(_currentColors.GetPropertiesColor());
        FunctionColorBox.Background = new SolidColorBrush(_currentColors.GetFunctionsColor());
        LabelColorBox.Background = new SolidColorBrush(_currentColors.GetLabelsColor());
        StringColorBox.Background = new SolidColorBrush(_currentColors.GetStringsColor());
        NumberColorBox.Background = new SolidColorBrush(_currentColors.GetNumbersColor());
        CommentColorBox.Background = new SolidColorBrush(_currentColors.GetCommentsColor());
        BooleanColorBox.Background = new SolidColorBrush(_currentColors.GetBooleansColor());
        OperatorColorBox.Background = new SolidColorBrush(_currentColors.GetOperatorsColor());
    }

    private void UpdatePreview()
    {
        PreviewText.Inlines.Clear();

        // Build preview with colored text
        AddColoredText("' Temperature Controller\n", _currentColors.GetCommentsColor());
        AddColoredText("ALIAS ", _currentColors.GetDeclarationsColor());
        AddColoredText("sensor ", _currentColors.GetOperatorsColor());
        AddColoredText("d0\n", _currentColors.GetDeviceRefsColor());
        AddColoredText("VAR ", _currentColors.GetDeclarationsColor());
        AddColoredText("temp ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("0\n\n", _currentColors.GetNumbersColor());
        AddColoredText("main:\n", _currentColors.GetLabelsColor());
        AddColoredText("    temp ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("sensor", _currentColors.GetOperatorsColor());
        AddColoredText(".Temperature\n", _currentColors.GetPropertiesColor());
        AddColoredText("    IF ", _currentColors.GetKeywordsColor());
        AddColoredText("temp ", _currentColors.GetOperatorsColor());
        AddColoredText("> ", _currentColors.GetOperatorsColor());
        AddColoredText("300 ", _currentColors.GetNumbersColor());
        AddColoredText("THEN\n", _currentColors.GetKeywordsColor());
        AddColoredText("        ", _currentColors.GetOperatorsColor());
        AddColoredText("PRINT ", _currentColors.GetKeywordsColor());
        AddColoredText("\"Hot!\"\n", _currentColors.GetStringsColor());
        AddColoredText("    ENDIF\n", _currentColors.GetKeywordsColor());
        AddColoredText("    x ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("ABS", _currentColors.GetFunctionsColor());
        AddColoredText("(", _currentColors.GetOperatorsColor());
        AddColoredText("temp", _currentColors.GetOperatorsColor());
        AddColoredText(")\n", _currentColors.GetOperatorsColor());
        AddColoredText("    isOn ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("TRUE\n", _currentColors.GetBooleansColor());
        AddColoredText("    YIELD\n", _currentColors.GetKeywordsColor());
        AddColoredText("    GOTO ", _currentColors.GetKeywordsColor());
        AddColoredText("main\n", _currentColors.GetLabelsColor());
    }

    private void AddColoredText(string text, Color color)
    {
        PreviewText.Inlines.Add(new Run(text) { Foreground = new SolidColorBrush(color) });
    }

    private void ColorBox_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string colorName) return;

        var currentColor = colorName switch
        {
            "Keywords" => _currentColors.GetKeywordsColor(),
            "Declarations" => _currentColors.GetDeclarationsColor(),
            "DeviceRefs" => _currentColors.GetDeviceRefsColor(),
            "Properties" => _currentColors.GetPropertiesColor(),
            "Functions" => _currentColors.GetFunctionsColor(),
            "Labels" => _currentColors.GetLabelsColor(),
            "Strings" => _currentColors.GetStringsColor(),
            "Numbers" => _currentColors.GetNumbersColor(),
            "Comments" => _currentColors.GetCommentsColor(),
            "Booleans" => _currentColors.GetBooleansColor(),
            "Operators" => _currentColors.GetOperatorsColor(),
            _ => Colors.White
        };

        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
            var hexColor = SyntaxColorSettings.ColorToHex(newColor);

            switch (colorName)
            {
                case "Keywords": _currentColors.Keywords = hexColor; break;
                case "Declarations": _currentColors.Declarations = hexColor; break;
                case "DeviceRefs": _currentColors.DeviceRefs = hexColor; break;
                case "Properties": _currentColors.Properties = hexColor; break;
                case "Functions": _currentColors.Functions = hexColor; break;
                case "Labels": _currentColors.Labels = hexColor; break;
                case "Strings": _currentColors.Strings = hexColor; break;
                case "Numbers": _currentColors.Numbers = hexColor; break;
                case "Comments": _currentColors.Comments = hexColor; break;
                case "Booleans": _currentColors.Booleans = hexColor; break;
                case "Operators": _currentColors.Operators = hexColor; break;
            }

            _currentColors.PresetName = "Custom";
            _suppressPresetChange = true;
            PresetCombo.SelectedItem = "Custom";
            _suppressPresetChange = false;

            UpdateColorBoxes();
            UpdatePreview();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _currentColors = SyntaxColorSettings.GetPreset("Default");
        _suppressPresetChange = true;
        PresetCombo.SelectedItem = "Default";
        _suppressPresetChange = false;
        UpdateColorBoxes();
        UpdatePreview();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        _settings.SyntaxColors = _currentColors;
        _settings.Save();
        ColorsChanged = true;
        DialogResult = true;
        Close();
    }
}
```

---

## Task 5: Update BasicHighlighting to Use Settings

**Files:**
- Modify: `Editor/Highlighting/BasicHighlighting.cs`

**Step 1: Modify Create() method to accept SyntaxColorSettings**

Replace the entire `BasicHighlighting` class with:

```csharp
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;

namespace BasicToMips.Editor.Highlighting;

public static class BasicHighlighting
{
    private static SyntaxColorSettings _colors = new();

    public static void SetColors(SyntaxColorSettings colors)
    {
        _colors = colors;
    }

    public static IHighlightingDefinition Create()
    {
        return Create(_colors);
    }

    public static IHighlightingDefinition Create(SyntaxColorSettings colors)
    {
        var definition = new CustomHighlightingDefinition();

        var keywordColor = colors.GetKeywordsColor();
        var typeColor = colors.GetDeclarationsColor();
        var stringColor = colors.GetStringsColor();
        var commentColor = colors.GetCommentsColor();
        var numberColor = colors.GetNumbersColor();
        var operatorColor = colors.GetOperatorsColor();
        var functionColor = colors.GetFunctionsColor();
        var propertyColor = colors.GetPropertiesColor();
        var labelColor = colors.GetLabelsColor();
        var deviceRefColor = colors.GetDeviceRefsColor();
        var booleanColor = colors.GetBooleansColor();

        // Keywords
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(IF|THEN|ELSE|ELSEIF|ENDIF|END IF|FOR|TO|STEP|NEXT|WHILE|WEND|ENDWHILE|DO|LOOP|UNTIL|GOTO|GOSUB|RETURN|END|SUB|ENDSUB|END SUB|FUNCTION|ENDFUNCTION|CALL|EXIT|BREAK|CONTINUE)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
            IgnoreCase = true
        });

        // Declaration keywords
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(VAR|CONST|DIM|LET|ALIAS|DEFINE|AS|INTEGER|SINGLE|BOOLEAN)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(typeColor) },
            IgnoreCase = true
        });

        // Control keywords
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(YIELD|SLEEP|PRINT|INPUT|REM)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
            IgnoreCase = true
        });

        // Boolean literals
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(TRUE|FALSE)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(booleanColor) },
            IgnoreCase = true
        });

        // Logical operators
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(AND|OR|NOT|MOD)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) },
            IgnoreCase = true
        });

        // Built-in functions
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(ABS|SIN|COS|TAN|ASIN|ACOS|ATAN|ATAN2|SQRT|SQR|EXP|LOG|LOG10|CEIL|FLOOR|ROUND|TRUNC|INT|FIX|MIN|MAX|RND|RAND|SGN|POW|IIF|INRANGE|LERP)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(functionColor) },
            IgnoreCase = true
        });

        // Device references
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(d[0-5]|db)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(deviceRefColor) },
            IgnoreCase = true
        });

        // Common device properties
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\.(Temperature|Pressure|Power|On|Off|Open|Lock|Setting|Ratio|Quantity|Occupied|Mode|Error|Charge|SolarAngle|Activate|Vertical|Horizontal)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(propertyColor) },
            IgnoreCase = true
        });

        // Labels (word followed by colon at line start)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"^\s*\w+:",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(labelColor) }
        });

        // Strings
        definition.AddRule(new HighlightingRule
        {
            Pattern = "\"[^\"]*\"",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(stringColor) }
        });

        // Numbers
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b\d+\.?\d*\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(numberColor) }
        });

        // Comments (BASIC style: ' and REM)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"'.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) }
        });

        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\bREM\b.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) },
            IgnoreCase = true
        });

        // IC10 style comments (# comment) - for hybrid mode
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"#.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) }
        });

        // IC10 instructions (for hybrid mode)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(move|add|sub|mul|div|mod|and|or|xor|nor|slt|sgt|seq|sne|sle|sge|beq|bne|blt|bgt|ble|bge|beqz|bnez|bgtz|bltz|bgez|blez|j|jal|jr|push|pop|peek|yield|sleep|hcf|select)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(keywordColor) }
        });

        // IC10 registers (for hybrid mode)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\br([0-9]|1[0-5])\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(deviceRefColor) }
        });

        return definition;
    }
}
```

Also update `MipsHighlighting` to accept colors (add after BasicHighlighting class):

```csharp
public static class MipsHighlighting
{
    public static IHighlightingDefinition Create()
    {
        return Create(new SyntaxColorSettings());
    }

    public static IHighlightingDefinition Create(SyntaxColorSettings colors)
    {
        var definition = new CustomHighlightingDefinition();

        var instructionColor = colors.GetKeywordsColor();
        var registerColor = colors.GetDeviceRefsColor();
        var labelColor = colors.GetLabelsColor();
        var numberColor = colors.GetNumbersColor();
        var commentColor = colors.GetCommentsColor();
        var directiveColor = colors.GetDeclarationsColor();

        // Instructions
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(move|add|sub|mul|div|mod|and|or|xor|nor|slt|sgt|seq|sne|sle|sge|beq|bne|blt|bgt|ble|bge|bna|bnan|beqz|bnez|bgtz|bltz|bgez|blez|j|jal|jr|l|s|ls|ss|lb|sb|lr|sr|push|pop|peek|poke|get|put|getd|putd|alias|define|yield|sleep|hcf|abs|ceil|floor|round|trunc|sqrt|exp|log|sin|cos|tan|asin|acos|atan|atan2|min|max|rand|select|sap|sdb|sdns|sdse)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(instructionColor) }
        });

        // Registers (r0-r15 only, plus sp and ra)
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(r[0-9]|r1[0-5]|sp|ra)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(registerColor) }
        });

        // Device references
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b(d[0-5]|db)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(registerColor) }
        });

        // Labels
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"^\w+:",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(labelColor) }
        });

        // Directives
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"^\s*(alias|define)\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(directiveColor) }
        });

        // Numbers
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"\b-?\d+\.?\d*\b",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(numberColor) }
        });

        // Comments
        definition.AddRule(new HighlightingRule
        {
            Pattern = @"#.*$",
            Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(commentColor) }
        });

        return definition;
    }
}
```

---

## Task 6: Add Menu Item and Wire Up MainWindow

**Files:**
- Modify: `UI/MainWindow.xaml` (add menu item)
- Modify: `UI/MainWindow.xaml.cs` (add click handler and refresh logic)

**Step 1: Add menu item in MainWindow.xaml**

Find the Settings menu item (around line 155-160) and add after it:

```xml
<MenuItem Header="Syntax _Colors..." Click="SyntaxColors_Click"/>
```

**Step 2: Add handler in MainWindow.xaml.cs**

Add this method (near other menu click handlers):

```csharp
private void SyntaxColors_Click(object sender, RoutedEventArgs e)
{
    var dialog = new SyntaxColorsWindow(_settings);
    dialog.Owner = this;
    if (dialog.ShowDialog() == true && dialog.ColorsChanged)
    {
        // Refresh editor highlighting
        RefreshSyntaxHighlighting();
    }
}

private void RefreshSyntaxHighlighting()
{
    BasicHighlighting.SetColors(_settings.SyntaxColors);
    BasicEditor.SyntaxHighlighting = BasicHighlighting.Create(_settings.SyntaxColors);
    MipsOutput.SyntaxHighlighting = MipsHighlighting.Create(_settings.SyntaxColors);
}
```

**Step 3: Initialize colors on startup**

In `Window_Loaded` method, add near the beginning:

```csharp
BasicHighlighting.SetColors(_settings.SyntaxColors);
```

And update editor initialization to use settings colors:

```csharp
BasicEditor.SyntaxHighlighting = BasicHighlighting.Create(_settings.SyntaxColors);
MipsOutput.SyntaxHighlighting = MipsHighlighting.Create(_settings.SyntaxColors);
```

---

## Task 7: Build and Test

**Step 1: Build the project**

Run: `dotnet build --configuration Release`
Expected: Build succeeded

**Step 2: Test manually**

1. Launch the application
2. Go to Tools > Syntax Colors
3. Select different presets (Protanopia, Deuteranopia, etc.)
4. Verify preview updates
5. Click individual color boxes and change colors
6. Click Apply
7. Verify editor colors updated
8. Close and reopen app - verify colors persisted

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add colorblind-friendly syntax highlighting presets and custom color picker

- Add SyntaxColorSettings model with 6 presets (Default, Protanopia, Deuteranopia, Tritanopia, High Contrast, Monochrome)
- Add SyntaxColorsWindow with preset dropdown and individual color pickers
- Add live preview panel showing all syntax element colors
- Persist color settings to JSON
- Update BasicHighlighting and MipsHighlighting to use configurable colors
- Add Tools > Syntax Colors menu item"
```

---

## Summary

| Task | Files | Description |
|------|-------|-------------|
| 1 | `Editor/Highlighting/SyntaxColorSettings.cs` | Color model with presets |
| 2 | `UI/Services/SettingsService.cs` | Persist colors to JSON |
| 3 | `UI/SyntaxColorsWindow.xaml` | Settings window XAML |
| 4 | `UI/SyntaxColorsWindow.xaml.cs` | Settings window logic |
| 5 | `Editor/Highlighting/BasicHighlighting.cs` | Use configurable colors |
| 6 | `UI/MainWindow.xaml`, `MainWindow.xaml.cs` | Menu item and wiring |
| 7 | - | Build and test |

**Total new files:** 3
**Total modified files:** 4
**Estimated implementation time:** Single focused session

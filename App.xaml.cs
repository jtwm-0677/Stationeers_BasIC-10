using System.Windows;
using BasicToMips.Data;
using BasicToMips.UI;

namespace BasicToMips;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set up global exception handling
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        // Initialize the living hash dictionary for decompilation support
        HashDictionary.Initialize();

        // Show splash screen
        var splash = new SplashWindow();
        splash.Show();

        // Wait for 2 seconds while showing splash
        await Task.Delay(2000);

        // Create and show main window
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        // Close splash
        splash.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Save the living hash dictionary
        HashDictionary.Save();
        base.OnExit(e);
    }
}

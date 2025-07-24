using System;
using System.Diagnostics;
using CommunityToolkit.Maui;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using MAUI_Class_Tracker;
using MAUI_Class_Tracker.Views;
using MAUI_Class_Tracker.Services;

namespace MAUI_Class_Tracker;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; }
    public static DataService _dataService { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        Services = serviceProvider;
        _dataService = Services.GetRequiredService<DataService>();

        //if (Preferences.Get("IsLoggedIn", false))
        //    MainPage = new AppShell(Services.GetRequiredService<DataService>());
        //else
        //    MainPage = new NavigationPage(new LoginPage(Services.GetRequiredService<DataService>()));
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        await InitAsync();

        // Handle initial routing based on login state
        if (Preferences.Get("IsLoggedIn", false))
        {
            await Shell.Current.GoToAsync("///HomePage"); 
        }
        else
        {
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
    }

    private async Task InitAsync()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "wgu_tracker.db");
        Debug.WriteLine($"SQLite DB Path: {dbPath}");
        if (File.Exists(dbPath))
            Debug.WriteLine("Database file exists.");
        else
            Debug.WriteLine("Database file not found!");
        await _dataService.InitializeAsync();

        await _dataService.SeedDataAsync();

        MainPage = new AppShell();
    }
    protected override void OnSleep()
    {

        base.OnSleep();
        Preferences.Set("IsLoggedIn", false);


    }

}

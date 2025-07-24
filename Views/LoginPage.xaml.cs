using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Plugin.LocalNotification;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MAUI_Class_Tracker.Views;

public partial class LoginPage : ContentPage
{
    private readonly DataService _dataService;

    public LoginPage() : this(App._dataService)
    {
    }

    public LoginPage(DataService dataService)
    {
        InitializeComponent();
        _dataService = dataService;
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UsernameEntry.Text = string.Empty;
        PasswordEntry.Text = string.Empty;
    }

    private void FocusPassword(object sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private bool IsInputValid(string username, string password)
    {
        return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
    }

    private async Task<User?> TryLoginTestUserAsync(string username, string password)
    {
        if (username == "test" && password == "test")
        {
            return new User
            {
                Id = 0,
                Username = "test",
                HashedPassword = ""
            };
        }

        return null;
    }

    private void SetUserPreferences(User user)
    {
        Preferences.Set("IsLoggedIn", false);
        Preferences.Set("Username", user.Username);
        Preferences.Set("UserId", user.Id);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim();
        string password = PasswordEntry.Text;

        if (!IsInputValid(username, password))
        {
            await DisplayAlert("Error", "Username and password are required.", "OK");
            return;
        }

        // DEBUG: List all registered users and their hashed passwords
        //var users = await _dataService.GetAllUsersAsync();
        //foreach (var u in users)
        //    Debug.WriteLine($"[VERIFY] User: {u.Username}, Hash: {u.HashedPassword}");

        // Proceed with login
        var user = await TryLoginTestUserAsync(username, password)
                   ?? await _dataService.AuthenticateUserAsync(username, password); 

        if (user == null)
        {
            await DisplayAlert("Login Failed", "Invalid credentials.", "OK");
            return;
        }

        SetUserPreferences(user);
        Preferences.Set("IsLoggedIn", true);

        await Task.Delay(100); // optional, to let Shell initialize
        Application.Current.MainPage = new AppShell();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim();
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Error", "Username and password are required.", "OK");
            return;
        }

        var existingUser = await _dataService.GetUserByUsernameAsync(username);
        if (existingUser != null)
        {
            await DisplayAlert("Error", "Username already exists.", "OK");
            return;
        }

        var newUser = new User
        {
            Username = username,
            HashedPassword = DataService.HashPassword(password),
            Active = true
        };

        await _dataService.SaveUserAsync(newUser);
        Debug.WriteLine($"[VERIFY] After SaveUserAsync, newUser.Id = {newUser.Id}");

        Preferences.Set("IsLoggedIn", true);
        Preferences.Set("Username", newUser.Username);
        Preferences.Set("UserId", newUser.Id);
        
        if (newUser.Id == 0)
        {
            Debug.WriteLine("[DEBUG] User.Id is still 0 after InsertAsync — persistence will break.");
        }

        //Application.Current.MainPage = new AppShell();
        await Shell.Current.GoToAsync(nameof(HomePage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // Clear login state
        Preferences.Set("IsLoggedIn", false);
        Preferences.Remove("Username");
        Preferences.Remove("UserId");

        Application.Current.MainPage = new NavigationPage(new LoginPage(_dataService));
        //await Shell.Current.GoToAsync(nameof(LoginPage));
    }

}
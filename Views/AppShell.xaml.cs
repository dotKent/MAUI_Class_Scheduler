using Microsoft.Maui.Controls;
using MAUI_Class_Tracker.Views;
using MAUI_Class_Tracker.Models;
using MAUI_Class_Tracker.Services;

namespace MAUI_Class_Tracker.Views
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            var homePage = App.Services.GetRequiredService<HomePage>();
            var calendar = App.Services.GetRequiredService<CalendarPage>();
            var termList = App.Services.GetRequiredService<TermListPage>();
            var search = App.Services.GetRequiredService<Search>();
            var reports = App.Services.GetRequiredService<ReportPage>();
            var logout = App.Services.GetRequiredService<LoginPage>();

            Items.Add(new FlyoutItem
            {
                Title = "Home",
                Icon = "home.png",
                Route = nameof(HomePage),
                Items = { new ShellContent { Title = "Home", Content = homePage, Route = nameof(HomePage) } }
            });

            Items.Add(new FlyoutItem
            {
                Title = "Terms",
                Icon = "calendar.png",
                Items = { new ShellContent { Title = "Terms", Content = termList, Route = nameof(TermListPage) } }
            });

            Items.Add(new FlyoutItem
            {
                Title = "Search",
                Icon = "search.png",
                Items = { new ShellContent { Title = "Search", Content = search, Route = nameof(Search) } }
            });

            Items.Add(new FlyoutItem
            {
                Title = "Reports",
                Icon = "reports.png",
                Items = { new ShellContent { Title = "Reports", Content = reports, Route = nameof(ReportPage) } }
            });

            Items.Add(new FlyoutItem
            {
                Title = "Calendar",
                Icon = "calendar.png",
                Items = { new ShellContent { Title = "Calendar", Content = calendar, Route = nameof(CalendarPage) } }
            });

            Items.Add(new FlyoutItem
            {
                Title = "Log Out",
                Icon = "logout.png",
                Items = { new ShellContent { Title = "Log Out", Content = logout, Route = nameof(LoginPage) } }
            });

            //Items.Add(new MenuItem
            //{
            //    Text = "Log Out",
            //    IconImageSource = "logout.png",o
            //    IsDestructive = true,
            //    Command = new Command(() => OnLogoutClicked(this, EventArgs.Empty))
            //});

            //_dataService = dataService;

            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(Search), typeof(Search));
            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
            Routing.RegisterRoute(nameof(TermListPage), typeof(TermListPage));
            Routing.RegisterRoute(nameof(TermDetailPage), typeof(TermDetailPage));
            Routing.RegisterRoute(nameof(CourseDetailPage), typeof(CourseDetailPage));
            Routing.RegisterRoute(nameof(AssessmentDetailPage), typeof(AssessmentDetailPage));
            Routing.RegisterRoute(nameof(ReportPage), typeof(ReportPage));
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (!isLoggedIn)
            {
                await Shell.Current.GoToAsync(nameof(LoginPage));
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Set("IsLoggedIn", false);
            Preferences.Remove("UserId");
            Preferences.Remove("Username");


            //// Replace the app's root UI with the login screen
            //Application.Current.MainPage = new NavigationPage(new LoginPage(_dataService));

            //// Optionally await a short task to avoid async warning
            //await Task.CompletedTask;

            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
    }
}

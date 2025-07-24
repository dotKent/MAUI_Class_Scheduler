using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using MAUI_Class_Tracker.Services;
using MAUI_Class_Tracker.Views;

namespace MAUI_Class_Tracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp(sp => new App(sp))
            .UseMauiCommunityToolkit()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<TermListPage>();
        builder.Services.AddTransient<TermDetailPage>();
        builder.Services.AddTransient<CourseDetailPage>();
        builder.Services.AddTransient<AssessmentDetailPage>();
        builder.Services.AddTransient<ReportPage>();
        builder.Services.AddTransient<CalendarPage>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<DataService>();
        builder.Services.AddSingleton<Search>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

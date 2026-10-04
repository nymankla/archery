using System.Globalization;
using System.Reflection;
using Archery.Mobile.Auth;
using Archery.Mobile.Configuration;
using Archery.Mobile.Core.ViewModels;
using Archery.Mobile.Services;
using Archery.Mobile.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archery.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Endpoints are embedded in the assembly: there is no appsettings.json on disk inside
        // an APK, and MAUI has no Aspire service discovery to resolve "https://apiservice".
        //
        // NOT READY TO SHIP: this is the only settings file, so a Release build still points at
        // the development stack on 10.0.2.2. Before any package leaves this machine, add an
        // appsettings.Production.json with the real HTTPS endpoints and select between the two
        // here with #if DEBUG. Release also drops the cleartext exemption, so a Release build
        // aimed at an http endpoint will simply fail to connect rather than silently misbehave.
        using var settings = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Archery.Mobile.appsettings.json")
            ?? throw new InvalidOperationException(
                "Embedded appsettings.json is missing; check the EmbeddedResource LogicalName.");
        builder.Configuration.AddJsonStream(settings);

        // Both server-side services force sv-SE. Matching it here keeps dates and the decimal
        // separator consistent with the web app. Setting only DefaultThreadCurrent* is not
        // enough: the UI thread is already running by this point.
        var culture = new CultureInfo(builder.Configuration["Locale"] ?? "sv-SE");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        builder.Services.Configure<ArcheryOptions>(builder.Configuration);

        // --- auth ---
        builder.Services.AddSingleton<ISecureTokenStore, SecureStorageTokenStore>();
        builder.Services.AddSingleton<ArcheryAuthService>();
        builder.Services.AddSingleton<IAuthService>(sp => sp.GetRequiredService<ArcheryAuthService>());
        // The same instance also feeds tokens to the shared client.
        builder.Services.AddSingleton<IArcheryTokenProvider>(sp => sp.GetRequiredService<ArcheryAuthService>());

        // --- api ---
        builder.Services.AddHttpClient<IArcheryApiClient, ArcheryApiClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<ArcheryOptions>>().Value;
            http.BaseAddress = new Uri(options.Api.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        // --- platform services ---
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, MauiDialogService>();
        builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();

        // --- shell, pages and view models ---
        // Transient: on an online-only app a cached view model means stale data after
        // navigating away and back.
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<MembersPage>();
        builder.Services.AddTransient<MembersViewModel>();
        builder.Services.AddTransient<MemberDetailPage>();
        builder.Services.AddTransient<MemberDetailViewModel>();
        builder.Services.AddTransient<MemberEditPage>();
        builder.Services.AddTransient<MemberEditViewModel>();
        builder.Services.AddTransient<TrainingAttendancePage>();
        builder.Services.AddTransient<TrainingAttendanceViewModel>();
        builder.Services.AddTransient<TrainingHistoryPage>();
        builder.Services.AddTransient<TrainingHistoryViewModel>();
        builder.Services.AddTransient<CompetitionsPage>();
        builder.Services.AddTransient<CompetitionsViewModel>();
        builder.Services.AddTransient<CompetitionDetailPage>();
        builder.Services.AddTransient<CompetitionDetailViewModel>();
        builder.Services.AddTransient<CompetitionEditPage>();
        builder.Services.AddTransient<CompetitionEditViewModel>();
        builder.Services.AddTransient<ParticipantRegisterPage>();
        builder.Services.AddTransient<ParticipantRegisterViewModel>();
        builder.Services.AddTransient<ResultEditPage>();
        builder.Services.AddTransient<ResultEditViewModel>();
        builder.Services.AddTransient<ExternalParticipantsPage>();
        builder.Services.AddTransient<ExternalParticipantsViewModel>();
        builder.Services.AddTransient<ExternalParticipantEditPage>();
        builder.Services.AddTransient<ExternalParticipantEditViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        return builder.Build();
    }
}

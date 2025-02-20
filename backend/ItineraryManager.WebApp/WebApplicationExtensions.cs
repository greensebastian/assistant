using ItineraryManager.Domain.Itineraries;
using ItineraryManager.Domain.Itineraries.Dependencies;
using ItineraryManager.WebApp.Components;
using ItineraryManager.WebApp.Infrastructure;
using ItineraryManager.WebApp.Infrastructure.Database;
using ItineraryManager.WebApp.Infrastructure.GoogleMaps;
using ItineraryManager.WebApp.Infrastructure.OpenAi;
using MudBlazor.Services;

namespace ItineraryManager.WebApp;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddItineraryManager(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        
        builder.Services.AddMudServices();

        var mongoDbSettings = builder.ConfigureAndSnapshot<MongoDbSettings>("MongoDb");
        builder.Services.AddMongoDB<ItineraryManagerDbContext>(mongoDbSettings.ConnectionString, mongoDbSettings.DatabaseName);
        builder.Services.AddHostedService<ItineraryManagerDbContextInitializationJob>();
        builder.Services.AddScoped<IItineraryRepository, ItineraryRepository>();
        builder.Services.AddScoped<ItineraryService>();
        builder.Services.AddSingleton<IItineraryChangeProvider, ItineraryChangeProvider>();
        builder.ConfigureAndSnapshot<OpenAiSettings>("OpenAi");
        builder.Services.AddSingleton<OpenAiClient>();
        builder.Services.AddSingleton<GoogleMapsClient>();
        
        return builder;
    }

    public static WebApplication AddItineraryManager(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        
        return app;
    }

    private static T ConfigureAndSnapshot<T>(this WebApplicationBuilder builder, string sectionKey) where T : class, new()
    {
        var section = builder.Configuration.GetSection(sectionKey);
        builder.Services.Configure<T>(section);
        var options = new T();
        section.Bind(options);
        return options;
    }
}
using Google.Maps.Places.V1;
using ItineraryManager.Domain.Itineraries;
using ItineraryManager.Domain.Itineraries.Dependencies;
using ItineraryManager.WebApp.Components;
using ItineraryManager.WebApp.Infrastructure;
using ItineraryManager.WebApp.Infrastructure.Database;
using ItineraryManager.WebApp.Infrastructure.GoogleMaps;
using ItineraryManager.WebApp.Infrastructure.MapTiler;
using ItineraryManager.WebApp.Infrastructure.OpenAi;
using Microsoft.Extensions.Caching.Hybrid;
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
        var googleMapsSettings = builder.ConfigureAndSnapshot<GoogleMapsSettings>("GoogleMaps");
        builder.Services.AddSingleton(new PlacesClientBuilder
        {
            ApiKey = googleMapsSettings.ApiKey
        }.Build());
#pragma warning disable EXTEXP0018
        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromHours(1),
                Expiration = TimeSpan.FromHours(1)
            };
        });
#pragma warning restore EXTEXP0018
        builder.ConfigureAndSnapshot<MapTilerSettings>("MapTiler");
        
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
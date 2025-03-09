using Assistant.WebApp.Infrastructure.Database;
using Assistant.WebApp.Infrastructure.GoogleMaps;
using Assistant.WebApp.Infrastructure.MapTiler;
using Google.Maps.Places.V1;
using Assistant.Domain.Itineraries;
using Assistant.Domain.MealPlans;
using Assistant.Domain.Projects;
using Assistant.WebApp.Components;
using Assistant.WebApp.Infrastructure.OpenAi.Itineraries;
using Assistant.WebApp.Infrastructure.OpenAi.MealPlans;
using Assistant.WebApp.Projects;
using Microsoft.Extensions.Caching.Hybrid;
using MudBlazor.Services;

namespace Assistant.WebApp;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddAssistant(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddMudServices();

        var mongoDbSettings = builder.ConfigureAndSnapshot<MongoDbSettings>("MongoDb");
        builder.Services.AddMongoDB<AssistantDbContext>(mongoDbSettings.ConnectionString, mongoDbSettings.DatabaseName);
        builder.Services.AddHostedService<AssistantDbContextInitializationJob>();
        
        builder.AddProjectType<Itinerary, string?, Activity, ItineraryChange, ItineraryChangeRequestResponseModel, ItineraryRepository>()
            .WithProcessor<GoogleMapsClient>();
        var googleMapsSettings = builder.ConfigureAndSnapshot<GoogleMapsSettings>("GoogleMaps");
        builder.Services.AddSingleton(new PlacesClientBuilder
        {
            ApiKey = googleMapsSettings.ApiKey
        }.Build());

        builder.Services.AddHttpClient<UrlValidator>();
        builder
            .AddProjectType<MealPlan, string?, Meal, IChange<MealPlan>, MealPlanChangeRequestModel,
                MealPlanRepository>().WithProcessor<UrlValidator>();
        
        
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

    public static WebApplication AddAssistant(this WebApplication app)
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

    internal static T ConfigureAndSnapshot<T>(this WebApplicationBuilder builder, string sectionKey) where T : class, new()
    {
        var section = builder.Configuration.GetSection(sectionKey);
        builder.Services.Configure<T>(section);
        var options = new T();
        section.Bind(options);
        return options;
    }
}
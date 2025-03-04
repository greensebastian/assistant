using Assistant.WebApp.Infrastructure;
using Assistant.WebApp.Infrastructure.Database;
using Assistant.WebApp.Infrastructure.GoogleMaps;
using Assistant.WebApp.Infrastructure.MapTiler;
using Assistant.WebApp.Infrastructure.OpenAi;
using Google.Maps.Places.V1;
using Assistant.Domain.Itineraries;
using Assistant.Domain.Projects;
using Assistant.WebApp.Components;
using Microsoft.Extensions.Caching.Hybrid;
using MudBlazor.Services;

namespace Assistant.WebApp;

public static class WebApplicationExtensions
{
    private static WebApplicationBuilder AddProjectType<TProject, TMeta, TItem, TChange, TResponse, TRepository>(this WebApplicationBuilder builder, string systemMessage) where TProject : Project<TMeta, TItem> where TMeta : new() where TItem : ProjectItem where TChange : IChange<TProject> where TRepository : class, IRepository<TProject, TMeta, TItem>
    {
        builder.Services.AddScoped<IRepository<TProject, TMeta, TItem>, TRepository>();
        builder.Services.AddScoped<ProjectService<TProject, TMeta, TItem, TChange>>();
        builder.ConfigureAndSnapshot<OpenAiSettings>("OpenAi");
        builder.Services.Configure<OpenAiSettings<TResponse>>(opt =>
        {
            opt.SystemMessage = """
                                You are an assistant to create travel itinerary change suggestions.
                                The user provides a prompt, and based on that prompt, you will suggest which changes you would make to the provided itinerary to accomodate those prompts.
                                The changes can be Creation, Removal, Reordering, and Rescheduling of activities.
                                When changing times, make sure other activities also have their start/end times changed accordingly.
                                Suggested changes include locations, which will need to be searchable in google maps.
                                If an activity should be changed beyond rescheduling, it has to be removed and added again as two separate actions.
                                Include the reasoning for the changes in a way that can be presented to the user.

                                Model details:
                                - All Id fields are unique numbers. When replacing an activity, the new activity should have a new Id
                                - *_Time are DateTimes, meaning they have the format "yyyy-MM-ddTHH:mm:ss" in the local time of that location. Example: "2025-04-14T15:23:56".
                                - *_Time_TzId MUST BE valid IANA timezone identifiers (TzId) for that location. The TzId is often tied to the capital of the country. Examples: "Europe/Paris", "Etc/UTC", "Asia/Singapore".
                                - *_Place_SearchQuery will be used to find the place in google maps, should contain a specific name suffixed by district, city, region, or country.
                                - *_Place_Description is a short human-readable description of the place.
                                """;
        });

        return builder;
    }
    
    public static WebApplicationBuilder AddItineraryManager(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        
        builder.Services.AddMudServices();

        var mongoDbSettings = builder.ConfigureAndSnapshot<MongoDbSettings>("MongoDb");
        builder.Services.AddMongoDB<ItineraryManagerDbContext>(mongoDbSettings.ConnectionString, mongoDbSettings.DatabaseName);
        builder.Services.AddHostedService<ItineraryManagerDbContextInitializationJob>();
        builder.ConfigureAndSnapshot<OpenAiSettings>("OpenAi");
        builder
            .AddProjectType<Itinerary, object?, Activity, ItineraryChange, ItineraryChangeRequestResponseModel,
                ItineraryRepository>("""
                                                           You are an assistant to create travel itinerary change suggestions.
                                                           The user provides a prompt, and based on that prompt, you will suggest which changes you would make to the provided itinerary to accomodate those prompts.
                                                           The changes can be Creation, Removal, Reordering, and Rescheduling of activities.
                                                           When changing times, make sure other activities also have their start/end times changed accordingly.
                                                           Suggested changes include locations, which will need to be searchable in google maps.
                                                           If an activity should be changed beyond rescheduling, it has to be removed and added again as two separate actions.
                                                           Include the reasoning for the changes in a way that can be presented to the user.

                                                           Model details:
                                                           - All Id fields are unique numbers. When replacing an activity, the new activity should have a new Id
                                                           - *_Time are DateTimes, meaning they have the format "yyyy-MM-ddTHH:mm:ss" in the local time of that location. Example: "2025-04-14T15:23:56".
                                                           - *_Time_TzId MUST BE valid IANA timezone identifiers (TzId) for that location. The TzId is often tied to the capital of the country. Examples: "Europe/Paris", "Etc/UTC", "Asia/Singapore".
                                                           - *_Place_SearchQuery will be used to find the place in google maps, should contain a specific name suffixed by district, city, region, or country.
                                                           - *_Place_Description is a short human-readable description of the place.
                                                           """);
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
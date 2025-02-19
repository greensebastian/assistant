using ItineraryManager.WebApp;

var builder = WebApplication.CreateBuilder(args);
builder.AddItineraryManager();

var app = builder.Build();
app.AddItineraryManager();

app.Run();

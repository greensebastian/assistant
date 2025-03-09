using Assistant.WebApp;

var builder = WebApplication.CreateBuilder(args);
builder.AddAssistant();

var app = builder.Build();
app.AddAssistant();

app.Run();

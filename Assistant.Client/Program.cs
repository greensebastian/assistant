using Assistant.Api.Interface;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Assistant.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("base", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddHttpClient<IAssistantApi, AssistantApiClient>((sp, client) =>
{
    var baseAddress = builder.Configuration["Services:AssistantApi:BaseAddress"];
    if (string.IsNullOrWhiteSpace(baseAddress))
        throw new ApplicationException("No address for AssistantApi in configuration");
    client.BaseAddress = new Uri(baseAddress);
});

await builder.Build().RunAsync();

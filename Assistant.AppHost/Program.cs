using Assistant.Api;

var builder = DistributedApplication.CreateBuilder(args);

var t = nameof(SomeFile);

builder.AddProject<Projects.>("api");

builder.Build().Run();
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Assistant_Api>("api");

builder.Build().Run();
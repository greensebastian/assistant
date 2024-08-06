var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Assistant_Api>("api");
builder.AddProject<Projects.Assistant_Client>("client");

builder.Build().Run();

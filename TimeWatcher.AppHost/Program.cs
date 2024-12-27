using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<TimeWatcher>("timewatcher");

builder.Build().Run();
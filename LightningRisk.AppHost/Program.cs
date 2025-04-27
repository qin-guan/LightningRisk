using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<LightningRisk_WebApi>("api");

builder.Build().Run();
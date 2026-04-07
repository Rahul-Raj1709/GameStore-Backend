// Allow the dashboard to run on HTTP for local development
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<Projects.GameStore_WebApi>("api")
       .WithReference(cache)
       .WithExternalHttpEndpoints();

builder.Build().Run();
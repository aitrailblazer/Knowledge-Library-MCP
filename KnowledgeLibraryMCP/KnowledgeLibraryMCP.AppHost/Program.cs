var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.KnowledgeLibraryMCP_ApiService>("apiservice");

builder.AddProject<Projects.KnowledgeLibraryMCP_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(dashboard => dashboard.PublishAsDockerComposeService((_, service) =>
    {
        service.Image = "mcr.microsoft.com/dotnet/aspire-dashboard:13.5";
        service.Ports = ["127.0.0.1:18888:18888"];
    }))
    .ConfigureComposeFile(file => file.Name = "noverina");

builder.AddProject<Projects.Noverina_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .PublishAsDockerFile(api => api.WithDockerfile("../..", "src/Noverina.Api/Dockerfile"))
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Image = "${API_IMAGE:-noverina-api:local}";
        service.Build = new Build { Context = "..", Dockerfile = "src/Noverina.Api/Dockerfile" };
        service.Ports = ["127.0.0.1:8080:8080"];
        service.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    });

builder.Build().Run();

using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(config =>
    {
        var built = config.Build();
        var keyVaultUri = built["Azure__KeyVaultUri"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            config.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["CosmosDb__ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(new CosmosClient(connectionString));
        }
    })
    .Build();

await host.RunAsync();

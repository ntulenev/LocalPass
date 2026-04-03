using Abstractions;

using Infrastructure;

using LocalPass.Utility;

using Logic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var cancellationTokenSource = new CancellationTokenSource();

var builder = Host.CreateDefaultBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        _ = hostContext;
        _ = services.AddSingleton<IApplication, Application>();
        _ = services.AddSingleton<IClock, DefaultClock>();
        _ = services.AddSingleton<ISecretVaultStore, FileSecretVaultStore>();
        _ = services.AddSingleton<IVaultAccessCoordinator, TerminalVaultAccessCoordinator>();
        _ = services.AddSingleton<ISecretVaultConsoleRenderer, LocalPassConsoleRenderer>();
        _ = services.AddSingleton<ILocalPassWorkflow, LocalPassWorkflow>();
    });

var host = builder.Build();
using var scope = host.Services.CreateScope();
var application = scope.ServiceProvider.GetRequiredService<IApplication>();
await application.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);

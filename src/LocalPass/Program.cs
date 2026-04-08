using Abstractions;

using Infrastructure;

using LocalPass.Application;

using LocalPass.Utility;

using Logic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var cancellationTokenSource = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    if (!cancellationTokenSource.IsCancellationRequested)
    {
        cancellationTokenSource.Cancel();
    }
};
Console.CancelKeyPress += cancelHandler;

try
{
    var builder = Host.CreateDefaultBuilder()
        .ConfigureServices((hostContext, services) =>
        {
            _ = services.AddSingleton<IApplication, LocalPassApplication>();
            _ = services.AddSingleton<IClock, DefaultClock>();
            _ = services.AddSingleton<FileSecretVaultStore>(serviceProvider =>
            {
                var clock = serviceProvider.GetRequiredService<IClock>();
                var storageDirectoryPath = StorageDirectoryConfiguration.ResolveStorageDirectoryPath(
                    hostContext.Configuration);
                return new FileSecretVaultStore(storageDirectoryPath, clock);
            });
            _ = services.AddSingleton<ISecretVaultStore>(serviceProvider =>
                serviceProvider.GetRequiredService<FileSecretVaultStore>());
            _ = services.AddSingleton<ISecretVaultStorageLocation>(serviceProvider =>
                serviceProvider.GetRequiredService<FileSecretVaultStore>());
            _ = services.AddSingleton<KeyboardLayoutProvider>();
            _ = services.AddSingleton<ISecretInputPrompter, ConsoleSecretInputPrompter>();
            _ = services.AddSingleton<IVaultAccessScreen, TerminalVaultAccessScreen>();
            _ = services.AddSingleton<ILocalPassConsoleSessionFactory, LocalPassConsoleSessionFactory>();
            _ = services.AddSingleton<IFolderOpener, SystemFolderOpener>();
            _ = services.AddSingleton<IVaultAccessCoordinator, TerminalVaultAccessCoordinator>();
            _ = services.AddSingleton<ISecretVaultConsoleRenderer, LocalPassConsoleRenderer>();
            _ = services.AddSingleton<ILocalPassWorkflow, LocalPassWorkflow>();
        });

    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var application = scope.ServiceProvider.GetRequiredService<IApplication>();
    await application.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

using k8s;
using k8sOperator.Tests.TestKube.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Net;

namespace k8sOperator.Tests.TestKube;

public sealed class TestKubeApiServer : IDisposable
{
    private readonly IHost _server;

    public TestKubeApiServer(Action<TestKubeApiBuilder>? configureApi = null)
    {
        _server = new HostBuilder()
            .ConfigureWebHost(config =>
            {
                config.ConfigureServices(services =>
                {
                    services.AddRouting();
                });
                config.UseKestrel(options => { options.Listen(IPAddress.Loopback, 0); });
                config.Configure(app =>
                {
                    // Mock Kube API routes
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        var builder = new TestKubeApiBuilder(endpoints);
                        configureApi?.Invoke(builder);
                        endpoints.Map("{*url}", (ILogger<TestKubeApiServer> logger, string url) =>
                        {
                            var safeUrl = url.Replace("\r", string.Empty).Replace("\n", string.Empty);

                            if (logger.IsEnabled(LogLevel.Information))
                                logger.LogInformation("route not handled: '{url}'", safeUrl);
                        });
                    });
                });
                config.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddTestLogging(TestContext.Current);
                });
            })
            .Build();

        _server.Start();

        Client = new Kubernetes(GetKubernetesClientConfiguration());
    }

    public Uri Uri => _server.Services.GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!.Addresses
            .Select(a => new Uri(a)).First();

    public KubernetesClientConfiguration GetKubernetesClientConfiguration()
        => new() { Host = Uri.ToString() };

    public IKubernetes Client { get; }

    public void Dispose()
    {
        try
        {
            _server?.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }
        catch { /* Ignore disposal errors */ }
        finally
        {
            _server?.Dispose();
        }
    }
}

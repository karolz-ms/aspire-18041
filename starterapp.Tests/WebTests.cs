using Microsoft.Extensions.Logging;

namespace starterapp.Tests;

public class WebTests
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.starterapp_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Trace);
            // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        var logger = app.Services.GetRequiredService<ILogger<WebTests>>();

        logger.LogInformation("Starting the application host...");
        await app.StartAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        logger.LogInformation("Waiting for the 'webfrontend' resource to become healthy...");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

        logger.LogInformation("Sending GET request to the root endpoint...");
        var response = await httpClient.GetAsync("/", cancellationToken).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

        // Assert
        logger.LogInformation("Received response with status code: {StatusCode}", response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

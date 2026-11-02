using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace WordBuddy.AppHost.Tests;

/// <summary>Builds the AppHost model without starting any container or process.</summary>
internal static class AppHostFixture
{
    /// <summary>Throwaway values so secret parameters resolve in tests. Not real secrets.</summary>
    private static readonly string[] BaseArgs =
    [
        "Parameters:sql-password=test-only-Sql1!",
        "Parameters:jwt-secret=test-only-jwt-secret-0123456789abcdef",
        "Parameters:rabbitmq-password=test-only-rabbit",
    ];

    /// <summary>Creates the testing builder, optionally with extra configuration arguments.</summary>
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(params string[] extraArgs)
        => await DistributedApplicationTestingBuilder.CreateAsync<Projects.WordBuddy_AppHost>(
            [.. BaseArgs, .. extraArgs]);

    /// <summary>Finds the <c>WordBuddy</c> backend folder by walking up from the test binaries.</summary>
    public static string FindBackendRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")) && Directory.Exists(Path.Combine(dir.FullName, "aspire")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the WordBuddy backend root.");
    }

    /// <summary>
    /// Collects the environment variable keys a resource would receive. Only the callbacks run; values
    /// stay unresolved (resolving needs allocated endpoints and would read secrets), so keys are all we assert.
    /// </summary>
    public static async Task<HashSet<string>> GetEnvironmentKeysAsync(IResource resource)
    {
        Dictionary<string, object> env = [];
        DistributedApplicationExecutionContext executionContext = new(DistributedApplicationOperation.Run);
        EnvironmentCallbackContext context = new(executionContext, env);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return [.. env.Keys];
    }
}

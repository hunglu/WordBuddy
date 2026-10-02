namespace WordBuddy.Progress.IntegrationTests;

/// <summary>Every API test class shares one <see cref="ProgressApiFactory"/> — its database name is
/// per-process, so two factories would race on migrating and dropping the same database.</summary>
[CollectionDefinition(Name)]
public sealed class ProgressApiCollection : ICollectionFixture<ProgressApiFactory>
{
    public const string Name = "ProgressApi";
}

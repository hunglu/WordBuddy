namespace WordBuddy.Progress.IntegrationTests;

/// <summary>Every API test class shares one <see cref="ProgressApiFactory"/> — its database name is
/// per-process, so two factories would race on migrating and dropping the same database. The only other
/// fixture, <see cref="ProgressGrpcApiFactory"/>, shares that database and never drops it.</summary>
[CollectionDefinition(Name)]
public sealed class ProgressApiCollection : ICollectionFixture<ProgressApiFactory>, ICollectionFixture<ProgressGrpcApiFactory>
{
    public const string Name = "ProgressApi";
}

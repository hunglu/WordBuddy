namespace WordBuddy.Content.IntegrationTests;

/// <summary>Every API test class shares one <see cref="ContentApiFactory"/> — its database name is
/// per-process, so two factories would race on seeding and dropping the same database.</summary>
[CollectionDefinition(Name)]
public sealed class ContentApiCollection : ICollectionFixture<ContentApiFactory>
{
    public const string Name = "ContentApi";
}

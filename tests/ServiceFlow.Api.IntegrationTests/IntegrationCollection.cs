namespace ServiceFlow.Api.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    public const string Name = "Integration";
}

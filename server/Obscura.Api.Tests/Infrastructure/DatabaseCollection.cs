namespace Obscura.Api.Tests.Infrastructure;

// Every database test class joins this collection: they share one DatabaseFixture
// and run one after another, never in parallel, because truncating would break a
// parallel test
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
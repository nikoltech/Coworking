using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Context;
using Coworking.External.Squidex.UnitTests.Helpers;
using NSubstitute;

namespace Coworking.External.Squidex.UnitTests.Context;

public sealed class SquidexContextTests
{
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();
    private readonly ISquidexPaginator _paginator = Substitute.For<ISquidexPaginator>();

    // Set<T>() only touches the default client + paginator, not the factory.
    private SquidexContext CreateContext() =>
        new(_client, _paginator, clientFactory: null!, appName: "test");

    [Fact]
    public async Task SetGeneric_ResolvesSchemaNameFromType()
    {
        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());

        var context = CreateContext();

        // No schema string — resolved from TestSchema.SchemaName ("test-schema").
        await context.Set<SquidexFakes.TestSchema>().QueryAsync(RequestQuery.Create());

        await _client.Received(1).QueryAsync<SquidexFakes.TestSchema>(
            "test-schema", Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }
}

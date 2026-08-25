using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Set;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Coworking.External.Squidex.UnitTests.Set;

public sealed class SquidexSetTests
{
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();
    private readonly ISquidexPaginator _paginator = Substitute.For<ISquidexPaginator>();

    public SquidexSetTests() =>
        _client.AppOptions.Returns(SquidexFakes.DefaultAppOptions());

    private SquidexSet<SquidexFakes.TestSchema> CreateRepo(string schema = "test-schema") =>
        new(_client, _paginator, schema);

    [Fact]
    public async Task GetAllAsync_DelegatesToPaginator()
    {
        // Arrange
        var expected = SquidexFakes.MakeResponse(
            SquidexFakes.MakeTestSchema("a"),
            SquidexFakes.MakeTestSchema("b"));

        _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
                "test-schema", _client, Arg.Any<RequestQuery>(), Arg.Any<int>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await CreateRepo().GetAllAsync();

        // Assert
        result.Should().Be(expected);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_DelegatesToClient_WithPublishFlag()
    {
        // Arrange
        var schema = SquidexFakes.MakeTestSchema("new");
        var expected = SquidexFakes.MakeContent(schema, "new-id");

        _client.CreateAsync("test-schema", schema, true, Arg.Any<CancellationToken>())
               .Returns(expected);

        // Act
        var result = await CreateRepo().CreateAsync(schema, publish: true);

        // Assert
        result.Id.Should().Be("new-id");
        await _client.Received(1).CreateAsync(
            "test-schema", schema, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_PassesExpectedVersion_ForOptimisticConcurrency()
    {
        var schema = SquidexFakes.MakeTestSchema("updated");

        _client.UpdateAsync("test-schema", "upd-id", schema, 7, Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeContent(schema, "upd-id"));

        await CreateRepo().UpdateAsync("upd-id", schema, expectedVersion: 7);

        await _client.Received(1).UpdateAsync(
            "test-schema", "upd-id", schema, 7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_PassesExpectedVersion_ForOptimisticConcurrency()
    {
        var schema = SquidexFakes.MakeTestSchema("patched");

        _client.PatchAsync("test-schema", "patch-id", schema, 7, Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeContent(schema, "patch-id"));

        await CreateRepo().PatchAsync("patch-id", schema, expectedVersion: 7);

        await _client.Received(1).PatchAsync(
            "test-schema", "patch-id", schema, 7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToClient_WithPermanentFlag()
    {
        // Act
        await CreateRepo().DeleteAsync("del-id", permanent: true);

        // Assert
        await _client.Received(1)
            .DeleteAsync("test-schema", "del-id", true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenAnItemComesBack()
    {
        // Arrange
        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(3, TestStatuses.Published,
                   SquidexFakes.MakeTestSchema("x")));

        // Act
        var exists = await CreateRepo().ExistsAsync(SquidexFilter.Eq("data.Name.iv", "x"));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenNothingComesBack()
    {
        // Arrange
        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse<SquidexFakes.TestSchema>(0, TestStatuses.Published));

        // Act
        var exists = await CreateRepo().ExistsAsync(SquidexFilter.Eq("data.Name.iv", "missing"));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenSquidexSkippedTheCount()
    {
        // Arrange — NoSlowTotal makes Squidex report -1; the item itself is the answer
        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(-1, TestStatuses.Published,
                   SquidexFakes.MakeTestSchema("x")));

        // Act
        var exists = await CreateRepo().ExistsAsync(SquidexFilter.Eq("data.Name.iv", "x"));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_SetsNoSlowTotal_ForPerformance()
    {
        // Arrange
        QueryOptions? capturedOptions = null;

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Do<QueryOptions?>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse<SquidexFakes.TestSchema>(0, TestStatuses.Published));

        // Act
        await CreateRepo().ExistsAsync(SquidexFilter.Eq("data.Name.iv", "x"));

        // Assert
        capturedOptions!.NoSlowTotal.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_SetsTakeToOne_ForPerformance()
    {
        // Arrange
        RequestQuery? capturedQuery = null;

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema",
                Arg.Do<RequestQuery>(q => capturedQuery = q),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse<SquidexFakes.TestSchema>(0, TestStatuses.Published));

        // Act
        await CreateRepo().ExistsAsync(SquidexFilter.Eq("data.Name.iv", "x"));

        // Assert
        capturedQuery!.Take.Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_PassesIncludeUnpublished_ToQueryOptions()
    {
        // Arrange
        QueryOptions? capturedOptions = null;

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Do<QueryOptions?>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse<SquidexFakes.TestSchema>(0, TestStatuses.Draft));

        // Act
        await CreateRepo().ExistsAsync(
            SquidexFilter.Eq("data.Name.iv", "draft"), includeUnpublished: true);

        // Assert
        capturedOptions!.IncludeUnpublished.Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_UsesTheConfiguredSchema()
    {
        // Arrange — different schema name
        var repo = CreateRepo("custom-schema");

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "custom-schema", Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakeResponse<SquidexFakes.TestSchema>());

        // Act
        await repo.QueryAsync(RequestQuery.Create());

        // Assert
        await _client.Received(1).QueryAsync<SquidexFakes.TestSchema>(
            "custom-schema", Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }
}
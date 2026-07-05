// Set/SquidexSetTests.cs
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
    public async Task QueryAsync_DelegatesToClient_WithSameArguments()
    {
        // Arrange
        var expected = SquidexFakes.MakeResponse<SquidexFakes.TestSchema>();
        var query = RequestQuery.Create().WithTake(5);
        var opts = new QueryOptions { IncludeUnpublished = true };

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", query, opts, Arg.Any<CancellationToken>())
               .Returns(expected);

        // Act
        var result = await CreateRepo().QueryAsync(query, opts);

        // Assert
        result.Should().Be(expected);
    }

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
    public async Task GetByIdAsync_DelegatesToClient()
    {
        // Arrange
        var expected = SquidexFakes.MakeContent(
            SquidexFakes.MakeTestSchema("city"), "city-id");

        _client.GetByIdAsync<SquidexFakes.TestSchema>(
                "test-schema", "city-id", Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(expected);

        // Act
        var result = await CreateRepo().GetByIdAsync("city-id");

        // Assert
        result.Should().Be(expected);
        result!.Data.Name!.Value.Should().Be("city");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _client.GetByIdAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns((ContentDto<SquidexFakes.TestSchema>?)null);

        // Act
        var result = await CreateRepo().GetByIdAsync("missing-id");

        // Assert
        result.Should().BeNull();
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
    public async Task UpdateAsync_DelegatesToClient()
    {
        // Arrange
        var schema = SquidexFakes.MakeTestSchema("updated");
        var expected = SquidexFakes.MakeContent(schema, "upd-id");

        _client.UpdateAsync("test-schema", "upd-id", schema, Arg.Any<int?>(), Arg.Any<CancellationToken>())
               .Returns(expected);

        // Act
        var result = await CreateRepo().UpdateAsync("upd-id", schema);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task PatchAsync_DelegatesToClient()
    {
        // Arrange
        var schema = SquidexFakes.MakeTestSchema("patched");
        var expected = SquidexFakes.MakeContent(schema, "patch-id");

        _client.PatchAsync("test-schema", "patch-id", schema, Arg.Any<int?>(), Arg.Any<CancellationToken>())
               .Returns(expected);

        // Act
        var result = await CreateRepo().PatchAsync("patch-id", schema);

        // Assert
        result.Should().Be(expected);
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
    public async Task ExistsAsync_ReturnsTrue_WhenTotalGreaterThanZero()
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
    public async Task ExistsAsync_ReturnsFalse_WhenTotalIsZero()
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
    public async Task Repository_UsesCorrectSchema_ForAllOperations()
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
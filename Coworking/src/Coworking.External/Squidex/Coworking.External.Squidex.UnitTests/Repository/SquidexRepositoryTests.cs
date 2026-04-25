using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Pagination;
using Coworking.External.Squidex.Repository;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Coworking.External.Squidex.UnitTests.Repository;

public sealed class SquidexRepositoryTests
{
    private readonly SquidexApiClient _client = Substitute.For<SquidexApiClient>();
    private readonly SquidexPaginator _paginator = Substitute.For<SquidexPaginator>();

    private SquidexRepository<SquidexFakes.TestSchema> CreateRepo() =>
        new(_client, _paginator, "test-schema");

    [Fact]
    public async Task QueryAsync_DelegatesToClient()
    {
        // Arrange
        var expected = SquidexFakes.MakeResponse<SquidexFakes.TestSchema>();
        var query = RequestQuery.Create().WithTake(5);

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", query, Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(expected);

        var repo = CreateRepo();

        // Act
        var result = await repo.QueryAsync(query);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToPaginator()
    {
        // Arrange
        var expected = SquidexFakes.MakeResponse<SquidexFakes.TestSchema>();

        _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
                "test-schema", _client, Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var repo = CreateRepo();

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToClient()
    {
        // Arrange
        var expected = SquidexFakes.MakeContent(new SquidexFakes.TestSchema(), "id-1");

        _client.GetByIdAsync<SquidexFakes.TestSchema>(
                "test-schema", "id-1", Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(expected);

        var repo = CreateRepo();

        // Act
        var result = await repo.GetByIdAsync("id-1");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenTotalGreaterThanZero()
    {
        // Arrange
        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(1,
                   new SquidexFakes.TestSchema()));

        var repo = CreateRepo();

        // Act
        var exists = await repo.ExistsAsync(SquidexFilter.Eq("data.Name.iv", "test"));

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
               .Returns(SquidexFakes.MakePagedResponse(0));

        var repo = CreateRepo();

        // Act
        var exists = await repo.ExistsAsync(SquidexFilter.Eq("data.Name.iv", "missing"));

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_UsesNoSlowTotal_ForPerformance()
    {
        // Arrange
        QueryOptions? capturedOptions = null;

        _client.QueryAsync<SquidexFakes.TestSchema>(
                "test-schema", Arg.Any<RequestQuery>(),
                Arg.Do<QueryOptions?>(o => capturedOptions = o),
                Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(0));

        var repo = CreateRepo();

        // Act
        await repo.ExistsAsync(SquidexFilter.Eq("data.Name.iv", "x"));

        // Assert
        capturedOptions!.NoSlowTotal.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToClient()
    {
        // Arrange
        var repo = CreateRepo();

        // Act
        await repo.DeleteAsync("del-id");

        // Assert
        await _client.Received(1)
            .DeleteAsync("test-schema", "del-id", false, Arg.Any<CancellationToken>());
    }
}
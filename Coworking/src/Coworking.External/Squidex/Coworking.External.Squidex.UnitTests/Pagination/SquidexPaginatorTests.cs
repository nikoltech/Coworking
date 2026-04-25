// Pagination/SquidexPaginatorTests.cs
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Pagination;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Coworking.External.Squidex.UnitTests.Pagination;

public sealed class SquidexPaginatorTests
{
    // MaxPageSize = 3 from SquidexFakes.DefaultOptions
    private readonly SquidexPaginator _paginator = new(SquidexFakes.OptionsMock());
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();

    [Fact]
    public async Task FetchAllAsync_ReturnsSinglePage_WhenTotalFitsInOnePage()
    {
        // Arrange — 2 items, pageSize 3 → no extra pages needed
        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(2,
                   TestStatuses.Published,
                   SquidexFakes.MakeTestSchema("a"),
                   SquidexFakes.MakeTestSchema("b")));

        // Act
        var result = await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, RequestQuery.Create());

        // Assert
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);

        await _client.Received(1).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_FetchesRemainingPages_WhenTotalExceedsPageSize()
    {
        // Arrange — total 7, pageSize 3 → pages: [3, 3, 1] = 3 calls
        var callCount = 0;

        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(_ =>
               {
                   callCount++;
                   return callCount == 1
                       ? SquidexFakes.MakePagedResponse(7, TestStatuses.Published,
                           SquidexFakes.MakeTestSchema("1"),
                           SquidexFakes.MakeTestSchema("2"),
                           SquidexFakes.MakeTestSchema("3"))
                       : SquidexFakes.MakePagedResponse(7, TestStatuses.Published,
                           SquidexFakes.MakeTestSchema("x"));
               });

        // Act
        var result = await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, RequestQuery.Create());

        // Assert
        result.Total.Should().Be(7);
        result.Items.Should().HaveCount(5); // 3 + 1 + 1

        await _client.Received(3).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_PreservesFilterAndSort_OnAllPages()
    {
        // Arrange
        var captured = new List<RequestQuery>();

        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(),
                Arg.Do<RequestQuery>(q => captured.Add(q)),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(4, TestStatuses.Published,
                   SquidexFakes.MakeTestSchema("1"),
                   SquidexFakes.MakeTestSchema("2"),
                   SquidexFakes.MakeTestSchema("3")));

        var baseQuery = RequestQuery.Create()
            .WithFilter(SquidexFilter.Eq(SquidexPaths.Iv("Active"), true))
            .WithSort([SortOption.Asc(SquidexPaths.Iv("Name"))]);

        // Act
        await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, baseQuery);

        // Assert — filter and sort preserved on all pages
        captured.Should().AllSatisfy(q =>
        {
            q.Filter.Should().NotBeNull();
            q.Sort.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task FetchAllAsync_SetsCorrectSkip_ForEachPage()
    {
        // Arrange — total 7, pageSize 3 → skips: 0, 3, 6
        var capturedSkips = new List<int>();

        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(),
                Arg.Do<RequestQuery>(q => capturedSkips.Add(q.Skip)),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(_ => SquidexFakes.MakePagedResponse(7, TestStatuses.Published,
                   SquidexFakes.MakeTestSchema("x")));

        // Act
        await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, RequestQuery.Create());

        // Assert
        capturedSkips.Should().BeEquivalentTo([0, 3, 6]);
    }

    [Fact]
    public async Task FetchAllAsync_PassesQueryOptions_ToAllPages()
    {
        // Arrange
        var capturedOptions = new List<QueryOptions?>();

        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Do<QueryOptions?>(o => capturedOptions.Add(o)),
                Arg.Any<CancellationToken>())
               .Returns(SquidexFakes.MakePagedResponse(4, TestStatuses.Draft,
                   SquidexFakes.MakeTestSchema("1"),
                   SquidexFakes.MakeTestSchema("2"),
                   SquidexFakes.MakeTestSchema("3")));

        var opts = new QueryOptions { IncludeUnpublished = true };

        // Act
        await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, RequestQuery.Create(), opts);

        // Assert — all pages get same options
        capturedOptions.Should().AllSatisfy(o =>
            o!.IncludeUnpublished.Should().BeTrue());
    }
}
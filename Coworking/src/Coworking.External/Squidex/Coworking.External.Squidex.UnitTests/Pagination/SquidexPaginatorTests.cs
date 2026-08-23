using System.Collections.Concurrent;
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Options;
using Coworking.External.Squidex.Pagination;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Coworking.External.Squidex.UnitTests.Pagination;

public sealed class SquidexPaginatorTests
{
    private readonly SquidexPaginator _paginator = new();
    private const int PageSize = 3; // matches SquidexFakes.DefaultAppOptions().MaxPageSize
    private readonly ISquidexApiClient _client = Substitute.For<ISquidexApiClient>();

    public SquidexPaginatorTests() =>
        _client.AppOptions.Returns(SquidexFakes.DefaultAppOptions());

    [Fact]
    public async Task FetchAllAsync_ReturnsSinglePage_WhenOnePageHoldsEverything()
    {
        // Arrange — 2 items, pageSize 3 → the first page is already short
        ServePages(itemCount: 2, reportedTotal: 2);

        // Act
        var result = await FetchAllAsync();

        // Assert
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);

        await _client.Received(1).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_FetchesRemainingPages_WhenMoreThanOnePage()
    {
        // Arrange — 7 items, pageSize 3 → pages: [3, 3, 1] = 3 calls
        ServePages(itemCount: 7, reportedTotal: 7);

        // Act
        var result = await FetchAllAsync();

        // Assert
        result.Total.Should().Be(7);
        result.Items.Should().HaveCount(7);

        await _client.Received(3).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_PagesWithoutATotal_WhenSquidexSkippedTheCount()
    {
        // Arrange — NoSlowTotal makes Squidex report -1; only the short page ends the walk
        ServePages(itemCount: 7, reportedTotal: -1);

        // Act
        var result = await FetchAllAsync();

        // Assert
        result.Total.Should().Be(7);
        result.Items.Should().HaveCount(7);
    }

    [Fact]
    public async Task FetchAllAsync_AsksOneMorePage_WhenItemsAreAnExactMultipleOfPageSize()
    {
        // Arrange — 6 items over pageSize 3: the last full page cannot be the last one
        ServePages(itemCount: 6, reportedTotal: 6);

        // Act
        var result = await FetchAllAsync();

        // Assert
        result.Total.Should().Be(6);
        result.Items.Should().HaveCount(6);

        await _client.Received(3).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_KeepsRequestsWithinMaxParallelRequests()
    {
        // Arrange — 30 items over pageSize 3 leaves 9 pages to fetch after the first
        _client.AppOptions.Returns(SquidexFakes.DefaultAppOptions() with
        {
            Limits = new SquidexLimitsOptions { MaxParallelRequests = 2 }
        });

        var gate = new object();
        var inFlight = 0;
        var peak = 0;

        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(async call =>
               {
                   lock (gate) peak = Math.Max(peak, ++inFlight);

                   await Task.Delay(20);

                   lock (gate) inFlight--;

                   return Page(call.Arg<RequestQuery>(), itemCount: 30, reportedTotal: 30);
               });

        // Act
        var result = await FetchAllAsync();

        // Assert
        result.Items.Should().HaveCount(30);
        peak.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task FetchAllAsync_PreservesFilterAndSort_OnAllPages()
    {
        // Arrange
        var captured = new ConcurrentBag<RequestQuery>();

        ServePages(itemCount: 7, reportedTotal: 7, onQuery: captured.Add);

        var baseQuery = RequestQuery.Create()
            .WithFilter(SquidexFilter.Eq(SquidexPaths.Iv("Active"), true))
            .WithSort([SortOption.Asc(SquidexPaths.Iv("Name"))]);

        // Act
        await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, baseQuery, PageSize);

        // Assert — filter and sort preserved on all pages
        captured.Should().HaveCountGreaterThan(1);
        captured.Should().AllSatisfy(q =>
        {
            q.Filter.Should().NotBeNull();
            q.Sort.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task FetchAllAsync_SetsCorrectSkip_ForEachPage()
    {
        // Arrange — 7 items, pageSize 3 → skips: 0, 3, 6
        var captured = new ConcurrentBag<RequestQuery>();

        ServePages(itemCount: 7, reportedTotal: 7, onQuery: captured.Add);

        // Act
        await FetchAllAsync();

        // Assert
        captured.Select(q => q.Skip).Should().BeEquivalentTo([0, 3, 6]);
    }

    [Fact]
    public async Task FetchAllAsync_PassesQueryOptions_ToAllPages()
    {
        // Arrange
        var capturedOptions = new ConcurrentBag<QueryOptions?>();

        ServePages(itemCount: 7, reportedTotal: 7, onOptions: capturedOptions.Add);

        var opts = new QueryOptions { IncludeUnpublished = true };

        // Act
        await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, RequestQuery.Create(), PageSize, opts);

        // Assert — all pages get same options
        capturedOptions.Should().HaveCountGreaterThan(1);
        capturedOptions.Should().AllSatisfy(o =>
            o!.IncludeUnpublished.Should().BeTrue());
    }

    // helpers

    private Task<ResponseSchema<SquidexFakes.TestSchema>> FetchAllAsync() =>
        _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", _client, RequestQuery.Create(), PageSize);

    /// <summary>
    /// Answers like a real server: each page holds what actually remains after its skip.
    /// A fake that always returns a full page would never let the walk end.
    /// </summary>
    /// <remarks>Pages are fetched in parallel, so callbacks here run on several threads.</remarks>
    private void ServePages(int itemCount, long reportedTotal,
        Action<RequestQuery>? onQuery = null,
        Action<QueryOptions?>? onOptions = null) =>
        _client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
               .Returns(call =>
               {
                   var query = call.Arg<RequestQuery>();

                   onQuery?.Invoke(query);
                   onOptions?.Invoke(call.Arg<QueryOptions?>());

                   return Page(query, itemCount, reportedTotal);
               });

    private static ResponseSchema<SquidexFakes.TestSchema> Page(
        RequestQuery query, int itemCount, long reportedTotal)
    {
        var remaining = Math.Clamp(itemCount - query.Skip, 0, PageSize);

        var items = Enumerable.Range(query.Skip, remaining)
            .Select(i => SquidexFakes.MakeTestSchema($"item-{i}"))
            .ToArray();

        return SquidexFakes.MakePagedResponse(reportedTotal, TestStatuses.Published, items);
    }
}

using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Pagination;
using Coworking.External.Squidex.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Coworking.External.Squidex.UnitTests.Pagination;

public sealed class SquidexPaginatorTests
{
    // MaxPageSize = 3 in SquidexFakes.DefaultOptions
    private readonly SquidexPaginator _paginator =
        new(SquidexFakes.DefaultOptionsMock());

    [Fact]
    public async Task FetchAllAsync_ReturnsSinglePage_WhenTotalFitsInOnePage()
    {
        // Arrange
        var client = Substitute.For<SquidexApiClient>();

        client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
              .Returns(SquidexFakes.MakePagedResponse(2,
                  new SquidexFakes.TestSchema(Name: new IvField<string>("a")),
                  new SquidexFakes.TestSchema(Name: new IvField<string>("b"))));

        // Act
        var result = await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", client, RequestQuery.Create());

        // Assert
        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);

        await client.Received(1).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_FetchesRemainingPages_WhenTotalExceedsPageSize()
    {
        // Arrange — total 7 items, pageSize 3 → pages: [3, 3, 1]
        var client = Substitute.For<SquidexApiClient>();
        var callCount = 0;

        client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Any<RequestQuery>(),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
              .Returns(_ =>
              {
                  callCount++;
                  return callCount == 1
                      ? SquidexFakes.MakePagedResponse(7,
                          new SquidexFakes.TestSchema(Name: new IvField<string>("1")),
                          new SquidexFakes.TestSchema(Name: new IvField<string>("2")),
                          new SquidexFakes.TestSchema(Name: new IvField<string>("3")))
                      : SquidexFakes.MakePagedResponse(7,
                          new SquidexFakes.TestSchema(Name: new IvField<string>("x")));
              });

        // Act
        var result = await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", client, RequestQuery.Create());

        // Assert
        result.Total.Should().Be(7);
        result.Items.Should().HaveCount(5); // 3 + 1 + 1 (two remaining pages)

        // First page + 2 remaining pages = 3 total calls
        await client.Received(3).QueryAsync<SquidexFakes.TestSchema>(
            Arg.Any<string>(), Arg.Any<RequestQuery>(),
            Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAllAsync_PreservesFilterAndSort_OnRemainingPages()
    {
        // Arrange
        var client = Substitute.For<SquidexApiClient>();
        var captured = new List<RequestQuery>();

        client.QueryAsync<SquidexFakes.TestSchema>(
                Arg.Any<string>(), Arg.Do<RequestQuery>(q => captured.Add(q)),
                Arg.Any<QueryOptions?>(), Arg.Any<CancellationToken>())
              .Returns(SquidexFakes.MakePagedResponse(4,
                  new SquidexFakes.TestSchema(),
                  new SquidexFakes.TestSchema(),
                  new SquidexFakes.TestSchema()));

        var baseQuery = RequestQuery.Create()
            .WithFilter(SquidexFilter.Eq("data.Active.iv", true))
            .WithSort([SortOption.Asc("data.Name.iv")]);

        // Act
        await _paginator.FetchAllAsync<SquidexFakes.TestSchema>(
            "cities", client, baseQuery);

        // Assert — all pages inherit filter and sort
        captured.Should().AllSatisfy(q =>
        {
            q.Filter.Should().NotBeNull();
            q.Sort.Should().NotBeNull();
        });

        // Pages have correct skip values
        captured[0].Skip.Should().Be(0);
        captured[1].Skip.Should().Be(3);
    }
}
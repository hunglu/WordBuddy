using FluentAssertions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.KernelPaging;

public class PagingSliceTests
{
    private static readonly int[] Source = [1, 2, 3, 4, 5, 6, 7];

    [Fact]
    public void Paging_Slice_FirstPage_ReturnsFirstItems()
    {
        PagedResult<int> page = Paging.Slice(Source, 1, 3);

        page.Items.Should().Equal(1, 2, 3);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(3);
        page.TotalCount.Should().Be(7);
    }

    [Fact]
    public void Paging_Slice_MiddlePage_ReturnsMiddleItems()
    {
        Paging.Slice(Source, 2, 3).Items.Should().Equal(4, 5, 6);
    }

    [Fact]
    public void Paging_Slice_LastPartialPage_ReturnsRemainder()
    {
        PagedResult<int> page = Paging.Slice(Source, 3, 3);

        page.Items.Should().Equal(7);
        page.TotalCount.Should().Be(7);
    }

    [Fact]
    public void Paging_Slice_PastTheEnd_ReturnsNoItemsAndRealTotal()
    {
        PagedResult<int> page = Paging.Slice(Source, 9, 3);

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(7);
        page.Page.Should().Be(9);
    }

    [Fact]
    public void Paging_Slice_EmptySource_ReturnsEmptyPage()
    {
        PagedResult<int> page = Paging.Slice(Array.Empty<int>(), 1, 25);

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }
}

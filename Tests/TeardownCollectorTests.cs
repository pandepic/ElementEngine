using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class TeardownCollectorTests
{
    [Fact]
    public void EveryTeardownPhaseRunsAfterEarlierFailures()
    {
        var phases = new List<int>();
        var teardown = new TeardownCollector();

        teardown.Run(() =>
        {
            phases.Add(1);
            throw new InvalidOperationException("first");
        });
        teardown.Run(() => phases.Add(2));
        teardown.Run(() =>
        {
            phases.Add(3);
            throw new NotSupportedException("third");
        });
        teardown.Run(() => phases.Add(4));

        var exception = Assert.Throws<AggregateException>(() => teardown.ThrowIfFailed("failed"));

        Assert.Equal([1, 2, 3, 4], phases);
        Assert.Collection(
            exception.InnerExceptions,
            first => Assert.Equal("first", first.Message),
            second => Assert.Equal("third", second.Message));
    }

    [Fact]
    public void ASingleFailureIsRethrownWithoutWrapping()
    {
        var expected = new InvalidOperationException("original");
        var teardown = new TeardownCollector(expected);

        var actual = Assert.Throws<InvalidOperationException>(() => teardown.ThrowIfFailed("failed"));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void NestedCleanupAggregatesAreFlattenedInFailureOrder()
    {
        var original = new InvalidOperationException("original");
        var teardown = new TeardownCollector(original);
        teardown.Run(() => throw new AggregateException(
            new NotSupportedException("cleanup one"),
            new ArgumentException("cleanup two")));

        var exception = Assert.Throws<AggregateException>(() => teardown.ThrowIfFailed("failed"));

        Assert.Collection(
            exception.InnerExceptions,
            first => Assert.Same(original, first),
            second => Assert.Equal("cleanup one", second.Message),
            third => Assert.Equal("cleanup two", third.Message));
    }
}

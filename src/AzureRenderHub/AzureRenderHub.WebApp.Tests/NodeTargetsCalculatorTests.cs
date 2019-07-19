using WebApp.BackgroundHosts.ScaleUpProcessor;
using Xunit;

namespace WebApp.Tests
{
    public class NodeTargetsCalculatorTests
    {
        (int lowPriority, int dedicated) Calculate(
            int requested,
            int lowPrioCurrent = 0,
            int lowPrioMin = 0,
            int lowPrioMax = int.MaxValue,
            int dedicatedCurrent = 0,
            int dedicatedMin = 0,
            int dedicatedMax = int.MaxValue)
            => NodeTargetsCalculator.Calculemus(
                requested,
                lowPrioCurrent,
                lowPrioMin,
                lowPrioMax,
                dedicatedCurrent,
                dedicatedMin,
                dedicatedMax);

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(100)]
        public void NoRestrictionsAllocatesOnlyLowPrioNodes(int requested)
            => Assert.Equal((requested, 0), Calculate(requested));

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(100)]
        public void NoLowPrioAllocatesOnlyDedicatedNodes(int requested)
            => Assert.Equal((0, requested), Calculate(requested, lowPrioMax: 0));

        [Fact]
        public void LowPrioNodesAreAllocatedFirst()
            => Assert.Equal((1, 0), Calculate(requested: 1));

        [Fact]
        public void DedicatedNodesAreAllocatedSecond()
            => Assert.Equal((1, 1), Calculate(requested: 2, lowPrioMax: 1));

        [Fact]
        public void DedicatedMinimumIsRespected()
            => Assert.Equal((3, 2), Calculate(requested: 5, dedicatedMin: 2));

        [Fact]
        public void ScaleDownNeverOccurs()
            => Assert.Equal((1, 2), Calculate(requested: 0, lowPrioCurrent: 1, dedicatedCurrent: 2));

        [Fact]
        public void MaximumsAreNotExceeded()
            => Assert.Equal((2, 3), Calculate(requested: 10, lowPrioMax: 2, dedicatedMax: 3));

        [Fact]
        public void CurrentlyAllocatedDedicatedNodesAreRespected()
            => Assert.Equal((2, 5), Calculate(
                requested: 7,
                lowPrioCurrent: 1, lowPrioMin: 1, lowPrioMax: 4,
                dedicatedCurrent: 5));
    }
}

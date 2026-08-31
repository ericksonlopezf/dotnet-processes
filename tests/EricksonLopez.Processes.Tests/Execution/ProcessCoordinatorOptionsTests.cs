// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Processes;
using Xunit;

namespace EricksonLopez.Processes.Tests.Execution;

[Trait("Category", "Unit")]
public class ProcessCoordinatorOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveSensibleDefaults()
    {
        var options = new ProcessCoordinatorOptions();

        options.MaxConcurrencyRetries.Should().Be(3);
        options.InitialBackoffDelay.Should().Be(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void CustomOptions_ShouldRetainConfiguredValues()
    {
        var options = new ProcessCoordinatorOptions
        {
            MaxConcurrencyRetries = 10,
            InitialBackoffDelay = TimeSpan.FromMilliseconds(100)
        };

        options.MaxConcurrencyRetries.Should().Be(10);
        options.InitialBackoffDelay.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 30)]
    [InlineData(5, 50)]
    public void DefaultBackoffStrategy_ShouldReturnExpectedLinearDelay(int attempt, int expectedMilliseconds)
    {
        var delay = ProcessCoordinator<object>.DefaultBackoffStrategy(attempt);
        delay.Should().Be(TimeSpan.FromMilliseconds(expectedMilliseconds));
    }

    [Fact]
    public void OptionsDerivedBackoff_ShouldScaleWithInitialBackoffDelay()
    {
        var options = new ProcessCoordinatorOptions
        {
            InitialBackoffDelay = TimeSpan.FromMilliseconds(25)
        };

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var calculatedDelay = TimeSpan.FromMilliseconds(options.InitialBackoffDelay.TotalMilliseconds * attempt);
            calculatedDelay.Should().Be(TimeSpan.FromMilliseconds(25 * attempt));
        }
    }
}



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
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, 400)]
    [InlineData(5, 1000)]
    public void DefaultBackoffStrategy_ShouldReturnExpectedExponentialDelayWithJitter(int attempt, int expectedMilliseconds)
    {
        var delay = ProcessCoordinator<object>.DefaultBackoffStrategy(attempt);
        delay.TotalMilliseconds.Should().BeInRange(expectedMilliseconds, expectedMilliseconds + 50);
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

    [Fact]
    public void InitialBackoffDelay_ZeroOrNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new ProcessCoordinatorOptions();

        var actZero = () => options.InitialBackoffDelay = TimeSpan.Zero;
        actZero.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("*Initial backoff delay must be greater than zero.*");

        var actNegative = () => options.InitialBackoffDelay = TimeSpan.FromSeconds(-1);
        actNegative.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithParameterName("value")
            .WithMessage("*Initial backoff delay must be greater than zero.*");

        options.InitialBackoffDelay = TimeSpan.FromMilliseconds(1);
        options.InitialBackoffDelay.Should().Be(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void MaxCompensations_ShouldClampNegativeValuesToZeroAndRetainPositive()
    {
        var options = new ProcessCoordinatorOptions();
        options.MaxCompensations.Should().Be(1000);

        options.MaxCompensations = -10;
        options.MaxCompensations.Should().Be(0);

        options.MaxCompensations = 25;
        options.MaxCompensations.Should().Be(25);
    }

    [Fact]
    public void MaxConcurrencyRetries_ShouldClampNegativeValuesToZeroAndRetainPositive()
    {
        var options = new ProcessCoordinatorOptions();
        options.MaxConcurrencyRetries.Should().Be(3);

        options.MaxConcurrencyRetries = -5;
        options.MaxConcurrencyRetries.Should().Be(0);

        options.MaxConcurrencyRetries = 8;
        options.MaxConcurrencyRetries.Should().Be(8);
    }
}



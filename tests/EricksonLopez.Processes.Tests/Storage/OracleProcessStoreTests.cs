// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Processes.Abstractions;
using EricksonLopez.Processes.Storage.Oracle;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Processes.Tests.Storage;

[Trait("Category", "Unit")]
public class OracleProcessStoreTests
{
    private readonly IProcessStateSerializer<object> _serializer = Substitute.For<IProcessStateSerializer<object>>();
    private const string ValidConnectionString = "Data Source=localhost:1521/FREE;User Id=system;Password=oracle;";

    [Fact]
    public void Constructor_WhenConnectionStringNull_ThrowsArgumentException()
    {
        var act = () => new OracleProcessStore<object>(null!, _serializer);
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("connectionString");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenConnectionStringEmptyOrWhitespace_ThrowsArgumentException(string invalidConnectionString)
    {
        var act = () => new OracleProcessStore<object>(invalidConnectionString, _serializer);
        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WhenTableNameNull_ThrowsArgumentException()
    {
        var act = () => new OracleProcessStore<object>(ValidConnectionString, _serializer, tableName: null!);
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("tableName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenTableNameEmptyOrWhitespace_ThrowsArgumentException(string invalidTableName)
    {
        var act = () => new OracleProcessStore<object>(ValidConnectionString, _serializer, tableName: invalidTableName);
        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("tableName");
    }

    [Fact]
    public void Constructor_WhenSerializerNull_ThrowsArgumentNullException()
    {
        var act = () => new OracleProcessStore<object>(ValidConnectionString, null!);
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("serializer");
    }

    [Fact]
    public async Task SaveAsync_WhenInstanceNull_ThrowsArgumentNullException()
    {
        var store = new OracleProcessStore<object>(ValidConnectionString, _serializer);
        Func<Task> act = async () => await store.SaveAsync(null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>()
            .WithParameterName("instance");
    }

    [Fact]
    public void AddOracleProcessStore_WhenServicesNull_ThrowsArgumentNullException()
    {
        Action act = () => ProcessOracleServiceCollectionExtensions.AddOracleProcessStore<object>(null!, ValidConnectionString);
        act.Should().ThrowExactly<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddOracleProcessStore_WhenConnectionStringInvalid_ThrowsArgumentException(string? invalidConnectionString)
    {
        var services = new ServiceCollection();
        Action act = () => services.AddOracleProcessStore<object>(invalidConnectionString!);
        if (invalidConnectionString is null)
        {
            act.Should().ThrowExactly<ArgumentNullException>()
                .WithParameterName("connectionString");
        }
        else
        {
            act.Should().ThrowExactly<ArgumentException>()
                .WithParameterName("connectionString");
        }
    }

    [Fact]
    public void AddOracleProcessStore_RegistersServiceInServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_serializer);
        services.AddOracleProcessStore<object>(ValidConnectionString, "CUSTOM_PROCESS_INSTANCES");

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessStore<object>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IProcessStore<object>>();

        store.Should().NotBeNull();
        store.Should().BeOfType<OracleProcessStore<object>>();
    }
}

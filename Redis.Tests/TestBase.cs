using NUnit.Framework;
using Testcontainers.Redis;

namespace Redis.Tests;

public abstract class TestBase
{
    private RedisContainer _redisTestContainer;
    protected string RedisTestHost;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _redisTestContainer = new RedisBuilder()
            .WithName("redis-test-container")
            .WithImage("redis:7.0.12")
            .Build();

        await _redisTestContainer.StartAsync();

        RedisTestHost = _redisTestContainer.GetConnectionString();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _redisTestContainer.StopAsync();
    }
}
using System.Diagnostics;
using StackExchange.Redis;

public class RedisClusterService
{
    private readonly Dictionary<RedisDataType, ConnectionMultiplexer> _clusters;

    public RedisClusterService(Dictionary<string, ConfigurationOptions> configurations)
    {
        _clusters = new Dictionary<RedisDataType, ConnectionMultiplexer>();
        InitializeClusters(configurations).Wait();
    }

    private async Task InitializeClusters(Dictionary<string, ConfigurationOptions> configurations)
    {
        _clusters[RedisDataType.Organization] = await ConnectToCluster(configurations["organization"]);
        _clusters[RedisDataType.Password] = await ConnectToCluster(configurations["passwords"]);
        _clusters[RedisDataType.Product] = await ConnectToCluster(configurations["products"]);
    }

    private static async Task<ConnectionMultiplexer> ConnectToCluster(ConfigurationOptions configurationOptions)
    {
        return await ConnectionMultiplexer.ConnectAsync(configurationOptions);
    }

    public async Task PerformReadWriteTest(RedisMessage message)
    {
        var cluster = _clusters[message.DataType];
        var db = cluster.GetDatabase();
        var stopwatch = new Stopwatch();

        stopwatch.Start();
        await db.StringSetAsync(message.Key, message.Value);
        stopwatch.Stop();
        Console.WriteLine("\n-------------------------------------------------\n");
        Console.WriteLine($"Write time for {message.Key}: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var retrievedValue = await db.StringGetAsync(message.Key);
        stopwatch.Stop();
        Console.WriteLine($"Retrieved value: {retrievedValue}");
        Console.WriteLine($"Read time for {message.Key}: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine(retrievedValue == message.Value ? "Read/Write successful" : "Read/Write failed");
    }
}
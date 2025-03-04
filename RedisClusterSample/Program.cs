using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Load cluster configurations from a JSON file
        // In normal scenario it will come from parameters.
        var clusterConfigurations = LoadClusterConfigurations("redis_clusters.json");

        // Establish connection to different Redis clusters
        var organizationCluster = await ConnectToCluster(clusterConfigurations["organization"]);
        var passwordsCluster = await ConnectToCluster(clusterConfigurations["passwords"]);
        var productsCluster = await ConnectToCluster(clusterConfigurations["products"]);

        // Perform read and write tests on each cluster
        await PerformReadWriteTest(organizationCluster, "organizationKey", "Organization Data");
        await PerformReadWriteTest(passwordsCluster, "passwordKey", "Password Data");
        await PerformReadWriteTest(productsCluster, "productKey", "Product Data");
    }

    /// <summary>
    /// Loads Redis cluster configurations from a JSON file.
    /// The JSON file should contain a dictionary with cluster names as keys and a list of endpoints as values.
    /// </summary>
    static Dictionary<string, ConfigurationOptions> LoadClusterConfigurations(string filePath)
    {
        // Read the JSON file as a string
        var json = File.ReadAllText(filePath);
        
        // Deserialize JSON into a dictionary format
        var parsedJson = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(json);

        // Ensure the JSON structure is valid and contains "clusters" key
        if (parsedJson == null || !parsedJson.TryGetValue("clusters", out var config))
        {
            throw new InvalidOperationException("Invalid cluster configuration.");
        }

        var clusterConfigurations = new Dictionary<string, ConfigurationOptions>();

        // Iterate through each cluster and create ConfigurationOptions
        foreach (var cluster in config)
        {
            var configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = false, // Do not abort if the connection fails
                ConnectTimeout = 10000,    // Set connection timeout to 10 seconds
                SyncTimeout = 10000,       // Set synchronization timeout to 10 seconds
                AllowAdmin = true          // Allow administrative commands
            };

            // Add each endpoint to the configuration options
            foreach (var endpoint in cluster.Value)
            {
                configurationOptions.EndPoints.Add(endpoint);
            }

            // Store configuration for the current cluster
            clusterConfigurations[cluster.Key] = configurationOptions;
        }

        return clusterConfigurations;
    }

    /// <summary>
    /// Establishes an asynchronous connection to a Redis cluster using provided configuration options.
    /// </summary>
    static async Task<ConnectionMultiplexer> ConnectToCluster(ConfigurationOptions configurationOptions)
    {
        return await ConnectionMultiplexer.ConnectAsync(configurationOptions);
    }

    /// <summary>
    /// Performs a read and write test on the given Redis cluster.
    /// Writes a key-value pair, then reads it back and measures execution time.
    /// </summary>
    static async Task PerformReadWriteTest(ConnectionMultiplexer cluster, string key, string value)
    {
        var db = cluster.GetDatabase();
        var stopwatch = new Stopwatch();

        // Measure time taken to write a value to Redis
        stopwatch.Start();
        await db.StringSetAsync(key, value);
        stopwatch.Stop();
        Console.WriteLine("\n-------------------------------------------------\n");
        Console.WriteLine($"Write time for {key}: {stopwatch.ElapsedMilliseconds} ms");

        // Measure time taken to read the value from Redis
        stopwatch.Restart();
        var retrievedValue = await db.StringGetAsync(key);
        stopwatch.Stop();
        Console.WriteLine($"Retrived value: {retrievedValue}.");
        Console.WriteLine($"Read time for {key}: {stopwatch.ElapsedMilliseconds} ms");

        // Validate if the read value matches the written value
        Console.WriteLine(retrievedValue == value ? "Read/Write successful" : "Read/Write failed");
    }
}
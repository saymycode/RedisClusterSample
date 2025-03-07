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
        var clusterConfigurations = LoadClusterConfigurations("redis_clusters.json");
        var redisService = new RedisClusterService(clusterConfigurations);

        var messages = new[]
        {
            new RedisMessage 
            { 
                DataType = RedisDataType.Organization,
                Key = "organizationKey",
                Value = "Organization Data"
            },
            new RedisMessage 
            { 
                DataType = RedisDataType.Password,
                Key = "passwordKey",
                Value = "Password Data"
            },
            new RedisMessage 
            { 
                DataType = RedisDataType.Product,
                Key = "productKey",
                Value = "Product Data"
            }
        };

        foreach (var message in messages)
        {
            await redisService.PerformReadWriteTest(message);
        }
    }
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
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text.Json;

namespace Performance
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;

        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {   
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time");
            cpuCounter.InstanceName = "_Total";
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            while (!stoppingToken.IsCancellationRequested)
            {   
                try{
                    String url = configuration.GetValue<String>("url");
                    await PostResult(url, cpuCounter, ramCounter);
                }
                catch(Exception ex){
                    logger.LogError(ex, "exception");
                }
                finally{
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task PostResult(String url, PerformanceCounter cpuCounter, PerformanceCounter ramCounter){
            try
            {   
                var client = httpClientFactory.CreateClient();

                Double cpuCounterValue = (Double)cpuCounter.NextValue();
                Double ramCounterValue = (Double) ramCounter.NextValue();
                Stats stats = new Stats(){ CPU = cpuCounterValue, RAM = ramCounterValue };
               
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                var todoItemJson = new StringContent(
                                    JsonSerializer.Serialize(stats),
                                    System.Text.Encoding.UTF8,
                                    "application/json");     
                logger.LogInformation($"CPU Percentage: {cpuCounterValue}, RAM in MB: {ramCounterValue}, Url: {url}");
                var response = await client.PostAsync(url, todoItemJson);
                if (response.IsSuccessStatusCode)
                    logger.LogInformation("pushed successfully.", url);
                else
                    logger.LogWarning("pushed failed", url);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{Url} is offline.", url);
            }
        }
    }
}

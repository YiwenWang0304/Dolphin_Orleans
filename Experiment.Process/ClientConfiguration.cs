using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Experiment.Process
{
    public class ClientConfiguration
    {
        private IClusterClient client;

        public async Task<IClusterClient> StartClient(int initializeAttemptsBeforeFailing = 10)
        {
            if(client == null)
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(5000);
                        client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = Constants.LOCALCLUSTERID;
                            options.ServiceId = Constants.SERVICEID;
                        })
                        //.ConfigureLogging(logging => logging.AddConsole())
                        .AddSimpleMessageStreamProvider(Constants.STREAMPROVIDER)
                        .Build();

                        await client.Connect();
                        Console.WriteLine("Client successfully connect to silo host");
                        break;
                    }
                    catch (SiloUnavailableException)
                    {
                        attempt++;
                        Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                        if (attempt > initializeAttemptsBeforeFailing)
                            throw;
                        await Task.Delay(TimeSpan.FromSeconds(4));
                    }
                }
            }
            return client;
        }

        public async Task<IClusterClient> StartClientToCluster(int initializeAttemptsBeforeFailing = 10)
        {
            if (client == null) 
            {
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        Action<DynamoDBGatewayOptions> dynamoDBOptions = options =>
                        {
                            options.AccessKey = Constants.ACCESSKEY;
                            options.SecretKey = Constants.SECRETKEY;
                            options.TableName = Constants.TABLENAME;
                            options.Service = Constants.SERVICE;
                            options.WriteCapacityUnits = 10;
                            options.ReadCapacityUnits = 10;
                        };

                        client = new ClientBuilder()
                            .Configure<ClusterOptions>(options =>
                            {
                                options.ClusterId = Constants.EC2CLUSTERID;
                                options.ServiceId = Constants.SERVICEID;
                            })
                            .UseDynamoDBClustering(dynamoDBOptions)
                            .AddSimpleMessageStreamProvider(Constants.STREAMPROVIDER)
                            .Build();

                        await client.Connect();
                        Console.WriteLine("Client successfully connect to silo host");
                        break;
                    }
                    catch(Exception e)
                    //catch (SiloUnavailableException)
                    {
                        Console.WriteLine(e);
                        attempt++;
                        Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client to cluser.");
                        if (attempt > initializeAttemptsBeforeFailing)
                            throw;
                        await Task.Delay(TimeSpan.FromSeconds(4));
                    }
                }
            }
            return client;
        }
    }
}

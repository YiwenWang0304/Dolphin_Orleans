using Dolphin;
using Dolphin.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SiloHost
{
    public class Program
    {
        static readonly bool localCluster = false;

        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                ISiloHost host;
                if (localCluster)
                    host = await StartSilo();
                else
                    host = await StartClusterSilo();
                Console.WriteLine("\n\n Press Enter to terminate...\n\n");
                Console.ReadLine();
                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {

            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Constants.LOCALCLUSTERID;
                    options.ServiceId = Constants.SERVICEID;
                })
                 .Configure<GrainCollectionOptions>(options =>
                 {
                     options.CollectionAge = TimeSpan.FromMinutes(1000);
                 })
                .ConfigureLogging(logging => logging.AddConsole())
                .AddSimpleMessageStreamProvider(Constants.STREAMPROVIDER).AddMemoryGrainStorage("PubSubStore");

            builder.AddStartupTask<CallGrainStartupTask>();
            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        private static async Task<ISiloHost> StartClusterSilo()
        {

            Action<DynamoDBClusteringOptions> dynamoDBOptions = options =>
            {
                options.AccessKey = Constants.ACCESSKEY;
                options.SecretKey = Constants.SECRETKEY;
                options.TableName = Constants.TABLENAME;
                options.Service = Constants.SERVICE;
                options.WriteCapacityUnits = 10;
                options.ReadCapacityUnits = 10;
            };

            var builder = new SiloHostBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Constants.EC2CLUSTERID;
                    options.ServiceId = Constants.SERVICEID;
                })
                .Configure<GrainCollectionOptions>(options =>
                {  
                    options.CollectionAge = TimeSpan.FromMinutes(1000);
                })
                .ConfigureServices(ConfigureServices)
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Parse(Helper.GetLocalIPAddress()))
                .UseDynamoDBClustering(dynamoDBOptions)
                .ConfigureLogging(logging => logging.AddConsole().AddFilter("Orleans", LogLevel.Information))
                .AddSimpleMessageStreamProvider(Constants.STREAMPROVIDER).AddMemoryGrainStorage("PubSubStore")
                .ConfigureLogging(logging => logging.AddConsole());

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingletonNamedService<PlacementStrategy, SpatialPreferPlacementStrategy>(nameof(SpatialPreferPlacementStrategy));
            services.AddSingletonKeyedService<Type, IPlacementDirector, SpatialPreferPlacementStrategyFixedSiloDirector>(typeof(SpatialPreferPlacementStrategy));
        }

        public class CallGrainStartupTask : IStartupTask
        {
            private readonly IGrainFactory grainFactory;

            public CallGrainStartupTask(IGrainFactory grainFactory)
            {
                this.grainFactory = grainFactory;
            }

            public Task Execute(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }     
        }
    }

}



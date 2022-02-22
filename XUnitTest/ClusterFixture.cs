using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.TestingHost;
using System;
using System.Threading.Tasks;
using Xunit;

namespace XunitTest
{
    public class ClusterFixture : IDisposable
    {
        public ClusterFixture()
        {
            //this.Cluster = new TestClusterBuilder(1).Build();

            var builder = new TestClusterBuilder();
            builder.AddSiloBuilderConfigurator<SiloHostBuilderConfigurator>();
            this.Cluster = builder.Build(); ;

            this.Cluster.Deploy();
        }

        private class SiloHostBuilderConfigurator: ISiloBuilderConfigurator
        {
            public void Configure(ISiloHostBuilder hostBuilder)
            {
                hostBuilder.UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Constants.LOCALCLUSTERID;
                    options.ServiceId = Constants.SERVICEID;
                })
                 .Configure<GrainCollectionOptions>(options =>
                 {
                     options.CollectionAge = TimeSpan.FromMinutes(1000);
                 })
                .AddSimpleMessageStreamProvider(Constants.STREAMPROVIDER).AddMemoryGrainStorage("PubSubStore");   
            }
        }

        public void Dispose()
        {
            this.Cluster.StopAllSilos();
        }

        public TestCluster Cluster { get; private set; }
    }

    [CollectionDefinition(ClusterCollection.Name)]
    public class ClusterCollection : ICollectionFixture<ClusterFixture>
    {
        public const string Name = "ClusterCollection";
    }

}

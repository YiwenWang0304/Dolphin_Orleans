using Dolphin.Interfaces;
using Dolphin.Utilities;
using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Experiment.Process
{
    interface IBenchmark
    {
        
        Task<Task> ExecuteMove(IClusterClient client);
        Task<Task<List<ActorInfo>>> ExecuteFindNearbyActors(IClusterClient client);
        void GenerateBenchmark(WorkloadConfiguration config,int threadId);

    }
}
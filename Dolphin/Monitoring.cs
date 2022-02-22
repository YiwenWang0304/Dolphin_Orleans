using Dolphin.Interfaces;
using NetTopologySuite.Geometries;
using Orleans;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace Dolphin
{
   [SpatialPreferPlacementStrategy]
    public class Monitoring : Grain, IMonitoring
    {
        private IAsyncStream<MonitoringInfo> stream;

        Task IMonitoring.BecomeProducer(Guid streamId)
        {
            IStreamProvider streamProvider = base.GetStreamProvider(Constants.STREAMPROVIDER);
            stream=streamProvider.GetStream<MonitoringInfo> (streamId, Constants.STREAMNAMESPACE_ENV);// a GUID and a namespace to identify unique streams
            return Task.CompletedTask;
        }

        Task IMonitoring.NOP(Point pst)
        {
            return Task.CompletedTask;
        }

        async Task IMonitoring.Produce(MonitoringInfo monitoringInfo)
        {
            if (stream != null) 
                await stream.OnNextAsync(monitoringInfo);//To produce events into the stream, an application just calls
        }

    }
}

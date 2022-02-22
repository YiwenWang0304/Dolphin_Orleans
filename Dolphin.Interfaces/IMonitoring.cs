using NetTopologySuite.Geometries;
using System;
using System.Threading.Tasks;

namespace Dolphin.Interfaces
{
    public class MonitoringInfo
    {
        public Guid Id { set; get; }
        public long MoveId { set; get; }
        public LineString Trajectory { set; get; }

        public MonitoringInfo(Guid id, long moveId, LineString trajectory)
        {
            Id = id;
            MoveId = moveId;
            Trajectory = trajectory;
        }

        public MonitoringInfo()
        {
            Id = new Guid();
            MoveId = 0;
            Trajectory = new LineString(new Coordinate[] { });
        }
    }

    public interface IMonitoring : Orleans.IGrainWithIntegerKey
    {
        Task BecomeProducer(Guid streamId);
        Task Produce(MonitoringInfo monitoringInfo);
        Task NOP(Point pst);
    }
}

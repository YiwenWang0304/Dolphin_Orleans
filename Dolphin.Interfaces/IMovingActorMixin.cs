using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolphin.Interfaces
{
    public class SpatialInfo {
        public List<Point> Points{set;get;}
        public List<LineString> Trajectories { set; get; }
        public List<Polygon> Polygons { set; get; }

        public SpatialInfo(List<Point> points,List<LineString> trajectories, List<Polygon> polygons) {
            Points = points;
            Trajectories = trajectories;
            Polygons = polygons;
        }

        public SpatialInfo()
        {
            Points = new List<Point>();
            Trajectories = new List<LineString>();
            Polygons = new List<Polygon>();
        }

        public List<Point> GetPoints() {
            return Points;
        }

        public List<LineString> GetTrajectories()
        {
            return Trajectories;
        }

        public List<Polygon> GetPolygons()
        {
            return Polygons;
        }

    }

    public enum Semantics{Snapshot = 0,Freshness = 1}
    public enum Predicates { Cross, Cover, Overlap, Nearby }//todo: nearby

    public class ActorInfo
    {
        public Guid Id { set; get; }
        public Point Lct { set; get; }

        public ActorInfo(Guid id, Point lct)
        {
            Id = id;
            Lct = lct;
        }

        public ActorInfo()
        {
            Id = new Guid();
            Lct = new Point(0,0);
        }
    }

    public class ReactionInfo
    {
        public Guid ReceiverId { set; get; }
        public long MoveId { set; get; }
        public ReactionInfo (Guid receiverId, long moveId)
        {
            ReceiverId = receiverId;
            MoveId = moveId;
        }

        public ReactionInfo()
        {
            ReceiverId = new Guid();
            MoveId = 0;
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                return false;
            else
            {
                ReactionInfo p = (ReactionInfo)obj;
                return (ReceiverId == p.ReceiverId) && (MoveId == p.MoveId);
            }
        }

        public override int GetHashCode()
        {
            Random random = new Random();
            return (int) (random.NextDouble()*MoveId +random.NextDouble()*MoveId+random.NextDouble() * MoveId+ random.NextDouble() * MoveId);
        }

        public override string ToString()
        {
            return String.Format("ReacionInfo ( ReceiverId {0}, MoveId {1})", ReceiverId, MoveId);
        }
    }

    public interface IMovingActorMixin
    {
        Task Move(Point dst);
        Task<List<ActorInfo>> FindActors(RBush.Envelope queryRange);
        Task Subscribe(Predicates predicate, Func<ReactionInfo, Task> asyncCallback);
        Task UnSubscribe(int handle);

        Task ReceiveMSG(long moveId);
        Task OnTimeSendBuffer();

        Task NOP(Point pst);
        Task<List<Tuple<long, double>>> GetReactionNumAndLatencies();
        Task<Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>> GetBreakDownLatencies();
        Task<int> GetSubscribeTaskNum();

    }
}

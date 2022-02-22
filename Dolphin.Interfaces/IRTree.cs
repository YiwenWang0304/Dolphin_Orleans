using NetTopologySuite.Geometries;
using RBush;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Envelope = RBush.Envelope;

namespace Dolphin.Interfaces
{
    public class BBOX : ISpatialData, IComparable<BBOX>, IEquatable<BBOX>
    {
        private readonly Envelope _envelope;
        private readonly Guid _id;

        public BBOX(Guid id, double minX, double minY, double maxX, double maxY)
        {
            _id = id;
            _envelope = new Envelope(
                minX: minX,
                minY: minY,
                maxX: maxX,
                maxY: maxY);
        }

        public BBOX()
        {
            _id = Guid.NewGuid();
            _envelope = new Envelope(
                minX: 0,
                minY: 0,
                maxX: 0,
                maxY: 0);
        }

        public ref readonly Envelope Envelope => ref _envelope;
        public ref readonly Guid Id => ref _id;

        public int CompareTo(BBOX other)
        {
            if (_envelope.MinX != other.Envelope.MinX)
                return _envelope.MinX.CompareTo(other.Envelope.MinX);
            if (_envelope.MinY != other.Envelope.MinY)
                return _envelope.MinY.CompareTo(other.Envelope.MinY);
            if (_envelope.MaxX != other.Envelope.MaxX)
                return _envelope.MaxX.CompareTo(other.Envelope.MaxX);
            if (_envelope.MaxY != other.Envelope.MaxY)
                return _envelope.MaxY.CompareTo(other.Envelope.MaxY);
            return 0;
        }

        public bool Equals(BBOX other)
        {
            if (object.ReferenceEquals(other, null))
                return false;
            else if (object.ReferenceEquals(this, other))
                return true;
            else if (this.GetType() != other.GetType())
                return false;
            else if (!_id.Equals(other.Id))
                return false;
            else return (_envelope.MinX == other.Envelope.MinX && _envelope.MinY == other.Envelope.MinY && _envelope.MaxX == other.Envelope.MaxX && _envelope.MaxY == other.Envelope.MaxY);
        }

    }

    public interface IRTree : Orleans.IGrainWithIntegerKey
    {
        Task Initialize(Guid id, Point lct);
        Task Update(Guid id, Point src, Point dst);
        Task Insert(Guid id, Point lct);
        Task Delete(Guid id,Point lct);
        Task DeleteUpdate(Guid id, Point deleteLct, Point src, Point dst);
        Task DeleteInsert(Guid id, Point deleteLct, Point insertLct);
        Task SnapshotUpdate(List<Tuple<Guid, Point, Point>> updateBuffer, List<Tuple<Guid, Point>> insertBuffer, List<Tuple<Guid, Point>> deleteBuffer);
        Task Clear();
        Task<Tuple<int,List<ActorInfo>>> RangeQuery(Envelope e);
        Task<bool> IfExist(Guid id, Point src, Point dst);
        Task<bool> IfNotExist(Guid id, Point src, Point dst);

        //Task UpdateVersionNum();

        Task NOP(Point pst);
    }
}

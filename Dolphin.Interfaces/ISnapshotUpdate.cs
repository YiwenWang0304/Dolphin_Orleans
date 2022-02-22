using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolphin.Interfaces
{
    public interface ISnapshotUpdate : Orleans.IGrainWithIntegerKey
    {
        Task Initialize( int id, double[] BOARDERS, double CELLSIZE);
        Task<bool> ReceiveUpdateBuffer(Guid movingActorId, List<Tuple<Point,long>> updateBuffer);
        Task AddMovingActors(List<Guid> movingActorIds);
        Task AddExceptCellId(List<int> cellIds);
        Task StartCommunication();
        Task AddInsertBuffer(int id, List<Tuple<Guid, Point>> insertMovingActors);
        Task StartUpdate();
    }
}

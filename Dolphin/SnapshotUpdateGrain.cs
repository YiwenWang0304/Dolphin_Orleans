using Dolphin.Interfaces;
using Dolphin.Utilities;
using NetTopologySuite.Geometries;
using Orleans;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dolphin
{
   [SpatialPreferPlacementStrategy]
    public class SnapshotUpdateGrain : Grain, ISnapshotUpdate
    {
        private Dictionary<Guid, List<Tuple<Point, long>>> UpdateBuffers;
        private List<Guid> MovingActorMissList { set; get; }
        private int Id { set; get; }
        private List<int> DstCellIds { set; get; }
        private double[] BOARDERS { set; get; }
        private double CELLSIZE { set; get; }
        //private bool AllQuery { set; get; }
        //private int NUMMONITORACTORSPERCELL { set; get; }
        private HashSet<int> ExpectedCellIds { set; get; }
        private List<Guid> MovingActorsForNextRound { set; get; }
        private List<Tuple<Guid, Point, Point>> RTreeToBulkUpdate { set; get; }
        private List<Tuple<Guid, Point>> RTreeToBulkInsert { set; get; }
        private Dictionary<int, List<Tuple<Guid, Point>>> DstBulkInsert { set; get; }
        private List<Tuple<Guid, Point>> RTreeToBulkDelete { set; get; }
        private IDisposable timer;

        //private List<Tuple<Guid, Point, int>> Test = new List<Tuple<Guid, Point,int>>();

        Task ISnapshotUpdate.Initialize(int id, double[] BOARDERS, double CELLSIZE)
        {
            Id = id;
            this.BOARDERS = BOARDERS;
            this.CELLSIZE = CELLSIZE;
            //this.NUMMONITORACTORSPERCELL = NUMMONITORACTORSPERCELL;

            UpdateBuffers = new Dictionary<Guid, List<Tuple<Point, long>>>();
            RTreeToBulkUpdate = new List<Tuple<Guid, Point, Point>>();
            RTreeToBulkInsert = new List<Tuple<Guid, Point>>();
            DstBulkInsert = new Dictionary<int, List<Tuple<Guid, Point>>>();
            RTreeToBulkDelete = new List<Tuple<Guid, Point>>();
            ExpectedCellIds = new HashSet<int>();
            MovingActorMissList = new List<Guid>();
            MovingActorsForNextRound = new List<Guid>();
            DstCellIds = new List<int>();

            return Task.CompletedTask;
        }

        Task ISnapshotUpdate.AddMovingActors(List<Guid> movingActorIds) {
            MovingActorMissList.AddRange(movingActorIds);
            return Task.CompletedTask;
        }

        Task<bool> ISnapshotUpdate.ReceiveUpdateBuffer(Guid movingActorId, List<Tuple<Point,long>> updateBuffer)
        {
            if (MovingActorMissList.Remove(movingActorId))
            {
                UpdateBuffers.Add(movingActorId, updateBuffer);
                if (MovingActorMissList.Count == 0)
                    timer = this.RegisterTimer(PrepareUpdate, null, TimeSpan.Zero, TimeSpan.FromHours(19));
                return Task.FromResult(true);
            }

            //Debug.WriteLine(new TooShortIntervalException()); 
            return Task.FromResult(false);
        }

        private async Task PrepareUpdate(object arg)
        {
            foreach (var id in UpdateBuffers.Keys)
            {
                var dstList = UpdateBuffers[id];
                var dst = dstList[^1].Item1;
                var src = dstList[0].Item1;
                var moveId = dstList[^1].Item2;

                //-------------------Monitoring----------------------
                if (dstList.Count > 1)//don't need to handle unmoved data
                {
                    var monitorCellIds = new HashSet<int>();
                    var lct0 = src;
                    var init = new List<Coordinate> { new Coordinate(lct0.X, lct0.Y) };
                    monitorCellIds.Add(Helper.CalCellId(lct0, BOARDERS, CELLSIZE));
                    foreach (var item in dstList)
                    {
                        var lct = item.Item1;
                        if (lct.CompareTo(lct0) != 0)
                        {
                            monitorCellIds.Add(Helper.CalCellId(lct, BOARDERS, CELLSIZE));
                            init.Add(new Coordinate(lct.X, lct.Y));
                        }
                        lct0 = lct;
                    }
                    if (init.Count != 1)
                    {
                        var monitoringInfo = new MonitoringInfo(id, moveId, new LineString(init.ToArray()));
                        var monitorTasks = new List<Task>();
                        foreach (var cellId in monitorCellIds)
                            monitorTasks.Add(GrainFactory.GetGrain<IMonitoring>(cellId).Produce(monitoringInfo));
                        await Task.WhenAll(monitorTasks);
                    }
                }

                var newCellId = Helper.CalCellId(dst, BOARDERS, CELLSIZE);
                var oldCellId = Helper.CalCellId(src, BOARDERS, CELLSIZE);
                //if (newCellId > 23)
                //    Console.WriteLine("-----------------" + src + "," + BOARDERS[0] + "," + BOARDERS[1] + "," + CELLSIZE + "=======================");
                //if (oldCellId > 23)
                //    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!" + dst + "," + BOARDERS[0] + "," + BOARDERS[1] + "," + CELLSIZE + "=======================");

                //Debug.Assert(oldCellId == Id);
                //Debug.Assert(await GrainFactory.GetGrain<IRTree>(Id).IfExist(id, src, dst));
                if (newCellId == oldCellId)//local move
                {
                    MovingActorsForNextRound.Add(id);
                    if (src != dst)
                        RTreeToBulkUpdate.Add(new Tuple<Guid, Point, Point>(id, src, dst));
                }
                else//non-localmove
                {
                    DstCellIds.Add(newCellId);
                    if (DstBulkInsert.ContainsKey(newCellId))
                        DstBulkInsert[newCellId].Add(new Tuple<Guid, Point>(id, dst));
                    else
                        DstBulkInsert.Add(newCellId, new List<Tuple<Guid, Point>> { new Tuple<Guid, Point>(id, dst) });
                    RTreeToBulkDelete.Add(new Tuple<Guid, Point>(id, src));
                }
            }

            var IsEmpty = false;
            if (MovingActorsForNextRound.Count == 0)
            {
                IsEmpty = true;
            }
            
            while (!(await GrainFactory.GetGrain<ISnapshotController>(0).BuildCommunicationGraph(new Tuple<int, List<int>>(Id, DstCellIds), IsEmpty)))
                ;

            timer.Dispose();
        }

        Task ISnapshotUpdate.AddExceptCellId(List<int> cellIds)
        {
            foreach(var cellId in cellIds)
                ExpectedCellIds.Add(cellId);
            return Task.CompletedTask;
        }

      Task ISnapshotUpdate.AddInsertBuffer(int id, List<Tuple<Guid, Point>> insertMovingActors) 
        {
            if (ExpectedCellIds.Remove(id))
            {
                foreach (var insertMovingActor in insertMovingActors) 
                {
                    RTreeToBulkInsert.Add(insertMovingActor);
                    MovingActorsForNextRound.Add(insertMovingActor.Item1);//moved into this cell
                    //Debug.Assert(await GrainFactory.GetGrain<IRTree>(Id).IfNotExist(insertMovingActor.Item1, insertMovingActor.Item2, insertMovingActor.Item2));
                }
                return Task.CompletedTask;
            }
            else 
                throw new Exception("I don't supposed to talk to this cell");
        }

        async Task ISnapshotUpdate.StartCommunication()
        {
            if (DstBulkInsert.Count != 0)//need to talk to others.
            {
                var communicationTasks = new List<Task>();
                foreach (var item in DstBulkInsert)
                    communicationTasks.Add(GrainFactory.GetGrain<ISnapshotUpdate>(item.Key).AddInsertBuffer(Id, item.Value));
                await Task.WhenAll(communicationTasks);
            }
            else             
                throw new Exception("It should talk to someone, but it doesn't have anyone to talk with - lonely actor");
        }

        async Task ISnapshotUpdate.StartUpdate()
        {
            if (ExpectedCellIds.Count == 0)
            {
                try
                {
                if (RTreeToBulkUpdate.Count!=0|| RTreeToBulkInsert.Count!=0|| RTreeToBulkDelete.Count!=0)
                    await GrainFactory.GetGrain<IRTree>(Id).SnapshotUpdate(RTreeToBulkUpdate, RTreeToBulkInsert, RTreeToBulkDelete);
                }
                catch (Exception e)
                {
                    throw e;
                }

                //foreach (var item in RTreeToBulkUpdate)
                //    Debug.Assert(await GrainFactory.GetGrain<IRTree>(Id).IfExist(item.Item1, item.Item3, item.Item2));
                //foreach (var item in RTreeToBulkDelete)
                //    Debug.Assert(await GrainFactory.GetGrain<IRTree>(Id).IfNotExist(item.Item1, item.Item2, item.Item2));
                //foreach (var item in RTreeToBulkInsert)
                //    Debug.Assert(await GrainFactory.GetGrain<IRTree>(Id).IfExist(item.Item1, item.Item2, item.Item2));

                RTreeToBulkUpdate.Clear();
                RTreeToBulkDelete.Clear();
                RTreeToBulkInsert.Clear();
                DstBulkInsert.Clear();
                DstCellIds.Clear();

                MovingActorMissList.AddRange(MovingActorsForNextRound);
                MovingActorsForNextRound.Clear();

                UpdateBuffers.Clear();//finsihed update, clear buffer
                                      

            //timer.Dispose();
            }
            else
                throw new Exception("Communication among SnapshotUpdate actors has not finished yet!");
        }
    }
}

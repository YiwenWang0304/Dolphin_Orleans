using Dolphin.Interfaces;
using Dolphin.Utilities;
using NetTopologySuite.Geometries;
using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Envelope = RBush.Envelope;

namespace Dolphin
{

    public class MovingActorMixin : IMovingActorMixin
    {
        public Guid Id { get; }
        public IGrainFactory GrainFactory { set; get; }
        public Point Lct { set; get; }
        public Polygon Fence { set; get; }
        public Envelope FenceEnvelope { set; get; }
        public int CellId { set; get; }
        public bool SubscribeTrue = false;
        protected IStreamProvider StreamProvider { set; get; }
        internal Predicates Predicate { set; get; }
        public Func<ReactionInfo, Task> AsyncCallBack { get; private set; }

        private Dictionary<int, Task<StreamSubscriptionHandle<MonitoringInfo>>> HandleToConsumerHandle = new Dictionary<int, Task<StreamSubscriptionHandle<MonitoringInfo>>>();
        public Semantics SEMANTICS { set; get; }
        public double[] BOARDERS { set; get; }
        public double CELLSIZE { set; get; }
        public List<Tuple<Point, long>> Buffer { set; get; }
        public List<Polygon> BufferedFence = new List<Polygon>();
        public Polygon AccumulatedFence = new Polygon(new LinearRing(new Coordinate[] { }));
        private Dictionary<int, Task<StreamSubscriptionHandle<MonitoringInfo>>> HandleToConsumerHandleForThisSnapshot = new Dictionary<int, Task<StreamSubscriptionHandle<MonitoringInfo>>>();

        private int MoveId { set; get; }

        private Dictionary<long, DateTime> ReactionStartTime = new Dictionary<long, DateTime>();
        private List<Tuple<long, DateTime>> ReactionEndTime = new List<Tuple<long, DateTime>>();
        public HashSet<MonitoringInfo> MonitoringInfoStorage = new HashSet<MonitoringInfo>();

        private readonly Random random = new Random();
        //private int NUMMONITORACTORSPERCELL { set; get; }

        private readonly Stopwatch movingActorWatch = new Stopwatch();
        private readonly List<Double> QueryLatencies = new List<double>();
        private readonly List<Double> UpdateLatencies = new List<double>();
        private readonly List<Double> IndexUpdateLatencies = new List<double>();
        private readonly List<Double> MonitorSendLatencies = new List<double>();
        private readonly List<Double> UpdateFenceLatencies = new List<double>();
        private readonly List<Double> UpdateSubscribeLatencies = new List<double>();
        private readonly List<Double> SubscribeTaskLatencies = new List<double>();
        private int numUpdateSubTasks = 0;

        private bool lastTimeWasNonLocalMove { set; get; }
        private int NeedDeleteCellId { set; get; }
        private Point needDeleteLCT { set; get; }

        public MovingActorMixin(IGrainFactory grainFactory, IStreamProvider streamProvider, Guid id, Point lct, Polygon fence, Semantics semantics, double[] boarders, double cellsize)
        {
            MoveId = 0;
            movingActorWatch.Start();
            GrainFactory = grainFactory;
            Id = id;
            Lct = lct;
            Fence = fence;
            FenceEnvelope = Helper.CalEnvelope(Fence);
            CellId = Helper.CalCellId(lct, boarders, cellsize);

            SEMANTICS = semantics;
            BOARDERS = boarders;
            CELLSIZE = cellsize;
            //this.NUMMONITORACTORSPERCELL = NUMMONITORACTORSPERCELL;

            StreamProvider = streamProvider;

            Task.Run(() => GrainFactory.GetGrain<IRTree>(CellId).Initialize(id, lct)).Wait();
            if (semantics == Semantics.Snapshot)
            {
                //Task.Run(() => GrainFactory.GetGrain<ISnapshotUpdate>(CellId).AddMovingActor(id)).Wait();
                Buffer = new List<Tuple<Point, long>> { new Tuple<Point, long>(lct, MoveId) };
                BufferedFence.Add(Fence);
            }
            else
            {
                lastTimeWasNonLocalMove = false;
                NeedDeleteCellId = 0;
                needDeleteLCT = new Point(0, 0);
            }

        }

        async Task IMovingActorMixin.Move(Point dst)
        {
            var UpdateStartTime = movingActorWatch.Elapsed;
            MoveId++;
            ReactionStartTime.Add(MoveId, DateTime.Now);
            

            if (SEMANTICS == Semantics.Snapshot)//TickBased
            {
                try
                {
                    Buffer.Add(new Tuple<Point, long>(dst, MoveId));
                    if (SubscribeTrue)
                    {
                        var coordinates = UpdateFence(dst);
                        var minx = coordinates[0];
                        var miny = coordinates[1];
                        var maxx = coordinates[2];
                        var maxy = coordinates[3];
                        var newFence = new Polygon(new LinearRing(new Coordinate[]{
                        new Coordinate(minx,miny),
                        new Coordinate(minx,maxy),
                        new Coordinate(maxx,maxy),
                        new Coordinate(maxx,miny),
                        new Coordinate(minx,miny)
                    }));

                        BufferedFence.Add(newFence);
                        Fence = newFence;
                    }
                }
                catch (Exception e)
                {
                    throw new SnapshotBufferedDataUpdateException(e);
                }

            }
            else if (SEMANTICS == Semantics.Freshness)//freshness
            {
                var newCellId = Helper.CalCellId(dst, BOARDERS, CELLSIZE);

                //==========================Indexing-----------------------
                var indexUpdateStartTime = movingActorWatch.Elapsed;
                if (lastTimeWasNonLocalMove)
                {
                    if (CellId == newCellId)//Local Move
                    {
                        if (NeedDeleteCellId == CellId) // the same as old cellId
                            await GrainFactory.GetGrain<IRTree>(CellId).DeleteUpdate(Id, needDeleteLCT, Lct, dst);
                        else
                        {
                            var tasks = new List<Task>
                            {
                                GrainFactory.GetGrain<IRTree>(NeedDeleteCellId).Delete(Id, needDeleteLCT),
                                GrainFactory.GetGrain<IRTree>(CellId).Update(Id, Lct, dst)
                            };
                            await Task.WhenAll(tasks);
                        }

                        lastTimeWasNonLocalMove = false;
                    }
                    else//Non-local Move
                    {
                        if (NeedDeleteCellId == newCellId)
                            await GrainFactory.GetGrain<IRTree>(newCellId).DeleteInsert(Id, needDeleteLCT, dst);
                        else
                        {
                            var tasks = new List<Task>
                            {
                                GrainFactory.GetGrain<IRTree>(NeedDeleteCellId).Delete(Id, needDeleteLCT),
                                GrainFactory.GetGrain<IRTree>(newCellId).Insert(Id, dst)
                            };
                            await Task.WhenAll(tasks);
                        }

                        lastTimeWasNonLocalMove = true;
                        needDeleteLCT = Lct;
                        NeedDeleteCellId = CellId;
                    }
                }
                else 
                {
                    if (CellId == newCellId)
                    {
                        await GrainFactory.GetGrain<IRTree>(CellId).Update(Id, Lct, dst);
                        lastTimeWasNonLocalMove = false;
                    }
                    else
                    {
                        await GrainFactory.GetGrain<IRTree>(newCellId).Insert(Id, dst);
                        lastTimeWasNonLocalMove = true;
                        needDeleteLCT = Lct;
                        NeedDeleteCellId = CellId;
                    }
                }
                IndexUpdateLatencies.Add((movingActorWatch.Elapsed - indexUpdateStartTime).TotalMilliseconds);
                //==========================Indexing-----------------------

                //-----------------monitoring---------------------------
                var monitorStartTime = movingActorWatch.Elapsed;
                if (CellId == newCellId)//Local Move
                {
                    var monitorInfo = new MonitoringInfo(Id, MoveId, new LineString(new List<Coordinate>() { new Coordinate(Lct.X, Lct.Y), new Coordinate(dst.X, dst.Y) }.ToArray()));
                    GrainFactory.GetGrain<IMonitoring>(CellId).Produce(monitorInfo);
                    //for (var monitoringId = CellId * NUMMONITORACTORSPERCELL; monitoringId < (CellId + 1) * NUMMONITORACTORSPERCELL; monitoringId++)
                    //    GrainFactory.GetGrain<IMonitoring>(monitoringId).Produce(monitorInfo);
                }
                else//Non-local Move
                {
                    Envelope e;
                    if (dst.X <= Lct.X && dst.Y <= Lct.Y)
                        e = new Envelope(dst.X, dst.Y, Lct.X, Lct.Y);
                    else if (dst.X <= Lct.X && dst.Y >= Lct.Y)
                        e = new Envelope(dst.X, Lct.Y, Lct.X, dst.Y);
                    else if (dst.X >= Lct.X && dst.Y <= Lct.Y)
                        e = new Envelope(Lct.X, dst.Y, dst.X, Lct.Y);
                    else
                        e = new Envelope(Lct.X, Lct.Y, dst.X, dst.Y);
                    var cellIds = Helper.FindCellIds(e, BOARDERS, CELLSIZE);

                    var monitorInfo = new MonitoringInfo(Id, MoveId, new LineString(new List<Coordinate>() { new Coordinate(Lct.X, Lct.Y), new Coordinate(dst.X, dst.Y) }.ToArray()));
                    foreach (var cellId in cellIds)
                        GrainFactory.GetGrain<IMonitoring>(cellId).Produce(monitorInfo);
                    //for (var monitoringId = cellId * NUMMONITORACTORSPERCELL; monitoringId < (cellId + 1) * NUMMONITORACTORSPERCELL; monitoringId++)
                    //        GrainFactory.GetGrain<IMonitoring>(monitoringId).Produce(monitorInfo);
                    MonitorSendLatencies.Add((movingActorWatch.Elapsed - monitorStartTime).TotalMilliseconds);
                }
                MonitorSendLatencies.Add((movingActorWatch.Elapsed - monitorStartTime).TotalMilliseconds);
                //-----------------monitoring---------------------------

                var updateFenceStartTime = movingActorWatch.Elapsed;
                if (SubscribeTrue)
                {
                    var coordinates = UpdateFence(dst);
                    var minx = coordinates[0];
                    var miny = coordinates[1];
                    var maxx = coordinates[2];
                    var maxy = coordinates[3];
                    Fence = new Polygon(new LinearRing(new Coordinate[]{
                        new Coordinate(minx,miny),
                        new Coordinate(minx,maxy),
                        new Coordinate(maxx,maxy),
                        new Coordinate(maxx,miny),
                        new Coordinate(minx,miny)
                    }));
                }
                UpdateFenceLatencies.Add((movingActorWatch.Elapsed - updateFenceStartTime).TotalMilliseconds);

                //=====================
                CellId = newCellId;
            }
            else throw new ArgumentException("Undefined semantics!");

            Lct = dst;

            var updateSubscribeStartTime = movingActorWatch.Elapsed;
            try
            {
                if (SubscribeTrue)
                {
                    if (SEMANTICS == Semantics.Freshness)
                        await UpdateSubscribe();
                    else
                        await IncreaseSubscribe();
                }
            }
            catch (Exception e)
            {
                throw new SubscribeUpdateException(e);
            }
            var updateSubscribeEndTime = movingActorWatch.Elapsed;
            UpdateSubscribeLatencies.Add((updateSubscribeEndTime - updateSubscribeStartTime).TotalMilliseconds);

            UpdateLatencies.Add((updateSubscribeEndTime - UpdateStartTime).TotalMilliseconds);
        }

        private double[] UpdateFence(Point dst)
        {
            var minx = Fence.Coordinates[0].X;
            var miny = Fence.Coordinates[0].Y;
            var maxx = Fence.Coordinates[2].X;
            var maxy = Fence.Coordinates[2].Y;

            var potentialminX = minx + (dst.X - Lct.X);
            var potentialmaxX = maxx + (dst.X - Lct.X);
            if (potentialminX < BOARDERS[0])
            {
                minx = BOARDERS[0] + (BOARDERS[0] - potentialminX);
                maxx = potentialmaxX + (BOARDERS[0] - potentialminX);
            }
            else if (potentialmaxX > BOARDERS[2])
            {
                maxx = BOARDERS[2] - (potentialmaxX - BOARDERS[2]);
                minx = potentialminX - (potentialmaxX - BOARDERS[2]);
            }
            else
            {
                minx = potentialminX;
                maxx = potentialmaxX;
            }

            var potentialminY = miny + (dst.Y - Lct.Y);
            var potentialmaxY = maxy + (dst.Y - Lct.Y);
            if (potentialminY < BOARDERS[1])
            {
                miny = BOARDERS[1] + (BOARDERS[1] - potentialminY);
                maxy = potentialmaxY + (BOARDERS[1] - potentialminY);
            }
            else if (potentialmaxY > BOARDERS[3])
            {
                maxy = BOARDERS[3] - (potentialmaxY - BOARDERS[3]);
                miny = potentialminY - (potentialmaxY - BOARDERS[3]);
            }
            else
            {
                miny = potentialminY;
                maxy = potentialmaxY;
            }

            return new double[] { minx, miny, maxx, maxy };
        }

        public async Task IncreaseSubscribe()
        {
            var SubscribeTaskStartTime = movingActorWatch.Elapsed;

            Geometry unionFence = null;
            foreach (var f in BufferedFence)
                if (unionFence == null)
                    unionFence = f;
                else
                    unionFence = unionFence.Union(f);

            var currentSubCells = Helper.FindCellIds(Helper.CalEnvelope((Polygon)unionFence), BOARDERS, CELLSIZE);

            var oldCells = new HashSet<int>();
            foreach (var monitoringId in HandleToConsumerHandle.Keys)
                oldCells.Add(monitoringId);
            //oldCells.Add(monitoringId / NUMMONITORACTORSPERCELL);

            var newSubCells = currentSubCells.ToArray().Except(oldCells).ToList();
            numUpdateSubTasks += newSubCells.Count();

            var subTasks = await GenerateSubscribeTasks(newSubCells);
            await Task.WhenAll(subTasks);

            SubscribeTaskLatencies.Add((movingActorWatch.Elapsed - SubscribeTaskStartTime).TotalMilliseconds);
        }

        public async Task UpdateSubscribe()
        {
            if (HandleToConsumerHandle.Count() != 0)//this moving actor has subscribed
            {
                var SubscribeTaskStartTime = movingActorWatch.Elapsed;

                var currentSubCells = Helper.FindCellIds(Helper.CalEnvelope(Fence), BOARDERS, CELLSIZE);

                var oldCells = HandleToConsumerHandle.Keys.ToArray();

                var unSubCells = new List<int>(oldCells).ToArray().Except(currentSubCells).ToList();
                var newSubCells = currentSubCells.ToArray().Except(oldCells).ToList();
                numUpdateSubTasks += newSubCells.Count()+ unSubCells.Count();

                var subTasks = await GenerateSubscribeTasks(newSubCells);
                var unsubTasks = await GenerateUnsubscribeTasks(unSubCells);
                await Task.WhenAll(unsubTasks);
                await Task.WhenAll(subTasks);
                SubscribeTaskLatencies.Add((movingActorWatch.Elapsed - SubscribeTaskStartTime).TotalMilliseconds);
            }
        }

        async Task<List<ActorInfo>> IMovingActorMixin.FindActors(Envelope queryRange)
        {
            var startTime = movingActorWatch.Elapsed;
            var actorInfos = new List<ActorInfo>();
            var rangequeryTasks = new List<Task<Tuple<int, List<ActorInfo>>>>();
            var cellIds = Helper.FindCellIds(queryRange, BOARDERS, CELLSIZE);
            if (SEMANTICS == Semantics.Snapshot)
            {
                var versionConsistent = false;
                while (!versionConsistent)
                {
                    foreach (var cellId in cellIds)
                        rangequeryTasks.Add(GrainFactory.GetGrain<IRTree>(cellId).RangeQuery(queryRange));
                    var actorInfoswithVersionArray = await Task.WhenAll(rangequeryTasks);
                    var versionNum = actorInfoswithVersionArray[0].Item1;
                    var count = 0;
                    foreach (var item in actorInfoswithVersionArray)
                    {
                        if (item.Item1 != versionNum)
                            break;
                        actorInfos.AddRange(item.Item2);
                        count++;
                    }
                    if (count == actorInfoswithVersionArray.Length)
                        versionConsistent = true;
                    else
                    {
                        actorInfos.Clear();
                        rangequeryTasks.Clear();
                    }
                }
            }
            else
            {
                foreach (var cellId in cellIds)
                    rangequeryTasks.Add(GrainFactory.GetGrain<IRTree>(cellId).RangeQuery(queryRange));
                var actorInfoswithVersionArray = await Task.WhenAll(rangequeryTasks);
                foreach (var item in actorInfoswithVersionArray)
                    actorInfos.AddRange(item.Item2);
            }

            QueryLatencies.Add((movingActorWatch.Elapsed - startTime).TotalMilliseconds);
            return actorInfos;
        }

        private Task<List<Task<StreamSubscriptionHandle<MonitoringInfo>>>> GenerateSubscribeTasks(List<int> subscribeCellIds)
        {
            var consumerObserver = new MovingActorObserver(this);

            var tasks = new List<Task<StreamSubscriptionHandle<MonitoringInfo>>>();
            foreach (var subCellId in subscribeCellIds)
            {
                //var monitoringId = random.Next(subCellId * NUMMONITORACTORSPERCELL, (subCellId + 1) * NUMMONITORACTORSPERCELL);
                var consumer = StreamProvider.GetStream<MonitoringInfo>(Helper.ConvertIntToGuid(subCellId), Constants.STREAMNAMESPACE_ENV);
                var task = consumer.SubscribeAsync(consumerObserver);
                tasks.Add(task);
                if (HandleToConsumerHandle.ContainsKey(subCellId))
                    throw new Exception("Already have subsribed to this monitoring stream, please check!");
                else
                    HandleToConsumerHandle.Add(subCellId, task);
            }
            return Task.FromResult(tasks);
        }

        async Task IMovingActorMixin.Subscribe(Predicates predicate, Func<ReactionInfo, Task> asyncCallback)
        {
            if (!SubscribeTrue)
                SubscribeTrue = true;
            Predicate = predicate;
            AsyncCallBack = asyncCallback;
            var subTasks = await GenerateSubscribeTasks(Helper.FindCellIds(Helper.CalEnvelope(Fence), BOARDERS, CELLSIZE));
            await Task.WhenAll(subTasks);
        }

        internal void EstablishConnection(ReactionInfo msg)
        {
            AsyncCallBack?.Invoke(msg);
        }

        private Task<List<Task>> GenerateUnsubscribeTasks(IEnumerable<int> unsubMonitoringIds)
        {
            var tasks = new List<Task>();
            foreach (var unSubMonitoringId in unsubMonitoringIds)
            {
                if (HandleToConsumerHandle[unSubMonitoringId] != null)
                {
                    tasks.Add(HandleToConsumerHandle[unSubMonitoringId].Result.UnsubscribeAsync());
                    HandleToConsumerHandle.Remove(unSubMonitoringId);
                }
            }
            return Task.FromResult(tasks);
        }

        async Task IMovingActorMixin.UnSubscribe(int handle)
        {
            await HandleToConsumerHandle[handle].Result.UnsubscribeAsync();
            HandleToConsumerHandle.Remove(handle);
        }

        Task<List<Tuple<long, double>>> IMovingActorMixin.GetReactionNumAndLatencies()
        {
            var reactionNumAndLatencies = new List<Tuple<long, double>>();
            foreach (var item in ReactionEndTime)
                reactionNumAndLatencies.Add(new Tuple<long, double>(item.Item1,item.Item2.Subtract(ReactionStartTime[item.Item1]).TotalMilliseconds));

            return Task.FromResult(reactionNumAndLatencies);
        }

        internal bool IsNearbyTrue(MonitoringInfo monitoringInfo)
        {
            throw new NotImplementedException();
        }

        async Task IMovingActorMixin.OnTimeSendBuffer()
        {
            var buffer = new List<Tuple<Point, long>>();
            buffer.AddRange(Buffer);
            var bufferedFence = new List<Polygon>();
            bufferedFence.AddRange(BufferedFence);

            var fence = Fence;
           
            if (await GrainFactory.GetGrain<ISnapshotUpdate>(CellId).ReceiveUpdateBuffer(Id, buffer))
            {
                Buffer.Clear();
                Buffer.Add(buffer[^1]);//add dst as src  
                BufferedFence.Clear();
                BufferedFence.Add(bufferedFence[^1]);
                Fence = fence;
                CellId = Helper.CalCellId(buffer[^1].Item1, BOARDERS, CELLSIZE);

                Geometry unionFence = null;
                foreach (var f in bufferedFence)
                    if (unionFence == null)
                        unionFence = f;
                    else
                        unionFence = unionFence.Union(f);

                AccumulatedFence = (Polygon)(unionFence.ConvexHull());
            }
        }

        Task IMovingActorMixin.ReceiveMSG(long moveId)
        {
            ReactionEndTime.Add(new Tuple<long, DateTime>(moveId, DateTime.Now));
            return Task.CompletedTask;
        }

        async Task IMovingActorMixin.NOP(Point pst)
        {
            if (SEMANTICS == Semantics.Freshness)
                await GrainFactory.GetGrain<IRTree>(CellId).NOP(pst);
        }

        Task<Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>> IMovingActorMixin.GetBreakDownLatencies()
        {
            return Task.FromResult(new Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>(
                QueryLatencies,
                UpdateLatencies,
                IndexUpdateLatencies,
                MonitorSendLatencies,
                UpdateFenceLatencies,
                UpdateSubscribeLatencies,
                SubscribeTaskLatencies));
        }

        Task<int> IMovingActorMixin.GetSubscribeTaskNum()
        {
            return Task.FromResult(numUpdateSubTasks);
        }
    }

    internal class MovingActorObserver : IAsyncObserver<MonitoringInfo>
    {
        private readonly MovingActorMixin hostingGrain;

        internal MovingActorObserver(MovingActorMixin hostingGrain)
        {
            this.hostingGrain = hostingGrain;
        }

        async Task IAsyncObserver<MonitoringInfo>.OnNextAsync(MonitoringInfo monitoringInfo, StreamSequenceToken token = null)
        {
            Polygon Fence;
            if (hostingGrain.SEMANTICS == Semantics.Snapshot)
            {
                if (monitoringInfo.Trajectory.Coordinates.Length == 0)
                {
                    await hostingGrain.UpdateSubscribe();
                    hostingGrain.MonitoringInfoStorage.Clear();
                    return;
                }
                else if (!hostingGrain.MonitoringInfoStorage.Add(monitoringInfo))
                        return;
                else
                    Fence = hostingGrain.AccumulatedFence;
            }
            else
                Fence = hostingGrain.Fence;
            switch (hostingGrain.Predicate)
            {
                case Predicates.Cross://The two geometries have at least one point in common.
                                      //Console.WriteLine("onnextasync");
                    if (monitoringInfo.Trajectory.Crosses(Fence))
                        hostingGrain.EstablishConnection(new ReactionInfo(monitoringInfo.Id, monitoringInfo.MoveId));
                    break;
                case Predicates.Cover://Every point of the other geometry is a point of this geometry (include boundary).
                    if (monitoringInfo.Trajectory.Covers(Fence))
                        hostingGrain.EstablishConnection(new ReactionInfo(monitoringInfo.Id, monitoringInfo.MoveId));
                    break;
                case Predicates.Overlap://whether this geometry overlaps the specified geometry.
                    if (monitoringInfo.Trajectory.Overlaps(Fence))
                        hostingGrain.EstablishConnection(new ReactionInfo(monitoringInfo.Id, monitoringInfo.MoveId));
                    break;
                case Predicates.Nearby://Todo FENCE
                    if (hostingGrain.IsNearbyTrue(monitoringInfo))
                        hostingGrain.EstablishConnection(new ReactionInfo(monitoringInfo.Id, monitoringInfo.MoveId));
                    break;
                default:
                    throw new Exception("Undefined predicate");

            }

            //return ;
        }

        Task IAsyncObserver<MonitoringInfo>.OnCompletedAsync()
        {
            Console.WriteLine("==== Stream is Complete ====", ConsoleColor.Green);
            return Task.CompletedTask;
        }

        Task IAsyncObserver<MonitoringInfo>.OnErrorAsync(Exception ex)
        {
            Console.WriteLine("==== Stream is error ====", ConsoleColor.Red);
            return Task.CompletedTask;
        }
    }
}

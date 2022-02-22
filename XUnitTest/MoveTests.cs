using Dolphin.Interfaces;
using Dolphin.Utilities;
using NetTopologySuite.Geometries;
using Orleans;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XunitTest;
using Constants = XunitTest.Constants;

namespace XUnitTest
{
    
    [Collection(ClusterCollection.Name)]
    public class MoveTests
    {
        private readonly TestCluster _cluster;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Random random = new Random();
        private readonly Semantics SEMANTICS = Semantics.SnapshotBased;
        private readonly double[] BOARDERS = new double[] { Constants.XMIN, Constants.YMIN, Constants.XMAX, Constants.YMAX };
        private readonly double CELLSIZE = Constants.CELLSIZE;

        private readonly List<ITestGrain> movingActors = new List<ITestGrain>();
        private ITestGrain stationaryActor;

        public MoveTests(ClusterFixture fixture, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _cluster = fixture.Cluster;
            MoveActorsInitialize().Wait();
        }

        private async Task MoveActorsInitialize()
        {
           
            var initializeActorTasks = new List<Task>();
            var movingActorInfo = new Dictionary<int, List<Tuple<Guid, Point>>>();
            for (var i = 0; i < Constants.MOVINGACTOR+1; i++)
            {          
                var id = Guid.NewGuid();

                var lat = random.NextDouble() * (Constants.XMAX - Constants.XMIN) + Constants.XMIN;
                var lng = random.NextDouble() * (Constants.YMAX - Constants.YMIN) + Constants.YMIN;
                var lct = new Point(lat, lng);
                var cellId = Helper.CalCellId(lct, BOARDERS, CELLSIZE,CELLSIZE);

                if (movingActorInfo.ContainsKey(cellId))
                    movingActorInfo[cellId].Add(new Tuple<Guid, Point>(id, lct));
                else
                    movingActorInfo.Add(cellId, new List<Tuple<Guid, Point>> { new Tuple<Guid, Point>(id, lct) });

                var minLat = lat - Constants.FENCE;
                var maxLat = lat + Constants.FENCE;
                var minLng = lng - Constants.FENCE;
                var maxLng = lng + Constants.FENCE;
                minLat = minLat <= Constants.XMIN ? Constants.XMAX - (Constants.XMIN - minLat) : minLat;
                maxLat = maxLat >= Constants.XMAX ? Constants.XMIN + (maxLat - Constants.XMAX) : maxLat;
                minLng = minLng <= Constants.YMIN ? Constants.YMAX - (Constants.YMIN - minLng) : minLng;
                maxLng = maxLng >= Constants.YMAX ? Constants.YMIN + (maxLng - Constants.YMAX) : maxLng;

                if (i == 0)
                {
                    stationaryActor = _cluster.GrainFactory.GetGrain<ITestGrain>(id);
                    initializeActorTasks.Add(stationaryActor.VibrationInitialize(lct, lct, new Polygon(new LinearRing(new List<Coordinate>(){
                    new Coordinate(minLat,minLng),
                    new Coordinate(minLat,maxLng),
                    new Coordinate(maxLat,maxLng),
                    new Coordinate(maxLat,minLng),
                    new Coordinate(minLat,minLng)
                 }.ToArray())), Constants.STREAMPROVIDER, SEMANTICS, BOARDERS, Constants.CELLSIZE));

                }
                else {
                    var lat1 = lat - 2 * Constants.FENCE;
                    lat1 = lat1 <= Constants.XMIN ? Constants.XMAX - (Constants.XMIN - lat1) : lat1;

                    var movingActor = _cluster.GrainFactory.GetGrain<ITestGrain>(id);
                    initializeActorTasks.Add(movingActor.VibrationInitialize(lct, new Point(lat1, lng), new Polygon(new LinearRing(new List<Coordinate>(){
                        new Coordinate(minLat,minLng),
                        new Coordinate(minLat,maxLng),
                        new Coordinate(maxLat,maxLng),
                        new Coordinate(maxLat,minLng),
                        new Coordinate(minLat,minLng)
                    }.ToArray())), Constants.STREAMPROVIDER, SEMANTICS, BOARDERS, CELLSIZE));

                    movingActors.Add(movingActor);
                }
              
            }
            await Task.WhenAll(initializeActorTasks);

           // await _cluster.GrainFactory.GetGrain<IGridRouting>(0).Initialize(SEMANTICS,BOARDERS,CELLSIZE);

            foreach (var item in movingActorInfo)
                foreach (var i in item.Value) {
                   // var c1 = await _cluster.GrainFactory.GetGrain<IRTree>(item.Key).GetCount();
                    var c2 = (await _cluster.GrainFactory.GetGrain<IUpdateBuffer>(item.Key).ReturnThenDeleteOldList(i.Item1)).Count();
                    var c3 = await _cluster.GrainFactory.GetGrain<ITestGrain>(i.Item1).GetCount();
                    _testOutputHelper.WriteLine( c2.ToString() + " :" + c3.ToString());
                   // _testOutputHelper.WriteLine(" c1:" + c1.ToString() + " c2:" + c2.ToString() + " c3:" + c3.ToString());
                }
        }

        [Fact]
        public async Task VibrationMoveTest()
        {
           // await MoveActorsInitialize();
            foreach (var movingActor in movingActors) {
                var lct0 = await movingActor.GetPoint();
                await movingActor.Move(await movingActor.VibrationMove());
                var lct1 = await movingActor.GetPoint();
                await movingActor.Move(await movingActor.VibrationMove());
                var lct2 = await movingActor.GetPoint();
                await movingActor.Move(await movingActor.VibrationMove());
                var lct3 = await movingActor.GetPoint();

                _testOutputHelper.WriteLine(lct0.X.ToString() + "," + lct0.Y.ToString());
                _testOutputHelper.WriteLine(lct1.X.ToString() + "," + lct1.Y.ToString());
                _testOutputHelper.WriteLine(lct2.X.ToString() + "," + lct2.Y.ToString());
                _testOutputHelper.WriteLine(lct3.X.ToString() + "," + lct3.Y.ToString());
                Assert.Equal(lct0, lct2);
                Assert.Equal(lct1, lct3);
                Assert.NotEqual(lct0, lct1);
            }
           
        }

        [Fact]
        public async Task RTreeDeleteTest()
        {
            foreach (var movingActor in movingActors)
            {
                var lct = await movingActor.GetPoint();
                var cellId = Helper.CalCellId(lct,BOARDERS,CELLSIZE);
                await _cluster.GrainFactory.GetGrain<IRTree>(cellId).Delete(movingActor.GetPrimaryKey(),lct);
                await _cluster.GrainFactory.GetGrain<IRTree>(cellId).SwitchTrees();
            }

            var actorInfos = new List<ActorInfo>();
            var cellNum = Helper.CalCellNum(BOARDERS,CELLSIZE);
            for (var i = 0; i < cellNum; i++)
                actorInfos.AddRange(await _cluster.GrainFactory.GetGrain<IRTree>(i).RangeQuery(new BBOX(Constants.XMIN, Constants.YMIN, Constants.XMAX, Constants.YMAX)));

            _testOutputHelper.WriteLine(actorInfos.Count.ToString());
            Assert.Single(actorInfos);
        }

        [Fact]
        public async Task RTreeInsertTest()
        {
           // await MoveActorsInitialize();
            var IdToId = new Dictionary<Guid, Guid>();

            int count = 0;
            int test_count = 0;
            foreach (var movingActor in movingActors)
            {
                var lct = await movingActor.GetPoint();
                var cell = Helper.CalCellId(lct,BOARDERS,CELLSIZE);
                var actorInfo = await _cluster.GrainFactory.GetGrain<IRTree>(cell).RangeQuery(new BBOX(lct.X, lct.Y, lct.X, lct.Y));
                _testOutputHelper.WriteLine(movingActor.GetPrimaryKey().ToString());
                Assert.Single(actorInfo);
                count++;
                Assert.NotEqual(0, await movingActor.GetCount());
                if (0 == await movingActor.GetCount())
                    test_count++;

                IdToId.Add(movingActor.GetPrimaryKey(), actorInfo[0].Id);
            }
            _testOutputHelper.WriteLine(test_count.ToString());

            var cellNum = Helper.CalCellNum(BOARDERS, CELLSIZE);
            var count1 = 0;
            for (var i = 0; i < cellNum; i++)
                count1 += await _cluster.GrainFactory.GetGrain<IRTree>(i).GetCount();

            foreach (var item in IdToId)
                Assert.Equal(item.Key, item.Value);
            Assert.Equal(count+1, count1);
        }

        [Fact]
        public async Task UpdateBufferInsertTest() 
        {
           // await MoveActorsInitialize();
            var count = 0;
            var test_count = 0;
            foreach (var movingActor in movingActors)
            {
                var lct = await movingActor.GetPoint();
                var cellId = Helper.CalCellId(lct, BOARDERS, CELLSIZE);
                _testOutputHelper.WriteLine(movingActor.GetPrimaryKey().ToString());
                var buffer = await _cluster.GrainFactory.GetGrain<IUpdateBuffer>(cellId).ReturnThenDeleteOldList(movingActor.GetPrimaryKey());
                count+=buffer.Count;
                if(buffer.Count!=1)
                    _testOutputHelper.WriteLine(count.ToString() + "xxx"+buffer.Count+"..."+buffer[0]+":"+ buffer[buffer.Count-1]); 
               
                if( await movingActor.GetCount()==0)
                    test_count++;
            }
            _testOutputHelper.WriteLine(test_count.ToString());

            var cellNum = Helper.CalCellNum(BOARDERS,CELLSIZE);
            var count1 = 0;
            for (var i = 0; i < cellNum; i++)
                count1 += await _cluster.GrainFactory.GetGrain<IUpdateBuffer>(i).GetCount();

            Assert.Equal(count+1, count1);
        }

        [Fact]
        public async Task TickbasedFindNearbyActorsTest()
        {
            //await MoveActorsInitialize();
            var listt = await stationaryActor.FindNearbyActors();
            _testOutputHelper.WriteLine("findnearbyactors: "+listt.Count.ToString());

            var actorInfosList = new List<List<ActorInfo>>
            {
                await stationaryActor.FindNearbyActors()//-------lct0-------
            };

            foreach (var movingActor in movingActors) 
            {
                var dst = await movingActor.VibrationMove();//next lct
                _testOutputHelper.WriteLine("lct: "+await movingActor.GetPoint());
                var cellId = await movingActor.GetCellId();
                _testOutputHelper.WriteLine("old cellId: " + cellId);

                _testOutputHelper.WriteLine("dst: "+ dst);
                _testOutputHelper.WriteLine("new cellId: "+ Helper.CalCellId(dst,BOARDERS,CELLSIZE));

                var count = await movingActor.GetCount();
                _testOutputHelper.WriteLine("Count: " + count.ToString());

                var buffer = await _cluster.GrainFactory.GetGrain<IUpdateBuffer>(cellId).ReturnThenDeleteOldList(movingActor.GetPrimaryKey());
                _testOutputHelper.WriteLine(_cluster.GrainFactory.GetGrain<IUpdateBuffer>(cellId).GetPrimaryKey()+" buffer: " + buffer[0]);


                await movingActor.Move(dst); //move   
            }

            actorInfosList.Add(await stationaryActor.FindNearbyActors());//-------lct0+lct0---------

            await _cluster.GrainFactory.GetGrain<IGridRouting>(0).TriggerTick();//tick tiggered.

            actorInfosList.Add(await stationaryActor.FindNearbyActors());//-------lct0+lct0+lct1---------

            foreach (var movingActor in movingActors)
                await movingActor.Move(await movingActor.VibrationMove());//move

            actorInfosList.Add(await stationaryActor.FindNearbyActors());//-------lct0+lct0lct1+lct1---------

            //----------------------------------------------------------------------------------
            var idLists = new List<List<Guid>>();
            foreach (var list in actorInfosList)
            {
                var idds = new List<Guid>();
                _testOutputHelper.WriteLine(list.Count.ToString());
                foreach (var actorInfo in list)
                    if (actorInfo.Id != stationaryActor.GetPrimaryKey())
                        idds.Add(actorInfo.Id);
                idLists.Add(idds);
            }

            var firstNotSecond = idLists[0].Except(idLists[1]).ToList();//empty
            var thirdNotFourth = idLists[2].Except(idLists[3]).ToList();//empty
            var firstNotThird = idLists[0].Except(idLists[2]).ToList();//=moving actor count
            _testOutputHelper.WriteLine(idLists[0].Count.ToString()+ ", "+idLists[1].Count.ToString());
            Assert.True(!firstNotSecond.Any() && !thirdNotFourth.Any() && firstNotThird.Count==Constants.MOVINGACTOR);
        }

        [Fact]
        public async Task FreshnessFindNearbyActorsTest()
        {
            var actorInfosList = new List<List<ActorInfo>>();
            for (int j = 0; j < 4; j++)
            {
                actorInfosList.Add(await stationaryActor.FindNearbyActors());//lct0,lct1,lct0,lct1
                foreach (var movingActor in movingActors)
                    await movingActor.Move(await movingActor.VibrationMove());
            }

            var idLists = new List<List<Guid>>();
            foreach (var list in actorInfosList)
            {
                var idds = new List<Guid>();
                foreach (var actorInfo in list)
                    if (actorInfo.Id != stationaryActor.GetPrimaryKey())
                        idds.Add(actorInfo.Id);
                idLists.Add(idds);
            }

            var firstNotThird = idLists[0].Except(idLists[2]).ToList();//empty
            var thirdNotFirst = idLists[2].Except(idLists[0]).ToList();//empty
            var secondNotFourth = idLists[1].Except(idLists[3]).ToList();//empty
            var fourthNotSecond = idLists[3].Except(idLists[1]).ToList();//empty
            Assert.True(!firstNotThird.Any() && !thirdNotFirst.Any() && !secondNotFourth.Any() && !fourthNotSecond.Any());

        }

        [Fact]
        public async Task SubscribeTest()
        {
            var cellIds = Helper.FindCellIds(Helper.CalBBOX(await stationaryActor.GetFence(), Helper.BOARDERS), Helper.BOARDERS, Helper.CELLSIZE);
            foreach (var cellId in cellIds)
                await stationaryActor.Subscribe(Predicates.Intersect, new StreamInfo(stationaryActor.GetPrimaryKey(), cellId.ToString()));

            var semantics = Helper.SEMANTICS;
            var count = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                foreach (var movingActor in movingActors)
                    await movingActor.Move(await movingActor.VibrationMove());//move   

                if (i == 2 && semantics == Semantics.TickBased)
                    await _cluster.GrainFactory.GetGrain<IGridRouting>(0).TriggerTick();//tick tiggered.

                count.Add(await stationaryActor.GetNumNotification());//for tickbased - 0,0,N,N ||for freshness - N,2N,3N,4N
            }

            Assert.True(semantics == Semantics.Freshness && count[1] - count[0] == count[2] - count[1] && count[3] - count[2] == count[2] - count[1]);
            Assert.True(semantics == Semantics.TickBased && count[3] == count[2] && count[1] == count[0]);

        }
    }
}


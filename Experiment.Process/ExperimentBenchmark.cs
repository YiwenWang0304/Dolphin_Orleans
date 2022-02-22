using Benchmark.Synthetic;
using Dolphin.Interfaces;
using Dolphin.Utilities;
using NetTopologySuite.Geometries;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Experiment.Process
{
    enum FunctionType { MOVE, FINDNEARBYACTORS, NOP }
    class ExperimentBenchmark : IBenchmark
    {
        readonly Random random = new Random();
        readonly double MAXSPEED = Constants.MAXSPEED;
        List<Guid> movingActorIds = new List<Guid>();
        Distribution distribution;
        BenchmarkType benchmarktype;
        double[] BOARDERS;
        Dictionary<Guid, Tuple<Point, DateTime>> MovingActorInfo = new Dictionary<Guid, Tuple<Point, DateTime>>();
        Dictionary<Guid, List<Point>> ActorTjy = new Dictionary<Guid, List<Point>>();

        void IBenchmark.GenerateBenchmark(WorkloadConfiguration workloadConfig, int threadId)
        {
            var threadNum = workloadConfig.numThreadsPerWorkerNode;
            benchmarktype = workloadConfig.benchmarktype;
            distribution = workloadConfig.distribution;
            BOARDERS = workloadConfig.BOARDERS;

            if (benchmarktype == BenchmarkType.SIMULATION) {
                var actorInfoCount = workloadConfig.MovingActorInfo.Count / threadNum;
                var actorInfoList = workloadConfig.MovingActorInfo.GetRange(threadId * actorInfoCount, actorInfoCount);
                foreach (var actorInfo in actorInfoList)
                {
                    MovingActorInfo.Add(actorInfo.Item1, actorInfo.Item2);
                    movingActorIds.Add(actorInfo.Item1);
                    ActorTjy.Add(actorInfo.Item1, workloadConfig.ActorTjy[actorInfo.Item1]);
                }
            }else if (distribution == Distribution.UNIFORM)
            {
                var actorInfoCount = workloadConfig.MovingActorInfo.Count / threadNum;
                var actorInfoList = workloadConfig.MovingActorInfo.GetRange(threadId * actorInfoCount, actorInfoCount);
                foreach (var actorInfo in actorInfoList)
                {
                    MovingActorInfo.Add(actorInfo.Item1, actorInfo.Item2);
                    movingActorIds.Add(actorInfo.Item1);
                }

            }
            else if(distribution==Distribution.GAUSS)
            {
                var actorInfoCount = workloadConfig.MovingActorInfo.Count / threadNum;
                var actorInfoList = workloadConfig.MovingActorInfo.GetRange(threadId * actorInfoCount, actorInfoCount);
                foreach (var actorInfo in actorInfoList)
                {
                    MovingActorInfo.Add(actorInfo.Item1, actorInfo.Item2);
                    movingActorIds.Add(actorInfo.Item1);
                    ActorTjy.Add(actorInfo.Item1, workloadConfig.ActorTjy[actorInfo.Item1]);
                }
            }
        }

        public async Task<Task> ExecuteMove(IClusterClient client)//This task synclly generated a async task
        {
            try
            {
                var movingActorId = movingActorIds[random.Next(movingActorIds.Count)];
                var movingActor = client.GetGrain<IAppDefMovingActor>(movingActorId);
                if (benchmarktype == BenchmarkType.SIMULATION)
                    return movingActor.Move(await ReadTjy(movingActorId));
                else if (distribution == Distribution.UNIFORM)
                    return movingActor.Move(await UniformMove(movingActorId));
                else
                    return movingActor.Move(await ReadTjy(movingActorId));
            }
            catch (ReadTjyException e)
            {
                throw e;
            }
            catch (Exception e) {

                throw new ExecuteMoveException(e);
            }
        }

        public async Task<Task<List<ActorInfo>>> ExecuteFindNearbyActors(IClusterClient client)//This task synclly generated a async task
        {
            var movingActorId = movingActorIds[random.Next(movingActorIds.Count)];
            var movingActor = client.GetGrain<IAppDefMovingActor>(movingActorId);
            return movingActor.FindActors(await GenerateQueryRange());
        }


        private Task<RBush.Envelope> GenerateQueryRange()
        {
            var minX = BOARDERS[0] + Constants.QUERYSIZE;
            var maxX = BOARDERS[2] - Constants.QUERYSIZE;
            var minY = BOARDERS[1] + Constants.QUERYSIZE;
            var maxY = BOARDERS[3] - Constants.QUERYSIZE;

            var centralX = random.NextDouble() * (maxX - minX) + minX;
            var centralY = random.NextDouble() * (maxY - minY) + minY;
            var minx = centralX - Constants.QUERYSIZE;
            var maxx = centralX + Constants.QUERYSIZE;
            var miny = centralY - Constants.QUERYSIZE;
            var maxy = centralY + Constants.QUERYSIZE;

            return Task.FromResult(Helper.CalEnvelope(new Polygon(new LinearRing(new Coordinate[] {
                        new Coordinate(minx, miny),
                        new Coordinate(minx, maxy),
                        new Coordinate(maxx, maxy),
                        new Coordinate(maxx, miny),
                        new Coordinate(minx, miny)
                    }))));
        }

        private Task<Point> UniformMove(Guid id)
        {
            var src = MovingActorInfo[id].Item1;

            if (MovingActorInfo[id].Item2.Equals(DateTime.MaxValue))
            {
                MovingActorInfo[id] = new Tuple<Point, DateTime>(src, DateTime.Now);
                return Task.FromResult(src);
            }

            var speed = random.NextDouble() * MAXSPEED;
            var updateTime = DateTime.Now;
            //var duration = updateTime.Subtract(MovingActorInfo[id].Item2).TotalMilliseconds / 1000;
            var duration = 1;
            var distance = duration * speed;

            var dst = Move(src, distance);

            MovingActorInfo[id] = new Tuple<Point, DateTime>(dst, updateTime);

            return Task.FromResult(dst);
        }

        private Point Move(Point src, double distance)
        {
            var dst = src;
            double degree = random.NextDouble() * 360;
            double angle = Math.PI * degree / 180.0;
            double x, y;

            var deltaX = distance * Math.Cos(angle);
            var deltaY = distance * Math.Sin(angle);
            x = src.X + deltaX;
            y = src.Y + deltaY;
            if (x > BOARDERS[2] || x < BOARDERS[0]) //x exceed boarder
            {
                deltaX = -deltaX;
                x = src.X + deltaX;
                if (x > BOARDERS[0] || x < BOARDERS[2])//x doesn't exceed boarder after reverse
                {
                    deltaY = -deltaY;
                    y = src.Y + deltaY;
                    if (y > BOARDERS[1] && y < BOARDERS[3]) //y doesn't exceed boarder after reverse
                        dst = new Point(x, y);
                }
                //too big move in x, dst=src
            }
            else if (y > BOARDERS[3] || y < BOARDERS[1]) //y exceed boarder
            {
                deltaY = -deltaY;
                y = src.Y + deltaY;
                if (y > BOARDERS[1] || y < BOARDERS[3])//y doesn't exceed boarder after reverse
                {
                    deltaX = -deltaX;
                    x = src.X + deltaX;
                    if (x > BOARDERS[0] && x < BOARDERS[2])//x doesn't exceed boarder after reverse
                        dst = new Point(x, y);
                }
                //too big move in Y, dst=src
            }
            else
                dst = new Point(x, y);

            return dst;
        }

        private Task<Point> ReadTjy(Guid id)
        {
            try
            {
                var src = MovingActorInfo[id].Item1;
                var tjy = ActorTjy[id];
                int srcIndex = tjy.IndexOf(src);
                if (srcIndex == -1)
                    throw new Exception("Trajectory of actor " + id + " doesn't contains Point (" + src.X + ", " + src.Y + ").");
                else if (srcIndex == tjy.Count - 1)
                {
                    tjy.Reverse();
                    ActorTjy[id] = tjy;
                    srcIndex = tjy.IndexOf(src);
                }
                var dst = tjy[srcIndex + 1];
                MovingActorInfo[id] = new Tuple<Point, DateTime>(dst, DateTime.Now);
                //if (dst.X < -122.514586||dst.X> -122.357189 || dst.Y < 37.708289||dst.Y> 37.810644)
                //    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + dst +"!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                return Task.FromResult(dst);
            }
            catch (Exception e){ throw new ReadTjyException(e); }
            
        }

        //private Task<Point> NetWorkBasedMovement(Guid id)
        //{
            //var src = SimulationMovingActorInfo[id].Item1;
            //var nextPoint = SimulationMovingActorInfo[id].Item2;
            //var pId = SimulationMovingActorInfo[id].Item3;
            //if (SimulationMovingActorInfo[id].Item4.Equals(DateTime.MaxValue))
            //{
            //    SimulationMovingActorInfo[id] = new Tuple<Point, Point,int, DateTime>(src, nextPoint, pId, DateTime.Now);
            //    return Task.FromResult(src);
            //}

            //var updateTime = DateTime.Now;
            //var duration = updateTime.Subtract(SimulationMovingActorInfo[id].Item4).TotalMilliseconds / 1000;
            //var distance = duration * random.NextDouble() * Constants.MAXSPEED;

            //var distanceDelta = ConvertLatLngToMeter(src, nextPoint);
            //while (distance > distanceDelta)
            //{
            //    src = nextPoint;
            //    var polyIds = PointToPolys[src];
            //    pId = polyIds[random.Next(polyIds.Count)];
            //    nextPoint = GetNextPoint(src, pId);
            //    distance -= distanceDelta;
            //    distanceDelta = ConvertLatLngToMeter(src, nextPoint);
            //}

            //var dst = ConvertMetersToLatLng(src, nextPoint, distance);
            //SimulationMovingActorInfo[id] = new Tuple<Point, Point, int, DateTime>(dst, nextPoint, pId, updateTime);
            //return Task.FromResult(dst);
        //}

        //private Tuple<Point, int> FounndDstAndPolyId(double distance, Point src, int polyId)
        //{
        //    var nextPoint = GetNextPoint(src, polyId);
        //    var distanceDelta = ConvertLatLngToMeter(src, nextPoint.Item2);
        //    while (distance > distanceDelta)
        //    {
        //        var middlePoint = nextPoint.Item2;
        //        nextPoint = GetNextPoint(middlePoint, polyId);
        //        distanceDelta += ConvertLatLngToMeter(middlePoint, nextPoint.Item2);
        //        Console.WriteLine(distance + " | " + distanceDelta);
        //    }

        //    var polyIds = PointToPolys[nextPoint.Item2];
        //    if (nextPoint.Item1 == true)//this point is a connection point
        //    {
        //        var dstIndex = polyIds.IndexOf(polyId);
        //        if (dstIndex == polyIds.Count - 1)
        //            polyId = polyIds[0];
        //        else
        //            polyId = polyIds[dstIndex + 1];
        //    }

        //    return new Tuple<Point, int>(nextPoint.Item2, polyId);
        //}

        //private double ConvertLatLngToMeter(Point src, Point nextPoint)//calclulate distance between two points
        //{
        //    var dX = nextPoint.X * Math.PI / 180 - src.X * Math.PI / 180;
        //    var dY = nextPoint.Y * Math.PI / 180 - src.Y * Math.PI / 180;
        //    var a = Math.Sin(dX / 2) * Math.Sin(dX / 2) + Math.Cos(src.X * Math.PI / 180) * Math.Cos(nextPoint.X * Math.PI / 180) * Math.Sin(dY / 2) * Math.Sin(dY / 2);
        //    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        //    var d = R * c;
        //    return d * 1000; // meters
        //}

        //private Point ConvertMetersToLatLng(Point src, Point limitPoint, double distance)//calculate a dst point from src to limitPoint at distanceExtra
        //{
        //    var p = distance / ConvertLatLngToMeter(src, limitPoint);
        //    var x = src.X + p * (limitPoint.X - src.X);
        //    var y = src.Y + p * (limitPoint.Y - src.Y);
        //    return new Point(x, y);
        //}

        //private Point GetNextPoint(Point src, int polyId)
        //{
        //    Point dst;
        //    var listpoints = PolyToPoints[polyId];
        //    int srcIndex = listpoints.IndexOf(src);
        //    if (srcIndex == -1)
        //        throw new Exception("Polygon " + polyId + " doesn't contains Point (" + src.X + ", " + src.Y + ").");
        //    else if (srcIndex == listpoints.Count - 1)
        //        dst = listpoints[0];
        //    else
        //        dst = listpoints[srcIndex + 1];
        //    return dst;
        //}
    }
}

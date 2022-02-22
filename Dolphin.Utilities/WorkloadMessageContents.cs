using Dolphin.Interfaces;
using NetTopologySuite.Geometries;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dolphin.Utilities
{
    public enum BenchmarkType { SYNTHETIC, SIMULATION };
    public enum Distribution { UNIFORM, GAUSS }

    [Serializable]
    public class WorkloadConfiguration
    {
        public int numConnToClusterPerWorkerNode;
        public int numWorkerNodes;
        public int numThreadsPerWorkerNode;
        public int asyncMsgLengthPerThread;
        public int numEpochs;
        public int epochDurationMSecs;
        public int[] percentilesToCalculate;
        public BenchmarkType benchmarktype;
        public Distribution distribution;
        public Semantics semantics;
        public List<Guid> movingActorIds = new List<Guid>();
       // public Dictionary<Guid, Point> MA2HS = new Dictionary<Guid, Point>();
        public double queryRate;
        public double[] BOARDERS;
        //public Dictionary<Point, List<int>> PointToPolys = new Dictionary<Point, List<int>>();
        //public Dictionary<int, List<Point>> PolyToPoints = new Dictionary<int, List<Point>>();
        public List<Tuple<Guid, Tuple<Point, DateTime>>> MovingActorInfo = new List<Tuple<Guid, Tuple<Point, DateTime>>>();
        //public List<Tuple<Guid, Tuple<Point, Point, int, DateTime>>> SimulationMovingActorInfo = new List<Tuple<Guid, Tuple<Point, Point, int, DateTime>>>();
        //public int ringNum;
        //public double hotspotRange;
        public Dictionary<Guid, List<Point>> ActorTjy = new Dictionary<Guid, List<Point>>();
    }

    [Serializable]
    public class WorkloadResults
    {
        public int numCommitted;
        public int numTransactions;
        public long startTime;
        public long endTime;
        public List<double> latencies;
        public int numQuery;
        public int numUpdate;
        public int SubscribeUpdateExceptionCount; 
        public int SnapshotBufferedDataUpdateExceptionCount;
        public int MovingActorExceptionCount;
        public int ReadTjyExceptionCount;
        public int OtherExceptionCount;

        public WorkloadResults(int numTransactions, int numCommitted, long startTime, long endTime, List<double> latencies, int numQuery, int numUpdate, int SubscribeUpdateExceptionCount, int SnapshotBufferedDataUpdateExceptionCount, int MovingActorExceptionCount,int ReadTjyExceptionCount, int OtherExceptionCount )
        {
            this.numTransactions = numTransactions;
            this.numCommitted = numCommitted;
            this.startTime = startTime;
            this.endTime = endTime;
            this.latencies = latencies;
            this.numQuery = numQuery;
            this.numUpdate = numUpdate;
            this.SubscribeUpdateExceptionCount=SubscribeUpdateExceptionCount;
            this.SnapshotBufferedDataUpdateExceptionCount=SnapshotBufferedDataUpdateExceptionCount;
            this.MovingActorExceptionCount = MovingActorExceptionCount;
            this.ReadTjyExceptionCount = ReadTjyExceptionCount;
            this.OtherExceptionCount=OtherExceptionCount;
    }

    }


    [Serializable]
    public class AggregatedWorkloadResults
    {
        public List<List<WorkloadResults>> results;

        public AggregatedWorkloadResults(List<WorkloadResults>[] input)
        {
            results = new List<List<WorkloadResults>>();
            for (int i = 0; i < input.Length; i++)
            {
                results.Add(input[i]);
            }
        }
    }

}

using Benchmark.Synthetic;
using Dolphin.Interfaces;
using Dolphin.Utilities;
using MathNet.Numerics.Statistics;
using NDesk.Options;
using NetMQ;
using NetMQ.Sockets;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Experiment.Controller
{

    class Program
    {

        static String workerAddress = "@tcp://*:5557";
        static String sinkAddress = "@tcp://172.31.25.74:5558";//"@tcp://controllerPrivateIP:5558"

        static String directoryPath = "";
        static int numWorkerNodes;
        static int numWarmupEpoch;
        static double reativeSensingRate;
        static IClusterClient client;
        static Boolean LocalCluster;
        static Boolean LocalExperiment;
        static volatile bool asyncInitializationDone = false;
        static volatile bool loadingDone = false;
        static CountdownEvent ackedWorkers;
        static CountdownEvent ackedRecord;
        static CountdownEvent ackedLatencies;
        static CountdownEvent ackedWrite;
        static WorkloadConfiguration workload;
        static WorkloadResults[,] results;

        static readonly Random random = new Random();
        static StringBuilder resultString;

        static double CELLSIZE;
        static int CELLNUM;
        static double INTERVAL;
        static int MOVINGACTORNUM;
        static int HOTSPOTNUM;
        //static int NUMMONITORACTORSPERCELL;

        private static void GenerateWorkLoadFromSettingsFile(char benchmark, char semantics, int actorNum, double queryRate, double reaSensing, double interval, int hotspotNum)
        {

            //Parse and initialize benchmarkframework section
            var benchmarkFrameWorkSection = ConfigurationManager.GetSection("BenchmarkFrameworkConfig") as NameValueCollection;
            LocalCluster = bool.Parse(benchmarkFrameWorkSection["LocalCluster"]);
            LocalExperiment = bool.Parse(benchmarkFrameWorkSection["LocalExperiment"]);
            if (LocalExperiment)
            {
                workerAddress = "@tcp://localhost:5557";
                sinkAddress = "@tcp://localhost:5558";
                directoryPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString()).ToString();
                directoryPath = directoryPath.Replace('\u005c', '\u002f');
                directoryPath += "/Benchmark.ActorDataGenerate";
            }
            else
                directoryPath = "/Users/Administrator/Documents/GitHub/Dolphin-Orleans/Orleans_Dolphin/Benchmark.ActorDataGenerate";

            workload.numWorkerNodes = int.Parse(benchmarkFrameWorkSection["numWorkerNodes"]);
            numWorkerNodes = workload.numWorkerNodes;
            workload.numConnToClusterPerWorkerNode = int.Parse(benchmarkFrameWorkSection["numConnToClusterPerWorkerNode"]);
            workload.numThreadsPerWorkerNode = int.Parse(benchmarkFrameWorkSection["numThreadsPerWorkerNode"]);
            workload.epochDurationMSecs = int.Parse(benchmarkFrameWorkSection["epochDurationMSecs"]);
            workload.numEpochs = int.Parse(benchmarkFrameWorkSection["numEpochs"]);
            numWarmupEpoch = int.Parse(benchmarkFrameWorkSection["numWarmupEpoch"]);
            workload.asyncMsgLengthPerThread = int.Parse(benchmarkFrameWorkSection["asyncMsgLengthPerThread"]);
            workload.percentilesToCalculate = Array.ConvertAll<string, int>(benchmarkFrameWorkSection["percentilesToCalculate"].Split(","), x => int.Parse(x));

            //Parse Dolphin configuration
            var dolphinConfigSection = ConfigurationManager.GetSection("DolphinConfig") as NameValueCollection;
            var maxNonDetWaitingLatencyInMSecs = int.Parse(dolphinConfigSection["maxNonDetWaitingLatencyInMSecs"]);
            var batchIntervalMSecs = int.Parse(dolphinConfigSection["batchIntervalMSecs"]);
            var idleIntervalTillBackOffSecs = int.Parse(dolphinConfigSection["idleIntervalTillBackOffSecs"]);
            var backoffIntervalMsecs = int.Parse(dolphinConfigSection["backoffIntervalMsecs"]);
            var numCoordinators = uint.Parse(dolphinConfigSection["numCoordinators"]);
            //NUMMONITORACTORSPERCELL = int.Parse(dolphinConfigSection["numMonitorActorsPerCell"]);

            //Parse workload specific configuration, assumes only one defined in file
            if (benchmark.Equals('u'))
            {
                workload.benchmarktype = BenchmarkType.SYNTHETIC;
                workload.distribution = Distribution.UNIFORM;
            }
            else if (benchmark.Equals('g'))
            {
                workload.benchmarktype = BenchmarkType.SYNTHETIC;
                workload.distribution = Distribution.GAUSS;
            }
            else {
                workload.benchmarktype = BenchmarkType.SIMULATION;
            }
               
            if (semantics.Equals('f'))
                workload.semantics = Semantics.Freshness;
            else {
                workload.semantics = Semantics.Snapshot;
                INTERVAL = interval;
            }
                        
            workload.queryRate = queryRate;
            reativeSensingRate= reaSensing;
            MOVINGACTORNUM = actorNum;             

            if (workload.benchmarktype == BenchmarkType.SYNTHETIC)
            {
                if (workload.distribution == Distribution.UNIFORM)
                {
                    switch (MOVINGACTORNUM)
                    {
                        case Constants.MOVINGACTOR_NUM_TEST:
                            workload.BOARDERS = new double[] { -Constants.BOARDER_TEST, -Constants.BOARDER_TEST, Constants.BOARDER_TEST, Constants.BOARDER_TEST };
                            CELLNUM = Constants.CELLNUM_TEST;
                            break;
                        case Constants.MOVINGACTOR_NUM0:
                            workload.BOARDERS = new double[] { -Constants.BOARDER0, -Constants.BOARDER0, Constants.BOARDER0, Constants.BOARDER0 };
                            CELLNUM = Constants.CELLNUM0;
                            break;
                        case Constants.MOVINGACTOR_NUM1:
                            workload.BOARDERS = new double[] { -Constants.BOARDER1, -Constants.BOARDER1, Constants.BOARDER1, Constants.BOARDER1 };
                            CELLNUM = Constants.CELLNUM1;
                            break;
                        case Constants.MOVINGACTOR_NUM2:
                            workload.BOARDERS = new double[] { -Constants.BOARDER2, -Constants.BOARDER2, Constants.BOARDER2, Constants.BOARDER2 };
                            CELLNUM = Constants.CELLNUM2;
                            break;
                        case Constants.MOVINGACTOR_NUM3:
                            workload.BOARDERS = new double[] { -Constants.BOARDER3, -Constants.BOARDER3, Constants.BOARDER3, Constants.BOARDER3 };
                            CELLNUM = Constants.CELLNUM3;
                            break;
                        default:
                            workload.BOARDERS = new double[] { -Constants.BOARDER0, -Constants.BOARDER0, Constants.BOARDER0, Constants.BOARDER0 };
                            CELLNUM = Constants.CELLNUM0;
                            break;
                    }
                }
                else if (workload.distribution == Distribution.GAUSS)
                {
                    workload.BOARDERS = new double[] { -Constants.BOARDER_GAUSS, -Constants.BOARDER_GAUSS, Constants.BOARDER_GAUSS, Constants.BOARDER_GAUSS };
                    CELLNUM = Constants.CELLNUM_GAUSS;
                    HOTSPOTNUM = hotspotNum;
                    //workload.hotspotRange= Math.Sqrt(((workload.BOARDERS[2] - workload.BOARDERS[0]) * (workload.BOARDERS[3] - workload.BOARDERS[1])) / (HOTSPOTNUM * Math.PI));
                }
                else
                    throw new Exception("Wrong distribution type.");
                CELLSIZE = (workload.BOARDERS[2] - workload.BOARDERS[0]) / Math.Sqrt(CELLNUM);
            }
            else if (workload.benchmarktype == BenchmarkType.SIMULATION)
            {
                MOVINGACTORNUM = Constants.MOVINGACTOR_ROADNETWORK;
                BuildFromNetworkFromFile();
            }
            else
                throw new Exception("Wrong benchmark type!");
            Console.WriteLine("Generated workload configuration");
        }

        private static void BuildFromNetworkFromFile()
        {
            var roadNetworkDir = directoryPath + "/rows.json";
            using StreamReader r = new StreamReader(roadNetworkDir);
            double minLng = 180;
            double minLat = 90;
            double maxLng = -180;
            double maxLat = -90;

            string json = r.ReadToEnd();
            var array = JsonConvert.DeserializeObject<RoadData>(json);

            //var polyId = 0;
            foreach (var item in array.data)
            {
                string[] words = item[10].Split(',');
                Array.Resize(ref words, words.Length - 1);
                words[0] = words[0].Replace("MULTIPOLYGON (((", "");

                foreach (var word in words)
                {
                    string[] n = word.Trim(' ').Split(' ');
                    var lng = Convert.ToDouble(n[0]);
                    var lat = Convert.ToDouble(n[1]);
                    if (lat > Constants.MINLAT && lat < Constants.MAXLAT && lng > Constants.MINLNG && lng < Constants.MAXLNG)
                    {
                        minLng = lng <= minLng ? lng : minLng;
                        maxLng = lng >= maxLng ? lng : maxLng;
                        minLat = lat <= minLat ? lat : minLat;
                        maxLat = lat >= maxLat ? lat : maxLat;
                    }
                }
            }
            workload.BOARDERS = new double[] { minLng, minLat, maxLng, maxLat };
            CELLSIZE = Math.Sqrt(((workload.BOARDERS[2] - workload.BOARDERS[0]) * (workload.BOARDERS[3] - workload.BOARDERS[1])) / (MOVINGACTORNUM / Constants.MOVINGACTORPERCELL));
            var CELLNUM_X = (int)(Math.Ceiling((workload.BOARDERS[2] - workload.BOARDERS[0]) / CELLSIZE));
            var CELLNUM_Y = (int)(Math.Ceiling((workload.BOARDERS[3] - workload.BOARDERS[1]) / CELLSIZE));
            CELLNUM = CELLNUM_X * CELLNUM_Y;
        }

        private static void AggregateResultsAndPrint()
        {
            Trace.Assert(workload.numEpochs >= 1);
            Trace.Assert(numWorkerNodes >= 1);
            var aggLatencies = new List<double>();
            var aggActualQueryrates = new List<double>();
            var throughPutAccumulator = new List<float>();
            var abortRateAccumulator = new List<float>();
            //Skip the epochs upto warm up epochs
            var maxLatency = 0.0;
            var maxLatencyEpoch = 0;
            var subsribeExceptionAccumulator = new List<float>();
            var infoUpdateExceptionAccumulator = new List<float>();
            var movingActorExceptionAccumulator = new List<float>();
            var readTjyExceptionAccumulator = new List<float>();
            var otherExceptionAccumulator = new List<float>();

            for (int epochNumber = 0; epochNumber < workload.numEpochs; epochNumber++)
            {
                for (var workerNode = 0; workerNode < numWorkerNodes; workerNode++)
                {
                    if (results[epochNumber, workerNode].latencies.Maximum() > maxLatency)
                    {
                        maxLatency = results[epochNumber, workerNode].latencies.Maximum();
                        maxLatencyEpoch = epochNumber;
                    }
                }
            }
            for (int epochNumber = numWarmupEpoch; epochNumber < workload.numEpochs; epochNumber++)
            {
                int aggNumCommitted = results[epochNumber, 0].numCommitted;
                int aggNumTransactions = results[epochNumber, 0].numTransactions;
                long aggStartTime = results[epochNumber, 0].startTime;
                long aggEndTime = results[epochNumber, 0].endTime;
                aggLatencies.AddRange(results[epochNumber, 0].latencies);
                int aggNumQuery = results[epochNumber, 0].numQuery;
                int aggNumUpdate = results[epochNumber, 0].numUpdate;
                int aggSubscribeExceptionCount = results[epochNumber, 0].SubscribeUpdateExceptionCount;
                int aggInfoUpdateExceptionCount = results[epochNumber, 0].SnapshotBufferedDataUpdateExceptionCount;
                int aggmovingActorExceptionCount = results[epochNumber, 0].MovingActorExceptionCount;
                int aggreadTjyExceptionCount = results[epochNumber, 0].ReadTjyExceptionCount;
                int aggOtherExceptionCount = results[epochNumber, 0].OtherExceptionCount;

                for (var workerNode = 1; workerNode < numWorkerNodes; workerNode++)
                {
                    aggNumCommitted += results[epochNumber, workerNode].numCommitted;
                    aggNumTransactions += results[epochNumber, workerNode].numTransactions;
                    aggStartTime = (results[epochNumber, workerNode].startTime < aggStartTime) ? results[epochNumber, workerNode].startTime : aggStartTime;
                    aggEndTime = (results[epochNumber, workerNode].endTime < aggEndTime) ? aggEndTime : results[epochNumber, workerNode].endTime;
                    aggLatencies.AddRange(results[epochNumber, workerNode].latencies);
                    aggNumQuery += results[epochNumber, workerNode].numQuery;
                    aggNumUpdate += results[epochNumber, workerNode].numUpdate;

                   aggSubscribeExceptionCount += results[epochNumber, workerNode].SubscribeUpdateExceptionCount;
                   aggInfoUpdateExceptionCount += results[epochNumber, workerNode].SnapshotBufferedDataUpdateExceptionCount;
                   aggmovingActorExceptionCount += results[epochNumber, workerNode].MovingActorExceptionCount;
                   aggreadTjyExceptionCount += results[epochNumber, workerNode].ReadTjyExceptionCount;
                   aggOtherExceptionCount += results[epochNumber, workerNode].OtherExceptionCount;
                }
                float committedTxnThroughput = (float)aggNumCommitted * 1000 / (float)(aggEndTime - aggStartTime);
                throughPutAccumulator.Add(committedTxnThroughput);
                float abortRate = (float)(aggNumTransactions - aggNumCommitted) * 100 / (float)aggNumTransactions;
                abortRateAccumulator.Add(abortRate);
                double actualQueryRate = (double)aggNumQuery / (double)(aggNumQuery + aggNumUpdate);
                aggActualQueryrates.Add(actualQueryRate);

               
                var exceptionSum = aggSubscribeExceptionCount + aggInfoUpdateExceptionCount + aggmovingActorExceptionCount +aggreadTjyExceptionCount+aggOtherExceptionCount;
                float subscribeExceptionRate = (float)aggSubscribeExceptionCount / (float)exceptionSum;
                float infoUpdateExceptionRate = (float)aggInfoUpdateExceptionCount / (float)exceptionSum;
                float movingActorExceptionRate=(float)aggmovingActorExceptionCount/ (float)exceptionSum;
                float readTjyExceptionRate= (float)aggreadTjyExceptionCount / (float)exceptionSum;
                float otherExceptionRate = (float)aggOtherExceptionCount / (float)exceptionSum;
                Console.WriteLine("exceptionSum="+exceptionSum);
                subsribeExceptionAccumulator.Add(subscribeExceptionRate);
                infoUpdateExceptionAccumulator.Add(infoUpdateExceptionRate);
                movingActorExceptionAccumulator.Add(movingActorExceptionRate);
                readTjyExceptionAccumulator.Add(readTjyExceptionRate);
                otherExceptionAccumulator.Add(otherExceptionRate);
            }

            //Compute statistics on the accumulators, maybe a better way is to maintain a sorted list
            var latencyMeanAndSd = ArrayStatistics.MeanStandardDeviation(aggLatencies.ToArray());
            var throughputMeanAndSd = ArrayStatistics.MeanStandardDeviation(throughPutAccumulator.ToArray());
            var abortRateMeanAndSd = ArrayStatistics.MeanStandardDeviation(abortRateAccumulator.ToArray());
            var actualQueryrateMeanAndSd = ArrayStatistics.MeanStandardDeviation(aggActualQueryrates.ToArray());
            var subsribeExceptionMeanAndSd = ArrayStatistics.MeanStandardDeviation(subsribeExceptionAccumulator.ToArray());
            var infoUpdateExceptionMeanAndSd = ArrayStatistics.MeanStandardDeviation(infoUpdateExceptionAccumulator.ToArray());
            var movingActorExceptionMeanAndSd = ArrayStatistics.MeanStandardDeviation(movingActorExceptionAccumulator.ToArray());
            var readTjyExceptionMeanAndSd = ArrayStatistics.MeanStandardDeviation(readTjyExceptionAccumulator.ToArray());
            var otherExceptionMeanAndSd = ArrayStatistics.MeanStandardDeviation(otherExceptionAccumulator.ToArray());
            resultString.Append($"Actual Query Rate, {actualQueryrateMeanAndSd.Item1}, standard deviation, { actualQueryrateMeanAndSd.Item2}\r\n");
            resultString.Append($"Finished Task Number, {aggLatencies.Count}, Maxium Lantency (ms), { maxLatency} at epoch {maxLatencyEpoch}\r\n");
            resultString.Append($"Task Mean Lantency (ms), { latencyMeanAndSd.Item1}, standard deviation, { latencyMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Task Throughput per second, { throughputMeanAndSd.Item1}, standard deviation, { throughputMeanAndSd.Item2}\r\nMean Abort rate (%), { abortRateMeanAndSd.Item1}, standard deviation, { abortRateMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Move SubscribeUpdateException Rate (%), { subsribeExceptionMeanAndSd.Item1 * 100}, standard deviation, { subsribeExceptionMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Move SnapshotBufferedDataUpdateException Rate (%), { infoUpdateExceptionMeanAndSd.Item1 * 100}, standard deviation, { infoUpdateExceptionMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Move Moving Actor Exception Rate (%), { movingActorExceptionMeanAndSd.Item1 * 100}, standard deviation, { movingActorExceptionMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean ReadTjyException Rate (%), { readTjyExceptionMeanAndSd.Item1 * 100}, standard deviation, { readTjyExceptionMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Move Other Exception Rate (%), { otherExceptionMeanAndSd.Item1*100}, standard deviation, { otherExceptionMeanAndSd.Item2}\r\n");
            foreach (var percentile in workload.percentilesToCalculate)
            {
                var lat = ArrayStatistics.PercentileInplace(aggLatencies.ToArray(), percentile);
                //Console.Write($", {percentile} = {lat}");
                resultString.Append($"{percentile} percentile, {lat}, ");
            }

            var date = DateTime.Now.ToString("d", System.Globalization.CultureInfo.CreateSpecificCulture("de-DE"));
            using (StreamWriter w = File.AppendText($"results_{workload.semantics}_{workload.benchmarktype}_{workload.distribution}_{date}.txt"))
            {
                w.WriteLine(resultString.ToString());
            }
            Console.WriteLine();
        }

        private static void WaitForWorkerAcksAndReset()
        {
            ackedWorkers.Wait();
            ackedWorkers.Reset(numWorkerNodes); //Reset for next ack, not thread-safe but provides visibility, ok for us to use due to lock-stepped (distributed producer/consumer) usage pattern i.e., Reset will never called concurrently with other functions (Signal/Wait)            
        }

        private static void WaitForRecordAcksAndReset()
        {
            ackedRecord.Wait();
            ackedRecord.Reset(1);
        }

        private static void WaitForLatenciesAcksAndReset()
        {
            ackedLatencies.Wait();
            ackedLatencies.Reset(1);
        }

        private static void WaitForWriteAcksAndReset()
        {
            ackedWrite.Wait();
            ackedWrite.Reset(1);
        }

        static async void PushToWorkers()
        {
            // Task Ventilator
            // Binds PUSH socket to tcp://*:5557
            // Sends batch of tasks to workers via that socket
            Console.WriteLine("====== PUSH TO WORKERS ======");
            resultString = new StringBuilder();
            resultString.Append($"\r\nMovingActorNum, {workload.movingActorIds.Count}, Query Rate, {workload.queryRate}, Reactive Sensing Rate, {reativeSensingRate}, Snapshot interval time, {INTERVAL}, Hotspots, {HOTSPOTNUM}\r\n");

            using var worker = new PublisherSocket(workerAddress);
            //Wait for the workers to connect to controller
            WaitForWorkerAcksAndReset();
            Console.WriteLine($"{numWorkerNodes} worker nodes have connected to Controller");

            //Send the workload configuration
            Console.WriteLine($"Sent workload configuration to {numWorkerNodes} worker nodes");
            var msg = new NetworkMessageWrapper(Dolphin.Utilities.MsgType.WORKLOAD_INIT)
            {
                contents = Helper.serializeToByteArray<WorkloadConfiguration>(workload)
            };
            worker.SendMoreFrame("WORKLOAD_INIT").SendFrame(Helper.serializeToByteArray<NetworkMessageWrapper>(msg));
            // worker.SendFrame(Helper.serializeToByteArray<NetworkMessageWrapper>(msg));
            //Wait for acks for the workload configuration
            WaitForWorkerAcksAndReset();
            Console.WriteLine($"Receive workload configuration ack froms {numWorkerNodes} worker nodes");

            var globalWatch = new Stopwatch();
            globalWatch.Start();
            var tickTimes = new List<double>();

            var reactionThroughtput = new List<double>();
            var reactionLatencies = new List<double>();
            var reactionNumAndLatencies = new List<List<Tuple<long,double>>>();

            var warmUpLatenciesEpoch = new List<Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>>();
            var warmUpSubscribeTaskNumEpoch = new List<int>();

            var queryLatencies = new List<double>();
            var updateLatencies = new List<double>();
            var indexUpdateLatencies = new List<double>();
            var monitorSendLatencies = new List<double>();
            var updateFenceLatencies = new List<double>();
            var updateSubscribeLatencies = new List<double>();
            var subscribeTasksLatencies = new List<double>();
            var numUpdateSubTasks = 0;
            for (int i = 0; i < workload.numEpochs; i++)
            {
                //Send the command to run an epoch
                Console.WriteLine("---------------------------------");
                Console.WriteLine($"Running Epoch {i} on {numWorkerNodes} worker nodes");
                msg = new NetworkMessageWrapper(Dolphin.Utilities.MsgType.RUN_EPOCH);
                worker.SendMoreFrame("RUN_EPOCH").SendFrame(Helper.serializeToByteArray<NetworkMessageWrapper>(msg));
                //worker.SendFrame(Helper.serializeToByteArray<NetworkMessageWrapper>(msg));
                WaitForWorkerAcksAndReset();

                if (reativeSensingRate > 0 && workload.queryRate < 1)
                {
                    var tasks = new List<Task<List<Tuple<long, double>>>>();
                    foreach (var id in workload.movingActorIds)
                        tasks.Add(client.GetGrain<IAppDefMovingActor>(id).GetReactionNumAndLatencies());
                    var tmp_reactionNumAndLatencies = (await Task.WhenAll(tasks)).ToList();
                    ackedRecord.Signal();

                    var singleEpoch_reactionNumAndLatencies = new List<Tuple<long, double>>();
                    foreach (var item in tmp_reactionNumAndLatencies)
                        singleEpoch_reactionNumAndLatencies.AddRange(item);

                    reactionNumAndLatencies.Add(singleEpoch_reactionNumAndLatencies);

                    if (i > numWarmupEpoch - 1)
                    {
                        var thisEpoch_reactionNumAndLatencies = reactionNumAndLatencies[i].Except(reactionNumAndLatencies[i - 1]);
                        reactionThroughtput.Add(thisEpoch_reactionNumAndLatencies.Count() / (workload.epochDurationMSecs / 1000));
                        foreach(var item in thisEpoch_reactionNumAndLatencies)
                            reactionLatencies.Add(item.Item2);
                    }
                }
               
                if (i == numWarmupEpoch - 1)
                {
                    var latencyTasks = new List<Task<Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>>>();
                    var updateSubscribeTasks = new List<Task<int>>();
                    foreach (var id in workload.movingActorIds)
                    {
                        latencyTasks.Add(client.GetGrain<IAppDefMovingActor>(id).GetBreakDownLatencies());
                        updateSubscribeTasks.Add(client.GetGrain<IAppDefMovingActor>(id).GetSubscribeTaskNum());
                    }
                    warmUpLatenciesEpoch = (await Task.WhenAll(latencyTasks)).ToList();
                    warmUpSubscribeTaskNumEpoch = (await Task.WhenAll(updateSubscribeTasks)).ToList();
                    ackedLatencies.Signal();
                }
                else if (i == workload.numEpochs - 1)
                {
                    var latencyTasks = new List<Task<Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>>>();
                    var updateSubscribeTasks = new List<Task<int>>();
                    foreach (var id in workload.movingActorIds)
                    {
                        latencyTasks.Add(client.GetGrain<IAppDefMovingActor>(id).GetBreakDownLatencies());
                        updateSubscribeTasks.Add(client.GetGrain<IAppDefMovingActor>(id).GetSubscribeTaskNum());
                    }
                    var latenciesEpoch = (await Task.WhenAll(latencyTasks)).ToList();
                    var subscribeTaskNumEpoch = (await Task.WhenAll(updateSubscribeTasks)).ToList();
                    ackedLatencies.Signal();

                    var usefulLatencies = latenciesEpoch.Except(warmUpLatenciesEpoch);
                    foreach (var item in usefulLatencies)
                    {
                        queryLatencies.AddRange(item.Item1);
                        updateLatencies.AddRange(item.Item2);
                        indexUpdateLatencies.AddRange(item.Item3);
                        monitorSendLatencies.AddRange(item.Item4);
                        updateFenceLatencies.AddRange(item.Item5);
                        updateSubscribeLatencies.AddRange(item.Item6);
                        subscribeTasksLatencies.AddRange(item.Item7);
                    }

                    numUpdateSubTasks = subscribeTaskNumEpoch.Sum() - warmUpSubscribeTaskNumEpoch.Sum();
                }

                Console.WriteLine($"Finished running epoch {i} on {numWorkerNodes} worker nodes");
            }

            double maxQueryLatency = Double.MinValue, maxUpdateLatency = Double.MinValue, maxUpdateSubscribeLatency = Double.MinValue, maxSubsribeTaskLatency = Double.MinValue;
            if (queryLatencies.Count != 0)
                maxQueryLatency = queryLatencies.Max();
            if (updateLatencies.Count != 0)
                maxUpdateLatency = updateLatencies.Max();
            if (updateSubscribeLatencies.Count != 0)
                maxUpdateSubscribeLatency = updateSubscribeLatencies.Max();
            if (subscribeTasksLatencies.Count != 0)
                maxSubsribeTaskLatency = subscribeTasksLatencies.Max();

            var NotfiThptMeanAndSd = ArrayStatistics.MeanStandardDeviation(reactionThroughtput.ToArray());
            var reactionLatencyMeanAndSd = ArrayStatistics.MeanStandardDeviation(reactionLatencies.ToArray());
            var queryLateniesMeanAndSd = ArrayStatistics.MeanStandardDeviation(queryLatencies.ToArray());
            var updateLateniesMeanAndSd = ArrayStatistics.MeanStandardDeviation(updateLatencies.ToArray());
            var indexUpdateLatenciesMeanAndSd = ArrayStatistics.MeanStandardDeviation(indexUpdateLatencies.ToArray());
            var monitorSendLatenciesMeanAndSd = ArrayStatistics.MeanStandardDeviation(monitorSendLatencies.ToArray());
            var updateFenceLatenciesMeanAndSd = ArrayStatistics.MeanStandardDeviation(updateFenceLatencies.ToArray());
            var updateSubscribeLatenciesMeanAndSd = ArrayStatistics.MeanStandardDeviation(updateSubscribeLatencies.ToArray());
            var subscribeTaskLatenciesMeanAndSd = ArrayStatistics.MeanStandardDeviation(subscribeTasksLatencies.ToArray());

            resultString.Append($"Mean Reaction Throughput per second, {NotfiThptMeanAndSd.Item1}, standard deviation, {NotfiThptMeanAndSd.Item2}\r");
            resultString.Append($"Mean Reaction Lantency (ms), { reactionLatencyMeanAndSd.Item1}, standard deviation, { reactionLatencyMeanAndSd.Item2}\r\n");
            foreach (var percentile in workload.percentilesToCalculate)
            {
                var lat = ArrayStatistics.PercentileInplace(reactionLatencies.ToArray(), percentile);
                resultString.Append($"{percentile} percentile, {lat}, ");
            }
            resultString.Append("\r\n");
            resultString.Append($"Mean Query Lantency (ms), { queryLateniesMeanAndSd.Item1}, standard deviation, { queryLateniesMeanAndSd.Item2}, Max Query Latency (ms),{maxQueryLatency}\r\n");
            resultString.Append($"Mean Update Lantency (ms), {  updateLateniesMeanAndSd.Item1}, standard deviation, {  updateLateniesMeanAndSd.Item2}, Max Update Latency (ms),{maxUpdateLatency}\r\n");
            resultString.Append($"Mean Index Update Lantency (ms), { indexUpdateLatenciesMeanAndSd.Item1}, standard deviation, { indexUpdateLatenciesMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Send Monitor Info Lantency (ms), {  monitorSendLatenciesMeanAndSd.Item1}, standard deviation, {  monitorSendLatenciesMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Fence Update Lantency (ms), { updateFenceLatenciesMeanAndSd.Item1}, standard deviation, { updateFenceLatenciesMeanAndSd.Item2}\r\n");
            resultString.Append($"Mean Subscribe Update Lantency for all moving actors (ms), {  updateSubscribeLatenciesMeanAndSd.Item1}, standard deviation, {  updateSubscribeLatenciesMeanAndSd.Item2}, Max Update Subsribe Latency (ms),{maxUpdateSubscribeLatency}\r\n");
            resultString.Append($"Mean subscribe Update Lantency for reactive moving actors (ms), { subscribeTaskLatenciesMeanAndSd.Item1}, standard deviation, { subscribeTaskLatenciesMeanAndSd.Item2}, Max Update Subsribe Latency (ms),{maxSubsribeTaskLatency}\r\n");
            resultString.Append($"Update Subscribe Tasks, {numUpdateSubTasks}\r\n");
            ackedWrite.Signal();
            globalWatch.Stop();
        }

        static void PullFromWorkers()
        {
            // Task Sink        
            // Bind PULL socket to tcp://localhost:5558   
            // Collects results from workers via that socket      
            Console.WriteLine("====== PULL FROM WORKERS ======");

            results = new WorkloadResults[workload.numEpochs, numWorkerNodes];
            //socket to receive results on
            using var sink = new PullSocket(sinkAddress);
            for (int i = 0; i < numWorkerNodes; i++)
            {
                var msg = Helper.deserializeFromByteArray<NetworkMessageWrapper>(sink.ReceiveFrameBytes());
                Trace.Assert(msg.msgType == Dolphin.Utilities.MsgType.WORKER_CONNECT);
                ackedWorkers.Signal();
            }

            for (int i = 0; i < numWorkerNodes; i++)
            {
                var msg = Helper.deserializeFromByteArray<NetworkMessageWrapper>(sink.ReceiveFrameBytes());
                Trace.Assert(msg.msgType == Dolphin.Utilities.MsgType.WORKLOAD_INIT_ACK);
                ackedWorkers.Signal();
            }

            //Wait for epoch acks 
            for (int i = 0; i < workload.numEpochs; i++)
            {
                for (int j = 0; j < numWorkerNodes; j++)
                {
                    var msg = Helper.deserializeFromByteArray<NetworkMessageWrapper>(sink.ReceiveFrameBytes());
                    Trace.Assert(msg.msgType == Dolphin.Utilities.MsgType.RUN_EPOCH_ACK);
                    results[i, j] = Helper.deserializeFromByteArray<WorkloadResults>(msg.contents);
                    ackedWorkers.Signal();
                }

                if (reativeSensingRate > 0 && workload.queryRate < 1)
                    WaitForRecordAcksAndReset();
                if (i == numWarmupEpoch - 1 || i == workload.numEpochs - 1)
                    WaitForLatenciesAcksAndReset();
            }
        }

        private static async void InitiateClientAndSpawnConfigurationCoordinator()
        {
            //Spawn the configuration grain
            if (client == null)
            {
                Process.ClientConfiguration config = new Process.ClientConfiguration();

                if (LocalCluster)
                    client = await config.StartClient();
                else
                    client = await config.StartClientToCluster();
            }

            asyncInitializationDone = true;
        }

        private static async void LoadGrains()
        {
            if (reativeSensingRate > 0)
            {
                var initializeMonitorTasks = new List<Task>();
                for (int i = 0; i < CELLNUM; i++)
                    initializeMonitorTasks.Add(client.GetGrain<IMonitoring>(i).BecomeProducer(Helper.ConvertIntToGuid(i)));
                //for (var monitoringId = i * NUMMONITORACTORSPERCELL; monitoringId < (i + 1) * NUMMONITORACTORSPERCELL; monitoringId++)
                //        initializeMonitorTasks.Add(client.GetGrain<IMonitoring>(monitoringId).BecomeProducer(Helper.ConvertIntToGuid(monitoringId)));
                await Task.WhenAll(initializeMonitorTasks);
            }

            if (workload.semantics == Semantics.Snapshot)
            {
                var snapshotIniTasks = new List<Task>();
                for (var i = 0; i < CELLNUM; i++)
                    snapshotIniTasks.Add(client.GetGrain<ISnapshotUpdate>(i).Initialize(i, workload.BOARDERS, CELLSIZE));
                await Task.WhenAll(snapshotIniTasks);
            }

            DateTime grainsLoadStart = DateTime.Now;
            var CellMovingActors = new Dictionary<int, List<Guid>>();
            switch (workload.benchmarktype)
            {
                case BenchmarkType.SYNTHETIC:
                    switch (workload.distribution)
                    {
                        case Distribution.UNIFORM:
                            CellMovingActors= await UniformActors(client);
                            break;
                        case Distribution.GAUSS:
                            CellMovingActors =await GaussActors(client);
                            break;
                        default:
                            throw new Exception("Unknown sythetic benchmark distribution type.");
                    }
                    break;
                case BenchmarkType.SIMULATION:
                    CellMovingActors = await RoadNetworkActors(client);
                    break;
                default:
                    throw new Exception("Unknown benchmark type");
            }

            if (workload.semantics == Semantics.Snapshot)
            {
                var fullCellIds = new List<int>();
                for (var i = 0; i < CELLNUM; i++)
                    fullCellIds.Add(i);

                var addMovingActorsTasks = new List<Task>();
                foreach (var item in CellMovingActors)
                    addMovingActorsTasks.Add(client.GetGrain<ISnapshotUpdate>(item.Key).AddMovingActors(item.Value));
                await Task.WhenAll(addMovingActorsTasks);

                await client.GetGrain<ISnapshotController>(0).Initialize(CellMovingActors.Keys.ToList(), fullCellIds);

                var initTime = DateTime.Now;
                var setTimerTasks = new List<Task>();
                foreach (var id in workload.movingActorIds)
                    setTimerTasks.Add(client.GetGrain<IAppDefMovingActor>(id).SetTimer(INTERVAL, initTime));
                await Task.WhenAll(setTimerTasks);
            }

            loadingDone = true;
            DateTime grainsLoadEnd = DateTime.Now;
            Console.WriteLine($"\nInitialized {workload.movingActorIds.Count } {workload.benchmarktype} {workload.distribution} distributed actors that will be operated under {workload.semantics}, cost of time: {(grainsLoadEnd - grainsLoadStart).TotalMilliseconds} ms.\n");

            ackedRecord = new CountdownEvent(1);
            ackedLatencies = new CountdownEvent(1);
            ackedWrite = new CountdownEvent(1);
        }

        private static async Task<Dictionary<int, List<Guid>>> UniformActors(IClusterClient client)
        {
            directoryPath += MOVINGACTORNUM switch
            {
                Constants.MOVINGACTOR_NUM_TEST => Constants.UNIFORM_TEST,
                Constants.MOVINGACTOR_NUM0 => Constants.UNIFORM_0,
                Constants.MOVINGACTOR_NUM1 => Constants.UNIFORM_1,
                Constants.MOVINGACTOR_NUM2 => Constants.UNIFORM_2,
                Constants.MOVINGACTOR_NUM3 => Constants.UNIFORM_3,
                _ => Constants.UNIFORM_TEST,
            };

            var CellMovingActors = new Dictionary<int, List<Guid>>();
            using (StreamReader r = new StreamReader(directoryPath))
            {
                var actorArray = JsonConvert.DeserializeObject<List<UniformActorData>>(r.ReadToEnd());
                var tasks = new List<Task>();
                foreach (var actor in actorArray)
                {
                    workload.movingActorIds.Add(actor.id);
                    var lct = new Point(actor.x, actor.y);
                    var fence = new Polygon(new LinearRing(new Coordinate[] {
                        new Coordinate(actor.minX, actor.minY),
                        new Coordinate(actor.minX, actor.maxY),
                        new Coordinate(actor.maxX, actor.maxY),
                        new Coordinate(actor.maxX, actor.minY),
                        new Coordinate(actor.minX, actor.minY)
                    }));
                    var movingActor = client.GetGrain<IAppDefMovingActor>(actor.id);
                    await movingActor.Initialize(lct, fence, workload.semantics, workload.BOARDERS, CELLSIZE);
                    workload.MovingActorInfo.Add(new Tuple<Guid, Tuple<Point, DateTime>>(actor.id, new Tuple<Point, DateTime>(lct, DateTime.MaxValue)));

                    if (random.NextDouble() < reativeSensingRate)
                        tasks.Add(movingActor.Subscribe(Predicates.Cross));

                    var cellId = Helper.CalCellId(lct,workload.BOARDERS,CELLSIZE);
                    if (CellMovingActors.ContainsKey(cellId))
                        CellMovingActors[cellId].Add(actor.id);
                    else
                        CellMovingActors.Add(cellId, new List<Guid> { actor.id });
                }
                await Task.WhenAll(tasks);
            }
            return CellMovingActors;
        }

        private static async Task<Dictionary<int, List<Guid>>> GaussActors(IClusterClient client)
        {
            var dir = HOTSPOTNUM switch
            {
                Constants.HOTSPOT_NUM0 => directoryPath + Constants.GAUSS_0,
                Constants.HOTSPOT_NUM1 => directoryPath + Constants.GAUSS_1,
                Constants.HOTSPOT_NUM2 => directoryPath + Constants.GAUSS_2,
                Constants.HOTSPOT_NUM3 => directoryPath + Constants.GAUSS_3,
                Constants.HOTSPOT_NUM4 => directoryPath + Constants.GAUSS_4,
                Constants.HOTSPOT_NUM5 => directoryPath + Constants.GAUSS_5,
                _ => directoryPath + Constants.GAUSS_0,
            };

            var CellMovingActors = new Dictionary<int, List<Guid>>();
            using (StreamReader r = new StreamReader(dir))
            {
                var actorArray = JsonConvert.DeserializeObject<List<GaussActorData>>(r.ReadToEnd());
                var tasks = new List<Task>();
                foreach (var actor in actorArray)
                {
                   // workload.MA2HS.Add(actor.id, new Point(actor.hotspotX, actor.hotspotY));
                    workload.movingActorIds.Add(actor.id);
                    var lct = new Point(actor.x, actor.y);
                    var fence = new Polygon(new LinearRing(new Coordinate[] {
                        new Coordinate(actor.minX, actor.minY),
                        new Coordinate(actor.minX,actor.maxY),
                        new Coordinate(actor.maxX, actor.maxY),
                        new Coordinate(actor.maxX,actor.minY),
                        new Coordinate(actor.minX, actor.minY)
                    }));

                    var movingActor = client.GetGrain<IAppDefMovingActor>(actor.id);
                    await movingActor.Initialize(lct, fence, workload.semantics, workload.BOARDERS, CELLSIZE);
                    workload.MovingActorInfo.Add(new Tuple<Guid, Tuple<Point, DateTime>>(actor.id, new Tuple<Point, DateTime>(lct, DateTime.MaxValue)));

                    if (random.NextDouble() < reativeSensingRate)
                        tasks.Add(movingActor.Subscribe(Predicates.Cross));

                    var cellId = Helper.CalCellId(lct, workload.BOARDERS, CELLSIZE);
                    if (CellMovingActors.ContainsKey(cellId))
                        CellMovingActors[cellId].Add(actor.id);
                    else
                        CellMovingActors.Add(cellId, new List<Guid> { actor.id });
                }
                await Task.WhenAll(tasks);
            }

            var tjyDir = HOTSPOTNUM switch
            {
                Constants.HOTSPOT_NUM0 => directoryPath + Constants.GAUSS_TJY_0,
                Constants.HOTSPOT_NUM1 => directoryPath + Constants.GAUSS_TJY_1,
                Constants.HOTSPOT_NUM2 => directoryPath + Constants.GAUSS_TJY_2,
                Constants.HOTSPOT_NUM3 => directoryPath + Constants.GAUSS_TJY_3,
                Constants.HOTSPOT_NUM4 => directoryPath + Constants.GAUSS_TJY_4,
                Constants.HOTSPOT_NUM5 => directoryPath + Constants.GAUSS_TJY_5,
                _ => directoryPath + Constants.GAUSS_TJY_0,
            };

            using (StreamReader r = new StreamReader(tjyDir))
            {
                var actorTjyArray = JsonConvert.DeserializeObject<List<ActorTjyData>>(r.ReadToEnd());
                var tasks = new List<Task>();
                foreach (var actor in actorTjyArray)
                    if (!workload.ActorTjy.ContainsKey(actor.id))
                        workload.ActorTjy.Add(actor.id, new List<Point> { new Point(actor.x, actor.y) });
                    else
                        workload.ActorTjy[actor.id].Add(new Point(actor.x, actor.y));
            }

            return CellMovingActors;
        }

        private static async Task<Dictionary<int, List<Guid>>> RoadNetworkActors(IClusterClient client)
        {
            var dir = directoryPath + Constants.ROADNETWORKACTOR;
            var CellMovingActors = new Dictionary<int, List<Guid>>();
            using (StreamReader r = new StreamReader(dir))
            {
                var actorArray = JsonConvert.DeserializeObject<List<RoadNetworkActorData>>(r.ReadToEnd());
                var tasks = new List<Task>();

                foreach (var actor in actorArray)
                {
                    workload.movingActorIds.Add(actor.id);
                    var lct = new Point(actor.x, actor.y);
                    var fence = new Polygon(new LinearRing(new Coordinate[] {
                        new Coordinate(actor.minX, actor.minY),
                        new Coordinate(actor.minX, actor.maxY),
                        new Coordinate(actor.maxX, actor.maxY),
                        new Coordinate(actor.maxX, actor.minY),
                        new Coordinate(actor.minX, actor.minY)
                    }));

                    var movingActor = client.GetGrain<IAppDefMovingActor>(actor.id);
                    await movingActor.Initialize(lct, fence, workload.semantics, workload.BOARDERS, CELLSIZE);
                    workload.MovingActorInfo.Add(new Tuple<Guid, Tuple<Point, DateTime>>(actor.id, new Tuple<Point, DateTime>(lct, DateTime.MaxValue)));

                    if (random.NextDouble() < reativeSensingRate)
                        tasks.Add(movingActor.Subscribe(Predicates.Cross));

                    var cellId = Helper.CalCellId(lct, workload.BOARDERS, CELLSIZE);
                    if (CellMovingActors.ContainsKey(cellId))
                        CellMovingActors[cellId].Add(actor.id);
                    else
                        CellMovingActors.Add(cellId, new List<Guid> { actor.id });
                }
                await Task.WhenAll(tasks);
            }

            dir = directoryPath + "/actortjy.json";
            using (StreamReader r = new StreamReader(dir))
            {
                var actorTjyArray = JsonConvert.DeserializeObject<List<ActorTjyData>>(r.ReadToEnd());
                var tasks = new List<Task>();

                var initTime = DateTime.Now;

                foreach (var actor in actorTjyArray)
                    if (!workload.ActorTjy.ContainsKey(actor.id))
                        workload.ActorTjy.Add(actor.id, new List<Point> { new Point(actor.x, actor.y) });
                    else
                        workload.ActorTjy[actor.id].Add(new Point(actor.x, actor.y));
            }

            return CellMovingActors;
        }

        private static void GetWorkloadSettings(char benchmark, char semantics, int actorNum, double queryRate, double reaSensingRate,  double interval, int hotspotNum)
        {
            workload = new WorkloadConfiguration();
            GenerateWorkLoadFromSettingsFile(benchmark, semantics, actorNum, queryRate, reaSensingRate,  interval, hotspotNum);

            try
            {
                ackedWorkers = new CountdownEvent(numWorkerNodes);
            }
            catch (Exception e) { throw new Exception($"xx, {e}"); }
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: start dotnet run --project [FileLocation]\\Dolphin-Orleans\\Experiment.Controller\\Experiment.Controller.csProj  [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        //cellsize querysize queryrate actornumber maxdistance boarder hotspotnum
        static int Main(string[] args)
        {
            bool show_help = false;

            char benchmark = 'u';
            char semantics = 'f';
            int actorNum = 0;
            double queryRate = 0.0;
            double reaSensing = 0.0;
           
            double interval = 1;
            int hotspotNum = 0;

            var p = new OptionSet() {
                { "b|benchmark=", "the {benchmark}, u for Uniform benchmark, g for Gaussian benchmark, c for C-ITS simulation benchmark, default u.",(char v) => benchmark=v },
                { "s|semantics=", "the {semantics}, f for Freshness semantics, s for Snapshot semantics, default f.",(char v) => semantics=v },
                { "a|actornum=", "the {moving actor number}, default 0.",(int v) => actorNum=v },
                { "q|queryrate=", "the {query rate}, default 0.0.",(double v) => queryRate=v },
                { "rs|reactivesensing=", "the {reactive sensing rate}, default 0.0.",(double v) => reaSensing=v },
                { "itl|interval=", "the {snapshot interval time}, default 1.",(double v) => interval=v},//interval(seconds) supposed to be bigger than snapshot update process time
                { "hn|hotspotnum=", "the {hotspots number}, default 0.",(int v) => hotspotNum=v },
                { "h|help",  "show this message and exit",v => show_help = v != null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `start dotnet run --project [FileLocation]\\Dolphin-Orleans\\Experiment.Controller\\Experiment.Controller.csProj --help' for more information.");
                return 1;
            }

            if (interval.CompareTo(0) < 0 || interval.CompareTo(59) > 0)
            {
                Console.WriteLine("0 < itl|interval <59.");
                return 1;
            }

            if(!benchmark.Equals('u')&&!benchmark.Equals('g') &&!benchmark.Equals('c'))
            {
                Console.WriteLine("u for Uniform benchmark, g for Gaussian benchmark, c for C-ITS simulation benchmark, default u.");
                return 1;
            }

            if (!semantics.Equals('f') && !semantics.Equals('s'))
            {
                Console.WriteLine("f for Freshness semantics, s for Snapshot semantics, default f.");
                return 1;
            }

            if (show_help)
            {
                ShowHelp(p);
                return 0;
            }

            GetWorkloadSettings(benchmark, semantics,actorNum, queryRate, reaSensing, interval, hotspotNum);

            //Initialize the client to silo cluster, create configurator grain
            InitiateClientAndSpawnConfigurationCoordinator();
            while (!asyncInitializationDone)
                Thread.Sleep(100);

            //Create the workload grains, load with data
            LoadGrains();
            while (!loadingDone)
                Thread.Sleep(100);

            //Start the controller thread
            Thread conducterThread = new Thread(PushToWorkers);
            conducterThread.Start();

            //Start the sink thread
            Thread sinkThread = new Thread(PullFromWorkers);
            sinkThread.Start();

            sinkThread.Join();
            conducterThread.Join();

            WaitForWriteAcksAndReset();
            Console.WriteLine("\nAggregating results and printing");
            AggregateResultsAndPrint();
            Console.WriteLine("Finished running experiment. Press Enter to exit");
            //Console.ReadLine();
            return 0;
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UniformActorData
    {
        [JsonProperty(PropertyName = "id")]
        public Guid id { set; get; }
        [JsonProperty(PropertyName = "x")]
        public double x { set; get; }
        [JsonProperty(PropertyName = "y")]
        public double y { set; get; }
        [JsonProperty(PropertyName = "minX")]
        public double minX { set; get; }
        [JsonProperty(PropertyName = "minY")]
        public double minY { set; get; }
        [JsonProperty(PropertyName = "maxX")]
        public double maxX { set; get; }
        [JsonProperty(PropertyName = "maxY")]
        public double maxY { set; get; }

        [JsonConstructor]
        public UniformActorData(Guid id, double x, double y, double minX, double minY, double maxX, double maxY)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GaussActorData
    {
        [JsonProperty(PropertyName = "id")]
        public Guid id { set; get; }
        [JsonProperty(PropertyName = "x")]
        public double x { set; get; }
        [JsonProperty(PropertyName = "y")]
        public double y { set; get; }
        [JsonProperty(PropertyName = "minX")]
        public double minX { set; get; }
        [JsonProperty(PropertyName = "minY")]
        public double minY { set; get; }
        [JsonProperty(PropertyName = "maxX")]
        public double maxX { set; get; }
        [JsonProperty(PropertyName = "maxY")]
        public double maxY { set; get; }
        [JsonProperty(PropertyName = "hotspotX")]
        public double hotspotX { set; get; }
        [JsonProperty(PropertyName = "hotspotY")]
        public double hotspotY { set; get; }

        [JsonConstructor]
        public GaussActorData(Guid id, double x, double y, double minX, double minY, double maxX, double maxY, double hotspotX, double hotspotY)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            this.hotspotX = hotspotX;
            this.hotspotY = hotspotY;
        }
    }

    //[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    //public class RoadNetworkData
    //{
    //    [JsonProperty(PropertyName = "minX")]
    //    public double minX { set; get; }
    //    [JsonProperty(PropertyName = "maxX")]
    //    public double maxX { set; get; }
    //    [JsonProperty(PropertyName = "minY")]
    //    public double minY { set; get; }
    //    [JsonProperty(PropertyName = "maxY")]
    //    public double maxY { set; get; }

    //    [JsonConstructor]
    //    public RoadNetworkData(double minX, double maxX, double minY, double maxY)
    //    {
    //        this.minX = minX;
    //        this.maxX = maxX;
    //        this.minY = minY;
    //        this.maxY = maxY;
    //    }
    //}


    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RoadNetworkActorData
    {
        [JsonProperty(PropertyName = "id")]
        public Guid id { set; get; }
        [JsonProperty(PropertyName = "x")]
        public double x { set; get; }
        [JsonProperty(PropertyName = "y")]
        public double y { set; get; }
        [JsonProperty(PropertyName = "minX")]
        public double minX { set; get; }
        [JsonProperty(PropertyName = "minY")]
        public double minY { set; get; }
        [JsonProperty(PropertyName = "maxX")]
        public double maxX { set; get; }
        [JsonProperty(PropertyName = "maxY")]
        public double maxY { set; get; }
        [JsonProperty(PropertyName = "polyId")]
        public int polyId { set; get; }

        [JsonConstructor]
        public RoadNetworkActorData(Guid id, double x, double y, double minX, double minY, double maxX, double maxY, int polyId)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
            this.polyId = polyId;
        }
    }


    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ActorTjyData
    {
        [JsonProperty(PropertyName = "id")]
        public Guid id { set; get; }
        [JsonProperty(PropertyName = "x")]
        public double x { set; get; }
        [JsonProperty(PropertyName = "y")]
        public double y { set; get; }

        [JsonConstructor]
        public ActorTjyData(Guid id, double x, double y)
        {
            this.id = id;
            this.x = x;
            this.y = y;
        }
    }

    //[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    //public class RoadNetwork
    //{
    //    [JsonProperty(PropertyName = "points")]
    //    public readonly List<XY> points = new List<XY>();
    //    [JsonProperty(PropertyName = "polyToPoint")]
    //    public readonly Dictionary<int, List<XY>> polyToPoints = new Dictionary<int, List<XY>>();
    //    [JsonProperty(PropertyName = "pointToPoly")]
    //    public readonly Dictionary<XY, List<int>> pointToPolys = new Dictionary<XY, List<int>>();
    //    public readonly double[] boarders = new double[] { };

    //    [JsonConstructor]
    //    public RoadNetwork(List<XY> points, Dictionary<int, List<XY>> polyToPoint, Dictionary<XY, List<int>> pointToPoly, double[] boarders)
    //    {
    //        this.points = points;
    //        this.polyToPoints = polyToPoint;
    //        this.pointToPolys = pointToPoly;
    //        this.boarders = boarders;
    //    }
    //}

    //[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    //public class XY {
    //    [JsonProperty(PropertyName = "X")]
    //    public double X;
    //    [JsonProperty(PropertyName = "Y")]
    //    public double Y;

    //    [JsonConstructor]
    //    public XY(double x, double y) {
    //        X = x;
    //        Y = y;
    //    }
    //}

    [JsonObject]
    public class RoadData
    {
        [JsonProperty("data")]
        public string[][] data { set; get; }
    }


}

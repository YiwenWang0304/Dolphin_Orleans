using Dolphin.Interfaces;
using Dolphin.Utilities;
using NetMQ;
using NetMQ.Sockets;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MsgType = Dolphin.Utilities.MsgType;

namespace Experiment.Process
{
    class Program
    {
        static Boolean LocalCluster;
        static Boolean LocalExperiment;
        static IClusterClient[] clients;
        static String sinkAddress = ">tcp://18.185.139.91:5558";//ControlerIP:5558
        static String controllerAddress = ">tcp://18.185.139.91:5557";//ControlerIP:5558

        static PushSocket sink ;
        static WorkloadConfiguration config;
        static WorkloadResults[] results;
        static IBenchmark[] benchmarks;
        static Barrier[] barriers;
        static CountdownEvent[] threadAcks;
        static bool initializationDone = false;
        static Thread[] threads;
        static readonly Random random = new Random();

        private static async void ThreadWorkAsync(Object obj)
        {

            int threadIndex = (int)obj;
            var globalWatch = new Stopwatch();            
            var benchmark = benchmarks[threadIndex];
            IClusterClient client = clients[threadIndex % config.numConnToClusterPerWorkerNode];

            for (int eIndex = 0; eIndex < config.numEpochs; eIndex++)
            {
                int numCommit = 0;
                int numTransaction = 0;
                int numUpdate = 0;
                int numQuery = 0;
                var latencies = new List<double>();
                int SubscribeUpdateExceptionCount = 0;
                int SnapshotBufferedDataUpdateExceptionCount = 0;
                int MovingActorExceptionCount = 0;
                int ReadTjyExceptionCount = 0;
                int OtherExceptionCount = 0;
                //Wait for all threads to arrive at barrier point
                barriers[eIndex].SignalAndWait();
                globalWatch.Restart();
                var startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                var moveTasks = new List<Task>();
                var queryTasks = new List<Task<List<ActorInfo>>>();
                var moveReqs = new Dictionary<Task, TimeSpan>();
                var queryReqs = new Dictionary<Task<List<ActorInfo>>, TimeSpan>();
                do
                {
                    while ((moveTasks.Count+queryTasks.Count) < config.asyncMsgLengthPerThread)
                    {
                        //Pipeline remaining tasks
                        var asyncReqStartTime = globalWatch.Elapsed;
                        if (random.NextDouble() < config.queryRate)
                        {
                            var queryTask = await benchmark.ExecuteFindNearbyActors(client);
                            queryReqs.Add(queryTask, asyncReqStartTime);
                            queryTasks.Add(queryTask);
                            numQuery++;
                        }
                        else 
                        {
                            var moveTask = await benchmark.ExecuteMove(client);
                            moveReqs.Add(moveTask, asyncReqStartTime);
                            moveTasks.Add(moveTask);
                            numUpdate++;
                        }
                            
                        numTransaction++;
                    }
                    
                    if (moveTasks.Count != 0) {
                        bool noException = true;
                        var task = await Task.WhenAny(moveTasks);
                        try
                        {
                            await task;
                        }
                        catch (SubscribeUpdateException)
                        {
                            SubscribeUpdateExceptionCount++;
                            noException = false;
                        }
                        catch (SnapshotBufferedDataUpdateException)
                        {
                            SnapshotBufferedDataUpdateExceptionCount++;
                            noException = false;
                        }
                        catch (MovingActorException)
                        {
                            MovingActorExceptionCount++;
                            noException = false;
                        }
                        catch (ReadTjyException)
                        {
                            ReadTjyExceptionCount++;
                            noException = false;
                        }
                        catch (ExecuteMoveException e) {
                            throw e;
                        }
                        catch (Exception e)
                        {
                            throw e;
                            OtherExceptionCount++;
                            noException = false;
                        }

                        if (noException)
                        {
                            numCommit++;
                            latencies.Add((globalWatch.Elapsed - moveReqs[task]).TotalMilliseconds);
                        }

                        moveTasks.Remove(task);
                        moveReqs.Remove(task);
                    }

                    if (queryTasks.Count != 0) {
                        bool noException = true;
                        var task = await Task.WhenAny(queryTasks);
                        try
                        {
                            await task;
                        }
                        catch (Exception)
                        {
                            noException = false;
                        }

                        if (noException)
                        {
                            numCommit++;
                            latencies.Add((globalWatch.Elapsed - queryReqs[task]).TotalMilliseconds);
                        }

                        queryTasks.Remove(task);
                        queryReqs.Remove(task);
                    }

                } while (globalWatch.ElapsedMilliseconds < config.epochDurationMSecs);
               
                //Wait for the tasks exceeding epoch time but do not count them
                //interval - time difference between process in global time
                while (moveTasks.Count != 0)
                {
                    var task = await Task.WhenAny(moveTasks);
                    //var asyncReqEndTime = globalWatch.Elapsed;
                    bool noException = true; 
                    try
                    {
                        await task;
                    }
                    catch (SubscribeUpdateException)
                    {
                        SubscribeUpdateExceptionCount++;
                        noException = false;
                    }
                    catch (SnapshotBufferedDataUpdateException)
                    {
                        SnapshotBufferedDataUpdateExceptionCount++;
                        noException = false;
                    }
                    catch (MovingActorException)
                    {
                        MovingActorExceptionCount++;
                        noException = false;
                    }
                    catch (ReadTjyException)
                    {
                        ReadTjyExceptionCount++;
                        noException = false;
                    }
                    catch (Exception e)
                    {
                        throw e;
                        OtherExceptionCount++;
                        noException = false;
                    }

                    if (noException)
                    {
                        numCommit++;
                        latencies.Add((globalWatch.Elapsed - moveReqs[task]).TotalMilliseconds);
                    }

                    moveTasks.Remove(task);
                   moveReqs.Remove(task);
                }

                while (queryTasks.Count != 0)
                {
                    var task = await Task.WhenAny(queryTasks);
                    //var asyncReqEndTime = globalWatch.Elapsed;
                    bool noException = true;
                    try
                    {
                        await task;
                    }
                    catch (Exception)
                    {
                        noException = false;
                    }

                    if (noException)
                    {
                        numCommit++;
                        latencies.Add((globalWatch.Elapsed - queryReqs[task]).TotalMilliseconds);
                    }

                    queryTasks.Remove(task);
                    queryReqs.Remove(task);
                }

                long endTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                globalWatch.Stop();
                WorkloadResults res = new WorkloadResults(numTransaction, numCommit, startTime, endTime, latencies, numQuery, numUpdate, SubscribeUpdateExceptionCount, SnapshotBufferedDataUpdateExceptionCount, MovingActorExceptionCount, ReadTjyExceptionCount, OtherExceptionCount);
                results[threadIndex] = res;
                //Signal the completion of epoch
                threadAcks[eIndex].Signal();
            }
        }

        private static void InitializeThreads()
        {
            barriers = new Barrier[config.numEpochs];
            threadAcks = new CountdownEvent[config.numEpochs];
            for (int i = 0; i < config.numEpochs; i++)
            {
                barriers[i] = new Barrier(config.numThreadsPerWorkerNode + 1);
                threadAcks[i] = new CountdownEvent(config.numThreadsPerWorkerNode);
            }

            //Spawn Threads        
            threads = new Thread[config.numThreadsPerWorkerNode];
            for (int threadIndex = 0; threadIndex < config.numThreadsPerWorkerNode; threadIndex++)
            {
                Thread thread = new Thread(ThreadWorkAsync);
                threads[threadIndex] = thread;
                thread.Start(threadIndex);
            }
        }

        private static async Task InitializeClients()
        {
            clients = new IClusterClient[config.numConnToClusterPerWorkerNode];
            ClientConfiguration clientConfig = new ClientConfiguration();

            for (int i = 0; i < config.numConnToClusterPerWorkerNode; i++)
            {
                if (LocalCluster)
                    clients[i] = await clientConfig.StartClient();
                else
                    clients[i] = await clientConfig.StartClientToCluster();
            }
        }

        private static async void Initialize()
        {
            results = new WorkloadResults[config.numThreadsPerWorkerNode];
            benchmarks = new ExperimentBenchmark[config.numThreadsPerWorkerNode];
            for (int i = 0; i < config.numThreadsPerWorkerNode; i++)
            {
                switch (config.benchmarktype)
                {
                    case BenchmarkType.SYNTHETIC:
                        benchmarks[i] = new ExperimentBenchmark();
                        benchmarks[i].GenerateBenchmark(config,i);
                        break;
                    case BenchmarkType.SIMULATION:
                        benchmarks[i] = new ExperimentBenchmark();
                        benchmarks[i].GenerateBenchmark(config,i);
                        break;
                    default:
                        throw new Exception("Unknown benchmark type");
                }
            }

            await InitializeClients();
            InitializeThreads();
            initializationDone = true;
        }

        static void ProcessWork()
        {
            Console.WriteLine("====== WORKER ======");

            //using (var controller = new PullSocket(controllerAddress))
            using var controller = new SubscriberSocket(controllerAddress);
            controller.Subscribe("WORKLOAD_INIT");
            //Acknowledge the controller thread
            var msg = new NetworkMessageWrapper(MsgType.WORKER_CONNECT);
            sink.SendFrame(Helper.SerializeToByteArray<NetworkMessageWrapper>(msg));

            controller.Options.ReceiveHighWatermark = 1000;
            
            var messageTopicReceived = controller.ReceiveFrameString();
            var messageReceived = controller.ReceiveFrameBytes();
            //Wait to receive workload msg
            msg = Helper.DeserializeFromByteArray<NetworkMessageWrapper>(messageReceived);
            Console.WriteLine("Connected to controller");
            controller.Unsubscribe("WORKLOAD_INIT");
            controller.Subscribe("RUN_EPOCH");
            Trace.Assert(msg.msgType == MsgType.WORKLOAD_INIT);
            config = Helper.DeserializeFromByteArray<WorkloadConfiguration>(msg.contents);
            Console.WriteLine("Received workload message from controller");

            //Initialize threads and other data-structures for epoch runs
            Initialize();
            while (!initializationDone)
                Thread.Sleep(100);
            //Send an ACK
            Console.WriteLine("Finished initialization, sending ACK to controller");
            msg = new NetworkMessageWrapper(MsgType.WORKLOAD_INIT_ACK);
            sink.SendFrame(Helper.SerializeToByteArray<NetworkMessageWrapper>(msg));

            for (int i = 0; i < config.numEpochs; i++)
            {
                messageTopicReceived = controller.ReceiveFrameString();
                messageReceived = controller.ReceiveFrameBytes();
                msg = Helper.DeserializeFromByteArray<NetworkMessageWrapper>(messageReceived);
                Trace.Assert(msg.msgType == MsgType.RUN_EPOCH);
                //Wait for EPOCH RUN signal
                Console.WriteLine($"Received signal from controller. Running epoch {i} across {config.numThreadsPerWorkerNode} worker threads");
                //Signal the barrier
                barriers[i].SignalAndWait();
                //Wait for all threads to finish the epoch
                threadAcks[i].Wait();
                var result = AggregateAcrossThreadsForEpoch();
                msg = new NetworkMessageWrapper(MsgType.RUN_EPOCH_ACK)
                {
                    contents = Helper.SerializeToByteArray<WorkloadResults>(result)
                };
                sink.SendFrame(Helper.SerializeToByteArray<NetworkMessageWrapper>(msg));
            }
            Console.WriteLine("Finished running epochs");

            foreach (var thread in threads)
                thread.Join();
        }

        private static WorkloadResults AggregateAcrossThreadsForEpoch()
        {
            Trace.Assert(results.Length >= 1);
            int aggNumCommitted = results[0].numCommitted;
            int aggNumTransactions = results[0].numTransactions;
            long aggStartTime = results[0].startTime;
            long aggEndTime = results[0].endTime;
            var aggLatencies = new List<double>();
            aggLatencies.AddRange(results[0].latencies);
            int aggNumQuery = results[0].numQuery;
            int aggNumUpdate = results[0].numUpdate;
            int aggSubscribeExceptionCount = results[0].SubscribeUpdateExceptionCount;
            int aggInfoUpdateExceptionCount = results[0].SnapshotBufferedDataUpdateExceptionCount;
            int aggMovingActorExceptionCount = results[0].MovingActorExceptionCount;
            int aggReadTjyExceptionCount = results[0].ReadTjyExceptionCount;
            int aggOtherExceptionCount = results[0].OtherExceptionCount;
            for (int i = 1; i < results.Length; i++)
            {
                aggNumCommitted += results[i].numCommitted;
                aggNumTransactions += results[i].numTransactions;
                aggStartTime = (results[i].startTime < aggStartTime) ? results[i].startTime : aggStartTime;
                aggEndTime = (results[i].endTime < aggEndTime) ? results[i].endTime : aggEndTime;
                aggLatencies.AddRange(results[i].latencies);
                aggNumQuery += results[i].numQuery;
                aggNumUpdate += results[i].numUpdate;
                aggSubscribeExceptionCount += results[i].SubscribeUpdateExceptionCount;
                aggInfoUpdateExceptionCount += results[i].SnapshotBufferedDataUpdateExceptionCount;
                aggMovingActorExceptionCount += results[i].MovingActorExceptionCount;
                aggReadTjyExceptionCount += results[i].ReadTjyExceptionCount;
                aggOtherExceptionCount += results[i].OtherExceptionCount;
            }
            return new WorkloadResults(aggNumTransactions, aggNumCommitted, aggStartTime, aggEndTime, aggLatencies, aggNumQuery,aggNumUpdate, aggSubscribeExceptionCount,aggInfoUpdateExceptionCount, aggMovingActorExceptionCount, aggReadTjyExceptionCount,aggOtherExceptionCount);
        }

        private static void InitializeValuesFromConfigFile()
        {
            var benchmarkFrameWorkSection = ConfigurationManager.GetSection("BenchmarkFrameworkConfig") as NameValueCollection;
            LocalCluster = bool.Parse(benchmarkFrameWorkSection["LocalCluster"]);   
            LocalExperiment = bool.Parse(benchmarkFrameWorkSection["LocalExperiment"]);
            if (LocalExperiment)
            {
                sinkAddress = ">tcp://localhost:5558";
                controllerAddress = ">tcp://localhost:5557";
            }
            sink = new PushSocket(sinkAddress);
        }

        static int Main(string[] args)
        {
            Console.WriteLine("Worker is Started...");
            InitializeValuesFromConfigFile();
            ProcessWork();
            Console.WriteLine("Processor finished running experiment. Press Enter to exit");
           // Console.ReadLine();
            return 0;
        }
    }
}
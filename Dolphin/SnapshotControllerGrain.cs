using Dolphin.Interfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolphin
{
    class SnapshotControllerGrain : Grain, ISnapshotController{
        private List<int> CellList { set; get; }
        private List<int> CellMissList { set; get; }
        private List<int> FullCellList = new List<int>();
        //private int NUMMONITORACTORSPERCELL;
        private List<Tuple<int, List<int>>> SrcAndDstCellIdsList { set; get; }
        private IDisposable timer;

        Task ISnapshotController.Initialize(List<int> CellIds, List<int> FullCellIds)
        {
            CellList = new List<int>();
            CellMissList = new List<int>();
            SrcAndDstCellIdsList = new List<Tuple<int, List<int>>>();
            //this.NUMMONITORACTORSPERCELL = NUMMONITORACTORSPERCELL;

            CellList.AddRange(CellIds);
            CellMissList.AddRange(CellIds);
            FullCellList.AddRange(FullCellIds);
            return Task.CompletedTask;
        }

         Task<bool> ISnapshotController.BuildCommunicationGraph(Tuple<int,List<int>> srcAndDstCellIds, bool isEmpty)
        {
            //Console.WriteLine("CellMissList.Count="+CellMissList.Count);
            //if (CellMissList.Count == 0)
                //Console.WriteLine("-------------StartGlobalCommunicationGraph ends at throwing exception---------------");
            if (CellMissList.Remove(srcAndDstCellIds.Item1))
            {
                SrcAndDstCellIdsList.Add(srcAndDstCellIds);
                if(isEmpty==true)
                    CellList.Remove(srcAndDstCellIds.Item1);
                if (CellMissList.Count == 0) 
                    timer=this.RegisterTimer(StartGlobalCommunicationGraph, null, TimeSpan.Zero, TimeSpan.FromHours(10));
                return Task.FromResult(true);
            } else
                return Task.FromResult(false);
        }

        private async Task StartGlobalCommunicationGraph(object arg)
        {
            await GrainFactory.GetGrain<ISnapshotController>(0).StartGlobalCommunicationGraph();
            timer.Dispose();
            //Console.WriteLine("CellList.Count=" + CellList.Count);
        }

        async Task ISnapshotController.StartGlobalCommunicationGraph() {
            // Console.WriteLine("---------------Ask All moving actor to update subsrcibe---------------");
            var monitoringInfo = new MonitoringInfo();
            foreach (var cellId in FullCellList)
                GrainFactory.GetGrain<IMonitoring>(cellId).Produce(monitoringInfo);
            //for (var monitoringId = cellId * NUMMONITORACTORSPERCELL; monitoringId < (cellId + 1) * NUMMONITORACTORSPERCELL; monitoringId++)
            //        GrainFactory.GetGrain<IMonitoring>(monitoringId).Produce(monitoringInfo);

            // Console.WriteLine("---------------Build Global Communication Graph Starts---------------");
            var ExpectedDictionary = new Dictionary<int, List<int>>();
            foreach (var sdci in SrcAndDstCellIdsList)
                foreach (var item in sdci.Item2)
                {
                    if (ExpectedDictionary.ContainsKey(item))
                        ExpectedDictionary[item].Add(sdci.Item1);
                    else
                        ExpectedDictionary.Add(item, new List<int> { sdci.Item1 });
                    if (!CellList.Contains(item))
                        CellList.Add(item);
                }

            var buildGraphTasks = new List<Task>();
            foreach (var item in ExpectedDictionary)
                buildGraphTasks.Add(GrainFactory.GetGrain<ISnapshotUpdate>(item.Key).AddExceptCellId(item.Value));
            try
            {
                await Task.WhenAll(buildGraphTasks);
            }
            catch (Exception e)
            {
                throw new Exception(e + "Build Global Communication Graph Error. ");
            }


            //Console.WriteLine("---------------Communication among SnapshotUpdate Actors Starts---------------");
            foreach (var sdci in SrcAndDstCellIdsList)
           // foreach (var cellId in FullCellList)
                if (sdci.Item2.Count != 0)
                    try
                    {
                        await GrainFactory.GetGrain<ISnapshotUpdate>(sdci.Item1).StartCommunication();
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e + "Communication among SnapshotUpdate Actors Error. ");
                    }


            var updateTasks = new List<Task>();
            foreach (var cellId in FullCellList)
                updateTasks.Add(GrainFactory.GetGrain<ISnapshotUpdate>(cellId).StartUpdate());
            try
            {
                await Task.WhenAll(updateTasks);
            }
            catch (Exception e)
            {
                throw new Exception(e + "Snapshot Updating Error. ");
            }

            CellMissList.AddRange(CellList);
            SrcAndDstCellIdsList.Clear();
        }

       
    }
}

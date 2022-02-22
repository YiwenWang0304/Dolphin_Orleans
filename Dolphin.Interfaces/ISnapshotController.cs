using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolphin.Interfaces
{
    public interface ISnapshotController : Orleans.IGrainWithIntegerKey
    {
        Task Initialize(List<int> CellIds, List<int> FullCellIds);
        Task<bool> BuildCommunicationGraph(Tuple<int, List<int>> srcAndDstCellIds, bool IsEmpty);

        Task StartGlobalCommunicationGraph();

        //Task RemoveEmptySnapshotUpdate(int id);
    }
}

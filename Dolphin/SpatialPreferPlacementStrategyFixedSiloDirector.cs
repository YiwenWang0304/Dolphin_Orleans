using Newtonsoft.Json;
using Orleans.Placement;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dolphin
{
    
    public class SpatialPreferPlacementStrategyFixedSiloDirector : IPlacementDirector
    {
        private readonly Random random = new Random();
        public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        { 
            var silos = context.GetCompatibleSilos(target).OrderBy(s => s).ToArray();
            try
            {
                int primaryKey = (int)target.GrainIdentity.PrimaryKeyLong;
                try 
                {
                    var siloIds = SpatialPreferPlacementStrategyAttribute.Instance.CellPlacement[primaryKey];
                    var siloNum = siloIds.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();//most occurring silo number
                    switch (silos.Length)
                    {
                        case 1:
                            return Task.FromResult(silos[0]);
                        case 2:
                            if (siloNum <= 3)
                                return Task.FromResult(silos[0]);
                            else
                                return Task.FromResult(silos[1]);
                        case 4:
                            if (siloNum == 0 || siloNum == 1)
                                return Task.FromResult(silos[0]);
                            else if (siloNum == 2 || siloNum == 3)
                                return Task.FromResult(silos[1]);
                            else if (siloNum == 4 || siloNum == 5)
                                return Task.FromResult(silos[2]);
                            else if (siloNum == 6 || siloNum == 7)
                                return Task.FromResult(silos[3]);
                            break;
                        //case 6:
                        //    if (siloNum == 0 || siloNum == 1)
                        //        return Task.FromResult(silos[random.Next(0, 3)]);
                        //    else if (siloNum == 2 || siloNum == 3)
                        //        return Task.FromResult(silos[random.Next(1, 4)]);
                        //    else if (siloNum == 4 || siloNum == 5)
                        //        return Task.FromResult(silos[random.Next(2, 5)]);
                        //    else if (siloNum == 6 || siloNum == 7)
                        //        return Task.FromResult(silos[random.Next(3, 6)]);
                        //    break;
                        case 8:
                            return Task.FromResult(silos[siloNum]);
                        default:
                            throw new Exception("Wrong silo num!");
                    }
                }
                catch //can't find in cell placement dictionary, use random placement
                {
                    return Task.FromResult(silos[random.Next(0, silos.Length)]);
                }
                
            }
            catch
            {//can't be repsentive as long key
                var siloNum = SpatialPreferPlacementStrategyAttribute.Instance.GrainPlacement[target.GrainIdentity.PrimaryKey];
                switch (silos.Length)
                {
                    case 1:
                        return Task.FromResult(silos[0]);
                    case 2:
                        if (siloNum <= 3)
                            return Task.FromResult(silos[0]);
                        else
                            return Task.FromResult(silos[1]);
                    case 4:
                        if (siloNum == 0 || siloNum == 1)
                            return Task.FromResult(silos[0]);
                        else if (siloNum == 2 || siloNum == 3)
                            return Task.FromResult(silos[1]);
                        else if (siloNum == 4 || siloNum == 5)
                            return Task.FromResult(silos[2]);
                        else if (siloNum == 6 || siloNum == 7)
                            return Task.FromResult(silos[3]);
                        break;
                    case 8:
                        return Task.FromResult(silos[siloNum]);
                    default:
                        throw new Exception("Wrong silo num!");
                }
            }
            
            return Task.FromResult(silos[0]);
        }
    }

    [Serializable]
    public class SpatialPreferPlacementStrategy : PlacementStrategy
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SpatialPreferPlacementStrategyAttribute : PlacementAttribute
    {
        public Dictionary<Guid, int> GrainPlacement = new Dictionary<Guid, int>();
        public Dictionary<int, List<int>> CellPlacement = new Dictionary<int, List<int>>();
        private static SpatialPreferPlacementStrategyAttribute instance = null;
        private static readonly object padlock = new object();

        public static SpatialPreferPlacementStrategyAttribute Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (padlock)
                    {
                        if (instance == null)
                        {
                            instance = new SpatialPreferPlacementStrategyAttribute();
                        }
                    }
                }
                return instance;
            }
        }
        public SpatialPreferPlacementStrategyAttribute() : base(new SpatialPreferPlacementStrategy())
        {
            var IsLocal = false;
            var i = 5000;//666(1000),5000,10000,20000,40000,8,80,400,800,8000,4000(40000),0

            string directoryPath;
            if (IsLocal)
            {
                directoryPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString()).ToString();
                directoryPath = directoryPath.Replace('\u005c', '\u002f');
                directoryPath += "/Benchmark.ActorDataGenerate";
            }
            else 
                directoryPath = "/Users/Administrator/Documents/GitHub/Dolphin-Orleans/Orleans_Dolphin/Benchmark.ActorDataGenerate";

            var grainPath = i switch
            {
                Constants.MOVINGACTORNUM_TEST => directoryPath + Constants.UNIFORM_GRAINPLACEMENT_TEST,
                Constants.MOVINGACTORNUM_0 => directoryPath + Constants.UNIFORM_GRAINPLACEMENT_0,
                Constants.MOVINGACTORNUM_1=> directoryPath + Constants.UNIFORM_GRAINPLACEMENT_1,
                Constants.MOVINGACTORNUM_2 => directoryPath + Constants.UNIFORM_GRAINPLACEMENT_2,
                Constants.MOVINGACTORNUM_3 => directoryPath + Constants.UNIFORM_GRAINPLACEMENT_3,
                Constants.HOTSPOTNUM_0 => directoryPath + Constants.GAUSS_GRAINPLACEMENT_0,
                Constants.HOTSPOTNUM_1 => directoryPath + Constants.GAUSS_GRAINPLACEMENT_1,
                Constants.HOTSPOTNUM_2 => directoryPath + Constants.GAUSS_GRAINPLACEMENT_2,
                Constants.HOTSPOTNUM_3 => directoryPath + Constants.GAUSS_GRAINPLACEMENT_3,
                Constants.HOTSPOTNUM_4 => directoryPath + Constants.GAUSS_GRAINPLACEMENT_4,
                Constants.HOTSPOTNUM_5 => directoryPath + Constants.GAUSS_GRAINPLACEMENT_5,
                Constants.ROADNETWORKNUM => directoryPath + Constants.ROADNETWORK_GRAINPLACEMENT,
                _ => directoryPath + Constants.UNIFORM_GRAINPLACEMENT_TEST,
            };
            using StreamReader r = new StreamReader(grainPath);
            var grainPlacementList = JsonConvert.DeserializeObject<List<GrainPlacement>>(r.ReadToEnd());
            foreach (var item in grainPlacementList)
            {
                try { GrainPlacement.Add(item.grainId, item.silo); }
                catch { throw new Exception("Error in add grain placement!"); }     
            }

            var cellPath = i switch
            {
                Constants.MOVINGACTORNUM_TEST  => directoryPath + Constants.UNIFORM_CELLPLACEMENT_TEST,
                Constants.MOVINGACTORNUM_0 => directoryPath + Constants.UNIFORM_CELLPLACEMENT_0,
                Constants.MOVINGACTORNUM_1 => directoryPath + Constants.UNIFORM_CELLPLACEMENT_1,
                Constants.MOVINGACTORNUM_2 => directoryPath + Constants.UNIFORM_CELLPLACEMENT_2,
                Constants.MOVINGACTORNUM_3 => directoryPath + Constants.UNIFORM_CELLPLACEMENT_3,
                Constants.HOTSPOTNUM_0 => directoryPath + Constants.GAUSS_CELLPLACEMENT_0,
                Constants.HOTSPOTNUM_1 => directoryPath + Constants.GAUSS_CELLPLACEMENT_1,
                Constants.HOTSPOTNUM_2 => directoryPath + Constants.GAUSS_CELLPLACEMENT_2,
                Constants.HOTSPOTNUM_3 => directoryPath + Constants.GAUSS_CELLPLACEMENT_3,
                Constants.HOTSPOTNUM_4 => directoryPath + Constants.GAUSS_CELLPLACEMENT_4,
                Constants.HOTSPOTNUM_5 => directoryPath + Constants.GAUSS_CELLPLACEMENT_5,
                Constants.ROADNETWORKNUM => directoryPath + Constants.ROADNETWORK_CELLPLACEMENT,
                _ => directoryPath + Constants.UNIFORM_CELLPLACEMENT_TEST,
            };

            using StreamReader r2 = new StreamReader(cellPath);
            var cellPlacementList = JsonConvert.DeserializeObject<List<CellPlacement>>(r2.ReadToEnd());
            foreach (var item in cellPlacementList)
                if (CellPlacement.ContainsKey(item.cellId))
                    CellPlacement[item.cellId].Add(item.silo);
                else
                    CellPlacement.Add(item.cellId, new List<int> { item.silo });
        }
    }

   
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GrainPlacement
    {
        [JsonProperty(PropertyName = "grainId")]
        public Guid grainId { set; get; }
        [JsonProperty(PropertyName = "silo")]
        public int silo { set; get; }

        [JsonConstructor]
        public GrainPlacement(Guid grainId, int silo)
        {
            this.grainId = grainId;
            this.silo = silo;
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class CellPlacement
    {
        [JsonProperty(PropertyName = "cellId")]
        public int cellId { set; get; }
        [JsonProperty(PropertyName = "silo")]
        public int silo { set; get; }

        [JsonConstructor]
        public CellPlacement(int cellId, int silo)
        {
            this.cellId = cellId;
            this.silo=silo;
        }
    }
}

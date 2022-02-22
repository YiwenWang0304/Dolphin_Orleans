//using Dolphin.Interfaces;
//using Orleans;
//using System;
//using System.Threading.Tasks;

//namespace Dolphin.Utilities
//{
//    public interface IConfig : IGrainWithIntegerKey
//    {
//        Task SetConstants(Semantics semantics, double[] boarder, double cellSize);
//        Task<Semantics> GetSemantic();
//        Task<double[]> GetBoarder();
//        Task<double> GetCellSize();
//        Task<bool> IsSetted();
//    }

//    public class Config : Grain, IConfig
//    {
//        public Semantics Semantics { get; set; }
//        public double[] Boarder { set; get; }
//        public double CellSize { set; get; }
//        public bool SETTED { set; get; }

//        async public override Task OnActivateAsync()
//        {
//            SETTED = false;
//            await base.OnActivateAsync();
//        }

//        Task IConfig.SetConstants(Semantics semantics, double[] boarder, double cellSize)
//        {
//            Semantics = semantics;
//            Boarder = boarder;
//            CellSize = cellSize;
//            Helper.SetConstants(semantics,boarder,cellSize);
//            SETTED = true;
//            return Task.CompletedTask;
//        }

//        Task<double[]> IConfig.GetBoarder()
//        {
//            return Task.FromResult(Boarder);
//        }

//        Task<Semantics> IConfig.GetSemantic()
//        {
//            return Task.FromResult(Semantics);
//        }

//        Task<double> IConfig.GetCellSize()
//        {
//            return Task.FromResult(CellSize);
//        }

//        Task<bool> IConfig.IsSetted()
//        {
//            return Task.FromResult(SETTED);
//        }

//        //Task IConfig.SetBoarder(double minx, double miny, double maxx, double maxy)
//        //{
//        //    Boarder[0] = minx;
//        //    Boarder[1] = miny;
//        //    Boarder[2] = maxx;
//        //    Boarder[3] = maxy;
//        //    return Task.CompletedTask;
//        //}

//        //Task IConfig.SetSemantic(Semantics semantic)
//        //{
//        //    Semantic=semantic;
//        //    return Task.CompletedTask;
//        //}


//        //private static Config instance = null;
//        //private static readonly object padlock = new object();
//        //private Semantics semantics;
//        //private double[] boarder;
//        //private double cellSize;

//        //Config(Semantics semantics, double[] boarder, double cellSize)
//        //{
//        //    this.semantics = semantics;
//        //    this.boarder = boarder;
//        //    this.cellSize = cellSize;
//        //}

//        //public static Config GetInstance()
//        //{
//        //    lock (padlock)
//        //    {
//        //        if (instance == null)
//        //            throw new Exception("Singleton has not been created - use GetInstance(Semantics semantics, double[] boarder, double cellSize) firat!");
//        //        return instance;
//        //    }
//        //}


//        //public static Config GetInstance(Semantics semantics, double[] boarder, double cellSize)
//        //{
//        //    if (instance != null)
//        //        throw new Exception("Singleton has already created - use GetInstance()!");
//        //    instance = new Config(semantics, boarder, cellSize);
//        //    return instance;
//        //}



//        //public Semantics GetSemantic()
//        //{
//        //    return semantics;
//        //}

//        //public double[] GetBoarder()
//        //{
//        //    return boarder;
//        //}

//        //public double GetCellSize()
//        //{
//        //    return cellSize;
//        //}
//    }

//}

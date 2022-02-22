namespace Experiment.Controller
{
    public class Constants
    {
        //Num. Moving Actors  - CONSISTENT WITH SIMULATION BENCHMARK
        public const int MOVINGACTOR_NUM_TEST = 1000;
        public const int MOVINGACTOR_NUM0 = 5000;
        public const int MOVINGACTOR_NUM1 = 10000;
        public const int MOVINGACTOR_NUM2 = 20000;
        public const int MOVINGACTOR_NUM3 = 40000;
        public const int MOVINGACTOR_GAUSSNUM = 40000;
        public const int MOVINGACTOR_ROADNETWORK = 38000;

        //Num. Hotspots- TO TEST THE SKEW RESISLIENCE
        public const int HOTSPOT_NUM0 = 8;//5000
        public const int HOTSPOT_NUM1 = 80;//500
        public const int HOTSPOT_NUM2 = 400;//100
        public const int HOTSPOT_NUM3 = 800;//50
        public const int HOTSPOT_NUM4 = 8000;//5
        public const int HOTSPOT_NUM5 = 40000;//1

        //Space size 
        public const double BOARDER_TEST = 500;
        public const double BOARDER0 = 5000;//10k
        public const double BOARDER1 = 7070;//14.14k
        public const double BOARDER2 = 10000;//20k
        public const double BOARDER3 = 14142;//28.28k
        public const double BOARDER_GAUSS = 14142;

        public const int CELLNUM_TEST = 100;//10*10(100)
        public const int CELLNUM0 = 100; //10*10(1000)- 50 moving actors/cell - cellsize: 1000x1000
        public const int CELLNUM1 = 196; //14*14 - 51 moving actors/cell - cellsize: 1010x1010
        public const int CELLNUM2 = 400; //20*20 - 50 moving actors/cell - cellsize: 1000x1000
        public const int CELLNUM3 = 784; //28*28 - 51 moving actors/cell - cellsize: 1010x1010
        public const int CELLNUM_GAUSS = 784;//todo: tunging in different level- finer in more skew-> reduce workload for queued 
        // public const int CELLNUM_ROADNETWORK = 784;

        public const double FENCESIZE = 500;//real - 1000x1000
        public const double FENCESIZE_TEST = 50;

        public const double FENCESIZE_ROADNETWORK = 0.005;//0.01-1km
                                                          // public const double CELLSIZE_ROADNETWORK = 0.01;//0.01-1km

        public const double MINLAT = 37.708289;
        public const double MAXLAT = 37.810644;
        public const double MINLNG = -122.514586;
        public const double MAXLNG = -122.357189;
        public const int MOVINGACTORPERCELL = 50;

        public const double LAT1 = 37.773562;
        public const double LNG1 = -122.419022;

        public const double LAT2 = 37.749077;
        public const double LNG2 = -122.434087;

        public const double P = 0.333;

        public const double GAUSSSTD = 1;
        public const double GAUSSSPCE = 1000;

        public const double MAXSPEED = 22;//max-80km/h
        public const double UPDATEFREQUENCY = 3;
        public const int TJYLENGTH = 20;

        //================================
        public const string UNIFORM_TEST = "/uniformactor1k.json";
        public const string UNIFORM_0 = "/uniformactor5k.json";
        public const string UNIFORM_1 = "/uniformactor10k.json";
        public const string UNIFORM_2 = "/uniformactor20k.json";
        public const string UNIFORM_3 = "/uniformactor40k.json";

        public const string UNIFORM_GRAINPLACEMENT_TEST = "/grainplacement1k.json";
        public const string UNIFORM_GRAINPLACEMENT_0 = "/grainplacement5k.json";
        public const string UNIFORM_GRAINPLACEMENT_1 = "/grainplacement10k.json";
        public const string UNIFORM_GRAINPLACEMENT_2 = "/grainplacement20k.json";
        public const string UNIFORM_GRAINPLACEMENT_3 = "/grainplacement40k.json";

        public const string UNIFORM_CELLPLACEMENT_TEST = "/cellplacement1k.json";
        public const string UNIFORM_CELLPLACEMENT_0 = "/cellplacement5k.json";
        public const string UNIFORM_CELLPLACEMENT_1 = "/cellplacement10k.json";
        public const string UNIFORM_CELLPLACEMENT_2 = "/cellplacement20k.json";
        public const string UNIFORM_CELLPLACEMENT_3 = "/cellplacement40k.json";

        //================================
        public const string GAUSS_0 = "/gaussactor1.json";
        public const string GAUSS_1 = "/gaussactor10.json";
        public const string GAUSS_2 = "/gaussactor50.json";
        public const string GAUSS_3 = "/gaussactor100.json";
        public const string GAUSS_4 = "/gaussactor1000.json";
        public const string GAUSS_5 = "/gaussactor5000.json";

        public const string GAUSS_GRAINPLACEMENT_0 = "/grainplacement1.json";
        public const string GAUSS_GRAINPLACEMENT_1 = "/grainplacement10.json";
        public const string GAUSS_GRAINPLACEMENT_2 = "/grainplacement50.json";
        public const string GAUSS_GRAINPLACEMENT_3 = "/grainplacement100.json";
        public const string GAUSS_GRAINPLACEMENT_4 = "/grainplacement1000.json";
        public const string GAUSS_GRAINPLACEMENT_5 = "/grainplacement5000.json";

        public const string GAUSS_CELLPLACEMENT_0 = "/cellplacement1.json";
        public const string GAUSS_CELLPLACEMENT_1 = "/cellplacement10.json";
        public const string GAUSS_CELLPLACEMENT_2 = "/cellplacement50.json";
        public const string GAUSS_CELLPLACEMENT_3 = "/cellplacement100.json";
        public const string GAUSS_CELLPLACEMENT_4 = "/cellplacement1000.json";
        public const string GAUSS_CELLPLACEMENT_5 = "/cellplacement5000.json";

        public const string GAUSS_TJY_0 = "/gaussactortjy1.json";
        public const string GAUSS_TJY_1 = "/gaussactortjy10.json";
        public const string GAUSS_TJY_2 = "/gaussactortjy50.json";
        public const string GAUSS_TJY_3 = "/gaussactortjy100.json";
        public const string GAUSS_TJY_4 = "/gaussactortjy1000.json";
        public const string GAUSS_TJY_5 = "/gaussactortjy5000.json";

        //================================
        public const string ROADNETWORK = "/roadnetwork.json";
        public const string ROADNETWORKACTOR = "/roadnetworkactor.json";
        public const string ROADNETWORk_TJY = "/actortjy.json";

        public const string ROADNETWORK_GRAINPLACEMENT = "/roadnetworkgrainplacement.json";

        public const string ROADNETWORK_CELLPLACEMENT = "/roadnetworkcellplacement.json";

        //===============================
        public const string UNIFORMSVG = "/uniformSVGData.html";
        public const string GAUSSSVG = "/gaussSVGData.html";
        public const string ROADNETWORKSVG = "/roadnetworkSVGData.html";
    }
}

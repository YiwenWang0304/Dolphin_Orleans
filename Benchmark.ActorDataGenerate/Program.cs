using Dolphin.Utilities;
using Experiment.Controller;
using MathNet.Numerics.Distributions;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Benchmark.ActorDataGenerate
{
    class Program
    {
        
        static readonly Random random = new Random();
        static double FENCESIZE = Constants.FENCESIZE;
        static double[] BOARDERS;
        static double CELLSIZE;
        static string directoryPath = "";
        static String dir = "";
        static String grainPlacementDir = "";
        static String cellPlacementDir = "";
        static readonly double R = 6378.137; // Radius of earth in KM
        static readonly Dictionary<Point, List<int>> pointToPolys = new Dictionary<Point, List<int>>();
        static readonly Dictionary<int, List<Point>> polyToPoints = new Dictionary<int, List<Point>>();

        static readonly Dictionary<Guid, Tuple<double, double>> ActorData = new Dictionary<Guid, Tuple<double, double>>();

        static void Main(string[] args)
        {
            //directoryPath = Directory.GetParent(Directory.GetCurrentDirectory()).ToString(); ;
            //directoryPath = directoryPath.Replace('\u005c', '\u002f');
            //directoryPath += "/Documents/GitHub/Dolphin-Orleans/Benchmark.ActorDataGenerate";

            directoryPath = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString();
            directoryPath = directoryPath.Replace('\u005c', '\u002f');

            //GenerateTestActors();
            //GenerateUniformActors();
            //GenerateGaussActors();
            //GenerateRoadNetworkActors();

           //GenerateUniformSVGDate();
          // GenerateGaussSVGDate();
           GenerateRoadNetworkSVGDate();
            return;
        }

        private static void GenerateUniformSVGDate()
        {
            var SCALEDOWN = 30;
            var DOTSIZE = 2;
            StringBuilder SVGString = new StringBuilder();
            SVGString.Append("<!DOCTYPE html>\r\n<html>\r\n<body>");
            SVGString.Append("<svg width=\""+Constants.BOARDER3*2/ SCALEDOWN + "\" height=\""+Constants.BOARDER3*2/ SCALEDOWN + "\">\r\n");

            var placementDir = directoryPath + Constants.UNIFORM_GRAINPLACEMENT_3;
            using StreamReader r0 = new StreamReader(placementDir);
            var grainPlacementArray = JsonConvert.DeserializeObject<List<Dolphin.GrainPlacement>>(r0.ReadToEnd());
            var grainPlacementDictionary = new Dictionary<Guid, int>();
            foreach (var item in grainPlacementArray) 
                grainPlacementDictionary.Add(item.grainId, item.silo);

            var actorDir = directoryPath + Constants.UNIFORM_3;
            using StreamReader r1 = new StreamReader(actorDir);
            var actorArray = JsonConvert.DeserializeObject<List<UniformActorData>>(r1.ReadToEnd());
            foreach (var actor in actorArray) 
            {
                if (grainPlacementDictionary[actor.id] == 0)
                    SVGString.Append("<circle cx=\"" + (actor.x+Constants.BOARDER3)/ SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\""+DOTSIZE+ "\" stroke=\"orange\" stroke-width=\"0\" fill=\"orange\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 1)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"black\" stroke-width=\"0\" fill=\"black\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 2)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"blue\" stroke-width=\"0\" fill=\"blue\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 3)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"magenta\" stroke-width=\"0\" fill=\"magenta\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 4)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"tan\" stroke-width=\"0\" fill=\"tan\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 5)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"green\" stroke-width=\"0\" fill=\"green\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 6)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"purple\" stroke-width=\"0\" fill=\"purple\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 7)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER3) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER3) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"red\" stroke-width=\"0\" fill=\"red\" />\r\n");
            }

            SVGString.Append("</svg>");
            SVGString.Append("</body>\r\n</html>");
            using StreamWriter w = File.AppendText(directoryPath + Constants.UNIFORMSVG);
            w.WriteLine(SVGString.ToString());
        }

        private static void GenerateGaussSVGDate()
        {
            var SCALEDOWN = 30;
            var DOTSIZE = 2;
            StringBuilder SVGString = new StringBuilder();
            SVGString.Append("<!DOCTYPE html>\r\n<html>\r\n<body>");
            SVGString.Append("<svg width=\"" + Constants.BOARDER_GAUSS * 2 / SCALEDOWN + "\" height=\"" + Constants.BOARDER_GAUSS * 2 / SCALEDOWN + "\">\r\n");

            var placementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_0;
            using StreamReader r0 = new StreamReader(placementDir);
            var grainPlacementArray = JsonConvert.DeserializeObject<List<Dolphin.GrainPlacement>>(r0.ReadToEnd());
            var grainPlacementDictionary = new Dictionary<Guid, int>();
            foreach (var item in grainPlacementArray)
                grainPlacementDictionary.Add(item.grainId, item.silo);

            var actorDir = directoryPath + Constants.GAUSS_0;
            using StreamReader r1 = new StreamReader(actorDir);
            var actorArray = JsonConvert.DeserializeObject<List<UniformActorData>>(r1.ReadToEnd());
            foreach (var actor in actorArray)
            {
                if (grainPlacementDictionary[actor.id] == 0)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"orange\" stroke-width=\"0\" fill=\"orange\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 1)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"black\" stroke-width=\"0\" fill=\"black\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 2)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"blue\" stroke-width=\"0\" fill=\"blue\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 3)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"magenta\" stroke-width=\"0\" fill=\"magenta\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 4)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"tan\" stroke-width=\"0\" fill=\"tan\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 5)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"green\" stroke-width=\"0\" fill=\"green\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 6)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"purple\" stroke-width=\"0\" fill=\"purple\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 7)
                    SVGString.Append("<circle cx=\"" + (actor.x + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" cy=\"" + (actor.y + Constants.BOARDER_GAUSS) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"red\" stroke-width=\"0\" fill=\"red\" />\r\n");
            }

            SVGString.Append("</svg>");
            SVGString.Append("</body>\r\n</html>");
            using StreamWriter w = File.AppendText(directoryPath + Constants.GAUSSSVG);
            w.WriteLine(SVGString.ToString());
        }
        private static void GenerateRoadNetworkSVGDate()
        {
            var roadNetworkDir = directoryPath + "/rows.json";
            using StreamReader r = new StreamReader(roadNetworkDir);
            string json = r.ReadToEnd();
            var array = JsonConvert.DeserializeObject<RoadData>(json);

            var polyId = 0;
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
                        var point = new Point(lng, lat);

                        if (polyToPoints.ContainsKey(polyId))
                            polyToPoints[polyId].Add(point);
                        else
                            polyToPoints.Add(polyId, new List<Point>() { point });
                    }

                }
                polyId++;
            }

            var SCALEDOWN = 0.00015;
            var DOTSIZE = 2;
            var SPACESIZE_X = Constants.MAXLNG - Constants.MINLNG;
            var SPACESIZE_Y = Constants.MAXLAT - Constants.MINLAT;
            StringBuilder SVGString = new StringBuilder();
            SVGString.Append("<!DOCTYPE html>\r\n<html>\r\n<body>");
            SVGString.Append("<svg width=\"" + SPACESIZE_X * 2 / SCALEDOWN + "\" height=\"" + SPACESIZE_Y * 2 / SCALEDOWN + "\">\r\n");

            
            StringBuilder polyline = new StringBuilder();
            foreach (var item in polyToPoints)
            {
                for (var i=0;i<item.Value.Count;i++)
                    polyline.Append((item.Value[i].X - Constants.MINLNG) / SCALEDOWN +","+(item.Value[i].Y - Constants.MINLAT )/ SCALEDOWN + " " );
                //polygonline.Append((item.Value[^1].X - Constants.MINLNG) / SCALEDOWN + "," + (item.Value[^1].Y - Constants.MINLAT) / SCALEDOWN);

                SVGString.Append("<polyline points = \"" + polyline + "\" style = \"fill:none;stroke:black;stroke-width:4\" />\r\n");
                polyline.Clear();
            }

            var placementDir = directoryPath + Constants.ROADNETWORK_GRAINPLACEMENT;
            using StreamReader r0 = new StreamReader(placementDir);
            var grainPlacementArray = JsonConvert.DeserializeObject<List<Dolphin.GrainPlacement>>(r0.ReadToEnd());
            var grainPlacementDictionary = new Dictionary<Guid, int>();
            foreach (var item in grainPlacementArray)
                grainPlacementDictionary.Add(item.grainId, item.silo);

            var actorDir = directoryPath + Constants.ROADNETWORKACTOR;
            using StreamReader r1 = new StreamReader(actorDir);
            var actorArray = JsonConvert.DeserializeObject<List<UniformActorData>>(r1.ReadToEnd());
            foreach (var actor in actorArray)
            {
                if (grainPlacementDictionary[actor.id] == 0)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"orange\" stroke-width=\"2.5\" fill=\"orange\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 1)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"black\" stroke-width=\"2.5\" fill=\"black\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 2)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"blue\" stroke-width=\"2.5\" fill=\"blue\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 3)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"magenta\" stroke-width=\"2.5\" fill=\"magenta\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 4)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"tan\" stroke-width=\"2.5\" fill=\"tan\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 5)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"green\" stroke-width=\"2.5\" fill=\"green\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 6)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"purple\" stroke-width=\"2.5\" fill=\"purple\" />\r\n");
                else if (grainPlacementDictionary[actor.id] == 7)
                    SVGString.Append("<circle cx=\"" + (actor.x - Constants.MINLNG) / SCALEDOWN + "\" cy=\"" + (actor.y - Constants.MINLAT) / SCALEDOWN + "\" r=\"" + DOTSIZE + "\" stroke=\"red\" stroke-width=\"2.5\" fill=\"red\" />\r\n");
            }

            SVGString.Append("</svg>");
            SVGString.Append("</body>\r\n</html>");
            using StreamWriter w = File.AppendText(directoryPath + Constants.ROADNETWORKSVG);
            w.WriteLine(SVGString.ToString());
        }

        private static void GenerateTestActors()
        {
            BOARDERS = new double[] { -Constants.BOARDER_TEST, -Constants.BOARDER_TEST, Constants.BOARDER_TEST, Constants.BOARDER_TEST };
            CELLSIZE = (BOARDERS[2] - BOARDERS[0]) / Math.Sqrt(Constants.CELLNUM_TEST);
            FENCESIZE = Constants.FENCESIZE_TEST;

            var testActorDataList = new List<UniformActorData>();
            for (var i = 0; i < Constants.MOVINGACTOR_NUM_TEST; i++)
            {
                var lat = random.NextDouble() * (BOARDERS[2] - BOARDERS[0]) + BOARDERS[0];
                var lng = random.NextDouble() * (BOARDERS[3] - BOARDERS[1]) + BOARDERS[1];
                lat = lat < BOARDERS[0] ? BOARDERS[0] : lat;
                lat = lat > BOARDERS[2] ? BOARDERS[2] : lat;
                lng = lng < BOARDERS[1] ? BOARDERS[1] : lng;
                lng = lng > BOARDERS[3] ? BOARDERS[3] : lng;
                var fence = CreateFence(lat, lng);
                var id = Guid.NewGuid();
                testActorDataList.Add(new UniformActorData(id, lat, lng, fence[0], fence[1], fence[2], fence[3]));
                ActorData.Add(id, new Tuple<double, double>(lat, lng));
            }
            dir = directoryPath + Constants.UNIFORM_TEST;
            grainPlacementDir = directoryPath + Constants.UNIFORM_GRAINPLACEMENT_TEST;
            cellPlacementDir = directoryPath + Constants.UNIFORM_CELLPLACEMENT_TEST;

            var testActorData = JsonConvert.SerializeObject(testActorDataList, Formatting.Indented);
            File.WriteAllText(dir, testActorData);

            GenerateGrainPlacement(grainPlacementDir, cellPlacementDir, ActorData);
            ActorData.Clear();
        }

        private static void GenerateRoadNetworkActors()
        {
            var roadNetworkDir = directoryPath + "/rows.json";
            using StreamReader r = new StreamReader(roadNetworkDir);
            //var points1 = new List<Point>();
            //var points2 = new List<Point>();
            //var points3 = new List<Point>();
            var points = new List<Point>();
            double minLng = 180;
            double minLat = 90;
            double maxLng = -180;
            double maxLat = -90;

            string json = r.ReadToEnd();
            var array = JsonConvert.DeserializeObject<RoadData>(json);

            var polyId = 0;

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
                    if (lat > Constants.MINLAT && lat < Constants.MAXLAT && lng > Constants.MINLNG && lng < Constants.MAXLNG) {
                        minLng = lng <= minLng ? lng : minLng;
                        maxLng = lng >= maxLng ? lng : maxLng;
                        minLat = lat <= minLat ? lat : minLat;
                        maxLat = lat >= maxLat ? lat : maxLat;

                        var point = new Point(lng, lat);

                        //generate point to polyId(s)
                        if (pointToPolys.ContainsKey(point))
                            pointToPolys[point].Add(polyId);
                        else
                            pointToPolys.Add(point, new List<int>() { polyId });

                        //if(lat>Constants.LAT1&&lng>Constants.LNG1)
                        //    points1.Add(point);
                        //else if (lat > Constants.LAT2 && lng > Constants.LNG2)
                        //    points2.Add(point);
                        //else
                        //    points3.Add(point);
                        points.Add(point);

                        if (polyToPoints.ContainsKey(polyId))
                            polyToPoints[polyId].Add(point);
                        else
                            polyToPoints.Add(polyId, new List<Point>() { point });
                   }
                    
                }
                polyId++;
            }

            //generate polyId to point

            BOARDERS = new double[] { minLng, minLat, maxLng,maxLat };
            FENCESIZE = Constants.FENCESIZE_ROADNETWORK;
            //CELLSIZE = Constants.CELLSIZE_ROADNETWORK;
            var MOVINGACTORNUM = Constants.MOVINGACTOR_ROADNETWORK;
            CELLSIZE = Math.Sqrt(((BOARDERS[2] - BOARDERS[0]) * (BOARDERS[3] - BOARDERS[1])) / (MOVINGACTORNUM / Constants.MOVINGACTORPERCELL));
            var CELLNUM_X = (int)(Math.Ceiling((BOARDERS[2] - BOARDERS[0]) / CELLSIZE));
            var CELLNUM_Y = (int)(Math.Ceiling((BOARDERS[3] - BOARDERS[1]) / CELLSIZE));

            //var count1 = points1.Count;
            //var count2 = points2.Count;
            //var count3 = points3.Count;
            var roadNetworkActorList = new List<RoadNetworkActorData>();
            var tjy = new Dictionary<Guid, List<Point>>();
            Dictionary<Guid, Tuple<Point, Point, int>> SimulationMovingActorInfo = new Dictionary<Guid, Tuple<Point, Point, int>>();
            for (var i = 0; i < MOVINGACTORNUM; i++)
            {
                Point point = points[random.Next(points.Count)];
                //var p = random.NextDouble();
                //if(p>=1-Constants.P)//x>0.667
                //    point = points1[random.Next(count1)];//0.334<x<0.667
                //else if (p >= 1-2*Constants.P&&p< 1 - Constants.P)
                //    point = points2[random.Next(count2)];
                //else
                //    point = points3[random.Next(count3)];


                var polyIds = pointToPolys[point];
                var pId = polyIds[random.Next(polyIds.Count)];
                Point nextPoint = GetNextPoint(point, pId);

                var fence = CreateFence(point.X, point.Y);
                var id = Guid.NewGuid();
                roadNetworkActorList.Add(new RoadNetworkActorData(id, point.X, point.Y, fence[0], fence[1], fence[2], fence[3], pId));
                ActorData.Add(id, new Tuple<double, double>(point.X, point.Y));

                SimulationMovingActorInfo.Add(id, new Tuple<Point, Point, int>(point, nextPoint, pId));
                tjy.Add(id, new List<Point> { point });
                var k = 0;
                while (k < Constants.TJYLENGTH)
                {
                    var distance = Constants.UPDATEFREQUENCY * random.NextDouble() * Constants.MAXSPEED;
                    var src = SimulationMovingActorInfo[id].Item1;
                    nextPoint = SimulationMovingActorInfo[id].Item2;
                    pId = SimulationMovingActorInfo[id].Item3;

                    var distanceDelta = ConvertLatLngToMeter(src, nextPoint);
                    while (distance > distanceDelta)
                    {
                        src = nextPoint;
                        polyIds = pointToPolys[src];
                        pId = polyIds[random.Next(polyIds.Count)];
                        nextPoint = GetNextPoint(src, pId);
                        distance -= distanceDelta;
                        distanceDelta = ConvertLatLngToMeter(src, nextPoint);
                    }

                    var dst = ConvertMetersToLatLng(src, nextPoint, distance);
                    tjy[id].Add(dst);
                    SimulationMovingActorInfo[id] = new Tuple<Point, Point, int>(dst, nextPoint, pId);
                    k++;
                }
            }


            dir = directoryPath + Constants.ROADNETWORKACTOR;
            json = JsonConvert.SerializeObject(roadNetworkActorList, Formatting.Indented);
            File.WriteAllText(dir, json);

            var TjyList = new List<ActorTjyData>();
            foreach (var item in tjy)
                foreach (var p in item.Value)
                    TjyList.Add(new ActorTjyData(item.Key, p.X, p.Y));
            dir = directoryPath + Constants.ROADNETWORk_TJY;
            json = JsonConvert.SerializeObject(TjyList, Formatting.Indented);
            File.WriteAllText(dir, json);

            grainPlacementDir = directoryPath + Constants.ROADNETWORK_GRAINPLACEMENT;
            cellPlacementDir = directoryPath + Constants.ROADNETWORK_CELLPLACEMENT;
            ROADNETWORKGenerateGrainPlacement(grainPlacementDir, cellPlacementDir, ActorData, CELLNUM_X,CELLNUM_Y);
            ActorData.Clear();
        }

        private static void ROADNETWORKGenerateGrainPlacement(string grainPlacementDir, string cellPlacementDir, Dictionary<Guid, Tuple<double, double>> actorDataList, int CELLNUM_X, int CELLNUM_Y)
        {
            List<double[]> pointsList = new List<double[]>();
            List<Guid> nodesList = new List<Guid>();
            foreach (var actorData in actorDataList)
            {
                pointsList.Add(new double[] { actorData.Value.Item1, actorData.Value.Item2 });
                nodesList.Add(actorData.Key);
            }

            var tree = new KDTree<double, Guid>(2,
               pointsList.ToArray(),
               nodesList.ToArray(),
               Utilities.L2Norm_Squared_Double,
               double.MinValue,
               double.MaxValue);

            double RootX = tree.InternalPointArray[0][0];
            double LeftY = tree.InternalPointArray[1][1], RightY = tree.InternalPointArray[2][1];
            double LeftLeftX = tree.InternalPointArray[3][0], LeftRightX = tree.InternalPointArray[4][0], RightLeftX = tree.InternalPointArray[5][0], RightRightX = tree.InternalPointArray[6][0];

            var cellPlacementDictionary = new Dictionary<int, int>();

            var cellRootX = (int)((RootX - BOARDERS[0]) / CELLSIZE);
            var cellLeftY = (int)(Math.Abs(LeftY - BOARDERS[3]) / CELLSIZE);
            var cellRightY = (int)(Math.Abs(RightY - BOARDERS[3]) / CELLSIZE);
            var cellLeftLeftX = (int)((LeftLeftX - BOARDERS[0]) / CELLSIZE);
            var cellLeftRightX = (int)((LeftRightX - BOARDERS[0]) / CELLSIZE);
            var cellRightLeftX = (int)((RightLeftX - BOARDERS[0]) / CELLSIZE);
            var cellRightRightX = (int)((RightRightX - BOARDERS[0]) / CELLSIZE);

            for (var y = 0; y < cellLeftY; y++)
            {
                for (var x = 0; x < cellLeftRightX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 0);
                for (var x = cellLeftRightX; x < cellRootX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 1);
            }
            for (var y = cellLeftY; y < CELLNUM_Y; y++)
            {
                for (var x = 0; x < cellLeftLeftX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 2);
                for (var x = cellLeftLeftX; x < cellRootX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 3);
            }

            for (var y = 0; y < cellRightY; y++)
            {
                for (var x = cellRootX; x < cellRightRightX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 4);
                for (var x = cellRightRightX; x < CELLNUM_X; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 5);
            }
            for (var y = cellRightY; y < CELLNUM_Y; y++)
            {
                for (var x = cellRootX; x < cellRightLeftX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 6);
                for (var x = cellRightLeftX; x < CELLNUM_X; x++)
                    cellPlacementDictionary.Add(y * CELLNUM_X + x, 7);
            }

            var grainPlacementList = new List<Dolphin.GrainPlacement>();
            foreach (var actordata in actorDataList)
            {
                var x = actordata.Value.Item1;
                var y = actordata.Value.Item2;
                var cId = Helper.CalCellId(new Point(x, y), BOARDERS, CELLSIZE);
                grainPlacementList.Add(new Dolphin.GrainPlacement(actordata.Key, cellPlacementDictionary[cId]));
            }

            var grainPlacementData = JsonConvert.SerializeObject(grainPlacementList, Formatting.Indented);
            File.WriteAllText(grainPlacementDir, grainPlacementData);

            var cellPlacementList = new List<Dolphin.CellPlacement>();
            foreach (var item in cellPlacementDictionary)
                cellPlacementList.Add(new Dolphin.CellPlacement(item.Key, item.Value));
            var cellPlacementData = JsonConvert.SerializeObject(cellPlacementList, Formatting.Indented);
            File.WriteAllText(cellPlacementDir, cellPlacementData);
        }

        private static double ConvertLatLngToMeter(Point src, Point nextPoint)//calclulate distance between two points
        {
            var dX = nextPoint.X * Math.PI / 180 - src.X * Math.PI / 180;
            var dY = nextPoint.Y * Math.PI / 180 - src.Y * Math.PI / 180;
            var a = Math.Sin(dX / 2) * Math.Sin(dX / 2) + Math.Cos(src.X * Math.PI / 180) * Math.Cos(nextPoint.X * Math.PI / 180) * Math.Sin(dY / 2) * Math.Sin(dY / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d * 1000; // meters
        }

        private static Point ConvertMetersToLatLng(Point src, Point limitPoint, double distance)//calculate a dst point from src to limitPoint at distanceExtra
        {
            var p = distance / ConvertLatLngToMeter(src, limitPoint);
            var x = src.X + p * (limitPoint.X - src.X);
            var y = src.Y + p * (limitPoint.Y - src.Y);
            return new Point(x, y);
        }

        private static Point GetNextPoint(Point src, int polyId)
        {
            Point dst;
            var listpoints = polyToPoints[polyId];
            int srcIndex = listpoints.IndexOf(src);
            if (srcIndex == -1)
                throw new Exception("Polygon " + polyId + " doesn't contains Point (" + src.X + ", " + src.Y + ").");
            else if (srcIndex == listpoints.Count - 1)
                dst = listpoints[0];
            else
                dst = listpoints[srcIndex + 1];

            return dst;
        }

        private static void GenerateGaussActors()
        {
            var HOTSPOTNUMARRAY = new int[] {
                Constants.HOTSPOT_NUM0,
                Constants.HOTSPOT_NUM1,
                Constants.HOTSPOT_NUM2,
                Constants.HOTSPOT_NUM3,
                Constants.HOTSPOT_NUM4,
                Constants.HOTSPOT_NUM5};
            var ACTORNUM = Constants.MOVINGACTOR_GAUSSNUM;//todo: not fixed 
            BOARDERS = new double[] { -Constants.BOARDER_GAUSS, -Constants.BOARDER_GAUSS, Constants.BOARDER_GAUSS, Constants.BOARDER_GAUSS };
            CELLSIZE = (BOARDERS[2] - BOARDERS[0]) / Math.Sqrt(Constants.CELLNUM_GAUSS);
            var gaussActorDataList = new List<GaussActorData>();
            var tjy = new Dictionary<Guid, List<Point>>();
            Dictionary<Guid, Point> GaussMovingActorInfo = new Dictionary<Guid, Point>();
            var tjyDir = "";
            for (var j = 0; j < HOTSPOTNUMARRAY.Length; j++)
            {
                var hotspotRange = Math.Sqrt(((BOARDERS[2] - BOARDERS[0]) * (BOARDERS[3] - BOARDERS[1])) / (HOTSPOTNUMARRAY[j] * Math.PI));
                var continuousDistribution = new Normal(0, hotspotRange/2);//from ben's paper-signma=range*0.5

                //generate hotspots
                var hotspots = new List<Point>();
                for (var i = 0; i < HOTSPOTNUMARRAY[j]; i++)
                {
                    var lat = random.NextDouble() * (BOARDERS[2] - BOARDERS[0]) + BOARDERS[0];
                    var lng = random.NextDouble() * (BOARDERS[3] - BOARDERS[1]) + BOARDERS[1];
                    lat = lat < BOARDERS[0] ? BOARDERS[0] : lat;
                    lat = lat > BOARDERS[2] ? BOARDERS[2] : lat;
                    lng = lng < BOARDERS[1] ? BOARDERS[1] : lng;
                    lng = lng > BOARDERS[3] ? BOARDERS[3] : lng;
                    hotspots.Add(new Point(lat, lng));
                }
                //generate actors
                for (var i = 0; i < ACTORNUM; i++)
                {
                    var k = random.Next(0, HOTSPOTNUMARRAY[j]);
                    var hotspot = hotspots[k];

                    var lct = GaussMove(hotspot, continuousDistribution.Sample());

                    lct.X = lct.X < BOARDERS[0] ? BOARDERS[0] : lct.X;
                    lct.X = lct.X > BOARDERS[2] ? BOARDERS[2] : lct.X;
                    lct.Y = lct.Y < BOARDERS[1] ? BOARDERS[1] : lct.Y;
                    lct.Y = lct.Y > BOARDERS[3] ? BOARDERS[3] : lct.Y;

                    var fence = CreateFence(lct.X, lct.Y);
                    var id = Guid.NewGuid();
                    gaussActorDataList.Add(new GaussActorData(id, lct.X, lct.Y, fence[0], fence[1], fence[2], fence[3], hotspot.X, hotspot.Y));
                    ActorData.Add(id, new Tuple<double, double>(lct.X, lct.Y));

                    GaussMovingActorInfo.Add(id,lct);
                    tjy.Add(id, new List<Point> {lct});
                    var n = 0;
                    while (n < Constants.TJYLENGTH)
                    {
                        var src = GaussMovingActorInfo[id];

                        var deltaLctX = src.X - hotspot.X;
                        var deltaLctY = src.Y - hotspot.Y;
                        double deltaLct = Math.Sqrt(Math.Pow(deltaLctX, 2) + Math.Pow(deltaLctY, 2));
                        var speedRate = deltaLct / (hotspotRange/2);
                        var deltaDistance = Constants.MAXSPEED * speedRate * Constants.UPDATEFREQUENCY;
                        Point dst;
                        if (deltaDistance < 0.01)
                            dst = src;
                        else
                            dst = GaussMove(hotspot, src, deltaDistance, continuousDistribution);
                        tjy[id].Add(dst);
                        GaussMovingActorInfo[id] =dst;
                        n++;
                    }
                }
                if (j == 0)
                {
                    dir = directoryPath + Constants.GAUSS_0;
                    grainPlacementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_0;
                    cellPlacementDir = directoryPath + Constants.GAUSS_CELLPLACEMENT_0;
                    tjyDir = directoryPath + Constants.GAUSS_TJY_0;
                }
                else if (j == 1)
                {
                    dir = directoryPath + Constants.GAUSS_1;
                    grainPlacementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_1;
                    cellPlacementDir = directoryPath + Constants.GAUSS_CELLPLACEMENT_1;
                    tjyDir = directoryPath + Constants.GAUSS_TJY_1;
                }
                else if (j == 2)
                {
                    dir = directoryPath + Constants.GAUSS_2;
                    grainPlacementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_2;
                    cellPlacementDir = directoryPath + Constants.GAUSS_CELLPLACEMENT_2;
                    tjyDir = directoryPath + Constants.GAUSS_TJY_2;
                }
                else if (j == 3)
                {
                    dir = directoryPath + Constants.GAUSS_3;
                    grainPlacementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_3;
                    cellPlacementDir = directoryPath + Constants.GAUSS_CELLPLACEMENT_3;
                    tjyDir = directoryPath + Constants.GAUSS_TJY_3;
                }
                else if (j == 4)
                {
                    dir = directoryPath + Constants.GAUSS_4;
                    grainPlacementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_4;
                    cellPlacementDir = directoryPath + Constants.GAUSS_CELLPLACEMENT_4;
                    tjyDir = directoryPath + Constants.GAUSS_TJY_4;
                }
                else if (j == 5)
                {
                    dir = directoryPath + Constants.GAUSS_5;
                    grainPlacementDir = directoryPath + Constants.GAUSS_GRAINPLACEMENT_5;
                    cellPlacementDir = directoryPath + Constants.GAUSS_CELLPLACEMENT_5;
                    tjyDir = directoryPath + Constants.GAUSS_TJY_5;
                }

                var json = JsonConvert.SerializeObject(gaussActorDataList, Formatting.Indented);
                File.WriteAllText(dir, json);
                gaussActorDataList.Clear();

                GenerateGrainPlacement(grainPlacementDir, cellPlacementDir, ActorData);
                ActorData.Clear();

                var TjyList = new List<ActorTjyData>();
                foreach (var item in tjy)
                    foreach (var p in item.Value)
                        TjyList.Add(new ActorTjyData(item.Key, p.X, p.Y));
                json = JsonConvert.SerializeObject(TjyList, Formatting.Indented);
                File.WriteAllText(tjyDir, json);
                tjy.Clear();
                GaussMovingActorInfo.Clear();
            }
        }

        private static Point GaussMove(Point hotspot, double distance)
        {
            var dst = hotspot;
            double degree = random.NextDouble() * 360;
            double angle = Math.PI * degree / 180.0;
            double x, y;

            var deltaX = distance * Math.Cos(angle);
            var deltaY = distance * Math.Sin(angle);
            x = hotspot.X + deltaX;
            y = hotspot.Y + deltaY;
            if (x > BOARDERS[2] || x < BOARDERS[0]) //x exceed boarder
            {
                deltaX = -deltaX;
                x = hotspot.X + deltaX;
                if (x > BOARDERS[0] && x < BOARDERS[2])//x doesn't exceed boarder after reverse
                {
                    deltaY = -deltaY;
                    y = hotspot.Y + deltaY;
                    if (y > BOARDERS[1] && y < BOARDERS[3]) //y doesn't exceed boarder after reverse
                        dst = new Point(x, y);
                }
                //too big move in x, dst=src
            }
            else if (y > BOARDERS[3] || y < BOARDERS[1]) //y exceed boarder
            {
                deltaY = -deltaY;
                y = hotspot.Y + deltaY;
                if (y > BOARDERS[1] && y < BOARDERS[3])//y doesn't exceed boarder after reverse
                {
                    deltaX = -deltaX;
                    x = hotspot.X + deltaX;
                    if (x > BOARDERS[0] && x < BOARDERS[2])//x doesn't exceed boarder after reverse
                        dst = new Point(x, y);
                }
                //too big move in Y, dst=src
            }
            else
                dst = new Point(x, y);

            if (dst.X > BOARDERS[2] || dst.X < BOARDERS[0] || dst.Y > BOARDERS[3] || dst.Y < BOARDERS[1])
                ;

            return dst;
        }

        private static Point GaussMove(Point hotspot, Point src, double deltaDistance, Normal normal)
        {
            var dst = hotspot;
            var validDst = false;
            while (!validDst) {
                var distance = normal.Sample();
                double degree = random.NextDouble() * 360;
                double angle = Math.PI * degree / 180.0;
                double x, y;

                var deltaX = distance * Math.Cos(angle);
                var deltaY = distance * Math.Sin(angle);
                x = hotspot.X + deltaX;
                y = hotspot.Y + deltaY;
                if (x > BOARDERS[2] || x < BOARDERS[0]) //x exceed boarder
                {
                    deltaX = -deltaX;
                    x = hotspot.X + deltaX;
                    if (x > BOARDERS[0] && x < BOARDERS[2])//x doesn't exceed boarder after reverse
                    {
                        deltaY = -deltaY;
                        y = hotspot.Y + deltaY;
                        if (y > BOARDERS[1] && y < BOARDERS[3]) //y doesn't exceed boarder after reverse
                            dst = new Point(x, y);
                    }
                    //too big move in x, dst=src
                }
                else if (y > BOARDERS[3] || y < BOARDERS[1]) //y exceed boarder
                {
                    deltaY = -deltaY;
                    y = hotspot.Y + deltaY;
                    if (y > BOARDERS[1] && y < BOARDERS[3])//y doesn't exceed boarder after reverse
                    {
                        deltaX = -deltaX;
                        x = hotspot.X + deltaX;
                        if (x > BOARDERS[0] && x < BOARDERS[2])//x doesn't exceed boarder after reverse
                            dst = new Point(x, y);
                    }
                    //too big move in Y, dst=src
                }
                else
                    dst = new Point(x, y);

                var currentDistance = Math.Sqrt(Math.Pow(dst.X - src.X, 2) + Math.Pow(dst.Y - src.Y, 2));
                if (currentDistance < deltaDistance)
                    validDst = true;
            }

            if (dst.X > BOARDERS[2] || dst.X < BOARDERS[0] || dst.Y > BOARDERS[3] || dst.Y < BOARDERS[1])
                ;

            return dst;
        }

        private static void GenerateUniformActors()
        {
            var BOARDERSLIST = new List<double[]> {
                new double[] { -Constants.BOARDER0, -Constants.BOARDER0, Constants.BOARDER0, Constants.BOARDER0 },
                new double[] { -Constants.BOARDER1, -Constants.BOARDER1, Constants.BOARDER1, Constants.BOARDER1 },
                new double[] { -Constants.BOARDER2, -Constants.BOARDER2, Constants.BOARDER2, Constants.BOARDER2 },
                new double[] { -Constants.BOARDER3, -Constants.BOARDER3, Constants.BOARDER3, Constants.BOARDER3 }
            };
            var ACTORNUMARRAY = new int[] { Constants.MOVINGACTOR_NUM0, Constants.MOVINGACTOR_NUM1, Constants.MOVINGACTOR_NUM2, Constants.MOVINGACTOR_NUM3 };
            var CELLNUMARRAY = new int[] { Constants.CELLNUM0, Constants.CELLNUM1, Constants.CELLNUM2, Constants.CELLNUM3 };

            var uniformActorDataList = new List<UniformActorData>();
            for (var j = 0; j < ACTORNUMARRAY.Length; j++)
            {
                BOARDERS = BOARDERSLIST[j];
                CELLSIZE = (BOARDERS[2] - BOARDERS[0]) / Math.Sqrt(CELLNUMARRAY[j]);
              

                for (var i = 0; i < ACTORNUMARRAY[j]; i++)
                {
                    var lat = random.NextDouble() * (BOARDERS[2] - BOARDERS[0]) + BOARDERS[0];
                    var lng = random.NextDouble() * (BOARDERS[3] - BOARDERS[1]) + BOARDERS[1];
                    lat = lat < BOARDERS[0] ? BOARDERS[0] : lat;
                    lat = lat > BOARDERS[2] ? BOARDERS[2] : lat;
                    lng = lng < BOARDERS[1] ? BOARDERS[1] : lng;
                    lng = lng > BOARDERS[3] ? BOARDERS[3] : lng;
                    var fence = CreateFence(lat, lng);
                    var id = Guid.NewGuid();
                    uniformActorDataList.Add(new UniformActorData(id, lat, lng, fence[0], fence[1], fence[2], fence[3]));
                    ActorData.Add(id, new Tuple<double, double>(lat, lng));
                }
                if (j == 0)
                {
                    dir = directoryPath + Constants.UNIFORM_0;
                    grainPlacementDir = directoryPath + Constants.UNIFORM_GRAINPLACEMENT_0;
                    cellPlacementDir = directoryPath + Constants.UNIFORM_CELLPLACEMENT_0;
                }
                if (j == 1)
                {
                    dir = directoryPath + Constants.UNIFORM_1;
                    grainPlacementDir = directoryPath + Constants.UNIFORM_GRAINPLACEMENT_1;
                    cellPlacementDir = directoryPath + Constants.UNIFORM_CELLPLACEMENT_1;
                }
                else if (j == 2)
                {
                    dir = directoryPath + Constants.UNIFORM_2;
                    grainPlacementDir = directoryPath + Constants.UNIFORM_GRAINPLACEMENT_2;
                    cellPlacementDir = directoryPath + Constants.UNIFORM_CELLPLACEMENT_2;
                }
                else if (j == 3)
                {
                    dir = directoryPath + Constants.UNIFORM_3;
                    grainPlacementDir = directoryPath + Constants.UNIFORM_GRAINPLACEMENT_3;
                    cellPlacementDir = directoryPath + Constants.UNIFORM_CELLPLACEMENT_3;
                }

                var uniformActorData = JsonConvert.SerializeObject(uniformActorDataList, Formatting.Indented);
                File.WriteAllText(dir, uniformActorData);
                uniformActorDataList.Clear();

                GenerateGrainPlacement(grainPlacementDir, cellPlacementDir, ActorData);
                ActorData.Clear();
            }
        }

        private static void GenerateGrainPlacement(string grainPlacementDir, string cellPlacementDir, Dictionary<Guid, Tuple<double, double>> actorDataList)
        {

            List<double[]> pointsList = new List<double[]>();
            List<Guid> nodesList = new List<Guid>();
            foreach (var actorData in actorDataList) {
                pointsList.Add(new double[] { actorData.Value.Item1, actorData.Value.Item2 });
                nodesList.Add(actorData.Key);
            }

            var tree = new KDTree<double, Guid>(2,
               pointsList.ToArray(),
               nodesList.ToArray(),
               Utilities.L2Norm_Squared_Double,
               double.MinValue,
               double.MaxValue);

            double RootX = tree.InternalPointArray[0][0];
            double LeftY = tree.InternalPointArray[1][1], RightY = tree.InternalPointArray[2][1];
            double LeftLeftX = tree.InternalPointArray[3][0], LeftRightX = tree.InternalPointArray[4][0], RightLeftX = tree.InternalPointArray[5][0], RightRightX = tree.InternalPointArray[6][0];

            var cellPlacementDictionary = new Dictionary<int, int>();


            var CELLNUM = (int)((BOARDERS[2] - BOARDERS[0]) / CELLSIZE);
            var cellRootX = (int)((RootX - BOARDERS[0]) / CELLSIZE);
            var cellLeftY = (int)(Math.Abs(LeftY - BOARDERS[3]) / CELLSIZE);
            var cellRightY = (int)(Math.Abs(RightY - BOARDERS[3]) / CELLSIZE);
            var cellLeftLeftX = (int)((LeftLeftX - BOARDERS[0]) / CELLSIZE);
            var cellLeftRightX = (int)((LeftRightX - BOARDERS[0]) / CELLSIZE);
            var cellRightLeftX = (int)((RightLeftX - BOARDERS[0]) / CELLSIZE);
            var cellRightRightX = (int)((RightRightX - BOARDERS[0]) / CELLSIZE);

            for (var y = 0; y < cellLeftY; y++)
            {
                for (var x = 0; x < cellLeftRightX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 0);
                for (var x = cellLeftRightX; x < cellRootX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 1);
            }
            for (var y = cellLeftY; y < CELLNUM; y++)
            {
                for (var x = 0; x < cellLeftLeftX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 2);
                for (var x = cellLeftLeftX; x < cellRootX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 3);
            }

            for (var y = 0; y < cellRightY; y++)
            {
                for (var x = cellRootX; x < cellRightRightX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 4);
                for (var x = cellRightRightX; x < CELLNUM; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 5);
            }
            for (var y = cellRightY; y < CELLNUM; y++)
            {
                for (var x = cellRootX; x < cellRightLeftX; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 6);
                for (var x = cellRightLeftX; x < CELLNUM; x++)
                    cellPlacementDictionary.Add(y * CELLNUM + x, 7);
            }

            var grainPlacementList = new List<Dolphin.GrainPlacement>();
            foreach (var actordata in actorDataList)
            {
                var lat = actordata.Value.Item1;
                var lng = actordata.Value.Item2;
                var cId = Helper.CalCellId(new Point(lat, lng), BOARDERS, CELLSIZE);   
                grainPlacementList.Add(new Dolphin.GrainPlacement(actordata.Key, cellPlacementDictionary[cId]));
            }

            var grainPlacementData = JsonConvert.SerializeObject(grainPlacementList, Formatting.Indented);
            File.WriteAllText(grainPlacementDir, grainPlacementData);

            var cellPlacementList = new List<Dolphin.CellPlacement>();
            foreach (var item in cellPlacementDictionary)
                cellPlacementList.Add(new Dolphin.CellPlacement(item.Key, item.Value));
            var cellPlacementData = JsonConvert.SerializeObject(cellPlacementList, Formatting.Indented);
            File.WriteAllText(cellPlacementDir, cellPlacementData);
        }

        private static double[] CreateFence(double x, double y)
        {
            var minX = x - FENCESIZE;
            var maxX = x + FENCESIZE;
            var minY = y - FENCESIZE;
            var maxY = y + FENCESIZE;
            minX = minX <= BOARDERS[0] ? BOARDERS[0] : minX;
            maxX = maxX >= BOARDERS[2] ? BOARDERS[2] : maxX;
            minY = minY <= BOARDERS[1] ? BOARDERS[1] : minY;
            maxY = maxY >= BOARDERS[3] ? BOARDERS[3] : maxY;

            return new double[] { minX, minY, maxX, maxY };
        }

    }
}

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NetTopologySuite.Geometries;
using System.Linq;
using System.Collections.Generic;
using Envelope = RBush.Envelope;

namespace Dolphin.Utilities
{
    public static class Helper
    {
        public static List<int> FindCellIds(Envelope e, double[] boarders, double cellsize)
        {
            var cellIds = new List<int>();

            int minYId = (int)Math.Ceiling((boarders[3] - e.MinY) / cellsize);
            int maxYId = (int)Math.Floor((boarders[3] - e.MaxY) / cellsize);
            int minXId = (int)Math.Floor((e.MinX - boarders[0]) / cellsize);
            int maxXId = (int)Math.Ceiling((e.MaxX - boarders[0]) / cellsize);
            for (int y = maxYId; y < minYId; y++)
                for (int x = minXId; x < maxXId; x++)
                    cellIds.Add(y * (int)((boarders[3] - boarders[1]) / cellsize) + x);
            return cellIds;
        }

        public static int CalCellId(Point lct, double[] boarders, double cellsize)
        {
            if (lct.X == boarders[2])
                lct.X = boarders[2] - boarders[2] / 1000000;
            if (lct.Y == boarders[1])
                lct.Y = boarders[1] + boarders[1] / 1000000;
            return (int)(Math.Floor((lct.X - boarders[0]) / cellsize) + Math.Floor((boarders[3] - lct.Y) / cellsize) * Math.Ceiling((boarders[2] - boarders[0]) / cellsize));
        }

        public static Envelope CalEnvelope(Polygon polygon)
        {
            var coordinates = polygon.Coordinates.ToList();
            var minX = Double.MaxValue;
            var minY = Double.MaxValue;
            var maxX = Double.MinValue;
            var maxY = Double.MinValue;
            foreach (var coordinate in coordinates) 
            {
                if (coordinate.X < minX)
                    minX = coordinate.X;
                if (coordinate.X > maxX)
                    maxX = coordinate.X;
                if (coordinate.Y < minY)
                    minY = coordinate.Y;
                if (coordinate.Y > maxY)
                    maxY = coordinate.Y;
            }

            return new Envelope(minX, minY, maxX, maxY);
        }


        public static void ReadConfigFile() {
            string line;
            StreamReader file =new StreamReader(@"");
            while ((line = file.ReadLine()) != null)
            {
            }

            file.Close();
        }

        public static Guid ConvertIntToGuid(int value)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 8);
            return new Guid(bytes);
        }

        public static byte[] SerializeToByteArray<T>(T obj)
        {
            if (obj == null)
            {
                return null;
            }

            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        public static T DeserializeFromByteArray<T>(byte[] obj)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            //Clean up 
            stream.Write(obj, 0, obj.Length);
            stream.Seek(0, SeekOrigin.Begin);
            var result = (T)formatter.Deserialize(stream);
            return result;
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static long IPAddressToLong(IPAddress address)
        {
            byte[] byteIP = address.GetAddressBytes();

            long ip = (long)byteIP[3] << 24;
            ip += (long)byteIP[2] << 16;
            ip += (long)byteIP[1] << 8;
            ip += (long)byteIP[0];
            return ip;
        }

        public static byte[] serializeToByteArray<T>(T obj)
        {
            if (obj == null)
            {
                return null;
            }

            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        public static T deserializeFromByteArray<T>(byte[] obj)
        {
            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            //Clean up 
            stream.Write(obj, 0, obj.Length);
            stream.Seek(0, SeekOrigin.Begin);
            var result = (T)formatter.Deserialize(stream);
            return result;
        }

    }
}

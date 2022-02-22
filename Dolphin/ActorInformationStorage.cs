using NetTopologySuite.Geometries;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dolphin
{
    public interface IActorInformationStorage : IGrainWithIntegerKey {
        Task SendBuffer(Guid id, List<Point> Buffer);
        Task<List<Point>> GetBuffer(Guid id);
    }

    public class ActorInformationStorage : Grain, IActorInformationStorage
    {
        private Dictionary<Guid, List<Point>> ActorTjy = new Dictionary<Guid, List<Point>>();

        Task<List<Point>> IActorInformationStorage.GetBuffer(Guid id)
        {
            return Task.FromResult(ActorTjy[id]);
        }

        Task IActorInformationStorage.SendBuffer(Guid id, List<Point> Buffer)
        {
            if (ActorTjy.ContainsKey(id)) {
                var src = Buffer[0];
                var dst = ActorTjy[id][ActorTjy[id].Count - 2];
                if (src != dst)
                    Console.WriteLine(id+","+Buffer+","+dst);

                ActorTjy[id].AddRange(Buffer);
            }
                
            else
                ActorTjy.Add(id, Buffer);
            return Task.CompletedTask;
        }


    }
}

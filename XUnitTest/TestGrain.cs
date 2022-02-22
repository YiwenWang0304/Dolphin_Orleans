using Dolphin;
using Dolphin.Interfaces;
using NetTopologySuite.Geometries;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XUnitTest
{

    public interface ITestGrain : IGrainWithGuidKey, IMovingActorMixin
    {
        Task VibrationInitialize(Point lct0, Point lct1, Polygon fence, string providerToUse, Semantics semantics, double[] boarders, double cellsize);
        Task<Point> GetLct0();
        Task<Point> VibrationMove();
    }

    public class TestGrain : Grain, ITestGrain
    {
        private IMovingActorMixin movingActor;
        private IGrainFactory grainFactory;
        private Point Lct0 = new Point(0, 0);
        private Point Lct1 = new Point(0, 0);

        async public override Task OnActivateAsync()
        {
            grainFactory = GrainFactory;
            await base.OnActivateAsync();
        }

        Task ITestGrain.VibrationInitialize(Point lct0, Point lct1, Polygon fence, string providerToUse, Semantics semantics, double[] boarders, double cellsize)
        {
            Lct0 = lct0;
            Lct1 = lct1;
            movingActor = new MovingActorMixin(grainFactory, this.GetPrimaryKey(), lct0, fence, base.GetStreamProvider(providerToUse), semantics, boarders, cellsize);
            return Task.CompletedTask;
        }

        async Task<Point> ITestGrain.VibrationMove()
        {
            var lct = await movingActor.GetPoint();
            if (lct.Equals(Lct0))
                lct = Lct1;
            else if (lct.Equals(Lct1))
                lct = Lct0;
            else throw new ArgumentException("Wrong Point in VibrationMove test.");
            return lct;
        }

        Task<Point> ITestGrain.GetLct0()
        {
            return Task.FromResult(Lct0);
        }

        Task<Point> IMovingActorMixin.GetPoint()
        {
            return movingActor.GetPoint();
        }

        Task<int> IMovingActorMixin.GetCellId()
        {
            return movingActor.GetCellId();
        }

        Task IMovingActorMixin.Subscribe(Predicates predicate, StreamInfo streamInfo)
        {
            return movingActor.Subscribe(predicate, streamInfo);
        }

        Task IMovingActorMixin.UnSubscribe(int handle)
        {
            movingActor.UnSubscribe(handle);
            return Task.CompletedTask;
        }

        Task<Polygon> IMovingActorMixin.GetFence()
        {
            return movingActor.GetFence();
        }

        Task<int> IMovingActorMixin.GetNumNotification()
        {
            return movingActor.GetNumNotification();
        }

        Task IMovingActorMixin.Move(Point dst)
        {
            return movingActor.Move(dst);
        }

        Task<List<ActorInfo>> IMovingActorMixin.FindNearbyActors()
        {
            return movingActor.FindNearbyActors();
        }

        Task<int> IMovingActorMixin.GetCount()
        {
            return movingActor.GetCount();
        }
    }

}

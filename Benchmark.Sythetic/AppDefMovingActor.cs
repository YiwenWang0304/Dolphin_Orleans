using Dolphin;
using Dolphin.Interfaces;
using Dolphin.Utilities;
using NetTopologySuite.Geometries;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark.Synthetic
{
    public interface IAppDefMovingActor : IGrainWithGuidKey, IMovingActorMixin
    {
        Task Initialize(Point lct, Polygon fence, Semantics semantics, double[] boarders, double cellsize);
        Task Subscribe(Predicates predicate);
        Task SetTimer(double interval, DateTime initTime);

        Task SendBuffer();
    }

    [SpatialPreferPlacementStrategy]
    public class AppDefMovingActor : Grain, IAppDefMovingActor
    {
        private IMovingActorMixin movingActor;

        Task IAppDefMovingActor.Initialize(Point lct,Polygon fence, Semantics semantics, double[] boarders, double cellsize)
        {
            movingActor = new MovingActorMixin(GrainFactory, base.GetStreamProvider(Constants.STREAMPROVIDER), this.GetPrimaryKey(), lct, fence, semantics, boarders, cellsize);
            return Task.CompletedTask;
        }

        Task IAppDefMovingActor.SetTimer(double interval, DateTime initTime)
        {
            var currentTime = DateTime.Now;
            var dueTime = (Constants.TIMER_DUETIME * 1000 - (currentTime.Ticks / TimeSpan.TicksPerMillisecond - initTime.Ticks / TimeSpan.TicksPerMillisecond)) / 1000;
            if (dueTime < 0)
                Console.WriteLine("Please give more time for actor initialize.");
            TimeSpan period;
            if (interval < 1)
                period = new TimeSpan(0, 0, 0, 0, (int)(interval * 1000));
            else
                period = new TimeSpan(0, 0, 0, (int)interval);
            this.RegisterTimer(OnTimeEvent, null, new TimeSpan(0, 0, (int)dueTime), period);
            return Task.CompletedTask;
        }


        private async Task OnTimeEvent(object arg)
        {
            //await movingActor.OnTimeSendBuffer();
            
                await GrainFactory.GetGrain<IAppDefMovingActor>(this.GetPrimaryKey()).SendBuffer();
            
        }
        async Task IAppDefMovingActor.SendBuffer()
        {
            await movingActor.OnTimeSendBuffer();
        }

        async Task IMovingActorMixin.Move(Point dst)
        {
            try
            {
                //if (dst.X < -122.514586 || dst.X > -122.357189 || dst.Y < 37.708289 || dst.Y > 37.810644)
                //    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + dst + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                await movingActor.Move(dst);
            }
            catch (SubscribeUpdateException e)
            {
                throw e;
            }
            catch (SnapshotBufferedDataUpdateException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new MovingActorException(e);
            }

        }

        async Task<List<ActorInfo>> IMovingActorMixin.FindActors(RBush.Envelope queryRange)
        {
            return await movingActor.FindActors(queryRange); 
        }

        Task IMovingActorMixin.UnSubscribe(int handle)
        {
            return movingActor.UnSubscribe(handle);
        }

        async Task IAppDefMovingActor.Subscribe(Predicates predicate)
        {
            await movingActor.Subscribe(predicate, AsyncCallback); 
        }

        async Task AsyncCallback(ReactionInfo msg)
        {
            await GrainFactory.GetGrain<IAppDefMovingActor>(msg.ReceiverId).ReceiveMSG(msg.MoveId);
        }

        async Task IMovingActorMixin.Subscribe(Predicates predicate, Func<ReactionInfo, Task>  asynCallBack)
        {
            await movingActor.Subscribe(predicate, asynCallBack); 
        }

        //async Task<int> IMovingActorMixin.GetCellId()
        //{
        //    return await movingActor.GetCellId();
        //}

        Task<List<Tuple<long, double>>> IMovingActorMixin.GetReactionNumAndLatencies()
        {
            return movingActor.GetReactionNumAndLatencies();
        }

        Task IMovingActorMixin.ReceiveMSG(long moveId)
        {
            return movingActor.ReceiveMSG(moveId);
        }

        Task IMovingActorMixin.OnTimeSendBuffer()
        {
            return movingActor.OnTimeSendBuffer();
        }

        Task IMovingActorMixin.NOP(Point pst)
        {
            return movingActor.NOP(pst);
        }

        Task<Tuple<List<double>, List<double>, List<double>, List<double>, List<double>, List<double>, List<double>>> IMovingActorMixin.GetBreakDownLatencies()
        {
            return movingActor.GetBreakDownLatencies();
        }

        Task<int> IMovingActorMixin.GetSubscribeTaskNum()
        {
            return movingActor.GetSubscribeTaskNum();
        }

    }

}

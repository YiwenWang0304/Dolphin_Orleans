using Dolphin.Interfaces;
using NetTopologySuite.Geometries;
using Orleans;
using RBush;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Envelope = RBush.Envelope;

namespace Dolphin
{
    [SpatialPreferPlacementStrategy]
    public class RTreeGrain : Grain, IRTree
    {
        public RBush<BBOX> Tree { set; get; } //Higher value means faster insertion and slower search; default 9
        //public RBush<BBOX> ReadOnlyTree { set; get; } //Higher value means faster insertion and slower search; default 9
        public int VersionNum { set; get; }
        //private Semantics semantics { set; get; }
     
        async public override Task OnActivateAsync()
        {
            //IdToBBOX = new Dictionary<Guid, BBOX>();
            //ReadOnlyIdToBBOX = new Dictionary<Guid, BBOX>();
            Tree = new RBush<BBOX>(maxEntries: 9);
            //ReadOnlyTree = new RBush<BBOX>(maxEntries: 9);
            VersionNum = 0;
            await base.OnActivateAsync();
        }

        Task IRTree.Initialize(Guid id, Point lct) {
            var bbox = new BBOX(id, lct.X, lct.Y, lct.X, lct.Y);
            Tree.Insert(bbox);
            //ReadOnlyTree.Insert(bbox);
            return Task.CompletedTask;
        }

        Task IRTree.Update(Guid id, Point src, Point dst)
        {
            Update(id,src,dst);    
            return Task.CompletedTask;
        }

        Task IRTree.SnapshotUpdate(List<Tuple<Guid, Point, Point>> updateBuffer, List<Tuple<Guid, Point>> insertBuffer, List<Tuple<Guid, Point>> deleteBuffer)
        {

            Debug.Assert(!CheckUpdateBufferDuplication(updateBuffer));
            Debug.Assert(!CheckInsertBufferDuplication(insertBuffer));
            Debug.Assert(!CheckDeleteBufferDuplication(deleteBuffer));
            Debug.Assert(!CheckBufferDuplication(updateBuffer, insertBuffer, deleteBuffer));
            //--------------------UPDATE--------------------------- 
            foreach (var item in updateBuffer)
                Update(item.Item1, item.Item2, item.Item3);

            //--------------------INSERT---------------------------
            try
            {
                foreach (var item in insertBuffer)
                    Insert(item.Item1, item.Item2);
            }catch(Exception e){
                throw e;
            }

            //--------------------DELETE---------------------------
            try
            {
                foreach (var item in deleteBuffer)
                    Delete(item.Item1, item.Item2);
            }catch(Exception e){
                throw e;
            }

            //try
            //{
            //   ReadOnlyTree.Clear();
            //   var bboxs = Tree.Search();
            //   ReadOnlyTree.BulkLoad(bboxs);
            //    VersionNum++; 
            //}catch (Exception e){
            //    throw e;
            //}

            VersionNum++;

            return Task.CompletedTask;
        }

        private bool CheckBufferDuplication(List<Tuple<Guid, Point, Point>> updateBuffer, List<Tuple<Guid, Point>> insertBuffer, List<Tuple<Guid, Point>> deleteBuffer)
        {
            var Ids = new List<Guid>();
            foreach (var item in updateBuffer)
                Ids.Add(item.Item1);
            foreach (var item in insertBuffer)
                Ids.Add(item.Item1);
            foreach (var item in deleteBuffer)
                Ids.Add(item.Item1);
            if (Ids.Count == 0)
                return false;
            return AreAnyDuplicates(Ids);
        }

        private bool CheckUpdateBufferDuplication(List<Tuple<Guid, Point, Point>> updateBuffer)
        {
            if (updateBuffer.Count == 0)
                return false;
            var updateIds = new List<Guid>();
            foreach (var item in updateBuffer)
                updateIds.Add(item.Item1);
            return AreAnyDuplicates(updateIds);
        }

        private bool CheckInsertBufferDuplication(List<Tuple<Guid, Point>> insertBuffer)
        {
            if (insertBuffer.Count == 0)
                return false;
            var insertIds = new List<Guid>();
            foreach (var item in insertBuffer)
                insertIds.Add(item.Item1);
            return AreAnyDuplicates(insertIds);

        }

        private bool CheckDeleteBufferDuplication(List<Tuple<Guid, Point>> deleteBuffer)
        {
            if (deleteBuffer.Count == 0)
                return false;
            var deleteIds = new List<Guid>();
            foreach (var item in deleteBuffer)
                deleteIds.Add(item.Item1);
            return AreAnyDuplicates(deleteIds);
        }

        private bool AreAnyDuplicates<T>(IEnumerable<T> list)
        {
            var hashset = new HashSet<T>();
            return list.Any(e => !hashset.Add(e));
        }

        private void Update(Guid id, Point src, Point dst)
        {
            var oldBBOXES = Tree.Search(new Envelope(src.X, src.Y, src.X, src.Y));
            var IfExist = false;
            BBOX oldBBOX_src = new BBOX();
            foreach (var oldBBOX in oldBBOXES)
                if (oldBBOX.Id.Equals(id))
                {
                    IfExist = true;
                    oldBBOX_src = oldBBOX;
                }

            var i = Tree.Search().Count;
            if (IfExist) {
                try
                {
                    Tree.Delete(oldBBOX_src);
                }
                catch (InvalidOperationException) {
                    Tree = new RBush<BBOX>();
                }
                catch (Exception e)
                {
                    Console.WriteLine(i);
                    Console.WriteLine(oldBBOX_src.Id + "," + oldBBOX_src.Envelope.MinX);
                    Console.WriteLine(Tree.Search().Count);
                    throw new Exception(e + "Update failed");
                }
            }
            else
                throw new Exception("Update failed! Moving actor " + id + " with lct " + src + " is wrong.");

            var newBBox = new BBOX(id, dst.X, dst.Y, dst.X, dst.Y);
            try
            { 
                Tree.Insert(newBBox);
            }
            catch (Exception e)
            {
                throw new Exception(e + " Update failed");
            }

        }

        Task IRTree.Insert(Guid id, Point lct)
        {
            Insert(id,lct);
            return Task.CompletedTask;
        }

        private void Insert(Guid id, Point lct) 
        {
            var bbox = new BBOX(id, lct.X, lct.Y, lct.X, lct.Y);
            try
            {
                Tree.Insert(bbox);
            }
            catch (Exception e)
            {
                throw new Exception(e + " Insert failed");
            }
        }

        Task IRTree.Delete(Guid id, Point lct)
        {
            Delete(id, lct);
            return Task.CompletedTask;
        }

        private void Delete(Guid id, Point lct)
        { 
            var oldBBOXES = Tree.Search(new Envelope(lct.X, lct.Y, lct.X, lct.Y));

            var IfExist = false;
            BBOX oldBBOX_src = new BBOX();
            foreach (var oldBBOX in oldBBOXES)
                if (oldBBOX.Id.Equals(id)) {
                    IfExist = true;
                    oldBBOX_src = oldBBOX;
                }

            if (IfExist) {
                try
                {
                    Tree.Delete(oldBBOX_src);
                }
                catch (InvalidOperationException)
                {
                    Tree = new RBush<BBOX>();
                }
                catch (Exception e)
                {
                    throw new Exception(e + "Delete failed");
                }
            }
            else
                throw new Exception("Delete failed! Moving actor " + id + " with " + lct + " is wrong.");
        }

         async Task<bool> IRTree.IfExist(Guid id, Point src, Point dst) {
            var bboxes = Tree.Search(new Envelope(src.X, src.Y, src.X, src.Y));
            foreach (var bbox in bboxes)
                if (bbox.Id.Equals(id))
                    return true;

            var b = await GrainFactory.GetGrain<IActorInformationStorage>(0).GetBuffer(id);
            foreach (var i in b)
                Console.WriteLine(i);
            Console.WriteLine(id + " src: " + src + ", dst: " + dst);
            return false;
        }

       async Task<bool> IRTree.IfNotExist(Guid id, Point dst, Point src)
        {
            var bboxes = Tree.Search(new Envelope(src.X, src.Y, src.X, src.Y));
            foreach (var bbox in bboxes)
                if (bbox.Id.Equals(id))
                {
                    var b = await GrainFactory.GetGrain<IActorInformationStorage>(0).GetBuffer(id);
                    foreach (var i in b)
                        Console.WriteLine(i);
                    Console.WriteLine("Version Num:" + VersionNum);
                    Console.WriteLine(id + "DST: " + dst + ", SRC: " + src);
                    Console.WriteLine(bboxes[0].Envelope.MinX + " | " + bboxes[0].Envelope.MinY);
                    return false;
                }

            return true;
        }

        Task IRTree.Clear()
        {
            Tree.Clear();
            return Task.CompletedTask;
        }

        Task<Tuple<int,List<ActorInfo>>> IRTree.RangeQuery(Envelope e)
        {
            var actorInfoswithVersion = new Tuple<int, List<ActorInfo>>(VersionNum,new List<ActorInfo>());
            //IReadOnlyList<BBOX> bboxes;
            //if (semantics == Semantics.Snapshot)
            //    bboxes = ReadOnlyTree.Search(e);
            //else 
            var  bboxes = Tree.Search(e);
               
            foreach (var bbox in bboxes)
                actorInfoswithVersion.Item2.Add(new ActorInfo(bbox.Id, new Point(bbox.Envelope.MinX, bbox.Envelope.MinY)));

            return Task.FromResult(actorInfoswithVersion);
        }

        Task IRTree.NOP(Point pst)
        {
            return Task.CompletedTask;
        }

        Task IRTree.DeleteUpdate(Guid id, Point deleteLct, Point src, Point dst)
        {
            Delete(id, deleteLct);
            Update(id, src, dst);
            return Task.CompletedTask;
        }

        Task IRTree.DeleteInsert(Guid id, Point deleteLct, Point insertLct)
        {
            Delete(id, deleteLct);
            Insert(id, insertLct);
            return Task.CompletedTask;
        }
    }
}

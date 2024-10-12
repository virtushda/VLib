using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace VLib
{
    /// <summary> An interface that facilitates gathering transforms for parallel AND/OR virtual access. </summary>
    public interface ITransformAccessGatherer
    {
        GameObject gameObject { get; }
    }

    public static class ITransformAccessGathererExtensions
    {
        public static void AcquireRequestorsWithPooledList(this ITransformAccessGatherer gatherer, out List<ITransformAccessRequestor> requestors)
        {
            requestors = ListPool<ITransformAccessRequestor>.Get();
            gatherer.gameObject.GetComponentsInChildren(requestors);
        }
        
        /// <summary> Passes a list reference around to gather up transforms.
        /// Automatically gathers by getcomponent calls. </summary>
        public static void GatherTransformsFromAllChildComponents(this ITransformAccessGatherer gatherer, bool respectComponentStates, 
            List<ITransformAccessRequestor> requestors, HashList<Transform> gatheredTransforms, HashList<Transform> gatheredWriteTransforms)
        {
            foreach (var requestor in requestors)
            {
                // Respect Unity Rules
                if (respectComponentStates && requestor is MonoBehaviour requestorObj)
                {
                    if (!requestorObj || !requestorObj.gameObject.activeInHierarchy || !requestorObj.enabled)
                        continue;
                }
                requestor.GatherTransforms(ref gatheredTransforms, ref gatheredWriteTransforms);
            }
        }
        
        public static void SendTransformTreeToChildComponents(this ITransformAccessGatherer gatherer, 
            bool respectComponentStates, VirtualTransformTree transformTree, List<ITransformAccessRequestor> requestors)
        {
            foreach (var requestor in requestors)
            {
                // Respect Unity Rules
                if (respectComponentStates && requestor is MonoBehaviour requestorObj)
                {
                    if (!requestorObj || !requestorObj.gameObject.activeInHierarchy || !requestorObj.enabled)
                        continue;
                } 
                requestor.ReceiveVirtualTransformTree(transformTree);
            }
        }
        
        public static void SendTransformNativeTreeToChildComponents(this ITransformAccessGatherer gatherer, 
            bool respectComponentStates, VirtualValueTransformTree transformTree, List<ITransformAccessRequestor> requestors)
        {
            // Setup for accessing the tree safely
            transformTree.EnableAccessGuard();
            
            foreach (var requestor in requestors)
            {
                // Respect Unity Rules
                if (respectComponentStates && requestor is MonoBehaviour requestorObj)
                {
                    if (!requestorObj || !requestorObj.gameObject.activeInHierarchy || !requestorObj.enabled)
                        continue;
                }

                try
                {
                    requestor.ReceiveVirtualTransformTreeNative(transformTree);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error sending transform tree to requestor: {requestor.GetType()}! -- {e}");
                }
            }
        }
        
        /*public static void SendTransformTreeNativeToChildComponents(this ITransformAccessGatherer gatherer, 
            bool respectComponentStates, VirtualValueTransformTree transformTree, List<ITransformAccessRequestor> requestors)
        {
            foreach (var requestor in requestors)
            {
                // Respect Unity Rules
                if (respectComponentStates && requestor is MonoBehaviour requestorObj)
                {
                    if (!requestorObj || !requestorObj.gameObject.activeInHierarchy || !requestorObj.enabled)
                        continue;
                } 
                requestor.ReceiveVirtualTransformTreeNative(transformTree);
            }
        }*/
        
        public static void ReleaseRequestorList(this ITransformAccessGatherer gatherer, List<ITransformAccessRequestor> requestors)
        {
            ListPool<ITransformAccessRequestor>.Release(requestors);
        }

        public static void AutoConstructAndDistributeTree(this ITransformAccessGatherer gatherer,
            bool respectComponentStates, out List<Transform> allTransforms, out List<Transform> writeTransforms, out VirtualTransformTree virtualTransformTree)
        {
            var requestors = ProcessTransformGathering(gatherer, respectComponentStates, out allTransforms, out writeTransforms);

            virtualTransformTree = new VirtualTransformTree(allTransforms);
            
            gatherer.SendTransformTreeToChildComponents(respectComponentStates, virtualTransformTree, requestors);
            gatherer.ReleaseRequestorList(requestors);
        }

        public static void AutoConstructAndDistributeNativeTree(this ITransformAccessGatherer gatherer, short ownerID,
            bool respectComponentStates, out List<Transform> allTransforms, out List<Transform> writeTransforms, out VirtualValueTransformTree virtualTransformTree)
        {
            var requestors = ProcessTransformGathering(gatherer, respectComponentStates, out allTransforms, out writeTransforms);

            virtualTransformTree = new VirtualValueTransformTree(ownerID, allTransforms);
            
            gatherer.SendTransformNativeTreeToChildComponents(respectComponentStates, virtualTransformTree, requestors);
            gatherer.ReleaseRequestorList(requestors);
        }
        
        static List<ITransformAccessRequestor> ProcessTransformGathering(ITransformAccessGatherer gatherer, bool respectComponentStates, out List<Transform> allTransforms, out List<Transform> writeTransforms)
        {
            var allTransformsProtected = new HashList<Transform>();
            var transformsWriteProtected = new HashList<Transform>();

            gatherer.AcquireRequestorsWithPooledList(out var requestors);
            gatherer.GatherTransformsFromAllChildComponents(respectComponentStates, requestors, allTransformsProtected, transformsWriteProtected);

            // Filter nulls
            // Affects the hashset too
            int sanityLimit = 4096;
            while (sanityLimit > 0 && allTransformsProtected.Contains(null))
            {
                sanityLimit--;
                allTransformsProtected.Remove(null);
            }

            allTransforms = allTransformsProtected.list;
            writeTransforms = transformsWriteProtected.list;
            return requestors;
        }
    }
}
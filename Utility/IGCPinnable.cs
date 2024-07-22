using System.Runtime.InteropServices;
using UnityEngine;

namespace VLib
{
    public interface IGCPinnable<T>
    {
        public T InternalObjRef { get; }
        public GCHandle? InternalDirectGCHandle { get; set; }
        public int InternalGCHandleHolderCount { get; set; }
    }

    public static class IGCPinnableExt
    {
        public static void EnsureGCHandleAllocated<T>(this IGCPinnable<T> pinnable)
        {
            if (!pinnable.InternalDirectGCHandle.HasValue)
                pinnable.InternalDirectGCHandle = GCHandle.Alloc(pinnable.InternalObjRef);
        }

        public static void EnsureGCHandleDeallocated<T>(this IGCPinnable<T> pinnable)
        {
            if (pinnable.InternalDirectGCHandle.HasValue)
            {
                pinnable.InternalDirectGCHandle.Value.Free();
                pinnable.InternalDirectGCHandle = null;
                pinnable.InternalGCHandleHolderCount = 0;
            }
        }

        /// <summary> Gets the GCHandle, increments an internal counter by 1.
        /// Call 'ReleaseGCHandleHold' when you're done! </summary>
        public static GCHandle GetGCHandleHold<T>(this IGCPinnable<T> pinnable)
        {
            pinnable.EnsureGCHandleAllocated();
            pinnable.InternalGCHandleHolderCount++;
            return pinnable.InternalDirectGCHandle.Value;
        }

        /// <summary> Deincrements an internal counter and if counter is (less than 1) GCHAndle is deallocated automatically. </summary>
        public static void ReleaseGCHandleHold<T>(this IGCPinnable<T> pinnable)
        {
            pinnable.InternalGCHandleHolderCount--;
            
            if (pinnable.InternalGCHandleHolderCount < 1)
            {
                pinnable.EnsureGCHandleDeallocated();
                return;
            }
        }
    }

    public abstract class MonoBehaviourPinnable : MonoBehaviour, IGCPinnable<MonoBehaviourPinnable>
    {
        public MonoBehaviourPinnable InternalObjRef => this;
        public GCHandle? InternalDirectGCHandle { get; set; }
        public int InternalGCHandleHolderCount { get; set; }
    }
}
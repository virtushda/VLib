/*using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using VLib.Libraries.VLib.Unsafe.Utility;
using VLib.Systems;

namespace VLib.Safety
{
    public struct ReadWriteSwitchLock
    {
        VUnsafeRef<LockValues> lockValues;
        
        public void Read(int timeoutMS)
        {
            ref var lockValuesRef = ref lockValues.ValueRef;
            do
            {
                // Lock Read
                Interlocked.Increment(ref lockValuesRef.readLock);

                // Check if write lock is held
                if (Volatile.Read(ref lockValuesRef.writeLock) == 0)
                    return; // Read lock taken

                // Release read lock, write lock is held
                Interlocked.Decrement(ref lockValuesRef.readLock);
                
                // Spin until we can proceed
                BurstSpinWait spinWait = default;
                
                while (true)
                {
                    spinWait.Spin(16);
                    
                    // Timeout
                    if (VTime.Milliseconds)
                    
                    if (Volatile.Read(ref lockValuesRef.writeLock) == 0)
                        break;
                }
            } while (true);
        }

        public void Write()
        {
            
        }
        
        /// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
        [DebuggerDisplay("READERS:{readLock} | WRITERS:{writeLock}")]
        [StructLayout(LayoutKind.Explicit, Size = 3 * JobsUtility.CacheLineSize)] // padding before/between/after fields
        internal struct LockValues
        {
            [FieldOffset(1 * JobsUtility.CacheLineSize)] public int readLock;
            [FieldOffset(2 * JobsUtility.CacheLineSize)] public int writeLock;
        }
    }
}*/
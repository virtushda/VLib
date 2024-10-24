#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define SAFETY
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VLib
{
    // TODO: Review enumerators and completely remove pointer usage
    
    /// <summary> A special list that uses a the shiny new burst locks to allow for universal thread-safety. </summary>
    public struct ParallelUnsafeList<T> : IEnumerable<T>, IAllocating
        where T : unmanaged
    {
        // Protect from invalid memory access
        VUnsafeBufferedRef<UnsafeList<T>> vListPtr;
        BurstSpinLockReadWrite burstLock;
        public readonly BurstSpinLockReadWrite BurstLock => burstLock;
        
        public readonly bool IsCreated => vListPtr.IsValid && vListPtr.ValueRef.IsCreated && BurstLock.IsCreatedAndValid;
        
        public readonly ref UnsafeList<T> ListRef => ref vListPtr.ValueRef;

        /// <summary> Access to list struct is forced through the safety checks of .TPtr </summary>
        //readonly UnsafeList<T>* RawListPtr => vListPtr.TPtr;

        /// <summary> Ignores lock. Safety guaranteed only when lock is properly held. </summary>
        public int LengthUnsafe
        {
            readonly get => ListRef.Length;
            set => ListRef.Length = value;
        }

        public readonly int CapacityUnsafe => ListRef.Capacity;

        public ParallelUnsafeList(int initialCapacity, Allocator allocator, NativeArrayOptions initialization = NativeArrayOptions.UninitializedMemory)
        {
            var list = new UnsafeList<T>(initialCapacity, allocator, initialization);
            // Store the list in a buffered ref to detect memory corruption and facilitate struct copy safety
            vListPtr = new VUnsafeBufferedRef<UnsafeList<T>>(list, allocator);
            
            burstLock = new BurstSpinLockReadWrite(allocator);
        }

        /// <summary> Make sure you don't hold onto the disposed struct! </summary>
        public void DisposeUnsafe()
        {
            if (burstLock.LockedAny)
                throw new LockRecursionException("ParallelUnsafeList trying to dispose while an exclusive lock is held!");
            
            if (vListPtr.IsValid)
            {
                // Dispose list
                vListPtr.ValueRef.Dispose();
                // Dispose Ref
                vListPtr.DisposeRefToDefault();
            }
            vListPtr = default;
            
            burstLock.DisposeRefToDefault();
        }

        public void Dispose() => DisposeUnsafe();

        public T this[int index]
        {
            get
            {
                ReadSafe(index, out var value);
                return value;
            }
            set => WriteSafe(index, value);
        }

        /// <summary> Checks if the index is valid without locking the list. Appropriate for situations where the list lock is already held properly. </summary>
        public readonly bool IsIndexValidUnsafe(int index) => index >= 0 && index < ListRef.Length;

        /// <summary> Auto locks for read and checks index validity. </summary>
        public readonly bool IsIndexValidSafe(int index)
        {
            using var scopeLock = BurstLock.ScopedReadLock(.25f);
            if (!scopeLock.Succeeded)
            {
                Debug.LogError("Failed to acquire read lock!");
                return false;
            }

            return IsIndexValidUnsafe(index);
        }
        
        public readonly bool CountSafe(out int count)
        {
            using var scopeLock = BurstLock.ScopedReadLock(.25f);
            if (!scopeLock.Succeeded)
            {
                count = 0;
                return false;
            }
            count = ListRef.Length;
            return true;
        }
        
        public readonly bool CapacitySafe(out int capacity)
        {
            using var scopeLock = BurstLock.ScopedReadLock(.25f);
            if (!scopeLock.Succeeded)
            {
                capacity = 0;
                return false;
            }
            capacity = ListRef.Capacity;
            return true;
        }

        public bool ClearSafe(float timeout = .25f)
        {
            using var scopeLock = BurstLock.ScopedExclusiveLock(timeout);
            if (!scopeLock.Succeeded)
                return false;
            ListRef.Clear();
            return true;
        }

        public readonly bool ReadSafe(int index, out T value, float timeout = .25f)
        {
            using var scopeLock = BurstLock.ScopedReadLock(timeout);
            if (!scopeLock.Succeeded)
            {
                value = default;
                return false;
            }
            return ReadUnsafe(index, out value);
        }
        
        public readonly bool ReadUnsafe(int index, out T value)
        {
            this.ConditionalCheckIsCreated();
            if (index < 0 || index >= ListRef.Length)
            {
                value = default;
                return false;
            }
            value = ListRef[index];
            return true;
        }

        public readonly unsafe ref T TryGetElementRefNoLock(int index, out bool success)
        {
            if (!IsIndexValidUnsafe(index))
            {
                success = false;
                return ref UnsafeUtility.AsRef<T>(null);
            }
            success = true;
            return ref ListRef.ElementAt(index);
        }

        public bool WriteSafe(int index, T value, float timeOut = 0.25f)
        {
            using var scopeLock = BurstLock.ScopedExclusiveLock(timeOut);
            if (!scopeLock.Succeeded)
                return false;
            ref var listRef = ref ListRef;
            if (index < 0 || index >= listRef.Length)
                return false;
            listRef[index] = value;
            return true;
        }
        
        public bool WriteUnsafe(int index, T value)
        {
            ref var listRef = ref ListRef;
            if (index < 0 || index >= listRef.Length)
                return false;
            listRef[index] = value;
            return true;
        }

        public bool AddSafe(T value, float timeOut = 0.25f)
        {
            using var scopeLock = BurstLock.ScopedExclusiveLock(timeOut);
            if (!scopeLock.Succeeded)
                return false;
            ListRef.Add(value);
            return true;
        }

        public bool RemoveAtSafe(int index, float timeOut = 0.25f)
        {
            using var scopeLock = BurstLock.ScopedExclusiveLock(timeOut);
            if (!scopeLock.Succeeded)
                return false;
            ListRef.RemoveAt(index);
            return true;
        }
        
        #region Enumeration
        
        /// <summary>
        /// Returns an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

        /// <summary>
        /// An enumerator over the elements of a list. Copied from UnsafeList.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first element of the list.
        /// </remarks>
        public unsafe struct Enumerator : IEnumerator<T>, IDisposable
        {
            private T* m_Ptr;
            private int m_Length;
            private int m_Index;
            private BurstSpinLockReadWrite burstLock;

            /// <summary>Will lock the collection for speedy reading</summary>
            public Enumerator(ParallelUnsafeList<T> list)
            {
                list.burstLock.EnterRead();
                
                m_Ptr = list.ListRef.Ptr;
                burstLock = list.burstLock;
                m_Length = list.LengthUnsafe;
                m_Index = -1;
            }

            /// <summary>Will unlock the collection</summary>
            public void Dispose()
            {
                burstLock.ExitRead();
            }

            /// <summary>
            /// Advances the enumerator to the next element of the list.
            /// </summary>
            /// <remarks>
            /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
            /// </remarks>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// The current element.
            /// </summary>
            /// <value>The current element.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Ptr[m_Index];
            }

            object IEnumerator.Current => Current;
        }
        
        public unsafe struct UnsafeEnumerator : IEnumerator<T>
        {
            private T* m_Ptr;
            private int m_Length;
            private int m_Index;
            
            public UnsafeEnumerator(ParallelUnsafeList<T> list)
            {
                m_Ptr = list.ListRef.Ptr;
                m_Length = list.LengthUnsafe;
                m_Index = -1;
            }

            /// <summary>Will unlock the collection</summary>
            public void Dispose(){}

            /// <summary>
            /// Advances the enumerator to the next element of the list.
            /// </summary>
            /// <remarks>
            /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
            /// </remarks>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// The current element.
            /// </summary>
            /// <value>The current element.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Ptr[m_Index];
            }

            object IEnumerator.Current => Current;
        }
        
        #endregion
        
        #region Readonly
        
        public static implicit operator ReadOnly(ParallelUnsafeList<T> list) => list.AsReadOnly();

        /// <summary> Returns a read only of this list. </summary>
        /// <returns>A read only of this list.</returns>
        public ReadOnly AsReadOnly()
        {
            this.ConditionalCheckIsCreated();
            // Lol, just wrap the list, idk why they would input the ptr and length manually, that stuff could be modified elsewhere leading to a crash, easily.
            return new ReadOnly(this);
        }

        /// <summary> A readonly version of ParallelUnsafeList, use AsReadOnly() to get one. </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe struct ReadOnly : IReadOnlyList<T>
        {
            private ParallelUnsafeList<T> list;
            
            public ReadOnly(ParallelUnsafeList<T> list) => this.list = list;
            
            /// <summary> The internal buffer of the list. </summary>
            public readonly T* Ptr => list.ListRef.Ptr;

            public int Count => list.CountSafe(out var count) ? count : default;
            public readonly int CountUnsafe => list.LengthUnsafe;

            public int Capacity => list.CapacitySafe(out var capacity) ? capacity : default;
            public readonly int CapacityUnsafe => list.CapacityUnsafe;
            
            public T this[int index] => list.ReadSafe(index, out var value) ? value : default;
            public readonly T ReadUnsafe(int index) => list[index];

            public bool TryGetValue(int index, out T value) => list.ReadSafe(index, out value);
            public bool TryGetValueUnsafe(int index, out T value) => list.ReadUnsafe(index, out value);

            /// <summary>
            /// Returns an enumerator over the elements of the list.
            /// </summary>
            /// <returns>An enumerator over the elements of the list.</returns>
            public Enumerator GetEnumerator() => new(list);

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        }
        
        public static implicit operator ScopedParallelReader(ParallelUnsafeList<T> list) => list.GetScopedParallelReader(0.25f);

        /// <summary>Locks the collection and returns a disposable struct that can be used to repeatedly access the collection before releasing the lock</summary>
        /// <example>using var listReader = list.GetScopedParallelReader()</example>
        public readonly ScopedParallelReader GetScopedParallelReader(float timeout = 0.25f)
        {
            this.ConditionalCheckIsCreated();
            // Lol, just wrap the list, idk why they would input the ptr and length manually, that stuff could be modified elsewhere leading to a crash, easily.
            return new ScopedParallelReader(this, timeout);
        }

        /// <summary>A disposable struct that can be used to repeatedly access the collection before releasing the lock</summary>
        /// <example>using var listReader = list.GetScopedParallelReader()</example>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public struct ScopedParallelReader : IReadOnlyList<T>, IDisposable
        {
            private ParallelUnsafeList<T> list;
            private bool locked;
            
            public ScopedParallelReader(ParallelUnsafeList<T> list, float timeout = 0.25f)
            {
                this.list = list;
                
                if (!list.IsCreated)
                    throw new NullReferenceException("Trying to acquire ScopedParallelReader for ParallelUnsafeList that does not exist!");
                
                locked = list.burstLock.EnterRead(timeout);
                if (!locked)
                    Debug.LogError("Failed to acquire read lock!");
            }

            public void Dispose()
            {
                list.burstLock.ExitRead();
                locked = false;
            }

            public readonly bool GetIsValid()
            {
                if (list.IsCreated && locked)
                    return true;
                
                Debug.LogError("Safety Exception: UnsafeReader is invalid! ParallelUnsafeList was not created, or failed to secure a read lock!");
                return false;
            }

            public static implicit operator bool(ScopedParallelReader reader) => reader.locked;

            /*/// <summary> The internal buffer of the list. </summary>
            public readonly T* Ptr => list.ListRef.Ptr;*/
            
            public readonly int Count => list.LengthUnsafe;
            
            public readonly int Capacity => list.CapacityUnsafe;
            
            public T this[int index] => list.ReadUnsafe(index, out var value) ? value : default;
            
            public bool IsIndexValid (int index) => list.IsIndexValidUnsafe(index);

            public bool TryGetValue(int index, out T value) => list.ReadUnsafe(index, out value);
            
            public ref T ElementAt(int index)
            {
                list.ConditionalCheckIsCreated();
                VCollectionUtils.ConditionalCheckIndexValid(index, list.LengthUnsafe);
                return ref list.ListRef.ElementAt(index);
            }

            /// <summary>
            /// Returns an enumerator over the elements of the list.
            /// </summary>
            /// <returns>An enumerator over the elements of the list.</returns>
            public UnsafeEnumerator GetEnumerator() => new(list);

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        }

        #endregion
    }
    
    public static class ParallelUnsafeListExtensions
    {
        public static void DisposeToDefault<T>(this ref ParallelUnsafeList<T> list)
            where T : unmanaged
        {
            list.DisposeUnsafe();
            list = default;
        }
    }
}
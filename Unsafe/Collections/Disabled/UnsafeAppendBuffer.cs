/*using System;
using System.Diagnostics;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

namespace VLib
{
    /// <summary> An unmanaged, untyped, heterogeneous buffer. </summary>
    /// <remarks> The values written to an individual append buffer can be of different types. </remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeAppendBuffer// : INativeDisposable
    {
        /// <summary> The internal buffer where the content is stored. </summary>
        /// <value>The internal buffer where the content is stored.</value>
        [NativeDisableUnsafePtrRestriction]
        public byte* Ptr;

        public T* GetPtr<T>() where T : unmanaged => (T*)Ptr;

        /// <summary>
        /// The size in bytes of the currently-used portion of the internal buffer.
        /// </summary>
        /// <value>The size in bytes of the currently-used portion of the internal buffer.</value>
        public int LengthBytes;

        public int LengthAs<T>() where T : unmanaged => LengthBytes / UnsafeUtility.SizeOf<T>();

        /// <summary>
        /// The size in bytes of the internal buffer.
        /// </summary>
        /// <value>The size in bytes of the internal buffer.</value>
        public int CapacityBytes;

        /// <summary>
        /// The allocator used to create the internal buffer.
        /// </summary>
        /// <value>The allocator used to create the internal buffer.</value>
        public AllocatorManager.AllocatorHandle Allocator;

        /// <summary>
        /// The byte alignment used when allocating the internal buffer.
        /// </summary>
        /// <value>The byte alignment used when allocating the internal buffer. Is always a non-zero power of 2.</value>
        public readonly int Alignment;

        /// <summary>
        /// Initializes and returns an instance of UnsafeAppendBuffer.
        /// </summary>
        /// <param name="initialByteCapacity">The initial allocation size in bytes of the internal buffer.</param>
        /// <param name="alignment">The byte alignment of the allocation. Must be a non-zero power of 2.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeBuffer(int initialByteCapacity, int alignment, AllocatorManager.AllocatorHandle allocator)
        {
            CheckAlignment(alignment);

            Alignment = alignment;
            Allocator = allocator;
            Ptr = null;
            LengthBytes = 0;
            CapacityBytes = 0;

            SetByteCapacity(initialByteCapacity);
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeAppendBuffer that aliases an existing buffer.
        /// </summary>
        /// <remarks>The capacity will be set to `length`, and <see cref="LengthBytes"/> will be set to 0.
        /// </remarks>
        /// <param name="ptr">The buffer to alias.</param>
        /// <param name="length">The length in bytes of the buffer.</param>
        public UnsafeBuffer(void* ptr, int length)
        {
            Alignment = 0;
            Allocator = AllocatorManager.None;
            Ptr = (byte*)ptr;
            LengthBytes = 0;
            CapacityBytes = length;
        }

        /// <summary>
        /// Whether the append buffer is empty.
        /// </summary>
        /// <value>True if the append buffer is empty.</value>
        public bool IsEmpty => LengthBytes == 0;

        /// <summary>
        /// Whether this append buffer has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this append buffer has been allocated (and not yet deallocated).</value>
        public bool IsCreated => Ptr != null;

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            var alloc = Allocator.ToAllocator;
            if (Ptr != null && alloc > Unity.Collections.Allocator.None)
            {
                UnsafeUtility.Free(Ptr, alloc);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            LengthBytes = 0;
            CapacityBytes = 0;
        }

        /#1#// <summary> Creates and schedules a job that will dispose this append buffer. </summary>
        /// <param name="inputDeps">The handle of a job which the new job will depend upon.</param>
        /// <returns>The handle of a new job that will dispose this append buffer. The new job depends upon inputDeps.</returns>
        [NotBurstCompatible /* This is not burst compatible because of IJob's use of a static IntPtr. Should switch to IJobBurstSchedulable in the future #2#]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var alloc = Allocator.ToAllocator;
            if (alloc > Unity.Collections.Allocator.None)
            {
                var jobHandle = new UnsafeDisposeJob { Ptr = Ptr, Allocator = Allocator }.Schedule(inputDeps);

                Ptr = null;
                Allocator = AllocatorManager.Invalid;

                return jobHandle;
            }

            Ptr = null;

            return inputDeps;
        }#1#
        
        /// <summary> Read buffer as type, indexes in types stride! </summary>
        /// <returns>Value in buffer as T!</returns>
        public T Read<T>(int index) where T : unmanaged
        {
#if UNITY_EDITOR
            if (index < 0)
            {
                Debug.LogError($"Index {index} less than zero...");
                return default;
            }
            if (index >= LengthAs<T>())
            {
                Debug.LogError($"Index {index} greater than or equal length {LengthAs<T>()}!");
                return default;
            }
#endif
            return GetPtr<T>()[index];
        }
        
        /// <summary> Write buffer as type, indexes in types stride! </summary>
        /// <returns>Writes value in buffer as T!</returns>
        public void Write<T>(T value, int index) where T : unmanaged
        {
#if UNITY_EDITOR
            if (index < 0)
            {
                Debug.LogError($"Index {index} less than zero...");
                return;
            }
            if (index >= LengthAs<T>())
            {
                Debug.LogError($"Index {index} greater than or equal length {LengthAs<T>()}!");
                return;
            }
#endif
            GetPtr<T>()[index] = value;
        }

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear() => Interlocked.Exchange(ref LengthBytes, 0);

        /// <summary>
        /// Sets the size in bytes of the internal buffer.
        /// </summary>
        /// <remarks>Does nothing if the new capacity is less than or equal to the current capacity.</remarks>
        /// <param name="byteCapacity">A new capacity in bytes.</param>
        public void SetByteCapacity(int byteCapacity)
        {
            if (byteCapacity <= CapacityBytes)
            {
                return;
            }

            byteCapacity = math.max(64, math.ceilpow2(byteCapacity));

            var newPtr = (byte*)UnsafeUtility.Malloc(byteCapacity, Alignment, Allocator.ToAllocator);
            if (Ptr != null)
            {
                UnsafeUtility.MemCpy(newPtr, Ptr, math.min(LengthBytes, byteCapacity));
                UnsafeUtility.Free(Ptr, Allocator.ToAllocator);
            }

            Ptr = newPtr;
            CapacityBytes = byteCapacity;
        }

        /// <summary> Force buffer to shift to smaller allocation, doesn't copy existing data. </summary> 
        public void ShrinkCapacityNoCopy(int newMaximumByteCapacity)
        {
            if (CapacityBytes <= newMaximumByteCapacity)
                return;
            
            CapacityBytes = math.ceilpow2(newMaximumByteCapacity);
            
            var newPtr = (byte*)UnsafeUtility.Malloc(CapacityBytes, Alignment, Allocator.ToAllocator);
            if (Ptr != null)
            {
                UnsafeUtility.Free(Ptr, Allocator.ToAllocator);
            }

            Ptr = newPtr;
            LengthBytes = 0;
        }

        /// <summary>
        /// Sets the length in bytes.
        /// </summary>
        /// <remarks>If the new length exceeds the capacity, capacity is expanded to the new length.</remarks>
        /// <param name="lengthBytes">The new length.</param>
        public void ResizeUninitialized(int lengthBytes)
        {
            SetByteCapacity(lengthBytes);
            LengthBytes = lengthBytes;
        }

        /// <summary>
        /// Appends an element to the end of this append buffer.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="value">The value to be appended.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(ushort), typeof(float) })]
        public void Add<T>(T value) where T : struct
        {
            var structSize = UnsafeUtility.SizeOf<T>();

            SetByteCapacity(LengthBytes + structSize);
            UnsafeUtility.CopyStructureToPtr(ref value, Ptr + LengthBytes);
            LengthBytes += structSize;
        }

        /// <summary>
        /// Appends an element to the end of this append buffer.
        /// </summary>
        /// <remarks>The value itself is stored, not the pointer.</remarks>
        /// <param name="ptr">A pointer to the value to be appended.</param>
        /// <param name="structSize">The size in bytes of the value to be appended.</param>
        public void Add(void* ptr, int structSize)
        {
            SetByteCapacity(LengthBytes + structSize);
            UnsafeUtility.MemCpy(Ptr + LengthBytes, ptr, structSize);
            LengthBytes += structSize;
        }

        /// <summary>
        /// Appends the elements of a buffer to the end of this append buffer.
        /// </summary>
        /// <typeparam name="T">The type of the buffer's elements.</typeparam>
        /// <remarks>The values themselves are stored, not their pointers.</remarks>
        /// <param name="ptr">A pointer to the buffer whose values will be appended.</param>
        /// <param name="length">The number of elements to append.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public void AddArray<T>(void* ptr, int length) where T : struct
        {
            Add(length);

            if (length != 0)
                Add(ptr, length * UnsafeUtility.SizeOf<T>());
        }

        /// <summary>
        /// Appends all elements of an array to the end of this append buffer.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="value">The array whose elements will all be appended.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public void Add<T>(NativeArray<T> value) where T : struct
        {
            Add(value.Length);
            Add(NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(value), UnsafeUtility.SizeOf<T>() * value.Length);
        }

        /#1#// <summary>
        /// Appends the content of a string as UTF-16 to the end of this append buffer.
        /// </summary>
        /// <remarks>Because some Unicode characters require two chars in UTF-16, each character is written as one or two chars (two or four bytes).
        ///
        /// The length of the string is itself appended before adding the first character. If the string is null, appends the int `-1` but no character data.
        ///
        /// A null terminator is not appended after the character data.</remarks>
        /// <param name="value">The string to append.</param>
        [NotBurstCompatible /* Deprecated #2#]
        [Obsolete("Please use `AddNBC` from `Unity.Collections.LowLevel.Unsafe.NotBurstCompatible` namespace instead. (RemovedAfter 2021-06-22)", false)]
        public void Add(string value) => NotBurstCompatible.Extensions.AddNBC(ref this, value);#1#

        public void Insert<T>(int indexTStride, T value)
            where T : unmanaged
        {
            int structSizeBytes = UnsafeUtility.SizeOf<T>();
            int lengthTStride = LengthAs<T>();
            int indexByteStride = indexTStride * structSizeBytes;
            
            SetByteCapacity(LengthBytes + structSizeBytes);
            
            byte* source = Ptr + indexByteStride;
            
            // If not last index, we need to move memory over by 1 index
            if (indexTStride < lengthTStride)
            {
                var byteSizeToMove = LengthBytes - indexByteStride;
                UnsafeUtility.MemMove(source + structSizeBytes, source, byteSizeToMove);
            }
            UnsafeUtility.CopyStructureToPtr(ref value, source);

            LengthBytes += structSizeBytes;
        } 
        
        public void RemoveAt<T>(int indexTStride)
            where T : unmanaged
        {
            int structSizeBytes = UnsafeUtility.SizeOf<T>();
            int lengthTStride = LengthAs<T>();
            int indexByteStride = indexTStride * structSizeBytes;
            
            // If not last index, we need to move memory over by 1 index
            if (indexTStride < lengthTStride - 1)
            {
                byte* destination = Ptr + indexByteStride;
                var byteSizeToMove = LengthBytes - indexByteStride - 1;
                UnsafeUtility.MemMove(destination, destination + structSizeBytes, byteSizeToMove);
            }

            LengthBytes -= structSizeBytes;
        }

        /// <summary>
        /// Removes and returns the last element of this append buffer.
        /// </summary>
        /// <typeparam name="T">The type of the element to remove.</typeparam>
        /// <remarks>It is your responsibility to specify the correct type. Do not pop when the append buffer is empty.</remarks>
        /// <returns>The element removed from the end of this append buffer.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public T Pop<T>() where T : struct
        {
            int structSize = UnsafeUtility.SizeOf<T>();
            long ptr = (long)Ptr;
            long size = LengthBytes;
            long addr = ptr + size - structSize;

            var data = UnsafeUtility.ReadArrayElement<T>((void*)addr, 0);
            LengthBytes -= structSize;
            return data;
        }

        /// <summary>
        /// Removes and copies the last element of this append buffer.
        /// </summary>
        /// <remarks>It is your responsibility to specify the correct `structSize`. Do not pop when the append buffer is empty.</remarks>
        /// <param name="ptr">The location to which the removed element will be copied.</param>
        /// <param name="structSize">The size of the element to remove and copy.</param>
        public void Pop(void* ptr, int structSize)
        {
            long data = (long)Ptr;
            long size = LengthBytes;
            long addr = data + size - structSize;

            UnsafeUtility.MemCpy(ptr, (void*)addr, structSize);
            LengthBytes -= structSize;
        }

        /#1#// <summary>
        /// Copies this append buffer to a managed array of bytes.
        /// </summary>
        /// <returns>A managed array of bytes.</returns>
        [NotBurstCompatible /* Deprecated #2#]
        [Obsolete("Please use `ToBytesNBC` from `Unity.Collections.LowLevel.Unsafe.NotBurstCompatible` namespace instead. (RemovedAfter 2021-06-22)", false)]
        public byte[] ToBytes() => NotBurstCompatible.Extensions.ToBytesNBC(ref this);#1#

        /#1#// <summary>
        /// Returns a reader for this append buffer.
        /// </summary>
        /// <returns>A reader for the append buffer.</returns>
        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        /// <summary>
        /// A reader for UnsafeAppendBuffer.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public unsafe struct Reader
        {
            /// <summary>
            /// The internal buffer where the content is stored.
            /// </summary>
            /// <value>The internal buffer where the content is stored.</value>
            public readonly byte* Ptr;

            /// <summary>
            /// The length in bytes of the append buffer's content.
            /// </summary>
            /// <value>The length in bytes of the append buffer's content.</value>
            public readonly int Size;

            /// <summary>
            /// The location of the next read (expressed as a byte offset from the start).
            /// </summary>
            /// <value>The location of the next read (expressed as a byte offset from the start).</value>
            public int Offset;

            /// <summary>
            /// Initializes and returns an instance of UnsafeAppendBuffer.Reader.
            /// </summary>
            /// <param name="buffer">A reference to the append buffer to read.</param>
            public Reader(ref UnsafeAppendBuffer buffer)
            {
                Ptr = buffer.Ptr;
                Size = buffer.Length;
                Offset = 0;
            }

            /// <summary>
            /// Initializes and returns an instance of UnsafeAppendBuffer.Reader that reads from a buffer.
            /// </summary>
            /// <remarks>The buffer will be read *as if* it is an UnsafeAppendBuffer whether it was originally allocated as one or not.</remarks>
            /// <param name="ptr">The buffer to read as an UnsafeAppendBuffer.</param>
            /// <param name="length">The length in bytes of the </param>
            public Reader(void* ptr, int length)
            {
                Ptr = (byte*)ptr;
                Size = length;
                Offset = 0;
            }

            /// <summary>
            /// Whether the offset has advanced past the last of the append buffer's content.
            /// </summary>
            /// <value>Whether the offset has advanced past the last of the append buffer's content.</value>
            public bool EndOfBuffer => Offset == Size;

            /// <summary>
            /// Reads an element from the append buffer.
            /// </summary>
            /// <remarks>Advances the reader's offset by the size of T.</remarks>
            /// <typeparam name="T">The type of element to read.</typeparam>
            /// <param name="value">Output for the element read.</param>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public void ReadNext<T>(out T value) where T : struct
            {
                var structSize = UnsafeUtility.SizeOf<T>();
                CheckBounds(structSize);

                UnsafeUtility.CopyPtrToStructure<T>(Ptr + Offset, out value);
                Offset += structSize;
            }

            /// <summary>
            /// Reads an element from the append buffer.
            /// </summary>
            /// <remarks>Advances the reader's offset by the size of T.</remarks>
            /// <typeparam name="T">The type of element to read.</typeparam>
            /// <returns>The element read.</returns>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public T ReadNext<T>() where T : struct
            {
                var structSize = UnsafeUtility.SizeOf<T>();
                CheckBounds(structSize);

                T value = UnsafeUtility.ReadArrayElement<T>(Ptr + Offset, 0);
                Offset += structSize;
                return value;
            }

            /// <summary>
            /// Reads an element from the append buffer.
            /// </summary>
            /// <remarks>Advances the reader's offset by `structSize`.</remarks>
            /// <param name="structSize">The size of the element to read.</param>
            /// <returns>A pointer to where the read element resides in the append buffer.</returns>
            public void* ReadNext(int structSize)
            {
                CheckBounds(structSize);

                var value = (void*)((IntPtr)Ptr + Offset);
                Offset += structSize;
                return value;
            }

            /// <summary>
            /// Reads an element from the append buffer.
            /// </summary>
            /// <remarks>Advances the reader's offset by the size of T.</remarks>
            /// <typeparam name="T">The type of element to read.</typeparam>
            /// <param name="value">Outputs a new array with length of 1. The read element is copied to the single index of this array.</param>
            /// <param name="allocator">The allocator to use.</param>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public void ReadNext<T>(out NativeArray<T> value, AllocatorManager.AllocatorHandle allocator) where T : struct
            {
                var length = ReadNext<int>();
                value = CollectionHelper.CreateNativeArray<T>(length, allocator);
                var size = length * UnsafeUtility.SizeOf<T>();
                if (size > 0)
                {
                    var ptr = ReadNext(size);
                    UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafePtr(value), ptr, size);
                }
            }

            /// <summary>
            /// Reads an array from the append buffer.
            /// </summary>
            /// <remarks>An array stored in the append buffer starts with an int specifying the number of values in the array.
            /// The first element of an array immediately follows this int.
            ///
            /// Advances the reader's offset by the size of the array (plus an int).</remarks>
            /// <typeparam name="T">The type of elements in the array to read.</typeparam>
            /// <param name="length">Output which is the number of elements in the read array.</param>
            /// <returns>A pointer to where the first element of the read array resides in the append buffer.</returns>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public void* ReadNextArray<T>(out int length) where T : struct
            {
                length = ReadNext<int>();
                return (length == 0) ? null : ReadNext(length * UnsafeUtility.SizeOf<T>());
            }

#if !NET_DOTS
            /// <summary>
            /// Reads a UTF-16 string from the append buffer.
            /// </summary>
            /// <remarks>Because some Unicode characters require two chars in UTF-16, each character is either one or two chars (two or four bytes).
            ///
            /// Assumes the string does not have a null terminator.
            ///
            /// Advances the reader's offset by the size of the string (in bytes).</remarks>
            /// <param name="value">Outputs the string read from the append buffer.</param>
            [NotBurstCompatible /* Deprecated #2#]
            [Obsolete("Please use `ReadNextNBC` from `Unity.Collections.LowLevel.Unsafe.NotBurstCompatible` namespace instead. (RemovedAfter 2021-06-22)", false)]
            public void ReadNext(out string value) => NotBurstCompatible.Extensions.ReadNextNBC(ref this, out value);
#endif

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckBounds(int structSize)
            {
                if (Offset + structSize > Size)
                {
                    throw new ArgumentException($"Requested value outside bounds of UnsafeAppendOnlyBuffer. Remaining bytes: {Size - Offset} Requested: {structSize}");
                }
            }
        }#1#

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAlignment(int alignment)
        {
            var zeroAlignment = alignment == 0;
            var powTwoAlignment = ((alignment - 1) & alignment) == 0;
            var validAlignment = (!zeroAlignment) && powTwoAlignment;

            if (!validAlignment)
            {
                throw new ArgumentException($"Specified alignment must be non-zero positive power of two. Requested: {alignment}");
            }
        }
    }
}*/
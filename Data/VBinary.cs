using System;
using System.IO;
using System.Security.Cryptography;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace VLib
{
    public static class VBinary
    {
        public const string DataExt = ".vdata";
        public const string CountExt = ".vcount";
        
        public const int MD5HashLength = 16;
        
        public static unsafe bool TryCreateFromSerializedData<TStream>
            (byte[][] dataObjects, TStream dataStream, TStream byteCountStream, bool closeStreams, out string errorMsg, bool extraInfo = false)
            where TStream : Stream
        {
            ulong objectByteCountsGCHandle = 0;
            
            try
            {
                Profiler.BeginSample("Pre-Flight Checks");
                if (dataObjects is not {Length: > 0})
                {
                    errorMsg = "Data objects array is null or empty, cannot create VBinary";
                    Profiler.EndSample();
                    return false;
                }
                foreach (var data in dataObjects)
                {
                    if (data is not {Length: > 0})
                    {
                        errorMsg = "Data object is null or empty, cannot create VBinary with any empty data";
                        Profiler.EndSample();
                        return false;
                    }
                }
                Profiler.EndSample();
                
                Profiler.BeginSample("Create MD5");
                using var md5 = MD5.Create();
                Profiler.EndSample();

                Profiler.BeginSample("Compute lengths");
                // Determine lengths and total size
                var totalByteSize = 0;
                
                var objectByteCountsArray = new byte[dataObjects.Length * sizeof(int)];
                // Int View
                var objectByteCountsPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(objectByteCountsArray, out objectByteCountsGCHandle);
                var objectByteCountsAsInt = new Span<int>(objectByteCountsPtr, dataObjects.Length);
                
                for (int i = 0; i < dataObjects.Length; i++)
                {
                    var objectLength = dataObjects[i].Length;
                    objectByteCountsAsInt[i] = objectLength;
                    totalByteSize += objectLength;
                }
                Profiler.EndSample();

                Profiler.BeginSample("Compute MD5");
                // Compute MD5 hash of data lengths as this is most important not to corrupt
                var objectByteCountsArrayHashValue = md5.ComputeHash(objectByteCountsArray);
                if (extraInfo)
                    Debug.Log($"MD5 hash of data lengths: {BitConverter.ToString(objectByteCountsArrayHashValue)}");
                Profiler.EndSample();

                Profiler.BeginSample("Data stream");
                // Data stream - No checks for this, it's too big to double up
                // Allocate all space at once to avoid fragmentation
                dataStream.SetLength(totalByteSize);
                dataStream.Seek(0, SeekOrigin.Begin);
                // Write all data
                for (int i = 0; i < dataObjects.Length; i++)
                {
                    var dataArray = dataObjects[i];
                    dataStream.Write(dataArray, 0, dataArray.Length);
                }

                // Get all data where it needs to go before continuing
                if (closeStreams)
                    dataStream.Close();
                else
                    dataStream.Flush();
                Profiler.EndSample();

                Profiler.BeginSample("Count stream");
                // MD5 hash at beginning, then two copies of the length data
                var desiredCountStreamLength = objectByteCountsArrayHashValue.Length + dataObjects.Length * sizeof(int) * 2;
                byteCountStream.SetLength(desiredCountStreamLength);
                byteCountStream.Seek(0, SeekOrigin.Begin);

                // First write hash value
                byteCountStream.Write(objectByteCountsArrayHashValue, 0, objectByteCountsArrayHashValue.Length);
                //var countStreamWriter = new BinaryWriter(countStream);

                // Write buffer twice so we have a backup of the smaller but most brittle part
                byteCountStream.Write(objectByteCountsArray, 0, objectByteCountsArray.Length);
                byteCountStream.Write(objectByteCountsArray, 0, objectByteCountsArray.Length);

                // Verify stream length
                AssertEquals((int) byteCountStream.Length, desiredCountStreamLength, "Count stream length");

                if (closeStreams)
                    byteCountStream.Close();
                else
                    byteCountStream.Flush();
                Profiler.EndSample();
                
                Dispose();
                
                errorMsg = null;
                return true;
            }
            catch (Exception e)
            {
                Dispose();
                Debug.LogException(e); // Should automatically be passed to cloud diagnostics
            }
            errorMsg = "An exception occurred while creating VBinary, logging... (should go to cloud diagnostics)";
            return false;

            void Dispose()
            {
                if (objectByteCountsGCHandle != 0)
                    UnsafeUtility.ReleaseGCObject(objectByteCountsGCHandle);
            }
        }

        public static unsafe bool TryExtractFromVBinary<TStream>(TStream dataStream, TStream byteCountStream, bool closeStreams, out byte[][] outputObjects, bool extraInfo = false)
            where TStream : Stream
        {
            ulong objectByteCountsGCHandle = 0;
            
            try
            {
                using var md5 = MD5.Create();
            
                // Read md5 from stream
                Span<byte> md5Span = stackalloc byte[MD5HashLength];
                byteCountStream.Seek(0, SeekOrigin.Begin);
                var readBytes = byteCountStream.Read(md5Span);
                AssertEquals(readBytes, MD5HashLength, "Read MD5 bytes");
            
                // Determine count buffer positioning
                var remainingBytes = (int) byteCountStream.Length - MD5HashLength;
                AssertEvenNumber(remainingBytes, "Remaining bytes in count buffer");
                var countBufferLength = remainingBytes / 2;
                AssertDivisibleByFour(countBufferLength, "Count buffer length");
                var objectCount = countBufferLength / sizeof(int);
                
                // Setup Memory for object byte counts
                var objectByteCountsByteView = new byte[countBufferLength];
                // Get 'int' view of 'byte' array, without copy is vastly more efficient
                var objectByteCountsPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(objectByteCountsByteView, out objectByteCountsGCHandle);
                var objectByteCounts = new Span<int>(objectByteCountsPtr, objectCount);
                
                // Read first count buffer
                readBytes = byteCountStream.Read(objectByteCountsByteView);
                AssertEquals(readBytes, countBufferLength, "Read first count buffer");
            
                // Compute new md5
                var newMd5 = md5.ComputeHash(objectByteCountsByteView);
                if (extraInfo)
                    Debug.Log($"MD5 hash of data lengths: {BitConverter.ToString(newMd5)}");
                
                // Compare md5
                if (!md5Span.SequenceEqual(newMd5))
                {
                    Debug.LogError("MD5 hash of data lengths does not match, data may be corrupted, reading second copy of count buffer...");
                    
                    // Read second count buffer
                    readBytes = byteCountStream.Read(objectByteCountsByteView);
                    AssertEquals(readBytes, countBufferLength, "Read second count buffer");
                    
                    // Check MD5 again
                    newMd5 = md5.ComputeHash(objectByteCountsByteView);
                    if (!md5Span.SequenceEqual(newMd5))
                        Debug.LogError("MD5 hash of data lengths does not match, data may be corrupted, proceeding within try block...");
                }

                // Read data stream using byteCountStream to determine lengths
                dataStream.Seek(0, SeekOrigin.Begin);
                var dataObjects = new byte[objectCount][];
                for (int i = 0; i < objectCount; i++)
                {
                    // Create object byte array
                    var objectLength = objectByteCounts[i];
                    var objectArray = new byte[objectLength];
                    dataObjects[i] = objectArray;
                    
                    // Read data
                    readBytes = dataStream.Read(objectArray);
                    AssertEquals(readBytes, objectLength, "Read data object");
                }
                
                Dispose();
                
                // Return data
                outputObjects = dataObjects;
                return true;
            }
            catch (Exception e)
            {
                Dispose();
                Debug.LogError("An error occurred while extracting from VBinary, logging exception... ");
                Debug.LogException(e);
            }
            
            outputObjects = null;
            return false;

            void Dispose()
            {
                if (objectByteCountsGCHandle != 0)
                    UnsafeUtility.ReleaseGCObject(objectByteCountsGCHandle);
            }
        }
        
        static void AssertEquals(int valueA, int valueB, string message)
        {
            if (valueA != valueB)
                Debug.LogException(new UnityException($"{message} {valueA} != {valueB}"));
        }
        
        static void AssertEvenNumber(int value, string message)
        {
            if (value % 2 != 0)
                Debug.LogException(new UnityException($"{message} {value} is not even"));
        }
        
        static void AssertDivisibleByFour(int value, string message)
        {
            if (value % 4 != 0)
                Debug.LogException(new UnityException($"{message} {value} is not divisible by 4"));
        }
    }
}
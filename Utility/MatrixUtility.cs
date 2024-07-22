using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    public static class MatrixUtility
    {
        const float ByteMaxFloat = 255f;
        
        public static JobHandle MatricesToTRS(in NativeArray<float4x4> matrices, out NativeArray<TRS> trsArray, Allocator allocator, JobHandle dependency = default)
        {
            if (matrices.Length < 1)
            {
                Debug.LogError("Matrices array is empty, returning 'default'...!");
                trsArray = default;
                return default;
            }

            trsArray = new NativeArray<TRS>(matrices.Length, allocator);
            return new Jobs.MatricesToTRSJob<TRS>(matrices, trsArray).ScheduleBatch(matrices.Length, 256, dependency);
        }

        /// <summary> Use MatricesToTRSUnity to compute a trsArray at maximum speed. </summary>
        public static void TRSToTransformsImmediate(in NativeArray<TRS> trsArray, List<Transform> transforms, JobHandle dependency = default)
        {
            TRSToTransforms(trsArray, transforms, out var accessArray, dependency).Complete();
            accessArray.Dispose();
        }

        /// <summary> Be sure to dispose 'transformAccess'!!! </summary>
        public static JobHandle TRSToTransforms(NativeArray<TRS> trsArray, List<Transform> transforms,
                                                out TransformAccessArray transformAccess, JobHandle dependency = default)
        {
            int transformCount = transforms.Count;
            var transformsArray = transforms.GetInternalArray();
            
            //Setup Transform Access
            if (transformCount == transformsArray.Length)
                transformAccess = new TransformAccessArray(transformsArray);
            else
            {
                transformAccess = new TransformAccessArray(transformCount);
                for (int i = 0; i < transformCount; i++)
                    transformAccess.Add(transformsArray[i]);
            }

            var jab = new Jobs.TRSToTransformJob(trsArray);
            return jab.Schedule(transformAccess, dependency);
        }
        
        ///<summary> Compressed biped format: <br/>
        /// Vector3: Position <br/>
        /// byte: Rotation Y, degrees, compressed to 0-255 <br/>
        /// byte: Scale Avg, in range 0.5 to 1.5. </summary>
        public static float4x4 CompressedBipedToMatrix(Vector3 position, byte rotation, byte scale)
        {
            float scaleF = math.remap(0, 1, 0.5f, 1.5f, scale.ToPercent01());
            return float4x4.TRS(position,
                quaternion.Euler(0, math.radians(rotation * 360f / ByteMaxFloat), 0),
                new float3(scaleF, scaleF, scaleF));
        }

        ///<summary> Compressed biped format: <br/>
        /// Vector3: Position <br/>
        /// byte: Rotation Y, degrees, compressed to 0-255 <br/>
        /// byte: Scale Avg, in range 0.5 to 1.5. </summary>
        public static void MatrixToCompressedBiped(float4x4 matrix, out Vector3 position, out byte rotation, out byte scale)
        {
            position = matrix.GetPositionDelta();
            rotation = (byte) math.round(math.degrees(math.EulerXYZ(matrix.RotationDelta()).y) * ByteMaxFloat / 360f);
            scale = math.remap(0.5f, 1.5f, 0, 1, math.csum(matrix.ScaleDelta()) * .33334f).ToByteAsPercent();
        }

        public static class Jobs
        {
            [BurstCompile]
            public struct MatricesToTRSJob<T> : IJobParallelForBatch
                where T : unmanaged, ITRS
            {
                [ReadOnly] NativeArray<float4x4> matrices;
                [WriteOnly] NativeArray<T> trs;

                public MatricesToTRSJob(NativeArray<float4x4> matrices, NativeArray<T> trs)
                {
                    this.matrices = matrices;
                    this.trs = trs;
                }

                public void Execute(int startIndex, int count)
                {
                    int end = startIndex + count;
                    for (int i = startIndex; i < end; i++)
                    {
                        trs[i] = matrices[i].ToTRS<T>();
                    }
                }
            }

            [BurstCompile]
            public struct MatricesToTRSJobWithCalc<T,TCalc> : IJobParallelForBatch
                where T : unmanaged, ITRS
                where TCalc : struct, NativeCollectionExt.IIterationActionReturn<float4x4>
            {
                [ReadOnly] NativeArray<float4x4> matrices;
                [WriteOnly] NativeArray<T> trs;
                TCalc calcAction;

                public MatricesToTRSJobWithCalc(NativeArray<float4x4> matrices, NativeArray<T> trs, TCalc calc)
                {
                    this.matrices = matrices;
                    this.trs = trs;
                    calcAction = calc;
                }

                public void Execute(int startIndex, int count)
                {
                    int end = startIndex + count;
                    for (int i = startIndex; i < end; i++)
                    {
                        var matrixModified = calcAction.Execute(i, matrices[i]);
                        trs[i] = matrixModified.ToTRS<T>();
                    }
                }
            }

            public struct MatrixTransformCalc : NativeCollectionExt.IIterationActionReturn<float4x4>
            {
                float4x4 transformation;
                NativeArray<float4x4> matricesBuffer;

                public MatrixTransformCalc(float4x4 transformation, NativeArray<float4x4> matricesBuffer)
                {
                    this.transformation = transformation;
                    this.matricesBuffer = matricesBuffer;
                }

                public float4x4 Execute(int index, float4x4 singleValue)
                {
                    return matricesBuffer[index] = math.mul(transformation, singleValue);
                }
            }
            
            [BurstCompile]
            public struct TRSToTransformJob : IJobParallelForTransform
            {
                [ReadOnly] NativeArray<TRS> trsUnityArray;

                public TRSToTransformJob(NativeArray<TRS> trsUnityArray)
                {
                    this.trsUnityArray = trsUnityArray;
                }

                public void Execute(int index, TransformAccess transform)
                {
                    var trs = trsUnityArray[index];
                    transform.position = trs.position;
                    transform.rotation = trs.rotation;
                    transform.localScale = trs.scale;
                }
            }
        }
    }
}
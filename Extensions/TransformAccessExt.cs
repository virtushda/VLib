using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace VLib
{
    public static class TransformAccessExt
    {
        public static Vector3 Forward(ref this TransformAccess transform) => transform.rotation * Vector3.forward;
        
        public static Vector3 Right(ref this TransformAccess transform) => transform.rotation * Vector3.right;
        
        public static Vector3 Up(ref this TransformAccess transform) => transform.rotation * Vector3.up;
    }
}
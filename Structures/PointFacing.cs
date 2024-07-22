using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public struct PointFacing
    {
        public float3 location;
        public float3 facing;

        public float3 SafeFacing => math.normalizesafe(facing);

        public PointFacing(float3 location, float3 facing)
        {
            this.location = location;
            this.facing = facing;
        }

        public TRS ToTRS()
        {
            return new TRS(location, quaternion.LookRotationSafe(math.normalizesafe(facing), VMath.Up3), VMath.One3);
        }

        public TRS ToTRS(Matrix4x4 matrix)
        {
            float3 transformedPos = matrix.MultiplyPoint(location);
            float3 transformedFacing = matrix.MultiplyVector(SafeFacing);
            return new TRS(transformedPos, quaternion.LookRotationSafe(transformedFacing, VMath.Up3), VMath.One3);
        }
    }
}
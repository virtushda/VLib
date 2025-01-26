﻿using System;
using Drawing;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    public static class ArmatureUtil
    {
        public static readonly float2 DefaultSkinningCapsuleWeightImportanceRemapRange = new(0f, .25f);
        
        public static bool TryCalculateSkinnedBoneCapsule(SkinnedMeshRenderer skinnedMeshRenderer, Transform bone, out CapsuleNative boneSpaceCapsule,
            float2? weightImportanceRemapRange = null, CommandBuilder? draw = null)
        {
            // Cannot be a leaf bone
            if (bone.childCount == 0)
            {
                boneSpaceCapsule = default;
                return false;
            }

            draw?.PushDuration(10f);
            
            /*// Track which transforms are bones
            HashSet<Transform> bonesSet = new HashSet<Transform>();
            foreach (var b in skinnedMeshRenderer.bones)
                bonesSet.Add(b);

            if (!bonesSet.Contains(bone))
            {
                boneSpaceCapsule = default;
                Debug.LogError("Input bone must be a bone in the skinned mesh renderer.");
                return false;
            }
            
            // Find root BONE
            var rootBone = bone;
            while (rootBone.parent != null && bonesSet.Contains(rootBone.parent))
                rootBone = rootBone.parent;*/

            var weightImportanceRemap = weightImportanceRemapRange ?? DefaultSkinningCapsuleWeightImportanceRemapRange;

            // Mesh data
            var mesh = skinnedMeshRenderer.sharedMesh;
            var vertices = mesh.vertices; // Alloc and copy
            var boneWeightsView = mesh.GetAllBoneWeights(); // Memory view
            var bonesPerVertexView = mesh.GetBonesPerVertex(); // Memory view
            
            // Bone data
            var meshTransform = skinnedMeshRenderer.transform;
            //var boneEnd = bone.GetChild(0);
            //var toBoneEnd = boneEnd.position - bone.position;

            // Which bone
            var boneIndex = Array.IndexOf(skinnedMeshRenderer.bones, bone); // Linear search, small collection
            if (boneIndex == -1)
            {
                boneSpaceCapsule = default;
                Debug.LogError("Input bone must be a bone in the skinned mesh renderer.");
                return false;
            }
            
            // Bone data
            var bindPose = mesh.GetBindposes()[boneIndex];
            //var vertexTransformMatrix = bone.localToWorldMatrix * bindPose;
            
            // Child bone data (to determine the "length" of the bone)
            var childBone = bone.GetChild(0); // Assume child bone roots are the "end" of the bone, since we just don't have that information in Unity...
            //var childBoneIndex = Array.IndexOf(skinnedMeshRenderer.bones, childBone);
            //var childBindPose = mesh.GetBindposes()[childBoneIndex];

            //var bindPosePosition = bindPose.MultiplyPoint3x4(Vector3.zero);
            //var childBindPosePosition = childBindPose.MultiplyPoint3x4(Vector3.zero);
            //var boneLength = math.distance(bindPosePosition, childBindPosePosition);
            
            // Collect data in bone space
            // Create a sphere for each end of the bone
            var startPoint = float3.zero;
            var endPoint = childBone.localPosition;
            var startBounds = new BoundsNative(startPoint, .01f);
            var endBounds = new BoundsNative(endPoint, 0.01f);

            if (draw.HasValue)
            {
                // Bone joints
                draw.Value.WireSphere(bone.TransformPoint(startBounds.Center), .2f, Color.red);
                draw.Value.WireSphere(bone.TransformPoint(endBounds.Center), .2f, Color.green);
            }

            // Iterate weights and consider the positioning of the vertices weighted to this bone
            int boneWeightIndex = 0;
            // For each vertex
            for (int v = 0; v < vertices.Length; v++)
            {
                var boneCount = bonesPerVertexView[v];

                // For each bone affecting this vertex
                float boneWeight = 0;
                for (int localBoneIndex = 0; localBoneIndex < boneCount; localBoneIndex++)
                {
                    var boneWeightSet = boneWeightsView[boneWeightIndex++];
                    // Only looking for this bone
                    if (boneWeightSet.boneIndex != boneIndex)
                        continue;

                    // This vertex is affected by this bone
                    boneWeight = boneWeightSet.weight;
                    // Only one vertex<->bone relationship is possible, exit as soon as we find it
                    // Breaking here actually ruins the boneWeightIndex incrementation
                    //break;
                }

                // Check if this vertex is weighted to this bone at all
                if (boneWeight <= 0.001f)
                    continue;

                // Where is the vertex in bone space?
                //var vertexWorldPos = skinnedMeshRenderer.transform.TransformPoint(vertices[v]);
                //var vertexPosLocalToBone = bone.InverseTransformPoint(vertexWorldPos);
                var vertexPosLocalToBone = bindPose.MultiplyPoint3x4(vertices[v]);
                // Which end of the bone is the vertex closer to?
                var distToStartSqr = math.distancesq(vertexPosLocalToBone, startPoint);
                var distToEndSqr = math.distancesq(vertexPosLocalToBone, endPoint);
                bool closerToEnd = distToEndSqr < distToStartSqr;

                float3 anchorPoint = closerToEnd ? endPoint : startPoint;
                if (draw.HasValue)
                {
                    // Retransform even though we know already, so that if the prior transforms are wrong, it is obvious
                    var anchorPointWorld = bone.TransformPoint(anchorPoint);
                    var vertexPosWorld = bone.TransformPoint(vertexPosLocalToBone);
                    var color = closerToEnd ? Color.green : Color.red;
                    color.a = boneWeight;
                    draw.Value.Line(anchorPointWorld, vertexPosWorld, color);
                }
                
                // Influence the vertex effects on bounds based on weight
                var positioningBoneWeight = math.saturate(math.unlerp(weightImportanceRemap.x, weightImportanceRemap.y, boneWeight));
                vertexPosLocalToBone = math.lerp(anchorPoint, vertexPosLocalToBone, positioningBoneWeight);
                
                ref var boundsRef = ref closerToEnd ? ref endBounds : ref startBounds;
                boundsRef.Encapsulate(vertexPosLocalToBone);
            }
            
            if (draw.HasValue)
            {
                var startBoundsWorld = new OBoundsNative(startBounds, Space.Self, bone.localToWorldMatrix, bone.worldToLocalMatrix);
                var endBoundsWorld = new OBoundsNative(endBounds, Space.Self, bone.localToWorldMatrix, bone.worldToLocalMatrix);
                
                startBoundsWorld.AlineDraw(draw.Value, Color.red, default);
                endBoundsWorld.AlineDraw(draw.Value, Color.green, default);
            }

            // Produce a capsule from the bounds pair
            var startRadius = math.csum(startBounds.Extents.xz) * .5f;
            var endRadius = math.csum(endBounds.Extents.xz) * .5f;
            
            // Fit the sphere to the furthest extent of the bounds to better encapsulate
            var startCenter = startBounds.Center;
            startCenter.y -= startBounds.Extents.y;
            startCenter.y += startRadius;
            var endCenter = endBounds.Center;
            endCenter.y += endBounds.Extents.y;
            endCenter.y -= endRadius;
            
            // Old method that just used the centers of the bounds
            //var startSphere = new SphereNative(startBounds.Center, math.csum(startBounds.Extents.xz) * .5f);
            //var endSphere = new SphereNative(endBounds.Center, math.csum(endBounds.Extents.xz) * .5f);
            
            var startSphere = new SphereNative(startCenter, startRadius);
            var endSphere = new SphereNative(endCenter, endRadius);
            boneSpaceCapsule = new CapsuleNative(startSphere, endSphere); //

            draw?.PopDuration();
            return true;
        }
    }
}
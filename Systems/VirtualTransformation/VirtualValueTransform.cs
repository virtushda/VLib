﻿#if UNITY_EDITOR
//#define NANCHECKS
#endif

using System;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using float4x4 = Unity.Mathematics.float4x4;

namespace VLib
{
    /// <summary> A thread-safe virtual VirtualTransform that can be used to do Transform operations on other threads.
    /// Not all transform features are implemented, but there is reference code at the bottom of this class that can help with that. </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct VirtualValueTransform : IEquatable<VirtualValueTransform>, IDisposable
    {
        public static implicit operator bool(VirtualValueTransform t) => t.IsCreated && t.DataPtr != null && t.DataPtr->transformID != 0;
        
        public struct Internal
        {
            public int transformID;
            /// <summary> Very cheap fetch. This is not directly equivalent to localToWorldMatrix, this is actually parentToWorld. If no parent, then it is localToWorld. </summary>
            public float4x4 localMatrix;

            public VirtualValueTransform _parent;
            public UnsafeList<VirtualValueTransform> _children;
        }

        /// <summary> Protected native buffer that must be keyed by an external collection for safety. </summary>
        VUnsafeKeyedRef<Internal> data;

        readonly Internal* DataPtr
        {
            get
            {
                var ptr = data.TPtr;
                // It will report errors and return null if it can't get the ptr, so we test for that and throw in this case
                if (ptr == null)
                    throw new NullReferenceException("VirtualValueTransform is not initialized!");
                return ptr;
            }
        }

        public readonly bool IsCreated => data.IsCreated;
        
        /// <summary> Creates a new VirtualTransform. </summary>
        public VirtualValueTransform(byte* key, Transform t, VirtualValueTransform parent = default)
        {
            data = default;
            SetData(key, t, parent);
        }

        /// <summary> Allows you to create null (or default) virtual transforms and populate them later. Be careful not to call this on a copy! </summary>
        public void SetData(byte* key, Transform t, VirtualValueTransform newParent = default)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t), "Transform is NULL!");

            if (!data.IsCreated)
            {
                var internalValue = new Internal
                {
                    _children = new UnsafeList<VirtualValueTransform>(4, Allocator.Persistent),
                };
                data = new VUnsafeKeyedRef<Internal>(internalValue, key, Allocator.Persistent);
                //data = RefStruct<Internal>.Create(internalValue);
            }

            // The virtual transform will be able to be related efficiently via this ID
            DataPtr->transformID = t.GetInstanceID();
            parent = newParent;

            //localMatrix = float4x4.TRS(t.localPosition, t.localRotation, t.localScale);
            CopyTRSFrom(t);
        }
        
        public void Dispose()
        {
            if (data.IsCreated)
                data.Dispose();
        }

        /// <summary> The transform.GetInstanceID() that this virtualtransform refers to. </summary>
        public readonly int TransformID => DataPtr->transformID;

        public readonly VirtualValueTransform parent
        {
            get => data.IsCreated ? DataPtr->_parent : default;
            set
            {
                if (!data.IsCreated)
                {
                    Debug.LogError("VirtualValueTransform not initialized, cannot set parent!");
                    return;
                }
                var p = DataPtr->_parent;
                if (p.IsCreated && p.TransformID != 0)
                    p.RemoveChild(this);
                DataPtr->_parent = value;
                if (value == default)
                    return;
                DataPtr->_parent.AddChild(this);
            }
        }

        /// <summary> Gets the root most parent, or if parentless, itself. </summary>
        public readonly VirtualValueTransform Root
        {
            get
            {
                var cur = this;
                for (int i = 0; i < 512; ++i)
                {
                    var p = cur.parent;
                    if (p.IsCreated)
                        cur = p;
                    else
                        break;
                }
                return cur;
            }
        }

        /// <summary> For write access use the DataPtr </summary>
        public readonly UnsafeList<VirtualValueTransform>.ReadOnly children => DataPtr->_children.AsReadOnly();
        
        public readonly void AddChild(VirtualValueTransform child)
        {
            if (DataPtr->_children.Contains(child))
                return;
            DataPtr->_children.Add(child);
            if (child.parent != this)
                child.parent = this; // TODO: Ensure recursive call doesn't cause infinite loop or performance issues
        }

        public readonly void RemoveChild(VirtualValueTransform child)
        {
            if (!DataPtr->_children.RemoveValue(child))
                return;
            var childPtr = child.DataPtr;
            if (childPtr->_parent == this)
                childPtr->_parent = default;
        }

        /// <summary> <inheritdoc cref="Internal.localMatrix"/> </summary>
        public readonly ref float4x4 LocalMatrixRef => ref DataPtr->localMatrix;
        /// <summary> <inheritdoc cref="Internal.localMatrix"/> </summary>
        public readonly float4x4 LocalMatrix
        {
            get => DataPtr->localMatrix;
            set => DataPtr->localMatrix = value;
        }

        public readonly float3 positionNative
        {
            get => GetPosition();
            set => SetPosition(value);
        }

        public readonly Vector3 position
        {
            get => positionNative;
            set => positionNative = value;
        }

        public readonly quaternion rotationNative
        {
            get => rotation;
            set => rotation = value;
        }

        public readonly Quaternion rotation
        {
            get => GetRotation();
            set => SetRotationSafe(value);
        }

        public readonly Vector3 lossyScale
        {
            get => lossyScaleNative;
        }

        public readonly float3 lossyScaleNative
        {
            get => GetWorldScale();
        }

        public readonly float3 localPosition
        {
            get => LocalMatrix.c3.xyz;
            set => LocalMatrixRef.c3.xyz = value;
        }

        public readonly Quaternion localRotation
        {
            get => localRotationNative;
            set => localRotationNative = value;
        }
        // Not sure if these are corrct, worry later
        public readonly quaternion localRotationNative
        {
            get => LocalMatrixRef.RotationDelta();
            set
            {
                // TODO: Opt
                LocalMatrix = float4x4.TRS(localPosition, value, localScale);
            }
        }

        public readonly float3 localScale
        {
            get => LocalMatrix.ScaleDelta();
            set => LocalMatrix = float4x4.TRS(localPosition, localRotation, value);
        }
        
        // The red axis of the VirtualTransform in world space.
        public readonly Vector3 right
        {
            get => rotation * Vector3.right;
            set => rotation = Quaternion.FromToRotation(Vector3.right, value);
        }

        // The green axis of the VirtualTransform in world space.
        public readonly Vector3 up
        {
            get => rotation * Vector3.up;
            set => rotation = Quaternion.FromToRotation(Vector3.up, value);
        }

        // The blue axis of the VirtualTransform in world space.
        public readonly Vector3 forward
        {
            get => rotation * Vector3.forward;
            set => rotation = Quaternion.LookRotation(value);
        }
        
        public readonly float4x4 localToWorldMatrix => GetLocalToWorldMatrix();
        
        public readonly AffineTransform localToWorldAffineTransform => new(localToWorldMatrix);

        // VirtualTransforms /direction/ from local space to world space.
        public readonly float3 TransformDirection(float3 inDirection)
        {
            return rotate(rotationNative, inDirection);
            //return Quaternion.RotateVectorByQuat(GetRotation(), inDirection);
        }

        // VirtualTransforms a /direction/ from world space to local space. The opposite of VirtualTransform.VirtualTransformDirection.
        public readonly float3 InverseTransformDirection(float3 inDirection)
        {
            return rotate(inverse(rotationNative), inDirection);
            //return Quaternion.RotateVectorByQuat(Quaternion.Inverse(GetRotation()), inDirection);
        }

        // VirtualTransforms /vector/ from local space to world space.
        public readonly float3 TransformVector(float3 inVector)
        {
            VirtualValueTransform cur = this;
            while (cur)
            {
                //worldVector.Scale(cur.m_LocalScale);
                //worldVector = Quaternion.RotateVectorByQuat(cur.m_LocalRotation, worldVector);
                inVector *= cur.localScale;
                inVector = rotate(cur.localRotationNative, inVector);

                cur = cur.parent;
            }
            return inVector;
        }

        /*// VirtualTransforms a /vector/ from world space to local space. The opposite of VirtualTransform.VirtualTransformVector.
        public readonly float3 InverseTransformVector(float3 inVector)
        {
            float3 newVector, localVector;
            VirtualTransform father = parent;
            if (father)
                localVector = father.InverseVirtualTransformVector(inVector);
            else
                localVector = inVector;

            newVector = Quaternion.RotateVectorByQuat(Quaternion.Inverse(m_LocalRotation), localVector);
            if (m_InternalVirtualTransformType != kNoScaleVirtualTransform)
                newVector.Scale(InverseSafe(m_LocalScale));

            return newVector;
        }*/

        static float InverseSafe(float f)
        {
            if (Mathf.Abs(f) > Vector3.kEpsilon)
                return 1.0F / f;
            return 0.0F;
        }

        static float3 InverseSafe(float3 v)
        {
            return new float3(InverseSafe(v.x), InverseSafe(v.y), InverseSafe(v.z));
        }

        // VirtualTransforms /position/ from local space to world space.
        public readonly float3 TransformPoint(float3 inPoint)
        {
            return transform(localToWorldMatrix, inPoint);
            //return = localToWorldMatrix.MultiplyPoint(inPoint);
        }

        // VirtualTransforms /position/ from world space to local space. The opposite of VirtualTransform.VirtualTransformPoint.
        public readonly float3 InverseTransformPoint(float3 inPosition)
        {
            float3 newPosition, localPosTemp;
            if (parent)
                localPosTemp = parent.InverseTransformPoint(inPosition);
            else
                localPosTemp = inPosition;

            localPosTemp -= localPosition;
            newPosition = rotate(inverse(localRotationNative), localPosTemp);
            //newPosition = Quaternion.RotateVectorByQuat(Quaternion.Inverse(m_LocalRotation), localPosition);
            ((Vector3)newPosition).Scale(InverseSafe(localScale));

            return newPosition;
        }
        
        // Doesn't seem to work right
        /*/// <summary> Copies Translations and Rotations from the given transform with safety checks for NANS (in editor only).
        /// More efficient than <see cref="CopyTRSFromSafe"/>.
        /// For speed, assumes scale is (1,1,1)! </summary>
        public readonly void CopyPosRotFrom(ref TransformAccess transformAccess)
        {
            transformAccess.GetLocalPositionAndRotation(out var tLocalPos, out var tLocalRot);

#if NANCHECKS
            if (any(isnan(tLocalPos)) || any(isnan(((quaternion) tLocalRot).value)))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            
            DataPtr->localMatrix = float4x4.TRS(tLocalPos, tLocalRot, VMath.One3);
        }*/

        // Mysteriously doesn't work, the NoOpt version does...
        /*/// <summary> Copies Translations, Rotations and Scales from the given transform with safety checks for (in editor only). </summary>
        public readonly void CopyTRSFrom(ref TransformAccess transformAccess)
        {
            transformAccess.GetLocalPositionAndRotation(out var tLocalPos, out var tLocalRot);
            var tLocalScale = transformAccess.localScale;

#if NANCHECKS
            if (any(isnan(tLocalPos)) || any(isnan(((quaternion) tLocalRot).value)) || any(isnan(tLocalScale)) || any((float3) tLocalScale == 0))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            
            DataPtr->localMatrix = float4x4.TRS(tLocalPos, tLocalRot, tLocalScale);
        }*/

        /// <summary> Copies Translations, Rotations and Scales from the given transform with safety checks for (in editor only). </summary>
        public readonly void CopyTRSFromNoOpt(ref TransformAccess transformAccess)
        {
            var tLocalPos = transformAccess.localPosition;
            var tLocalRot = transformAccess.localRotation;
            var tLocalScale = transformAccess.localScale;

#if NANCHECKS
            if (any(isnan(tLocalPos)) || any(isnan(((quaternion) tLocalRot).value)) || any(isnan(tLocalScale)) || any((float3) tLocalScale == 0))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            
            DataPtr->localMatrix = float4x4.TRS(tLocalPos, tLocalRot, tLocalScale);
        }

        /// <summary> Copies Translations, Rotations and Scales from the given transform with safety checks for (in editor only). </summary>
        public readonly void CopyTRSFrom(Transform transform)
        {
            transform.GetLocalPositionAndRotation(out var tLocalPos, out var tLocalRot);
            var tLocalScale = transform.localScale;

#if NANCHECKS
            if (any(isnan(tLocalPos)) || any(isnan(((quaternion) tLocalRot).value)) || any(isnan(tLocalScale)) || any((float3) tLocalScale == 0))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            
            DataPtr->localMatrix = float4x4.TRS(tLocalPos, tLocalRot, tLocalScale);
        }
        
        /// <summary> Copies Translations, Rotations and Scales TO the given transform with safety checks for (in editor only). </summary>
        public readonly void CopyTRSTo(ref TransformAccess transformAccess)
        {
            // Get all at once
            DataPtr->localMatrix.Decompose(out float3 localPos, out quaternion localRot, out float3 locScale);

#if NANCHECKS
            if (any(isnan(localPos)) || any(isnan(localRot.value)) || any(isnan(locScale)) || any(locScale == 0))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            
            transformAccess.SetLocalPositionAndRotation(localPos, localRot);
            transformAccess.localScale = locScale;
        }
        
        /// <summary> Copies Translations, Rotations and Scales TO the given transform with safety checks for (in editor only). </summary>
        public readonly void CopyTRSToNoOpt(ref TransformAccess transformAccess)
        {
            // Get all at once
            DataPtr->localMatrix.Decompose(out float3 localPos, out quaternion localRot, out float3 locScale);

#if NANCHECKS
            if (any(isnan(localPos)) || any(isnan(localRot.value)) || any(isnan(locScale)) || any(locScale == 0))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            transformAccess.localPosition = localPos;
            transformAccess.localRotation = localRot;
            transformAccess.localScale = locScale;
        }
        
        /// <summary> Copies Translations and Rotations TO the given transform with safety checks for (in editor only).
        /// More efficient than <see cref="CopyTRSToSafe"/>. </summary>
        public readonly void CopyPosRotTo(ref TransformAccess transformAccess)
        {
            // Get all at once
            DataPtr->localMatrix.Decompose(out float3 localPos, out quaternion localRot, out float3 locScale);

#if NANCHECKS
            if (any(isnan(localPos)) || any(isnan(localRot.value)))
                Debug.LogError($"Nans detected in transform, ID of virtual is: {DataPtr->transformID}");
#endif
            
            transformAccess.SetLocalPositionAndRotation(localPos, localRot);
            transformAccess.localScale = locScale;
        }

        #region Internal Methods
        
        readonly float4x4 GetLocalToWorldMatrix()
        {
            float4x4 t = DataPtr->localMatrix; //float4x4.TRS(localPosition, m_LocalRotation, m_LocalScale);
            if (parent)
                t = mul(parent.GetLocalToWorldMatrix(), t); //parent.GetLocalToWorldMatrix() * t;

            return t;
        }
        
        readonly float3 GetPosition()
        {
            if (parent)
                return transform(parent.GetLocalToWorldMatrix(), localPosition);
            return localPosition;
        }

        readonly void SetPosition(float3 newPosition)
        {
            var p = parent;
            if (p)
                newPosition = p.InverseTransformPoint(newPosition);

            localPosition = newPosition;
        }

        readonly Quaternion GetRotation()
        {
            Quaternion worldRot = localRotation;
            if (!parent)
                return worldRot;
            
            // Recurse up parent chain
            return parent.GetRotation() * worldRot;
        }
        
        readonly void SetRotationSafe(Quaternion q)
        {
            var p = parent;
            if (p)
                SetLocalRotationSafe(Quaternion.Inverse(p.GetRotation()) * q);
            else
                SetLocalRotationSafe(q);
        }
        
        readonly void SetLocalRotation(Quaternion q) => localRotation = q;

        readonly void SetLocalRotationSafe(Quaternion q) => SetLocalRotation(q.normalized);
        
        readonly float3 GetWorldScale()
        {
            float3 scale = localScale;
            if (!parent)
                return scale;
            
            // Recurse up parent chain
            return parent.GetWorldScale() * scale;
        }

        #endregion

        /// <summary> Returns true if both virtual transforms share the same transformID or are both unallocated (NULL). </summary>
        public readonly bool Equals(VirtualValueTransform other)
        {
            // Access allocation status and get pointers at the same time
            bool thisNull = !data.TryGetPtr(out var thisDataPtr);
            bool otherNull = !other.data.TryGetPtr(out var otherDataPtr);
            
            // Null handling
            bool anyNull = otherNull || thisNull;
            if (Hint.Unlikely(anyNull))
            {
                // One must be null inside this block
                // True if both are null
                // False if only one is null
                return thisNull == otherNull;
            }

            // Now that we know both transforms are created, compare their transform IDs
            return thisDataPtr->transformID.Equals(otherDataPtr->transformID);
            
            // This is now availble in EqualsInstance:
            
            /*var hasPtrA = data.TryGetPtr(out var ptr);
            var hasPtrB = other.data.TryGetPtr(out var ptrB);

            // Only one has ptr, must be different
            if (hasPtrA != hasPtrB)
                return false;
            // Neither have ptr, must be equal as default values
            if (!hasPtrA)
                return true;
            
            // Both have ptr, compare
            return ptr == ptrB;*/
        }
        
        public readonly override bool Equals(object obj) => obj is VirtualValueTransform other && Equals(other);

        /// <summary> Only the same transform instance if sharing an internal memory pointer. </summary>
        public readonly bool EqualsInstance(VirtualValueTransform other) => data.Equals(other.data);

        public static bool operator ==(VirtualValueTransform left, VirtualValueTransform right) => left.Equals(right);
        public static bool operator !=(VirtualValueTransform left, VirtualValueTransform right) => !left.Equals(right);

        public readonly override int GetHashCode()
        {
            bool hasPtr = data.TryGetPtr(out var ptr);
            if (Hint.Likely(hasPtr))
                return ((IntPtr) ptr).GetHashCode();
            return 0;
        }
    }
    
    // REFERENCE CODE for writing found on da github
    
    /*public class VirtualTransform : Component, IEnumerable
    {
        // The position of the VirtualTransform in world space.
        public Vector3 position
        {
            get => GetPosition();
            set => SetPosition(value);
        }

        // Position of the VirtualTransform relative to the parent VirtualTransform.
        public Vector3 localPosition
        {
            get => GetLocalPosition();
            set => SetLocalPosition(value);
        }

        // The rotation as Euler angles in degrees.
        public Vector3 eulerAngles
        {
            get => rotation.eulerAngles;
            set => rotation = Quaternion.Euler(value);
        }

        // The rotation as Euler angles in degrees relative to the parent VirtualTransform's rotation.
        public Vector3 localEulerAngles
        {
            get => GetLocalEulerAngles();
            set => SetLocalEulerAngles(value);
        }

        // The red axis of the VirtualTransform in world space.
        public Vector3 right
        {
            get => rotation * Vector3.right;
            set => rotation = Quaternion.FromToRotation(Vector3.right, value);
        }

        // The green axis of the VirtualTransform in world space.
        public Vector3 up
        {
            get => rotation * Vector3.up;
            set => rotation = Quaternion.FromToRotation(Vector3.up, value);
        }

        // The blue axis of the VirtualTransform in world space.
        public Vector3 forward
        {
            get => rotation * Vector3.forward;
            set => rotation = Quaternion.LookRotation(value);
        }

        // The rotation of the VirtualTransform in world space stored as a [[Quaternion]].
        public Quaternion rotation
        {
            get => GetRotation();
            set => SetRotationSafe(value);
        }

        // The rotation of the VirtualTransform relative to the parent VirtualTransform's rotation.
        public Quaternion localRotation
        {
            get => GetLocalRotation();
            set => SetLocalRotationSafe(value);
        }

        // The scale of the VirtualTransform relative to the parent.
        public Vector3 localScale
        {
            get => GetLocalScale();
            set => SetLocalScale(value);
        }

        // The parent of the VirtualTransform.
        public VirtualTransform parent
        {
            get => parentInternal;
            set => parentInternal = value;
        }

        public void SetParent(VirtualTransform parent)
        {
            SetParent(parent, true);
        }

        // Matrix that VirtualTransforms a point from world space into local space (RO).
        public Matrix4x4 worldToLocalMatrix => GetWorldToLocalMatrix();

        // Matrix that VirtualTransforms a point from local space into world space (RO).
        public Matrix4x4 localToWorldMatrix => GetLocalToWorldMatrix();

        bool _hasChanged = false;

        bool _hackDirty = true;
        bool _needUpdateToRender = true;

        Matrix4x4 m_cachedLTW; //local to world
        // bool _hackDirtyWTL = true;
        // Matrix4x4 m_cachedWTL;	//world to local

        internal bool hackDirty
        {
            get => _hackDirty;
            set
            {
                _hackDirty = value;
                _needUpdateToRender = value;
                if (value)
                {
                    _hasChanged = true;
                    foreach (var c in children)
                    {
                        c.hackDirty = true;
                    }
                }
            }
        }

        // Moves the VirtualTransform in the direction and distance of /translation/.
        public void Translate(Vector3 translation, Space relativeTo = Space.Self)
        {
            if (relativeTo == Space.World)
                position += translation;
            else
                position += VirtualTransformDirection(translation);
            hackDirty = true;
        }

        // Moves the VirtualTransform by /x/ along the x axis, /y/ along the y axis, and /z/ along the z axis.
        public void Translate(float x, float y, float z, Space relativeTo = Space.Self)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        // Moves the VirtualTransform in the direction and distance of /translation/.
        public void Translate(Vector3 translation, VirtualTransform relativeTo)
        {
            if (relativeTo)
                position += relativeTo.VirtualTransformDirection(translation);
            else
                position += translation;
            hackDirty = true;
        }

        // Moves the VirtualTransform by /x/ along the x axis, /y/ along the y axis, and /z/ along the z axis.
        public void Translate(float x, float y, float z, VirtualTransform relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        // Applies a rotation of /eulerAngles.z/ degrees around the z axis, /eulerAngles.x/ degrees around the x axis, and /eulerAngles.y/ degrees around the y axis (in that order).
        public void Rotate(Vector3 eulerAngles, Space relativeTo = Space.Self)
        {
            Quaternion eulerRot = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
            if (relativeTo == Space.Self)
                localRotation = localRotation * eulerRot;
            else
            {
                rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
            }

            hackDirty = true;
        }

        // Applies a rotation of /zAngle/ degrees around the z axis, /xAngle/ degrees around the x axis, and /yAngle/ degrees around the y axis (in that order).
        public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo = Space.Self)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
        }

        // Rotates the VirtualTransform around /axis/ by /angle/ degrees.
        public void Rotate(Vector3 axis, float angle, Space relativeTo = Space.Self)
        {
            if (relativeTo == Space.Self)
                RotateAroundInternal(VirtualTransform.VirtualTransformDirection(axis), angle * Mathf.Deg2Rad);
            else
                RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
            hackDirty = true;
        }

        // Rotates the VirtualTransform about /axis/ passing through /point/ in world coordinates by /angle/ degrees.
        public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Vector3 worldPos = position;
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            Vector3 dif = worldPos - point;
            dif = q * dif;
            worldPos = point + dif;
            position = worldPos;
            RotateAroundInternal(axis, angle * Mathf.Deg2Rad);
            hackDirty = true;
        }

        // Rotates the VirtualTransform so the forward vector points at /target/'s current position.
        public void LookAt(VirtualTransform target)
        {
            LookAt(target, Vector3.up);
        }

        public void LookAt(VirtualTransform target, Vector3 worldUp)
        {
            if (target)
                LookAt(target.position, worldUp);
        }

        // Rotates the VirtualTransform so the forward vector points at /worldPosition/.
        public void LookAt(Vector3 worldPosition)
        {
            LookAt(worldPosition, Vector3.up);
        }

        // VirtualTransforms direction /x/, /y/, /z/ from local space to world space.
        public Vector3 VirtualTransformDirection(float x, float y, float z)
        {
            return VirtualTransformDirection(new Vector3(x, y, z));
        }

        // VirtualTransforms the direction /x/, /y/, /z/ from world space to local space. The opposite of VirtualTransform.VirtualTransformDirection.
        public Vector3 InverseVirtualTransformDirection(float x, float y, float z)
        {
            return InverseVirtualTransformDirection(new Vector3(x, y, z));
        }

        // VirtualTransforms vector /x/, /y/, /z/ from local space to world space.
        public Vector3 VirtualTransformVector(float x, float y, float z)
        {
            return VirtualTransformVector(new Vector3(x, y, z));
        }

        // VirtualTransforms the vector /x/, /y/, /z/ from world space to local space. The opposite of VirtualTransform.VirtualTransformVector.
        public Vector3 InverseVirtualTransformVector(float x, float y, float z)
        {
            return InverseVirtualTransformVector(new Vector3(x, y, z));
        }

        // VirtualTransforms the position /x/, /y/, /z/ from local space to world space.
        public Vector3 VirtualTransformPoint(float x, float y, float z)
        {
            return VirtualTransformPoint(new Vector3(x, y, z));
        }

        // VirtualTransforms the position /x/, /y/, /z/ from world space to local space. The opposite of VirtualTransform.VirtualTransformPoint.
        public Vector3 InverseVirtualTransformPoint(float x, float y, float z)
        {
            return InverseVirtualTransformPoint(new Vector3(x, y, z));
        }

        // The global scale of the object (RO).
        public Vector3 lossyScale => GetWorldScaleLossy();

        // Has the VirtualTransform changed since the last time the flag was set to 'false'?
        public bool hasChanged
        {
            get => GetChangedFlag();
            set => SetChangedFlag(value);
        }

        //*undocumented*
        public VirtualTransform FindChild(string name)
        {
            return Find(name);
        }

        //*undocumented* Documented separately
        public IEnumerator GetEnumerator()
        {
            return new VirtualTransform.Enumerator(this);
        }

        private class Enumerator : IEnumerator
        {
            VirtualTransform outer;
            int currentIndex = -1;

            internal Enumerator(VirtualTransform outer)
            {
                this.outer = outer;
            }

            //*undocumented*
            public object Current => outer.GetChild(currentIndex);

            //*undocumented*
            public bool MoveNext()
            {
                int childCount = outer.childCount;
                return ++currentIndex < childCount;
            }

            //*undocumented*
            public void Reset()
            {
                currentIndex = -1;
            }
        }

        Vector3 m_LocalPosition;
        Vector3 m_LocalScale;
        Quaternion m_LocalRotation;

        Vector3 GetPosition()
        {
            if (parent)
                return parent.localToWorldMatrix.MultiplyPoint(m_LocalPosition);
            else
                return m_LocalPosition;
        }

        void SetPosition(Vector3 position)
        {
            Vector3 newPosition = position;
            VirtualTransform p = parent;
            if (p)
            {
                newPosition = p.InverseVirtualTransformPoint(newPosition);
            }

            SetLocalPosition(newPosition);
        }

        Vector3 GetLocalPosition()
        {
            return m_LocalPosition;
        }

        void SetLocalPosition(Vector3 localPosition)
        {
            if (m_LocalPosition != localPosition)
            {
                m_LocalPosition = localPosition;
                SetDirty();
                hackDirty = true;
            }
        }

        Vector3 GetLocalEulerAngles()
        {
            Quaternion rotation = Quaternion.NormalizeSafe(m_LocalRotation);
            return Quaternion.QuaternionToEuler(rotation) * Mathf.Rad2Deg;
        }

        void SetLocalEulerAngles(Vector3 value)
        {
            SetLocalRotationSafe(Quaternion.EulerToQuaternion(value * Mathf.Deg2Rad));
        }

        Quaternion GetRotation()
        {
            // Matrix4x4 mat = new Matrix4x4();
            // Quaternion.QuaternionToMatrix(m_LocalRotation, ref mat);
            // mat = localToWorldMatrix * mat;

            // Quaternion worldRot = new Quaternion();
            // Quaternion.MatrixToQuaternion(mat, ref worldRot);

            Quaternion worldRot = m_LocalRotation;
            VirtualTransform p = parent;
            while (p)
            {
                worldRot = p.m_LocalRotation * worldRot;
                p = p.parent;
            }

            return worldRot;
        }

        void SetRotationSafe(Quaternion q)
        {
            VirtualTransform p = parent;
            if (p)
                SetLocalRotation(Quaternion.NormalizeSafe(Quaternion.Inverse(p.GetRotation()) * q));
            else
                SetLocalRotation(Quaternion.NormalizeSafe(q));
        }

        Quaternion GetLocalRotation()
        {
            return m_LocalRotation;
        }

        void SetLocalRotation(Quaternion q)
        {
            if (m_LocalRotation != q)
            {
                m_LocalRotation = q;
                SetDirty();
                hackDirty = true;
            }
        }

        void SetLocalRotationSafe(Quaternion q)
        {
            SetLocalRotation(Quaternion.NormalizeSafe(q));
        }

        Vector3 GetLocalScale()
        {
            return m_LocalScale;
        }

        void SetLocalScale(Vector3 scale)
        {
            if (m_LocalScale != scale)
            {
                m_LocalScale = scale;
                RecalculateVirtualTransformType();
                SetDirty();
                hackDirty = true;
            }
        }

        const int kNoScaleVirtualTransform = 0;
        const int kUniformScaleVirtualTransform = 1 << 0;
        const int kNonUniformScaleVirtualTransform = 1 << 1;
        const int kOddNegativeScaleVirtualTransform = 1 << 2;

        int m_InternalVirtualTransformType;

        void RecalculateVirtualTransformType()
        {
            if (Mathf.Approximately(m_LocalScale.x, m_LocalScale.y) && Mathf.Approximately(m_LocalScale.y, m_LocalScale.z))
            {
                if (Mathf.Approximately(m_LocalScale.x, 1.0F))
                {
                    m_InternalVirtualTransformType = kNoScaleVirtualTransform;
                }
                else
                {
                    m_InternalVirtualTransformType = kUniformScaleVirtualTransform;
                    if (m_LocalScale.x < 0.0F)
                    {
                        m_InternalVirtualTransformType = kOddNegativeScaleVirtualTransform | kNonUniformScaleVirtualTransform;
                    }
                }
            }
            else
            {
                m_InternalVirtualTransformType = kNonUniformScaleVirtualTransform;

                int hasOddNegativeScale = m_LocalScale.x * m_LocalScale.y * m_LocalScale.z < 0.0F ? 1 : 0;
                m_InternalVirtualTransformType |= hasOddNegativeScale * kOddNegativeScaleVirtualTransform;
            }
        }

        VirtualTransform _parent;

        internal VirtualTransform parentInternal
        {
            get => _parent;
            set => SetParent(value, true);
        }

        internal bool SetParent(VirtualTransform NewParent, bool worldPositionStays)
        {
            if (NewParent == parent)
                return true;

            SetDirty();
            SetCacheDirty();
            hackDirty = true;

            var go = gameObject;

            if (go.IsDestroying() || (NewParent && NewParent.gameObject.IsDestroying()))
                return false;

            if (parent && parent.gameObject.IsActivating() || (NewParent && NewParent.gameObject.IsActivating()))
            {
                Debug.LogError("Cannot change GameObject hierarchy while activating or deactivating the parent.");
                return false;
            }

            // Make sure that the new father is not a child of this VirtualTransform.
            if (IsChildOrSameVirtualTransform(NewParent, this))
                return false;

            // Save the old position in worldspace
            Vector3 worldPosition = new Vector3();
            Quaternion worldRotation = new Quaternion();
            Matrix4x4 worldScale = new Matrix4x4();

            if (worldPositionStays)
            {
                worldPosition = GetPosition();
                worldRotation = GetRotation();
                worldScale = GetWorldRotationAndScale();
            }

            VirtualTransform previousParent = parent;
            if (previousParent)
                previousParent.SetDirty();

            // At this point SetParentInternal MUST return true only if we really changed a parent
            SetParentInternal(NewParent);

            SendParentChanged();

            if (NewParent)
                NewParent.SetDirty();

            if (worldPositionStays)
            {
                SetPositionAndRotationSafeWithoutNotification(worldPosition, worldRotation);
                SetWorldRotationAndScaleWithoutNotification(worldScale);
            }

            return true;
        }

        static bool IsChildOrSameVirtualTransform(VirtualTransform VirtualTransform, VirtualTransform inParent)
        {
            VirtualTransform child = VirtualTransform;
            while (child)
            {
                if (child == inParent)
                    return true;
                child = child.parent;
            }

            return false;
        }

        Matrix4x4 GetWorldRotationAndScale()
        {
            Matrix4x4 ret = new Matrix4x4();
            ret.SetTRS(new Vector3(0, 0, 0), m_LocalRotation, m_LocalScale);
            if (parent)
            {
                Matrix4x4 parentVirtualTransform = parent.GetWorldRotationAndScale();
                ret = parentVirtualTransform * ret;
            }

            return ret;
        }

        void SetParentInternal(VirtualTransform NewParent)
        {
            //Early out if the new parent is already the current parent
            if (NewParent == parent)
                return;

            // If it already has an father, remove this from fathers children
            if (parent)
            {
                parent.__RemoveChild(this);
            }

            if (NewParent)
            {
                NewParent.__AddChild(this);
            }

            _parent = NewParent;
        }

        void SetPositionAndRotationSafeWithoutNotification(Vector3 p, Quaternion q)
        {
            if (parent)
            {
                m_LocalPosition = parent.InverseVirtualTransformPoint(p);
                m_LocalRotation = Quaternion.NormalizeSafe(Quaternion.Inverse(parent.GetRotation()) * q);
                hackDirty = true;
            }
            else
            {
                m_LocalPosition = p;
                m_LocalRotation = Quaternion.NormalizeSafe(q);
                hackDirty = true;
            }
        }

        void SetWorldRotationAndScaleWithoutNotification(Matrix4x4 scale)
        {
            m_LocalScale = Vector3.one;

            Matrix4x4 inverseRS = GetWorldRotationAndScale();
            inverseRS.Invert_Full();

            inverseRS = inverseRS * scale;

            m_LocalScale.x = inverseRS[0, 0];
            m_LocalScale.y = inverseRS[1, 1];
            m_LocalScale.z = inverseRS[2, 2];

            RecalculateVirtualTransformType();
            hackDirty = true;
        }

        void SetCacheDirty()
        {
            // do nothing for now.
        }

        Matrix4x4 GetWorldToLocalMatrix()
        {
            Matrix4x4 m = GetLocalToWorldMatrix();
            m.Invert_Full();
            return m;
        }

        Matrix4x4 GetLocalToWorldMatrix()
        {
            if (_hackDirty)
            {
                Matrix4x4 t = new Matrix4x4();
                t.SetTRS(m_LocalPosition, m_LocalRotation, m_LocalScale);
                if (parent != null)
                {
                    m_cachedLTW = parent.GetLocalToWorldMatrix() * t;
                }
                else
                {
                    m_cachedLTW = t;
                }

                _hackDirty = false;
            }

            return m_cachedLTW;
        }

        void GetPositionAndRotation(out Vector3 pos, out Quaternion q)
        {
            Vector3 worldPos = m_LocalPosition;
            Quaternion worldRot = m_LocalRotation;
            VirtualTransform cur = parent;
            while (cur)
            {
                worldPos.Scale(cur.m_LocalScale);
                worldPos = Quaternion.RotateVectorByQuat(cur.m_LocalRotation, worldPos);
                worldPos += cur.m_LocalPosition;

                worldRot = cur.m_LocalRotation * worldRot;

                cur = cur.parent;
            }

            pos = worldPos;
            q = worldRot;
        }

        internal Matrix4x4 GetWorldToLocalMatrixNoScale()
        {
            Vector3 pos;
            Quaternion rot;
            GetPositionAndRotation(out pos, out rot);
            Matrix4x4 m = new Matrix4x4();
            m.SetTRInverse(pos, rot);
            return m;
        }

        internal void RotateAroundInternal(Vector3 worldAxis, float rad)
        {
            Vector3 localAxis = InverseVirtualTransformDirection(worldAxis);
            if (localAxis.sqrMagnitude > Vector3.kEpsilon)
            {
                localAxis.Normalize();
                Quaternion q = Quaternion.AxisAngleToQuaternionSafe(localAxis, rad);
                m_LocalRotation = Quaternion.NormalizeSafe(m_LocalRotation * q);
                SetDirty();
            }
        }

        public void LookAt(Vector3 worldPosition, Vector4 worldUp)
        {
            Vector3 forward = worldPosition - GetPosition();
            Quaternion q = Quaternion.identity;
            if (Quaternion.LookRotationToQuaternion(forward, worldUp, ref q))
                SetRotationSafe(q);
            else
            {
                float mag = forward.magnitude;
                if (mag > Vector3.kEpsilon)
                {
                    SetRotationSafe(Quaternion.FromToQuaternionSafe(Vector3.back, forward / mag));
                }
            }

            hackDirty = true;
        }

        // VirtualTransforms /direction/ from local space to world space.
        public Vector3 VirtualTransformDirection(Vector3 inDirection)
        {
            return Quaternion.RotateVectorByQuat(GetRotation(), inDirection);
        }

        // VirtualTransforms a /direction/ from world space to local space. The opposite of VirtualTransform.VirtualTransformDirection.
        public Vector3 InverseVirtualTransformDirection(Vector3 inDirection)
        {
            return Quaternion.RotateVectorByQuat(Quaternion.Inverse(GetRotation()), inDirection);
        }

        // VirtualTransforms /vector/ from local space to world space.
        public Vector3 VirtualTransformVector(Vector3 inVector)
        {
            Vector3 worldVector = inVector;

            VirtualTransform cur = this;
            while (cur)
            {
                worldVector.Scale(cur.m_LocalScale);
                worldVector = Quaternion.RotateVectorByQuat(cur.m_LocalRotation, worldVector);

                cur = cur.parent;
            }

            return worldVector;
        }

        // VirtualTransforms a /vector/ from world space to local space. The opposite of VirtualTransform.VirtualTransformVector.
        public Vector3 InverseVirtualTransformVector(Vector3 inVector)
        {
            Vector3 newVector, localVector;
            VirtualTransform father = parent;
            if (father)
                localVector = father.InverseVirtualTransformVector(inVector);
            else
                localVector = inVector;

            newVector = Quaternion.RotateVectorByQuat(Quaternion.Inverse(m_LocalRotation), localVector);
            if (m_InternalVirtualTransformType != kNoScaleVirtualTransform)
                newVector.Scale(InverseSafe(m_LocalScale));

            return newVector;
        }

        static float InverseSafe(float f)
        {
            if (Mathf.Abs(f) > Vector3.kEpsilon)
                return 1.0F / f;
            else
                return 0.0F;
        }

        static Vector3 InverseSafe(Vector3 v)
        {
            return new Vector3(InverseSafe(v.x), InverseSafe(v.y), InverseSafe(v.z));
        }

        // VirtualTransforms /position/ from local space to world space.
        public Vector3 VirtualTransformPoint(Vector3 inPoint)
        {
            Vector3 worldPos = localToWorldMatrix.MultiplyPoint(inPoint);
            return worldPos;
        }

        // VirtualTransforms /position/ from world space to local space. The opposite of VirtualTransform.VirtualTransformPoint.
        public Vector3 InverseVirtualTransformPoint(Vector3 inPosition)
        {
            Vector3 newPosition, localPosition;
            if (parent)
                localPosition = parent.InverseVirtualTransformPoint(inPosition);
            else
                localPosition = inPosition;

            localPosition -= m_LocalPosition;
            newPosition = Quaternion.RotateVectorByQuat(Quaternion.Inverse(m_LocalRotation), localPosition);
            if (m_InternalVirtualTransformType != kNoScaleVirtualTransform)
                newPosition.Scale(InverseSafe(m_LocalScale));

            return newPosition;
        }

        // Returns the topmost VirtualTransform in the hierarchy.
        public VirtualTransform root => parent == null ? this : parent.root;

        // The number of children the VirtualTransform has.
        public int childCount => children.Count;

        // Unparents all children.
        public void DetachChildren()
        {
            List<VirtualTransform> tl = new List<VirtualTransform>(children);
            foreach (var t in tl)
            {
                t.SetParent(null);
            }
        }

        // Move itself to the end of the parent's array of children
        public void SetAsFirstSibling()
        {
            SetSiblingIndex(0);
        }

        // Move itself to the beginning of the parent's array of children
        public void SetAsLastSibling()
        {
            SetSiblingIndex(10000);
        }

        public void SetSiblingIndex(int index)
        {
            if (parent)
            {
                children.Remove(this);
                children.Insert(index, this);
            }
        }

        public int GetSiblingIndex()
        {
            if (parent)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] == parent)
                    {
                        return i;
                    }
                }
            }

            return 0;
        }

        // Is this VirtualTransform a child of /parent/?
        public bool IsChildOf(VirtualTransform parent)
        {
            return IsChildOrSameVirtualTransform(this, parent);
        }

        // Get a VirtualTransform child by index
        public VirtualTransform GetChild(int index)
        {
            if (children.Count > index)
            {
                return children[index];
            }
            else
            {
                return null;
            }
        }

        Matrix4x4 GetWorldScale()
        {
            Matrix4x4 invRotation = new Matrix4x4();
            Quaternion.QuaternionToMatrix(Quaternion.Inverse(GetRotation()), ref invRotation);
            Matrix4x4 scaleAndRotation = GetWorldRotationAndScale();
            return invRotation * scaleAndRotation;
        }

        Vector3 GetWorldScaleLossy()
        {
            Matrix4x4 rot = GetWorldScale();
            return new Vector3(rot[0, 0], rot[1, 1], rot[2, 2]);
        }

        bool GetChangedFlag()
        {
            return _hasChanged;
        }

        void SetChangedFlag(bool value)
        {
            _hasChanged = value;
        }

        internal void RemoveFromParent()
        {
            if (!parent)
                return;
            parent.children.Remove(this);
            parent.SetDirty();
            SetDirty();
            //SendParentChanged();
        }

        // implement details.
        internal List<VirtualTransform> children = new List<VirtualTransform>();

        internal void __RemoveChild(VirtualTransform child) => children.Remove(child);

        internal void __AddChild(VirtualTransform child) => children.Add(child);
    }*/
}
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Animation
{
    /// <summary>
    /// The data associated at a point in time in a bezier curve. If the weights of the tangents are
    /// <see cref="DEFAULT_WEIGHT"/>, then the tangent is equivalent to a hermit curve.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct KeyframeData
    {
        /// <summary>
        /// The value of the curve at this keyframe.
        /// </summary>
        public float Value;

        /// <summary>
        /// The incoming tangent for this key.
        /// It affects the slope of the curve from the previous key to this one.
        /// </summary>
        public float InTangent;

        /// <summary>
        /// The outgoing tangent for this key.
        /// It affects the slope of the curve from this key to the next.
        /// </summary>
        public float OutTangent;

        /// <summary>
        /// The weight of the incoming tangent for this key.
        /// It affects the slope of the curve from the previous key to this one.
        /// If the value is <see cref="DEFAULT_WEIGHT"/>, this has the same effect as a <see cref="HermiteKeyframeData"/>.
        /// </summary>
        public float InWeight;

        /// <summary>
        /// The weight of the outgoing tangent for this key.
        /// It affects the slope of the curve from this key to the next.
        /// If the value is <see cref="DEFAULT_WEIGHT"/>, this has the same effect as a <see cref="HermiteKeyframeData"/>.
        /// </summary>
        public float OutWeight;

        /// <summary>
        /// The default weight of the tangents. Used for tangents without a specified weight.
        /// </summary>
        public const float DEFAULT_WEIGHT = 1.0f / 3.0f;
    }

    /// <summary>
    /// Type of an animation curve. The Hermite type is a subset of the Bezier type, but requires less memory.
    /// </summary>
    public enum AnimationCurveType
    {
        Hermite,
        Bezier,
        // TODO: Maybe we should have a constant curve type where there is only a value (like what is needed for int channels / object properties)
    }

    /// <summary>
    /// The blob representation of an animation curve. It contains the keys defined in
    /// the animation curve, split in two separate blob arrays: one for the times, and one for
    /// the keyframes' data.
    /// </summary>
    /// <remarks>
    /// We split the times and data because we only need to go through the times when we
    /// search for the right interval for evaluation. Once the interval is found, we don't
    /// need the times anymore as we interpolate between the values of the two keyframes.
    ///
    /// The order in the arrays is preserved, so that for any index 'i', the data in
    /// `KeyframesData[i]` is the one set at the time `KeyframesTime[i]`.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public struct AnimationCurveNative : IDisposable
    {
        /// <summary>
        /// Type of the animation curve. To set this at creation time, call either <see cref="AllocateBezierKeyframes"/> or
        /// <see cref="AllocateHermiteKeyframes"/>.
        /// </summary>
        public AnimationCurveType Type { get; private set; }

        /// <summary>
        /// The array of times at which data has been set.
        /// </summary>
        public UnsafeList<float> KeyframesTime;

        /// <summary>
        /// The data associated with the times.
        /// </summary>
        public UnsafeList<float> RawKeyframesData;

        static readonly int k_RelativeHermiteKeyframeSize = UnsafeUtility.SizeOf<HermiteKeyframeData>() / UnsafeUtility.SizeOf<float>();
        //static readonly int k_RelativeBezierKeyframeSize = UnsafeUtility.SizeOf<KeyframeData>() / UnsafeUtility.SizeOf<float>();

        public AnimationCurveNative(AnimationCurve managedAnimationCurve, Allocator allocator = Allocator.Persistent)
        {
            var keys = managedAnimationCurve.keys;
            int keyframeCount = keys.Length;
            KeyframesTime = new UnsafeList<float>(keyframeCount, allocator);
            RawKeyframesData = new UnsafeList<float>(keyframeCount * k_RelativeHermiteKeyframeSize, allocator);
            Type = AnimationCurveType.Hermite;
        }

        public void Dispose()
        {
            KeyframesTime.Dispose();
            RawKeyframesData.Dispose();
        }

        /// <summary>
        /// Seth note: This should be the default method corresponding to the default Unity AnimationCurve.
        /// Gets the keyframe at the given index, assuming the curve is a hermit curve. It is the callers responsibility
        /// to check that the type of the curve is hermite (<c>curve.Type == AnimationCurveType.Hermite</c>).
        /// </summary>
        /// <param name="index">The index of the keyframe. Must be between <c>0</c> and <c>curve.KeyframesTime.Length</c></param>
        /// <returns>The keyframe at index <c>index</c>. <b>WARNING: Never dereference this keyframe, only access its members.</b></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe ref KeyframeData GetHermiteKeyframe(int index)
        {
            ValidateKeyframeGet(index, AnimationCurveType.Hermite);
            return ref UnsafeUtility.AsRef<KeyframeData>(((HermiteKeyframeData*)RawKeyframesData.Ptr) + index);
        }

        /// <summary>
        /// Gets the keyframe at the given index, assuming the curve is a bezier curve. It is the callers responsibility
        /// to check that the type of the curve is bezier (<c>curve.Type == AnimationCurveType.Bezier</c>).
        /// </summary>
        /// <param name="index">The index of the keyframe. Must be between <c>0</c> and <c>curve.KeyframesTime.Length</c></param>
        /// <returns>The keyframe at index <c>index</c></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe ref KeyframeData GetBezierKeyframe(int index)
        {
            ValidateKeyframeGet(index, AnimationCurveType.Bezier);
            return ref UnsafeUtility.AsRef<KeyframeData>(((KeyframeData*)RawKeyframesData.Ptr) + index);
        }

        /// <summary>
        /// Gets a keyframe from the curve. If the curve was a hermite curve, the keyframe weights get filled with the default
        /// value <see cref="KeyframeData.DEFAULT_WEIGHT"/>
        /// </summary>
        /// <param name="index">The index of the keyframe. Must be between <c>0</c> and <c>curve.KeyframesTime.Length</c></param>
        /// <returns>The keyframe at index <c>index</c></returns>
        public KeyframeData this[int index]
        {
            get
            {
                if (Type == AnimationCurveType.Bezier)
                    return GetBezierKeyframe(index);

                if (Type == AnimationCurveType.Hermite)
                {
                    ref var keyframe = ref GetHermiteKeyframe(index);
                    return new KeyframeData
                    {
                        Value = keyframe.Value,
                        InTangent = keyframe.InTangent,
                        OutTangent = keyframe.OutTangent,
                        InWeight = KeyframeData.DEFAULT_WEIGHT,
                        OutWeight = KeyframeData.DEFAULT_WEIGHT
                    };
                }

                ThrowInvalidCurveType();
                return default;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void ValidateKeyframeGet(int index, AnimationCurveType expectedType)
        {
            if (Type != expectedType)
                throw new InvalidOperationException($"Tried to get {expectedType} curve data on a curve where the type is {Type}");
            if (index < 0 || index > KeyframesTime.Length)
                throw new IndexOutOfRangeException($"Index {index} is not in the range of the curve keyframes: [0, {KeyframesTime.Length})");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void ThrowInvalidCurveType()
        {
            throw new InvalidOperationException($"Curve has a type that is not valid ({Type}).");
        }

        [GenerateTestsForBurstCompatibility]
        struct HermiteKeyframeData
        {
            public float Value;
            public float InTangent;
            public float OutTangent;
        }
    }

    /// <summary>
    /// Stores the indices of the last two keyframes that were used for evaluation.
    /// Speeds up the performance when evaluating several times within the same interval.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public struct AnimationCurveCache
    {
        /// <summary>
        /// Index of the key on the left-hand side of the interval.
        /// </summary>
        public int LhsIndex;
        /// <summary>
        /// Index of the key on the right-hand side of the interval.
        /// </summary>
        public int RhsIndex;

        /// <summary>
        /// Resets the indices of the cache to the start of the curve.
        /// </summary>
        public void Reset()
        {
            LhsIndex = RhsIndex = 0;
        }
    }
}
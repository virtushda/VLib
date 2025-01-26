using System;
using Sirenix.Utilities;
using Unity.Burst;
using UnityEngine;

namespace VLib.Unsafe.Structures
{
    /// <summary> A wrapper for <see cref="AnimationClip"/> that makes .Equals work off-main-thread. <br/>
    /// Be extremely careful using this. Ya gotta know what ya doin'! <br/>
    /// This should be safe for animation clips as they are not dynamically created and destroyed. </summary>
    public struct UnguardedAnimClip : IEquatable<UnguardedAnimClip>
    {
        public AnimationClip Clip { get; }
        public int InstanceID => ReferenceEquals(Clip, null) ? 0 : Clip.GetHashCode();
        
        public UnguardedAnimClip(AnimationClip clip) => Clip = clip;
        
        // Casts
        public static implicit operator AnimationClip(UnguardedAnimClip obj) => obj.Clip;
        public static implicit operator UnguardedAnimClip(AnimationClip obj) => new(obj);

        [BurstDiscard]
        public bool Equals(UnguardedAnimClip other)
        {
            // Hopefully this isn't too slow, Unity really reallllllly needs to rework this whole paradigm
            var thisNull = this.Clip.SafeIsUnityNull();
            var otherNull = other.Clip.SafeIsUnityNull();
            // Only one is null
            if (thisNull != otherNull)
                return false;
            // Both are null
            if (thisNull)
                return true;
            // Past this point neither is null
            return InstanceID.Equals(other.InstanceID);
        }

        [BurstDiscard]
        public override bool Equals(object obj) => obj is UnguardedAnimClip other && Equals(other);

        [BurstDiscard]
        public override int GetHashCode() => InstanceID;

        public static bool operator ==(UnguardedAnimClip left, UnguardedAnimClip right) => left.Equals(right);
        public static bool operator !=(UnguardedAnimClip left, UnguardedAnimClip right) => !left.Equals(right);
    }
}
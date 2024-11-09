﻿using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Libraries.VLib.Unsafe.Utility
{
    public static unsafe class VUnsafeUtil
    {
        /// <summary> Allows you to return a 'ref' to null, such that TryGetRef patterns can cleanly return null refs when false. </summary>
        public static ref T NullRef<T>() where T : struct => ref UnsafeUtility.AsRef<T>(null);
    }
}
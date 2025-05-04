using System.Threading;
using Unity.Burst;
using UnityEngine;

namespace VLib.Utility
{
    public static class VDebugUtil
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            SharedStaticData.Data = default;
        }
        
        static readonly SharedStatic<VDebugUtilData> SharedStaticData = SharedStatic<VDebugUtilData>.GetOrCreate<VDebugUtilDataID, VDebugUtilData>();
        class VDebugUtilDataID {}

        struct VDebugUtilData
        {
            public int debugColorHash;
        }
        
        public static Color NextRandomColor()
        {
            var hash = Interlocked.Increment(ref SharedStaticData.Data.debugColorHash);
            return VColorUtility.HashToColor((uint)hash);
        }
    }
}
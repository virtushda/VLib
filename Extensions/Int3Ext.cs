using Unity.Mathematics;
using VLib.Structures;

namespace VLib
{
    public static class Int3Ext
    {
        public static Ushort3 ToUshort3Safe(this int3 value, bool logIfClamped = true)
        {
            var result = new Ushort3((ushort)value.x, (ushort)value.y, (ushort)value.z);

            if (logIfClamped && !math.all(result == value))
                UnityEngine.Debug.LogError($"Clamped int3 {value} to ushort3 {result}");

            return result;
        }
        
        public static Short3 ToShort3Safe(this int3 value, bool logIfClamped = true)
        {
            var result = new Short3((short)value.x, (short)value.y, (short)value.z);

            if (logIfClamped && !math.all(result == value))
                UnityEngine.Debug.LogError($"Clamped int3 {value} to short3 {result}");

            return result;
        }
    }
}
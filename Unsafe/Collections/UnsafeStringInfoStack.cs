/*using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib
{
    /// <summary> Allows you to generate an arbitrary collection of output strings. </summary>
    [GenerateTestsForBurstCompatibility]
    public struct UnsafeStringInfoStack
    {
        /// <summary> Is flags in case someone wants to bitmask behaviour based on this. </summary>
        [Flags]
        public enum InfoType : byte
        {
            Unknown = 0,
            Info = 1,

            /// <summary> Lets you dynamically reconstruct an enum value </summary>
            EnumTypeName = 1 << 1,
            StructTypeName = 1 << 2,
            
            Warning = 1 << 3,
            Error = 1 << 4,
            Critical = 1 << 5,
        }
        
        VUnsafeRef<Native> nativeBuffer;
        struct Native
        {
            public InfoType highestInfoType;
            public UnsafeList<ManifestEntry> manifest;
            public UnsafeAppendBuffer buffer;
            
            
        }
        
        struct ManifestEntry
        {
            public InfoType type;
            public FixedStringSize size;
        }
        
        public void RecordEnum<T>(T value, InfoType type = InfoType.Info) where T : Enum
        {
            
            
            RecordEnum(value.ToString(), type);
        }
    }
}*/
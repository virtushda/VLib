using System;
using UnityEngine;

namespace VLib
{
    /// <summary> Version of DateTime that can be serialized properly by Unity. </summary>
    [Serializable]
    public struct DateTimeSerializable : ISerializationCallbackReceiver
    {
        [SerializeField] ulong dateData;

        public DateTime Value { get; private set; }

        // Packing: lower 62 bits are ticks, upper 2 bits are kind
        public DateTimeSerializable(DateTime dateTime)
        {
            dateData = Pack(dateTime);
            Value = dateTime;
        }

        // Packs ticks and Kind into a ulong (62 + 2 bits)
        static ulong Pack(DateTime dt)
        {
            ulong ticks = (ulong)dt.Ticks;
            ulong kind = (ulong)dt.Kind;
            return ticks | (kind << 62);
        }

        // Unpacks ulong into DateTime with correct Kind
        static DateTime Unpack(ulong data)
        {
            long ticks = (long)(data & ((1UL << 62) - 1));
            DateTimeKind kind = (DateTimeKind)(data >> 62);
            return new DateTime(ticks, kind);
        }

        public static implicit operator DateTime(DateTimeSerializable sdt) => sdt.Value;
        public static implicit operator DateTimeSerializable(DateTime dt) => new DateTimeSerializable(dt);

        public void OnBeforeSerialize()
        {
            dateData = Pack(Value);
        }

        public void OnAfterDeserialize()
        {
            Value = Unpack(dateData);
        }
    }
}
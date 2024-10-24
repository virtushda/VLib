using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Unsafe.Structures
{
    /// <summary> This allows you to have a single collection of two different value types. <br/>
    /// This was designed mainly to save memory by having one type be a single element and the other be a collection of elements. <br/> 
    /// The first type must always be larger, this allows both values to be stored in a consistent and testable manner. </summary>
    public struct DualTypedValue<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        const byte Type1 = 0;
        
        byte type;
        T1 value;
        
        public bool IsType1 => type == Type1;
        public bool IsType2 => type != Type1;
        
        public T1 RawValueUnsafe => value;

        public DualTypedValue(T1 value)
        {
            type = 0;
            this.value = value;
            CheckTypeSizesValid();
        }
        
        public DualTypedValue(T2 value)
        {
            type = 1;
            this.value = UnsafeUtility.As<T2, T1>(ref value);
            CheckTypeSizesValid();
        }
        
        /// <summary> Returns true if the value is of type <see cref="T1"/>. Other outed value will be 'default'. </summary>
        public bool GetValue(out T1 value1, out T2 value2)
        {
            if (IsType1)
            {
                value1 = value;
                value2 = default;
                return true;
            }
            else
            {
                value1 = default;
                value2 = UnsafeUtility.As<T1, T2>(ref value);
                return false;
            }
        }

        /// <summary> Be careful not to call this on a value copy. </summary>
        public void SetValue(T1 newValue)
        {
            CheckIsType(false);
            value = newValue;
        }
        
        /// <summary> Be careful not to call this on a value copy. </summary>
        public void SetValue(T2 newValue)
        {
            CheckIsType(true);
            value = UnsafeUtility.As<T2, T1>(ref newValue);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckTypeSizesValid()
        {
            var size1 = UnsafeUtility.SizeOf<T1>();
            var size2 = UnsafeUtility.SizeOf<T2>();
            if (size1 < size2)
                throw new InvalidOperationException($"Type 1 size '{size1}' is less than type 2 size '{size2}'!");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void CheckIsType(bool isType2)
        {
            if (IsType2 != isType2)
            {
                if (isType2)
                    throw new InvalidOperationException("Value is not of type 2!");
                throw new InvalidOperationException("Value is not of type 1!");
            }
        }
    }
}
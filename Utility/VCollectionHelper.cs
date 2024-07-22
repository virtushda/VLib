using System.Runtime.CompilerServices;
using UnityEngine;

namespace VLib
{
    public static class VCollectionHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IndexIsValid(int index, int length)
        {
            if (index >= 0 && index < length)
                return true;
            Debug.LogError($"Invalid index {index}, collection length is {length}");
            return false;

        }
    }
}
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace VLib
{
    public interface IListSlice
    {
        int StartIndex { get; set; }
        int SliceLength { get; set; }
        int Count { get; }
    }

    public static class ListSliceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetEndIndex<T>(this T slice) where T : IListSlice =>
            slice.StartIndex + slice.Count - 1;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetEndIndex<T>(this T slice, int endIndex, int? sourceCount) where T : IListSlice
        {
            if (sourceCount is null or <= 0)
            {
                slice.StartIndex = 0;
                slice.SliceLength = 0;
                return;
            }
            slice.StartIndex = math.clamp(math.min(endIndex, sourceCount.Value - 1) - slice.SliceLength + 1, 0, sourceCount.Value - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSliceSizeNormalized<T>(this T slice, int? sourceCount) where T : IListSlice
        {
            if (sourceCount is null or <= 0)
                return 0f;

            return (float)slice.Count / sourceCount.Value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSliceSizeNormalized<T>(this T slice, float normalizedSize, int? sourceCount) where T : IListSlice
        {
            if (sourceCount is null or <= 0)
            {
                slice.SliceLength = 0;
                return;
            }
            slice.SliceLength = (int)math.ceil(math.saturate(normalizedSize) * sourceCount.Value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSlicePositionNormalized<T>(this T slice, int? sourceCount) where T : IListSlice
        {
            if (sourceCount is null || sourceCount <= 0 || slice.Count <= 0 || sourceCount == slice.Count)
                return 1f;

            return (float)slice.StartIndex / (sourceCount.Value - slice.Count);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSlicePositionNormalized<T>(this T slice, float normalizedPosition, int? sourceCount) where T : IListSlice
        {
            if (sourceCount is null || sourceCount <= 0 || sourceCount == slice.Count)
            {
                slice.StartIndex = 0;
                return;
            }
            slice.StartIndex = (int)(math.saturate(normalizedPosition) * (sourceCount.Value - slice.Count));
        }
    }
}
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using VLib;
using Object = System.Object;

[assembly: RegisterGenericJobType(typeof(Texture2DExt.Tex2DColorFillParallel<byte>))]
[assembly: RegisterGenericJobType(typeof(Texture2DExt.Tex2DColorFillParallel<Color32>))]
[assembly: RegisterGenericJobType(typeof(Texture2DExt.Tex2DColorFillParallel<Color>))]
[assembly: RegisterGenericJobType(typeof(Texture2DExt.Tex2DColorFillParallel<float>))]

namespace VLib
{
    public static class Texture2DExt
    {
        public static RectInt GetPixelRect(this Texture2D texture)
        {
            return new RectInt(0, 0, texture.width, texture.height);
        }

        public static void ColorFillBurst(this Texture2D texture, Color fillColor, bool needMips)
        {
            NativeArray<Color> nativePixelArray = texture.GetRawTextureData<Color>();
        }

        public static void ColorFillBurstDirect<T>(this Texture2D texture, NativeArray<T> nativePixelArray, T fillValue, bool needMips, bool apply = true)
            where T : struct
        {
            var jab = new Tex2DColorFillParallel<T>(nativePixelArray, fillValue);
            var jabHandle = jab.Schedule(nativePixelArray.Length, 512);
            jabHandle.Complete();

            if (apply)
                texture.Apply(needMips, false);
        }

        public static void ColorFillBurst(this Texture2D texture, Color fillColor, bool needMips, int borderPixelWidth)
        {
            NativeArray<Color> nativePixelArray = texture.GetRawTextureData<Color>();

            var jab = new Tex2DColorFillWithBorderParallel()
            {
                pixelArray = nativePixelArray,
                newColor = fillColor,
                stride = texture.width,
                borderWidth = math.max(0, borderPixelWidth)
            };
            var jabHandle = jab.Schedule(nativePixelArray.Length, 512);
            jabHandle.Complete();

            texture.Apply(needMips, false);
        }

        public static int2 Size(this Texture2D tex) => new int2(tex.width, tex.height);

        public static bool HasDimensionOverSize(this Texture2D tex, int2 size) => tex.width > size.x || tex.height > size.y;

        /// <summary> If texture is too big, it will be blit into a smaller texture and the larger texture may be destroyed. </summary>
        /// <param name="inTex">Input Texture</param>
        /// <param name="maxSize">Maximum size of the texture X and Y as an int2.</param>
        /// <param name="maintainAspect">If the aspect ratio of the texture should be maintained, or if it can be squished.</param>
        /// <param name="destroyOld">Default: TRUE -- Destroys the input texture after it's been resized to prevent leaking memory.</param>
        /// <returns>Correct Texture</returns>
        public static Texture2D TryClampSize(this Texture2D inTex, int2 maxSize, bool maintainAspect = true, bool destroyOld = true, GraphicsFormat? overrideFormat = null)
        {
            if (inTex == null || !inTex.HasDimensionOverSize(maxSize))
                return inTex;

            float2 inTexSizeF = inTex.Size(); // 300 x 600
            float2 multiplier = maxSize / inTexSizeF; // 400 x 400 / inTexSizeF = 1.33f x .66f;
            float maxMult = math.cmin(multiplier); // .66f
            
            int2 newSize = (int2)math.min(inTexSizeF, maxSize);
            if (maintainAspect)
                newSize = (int2)(inTexSizeF * maxMult);

            var format = overrideFormat.HasValue ? overrideFormat.Value : inTex.graphicsFormat;

            var renderTexture = new RenderTexture(newSize.x, newSize.y, 0, format, inTex.mipmapCount);
            
            Graphics.Blit(inTex, renderTexture);
            
            if (destroyOld)
                inTex.TryDestroy();
            var newTex = renderTexture.ToTexture2D(true);
            renderTexture.TryDestroy();

            return newTex;
        }

        /*[BurstCompile(CompileSynchronously = true)]
        public struct Tex2DColorFillParallel : IJobParallelFor
        {
            public NativeArray<Color> pixelArray;
            [ReadOnly] public Color newColor;

            public void Execute(int index)
            {
                pixelArray[index] = newColor;
            }
        }*/

        [BurstCompile]
        public struct Tex2DColorFillParallel<T> : IJobParallelFor
            where T : struct
        {
            NativeArray<T> pixelArray;
            T newValue;

            public Tex2DColorFillParallel(NativeArray<T> pixelArray, T newValue)
            {
                this.pixelArray = pixelArray;
                this.newValue = newValue;
            }

            public void Execute(int index)
            {
                pixelArray[index] = newValue;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct Tex2DColorFillWithBorderParallel : IJobParallelFor
        {
            public NativeArray<Color> pixelArray;
            [ReadOnly] public Color newColor;
            [ReadOnly] public int stride;
            [ReadOnly] public int borderWidth;

            public void Execute(int index)
            {
                pixelArray[index] = newColor;
                var x = index % stride;
                var y = index / stride;
                if (x < borderWidth || x > stride - borderWidth)
                    pixelArray[index] = Color.white;
                if (y < borderWidth || y > stride - borderWidth)
                    pixelArray[index] = Color.white;
            }
        }
    }
}
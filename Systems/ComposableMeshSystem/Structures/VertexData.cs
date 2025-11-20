using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct VertexData
{
    public float3 position;
    public float3 normal;
    public half4 tangent;
    public Color32 color;
    public half2 uv;
}
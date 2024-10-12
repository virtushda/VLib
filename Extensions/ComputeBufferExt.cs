using System;
using UnityEngine;

namespace VLib
{
    public static class ComputeBufferExt
    {
        public static void ReleaseWithDisguiseFromGC(this ComputeBuffer buffer)
        {
            if (buffer == null)
                return;
            
            GC.KeepAlive(buffer);
            buffer.Release();
        }
    }
}
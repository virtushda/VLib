// Code converted from the open-source Embree library, which is licensed under the Apache License, Version 2.0.
// https://github.com/RenderKit/embree

/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace VLib.Licensed
{
    public static class FromEmbree
    {
        // Converted from C++ to C# from the open-source Embree library.
        public static float3 ClosestPointTriangle(in float3 p, in float3 a, in float3 b, in float3 c)
        {
            float3 ab = b - a;
            float3 ac = c - a;
            
            // Check if P in vertex region outside A
            float3 ap = p - a;
            float d1 = dot(ab, ap);
            float d2 = dot(ac, ap);
            if (max(d1, d2) <= 0) 
                return a;

            // Check if P in vertex region outside B
            float3 bp = p - b;
            float d3 = dot(ab, bp);
            float d4 = dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) 
                return b;

            // Check if P in vertex region outside C
            float3 cp = p - c;
            float d5 = dot(ab, cp);
            float d6 = dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) 
                return c;

            float v = default;
            // Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                v = d1 / (d1 - d3);
                return a + v * ab;
            }
    
            // Check if P in edge region of BC, if so return projection of P onto BC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                v = d2 / (d2 - d6);
                return a + v * ac;
            }
    
            // Check if P in edge region of AC, if so return projection of P onto AC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + v * (c - b);
            }

            // P inside face region.
            float denom = 1 / (va + vb + vc);
            v = vb * denom;
            float w = vc * denom;
            return a + v * ab + w * ac;
        }
    }
}
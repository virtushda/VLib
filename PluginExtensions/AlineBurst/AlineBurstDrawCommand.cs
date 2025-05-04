using Drawing;
using Unity.Mathematics;
using UnityEngine;
using VLib.Aline.Drawers;
using VLib.Unsafe.Structures;

namespace VLib.Aline
{
    /// <summary> Contains generalized data in a uniform storage format for making aline draw commands from burst. </summary>
    internal struct AlineBurstDrawCommand
    {
        public AlineDrawShape shape;
        /// <summary> A portion of memory to store drawing data, not an actual matrix. Usage defined by <see cref="shape"/> </summary>
        public UnmanagedData64 drawData;
        public Color color;
        public float duration;
        public byte lineThickness;
        
        public AlineBurstDrawCommand(AlineDrawShape shape, in UnmanagedData64 drawData, in Color color, float duration, byte lineThickness)
        {
            this.shape = shape;
            this.drawData = drawData;
            this.color = color;
            this.duration = duration;
            this.lineThickness = lineThickness;
        }

        public void Draw(ref CommandBuilder draw)
        {
            switch (shape)
            {
                case AlineDrawShape.Ray:
                    AlineBurstCommands.Rays.DrawRay(ref draw, ref this);
                    break;
                case AlineDrawShape.Sphere:
                    AlineBurstCommands.Spheres.DrawWireSphere(ref draw, ref this);
                    break;
                case AlineDrawShape.CubeSolid:
                    AlineBurstCommands.Boxes.DrawWireCube(ref draw, ref this);
                    break;
                case AlineDrawShape.Capsule:
                    AlineBurstCommands.Capsules.DrawWireCapsule(ref draw, ref this);
                    break;
            }
        }

        /// <summary> Sets up color, line width and duration for the draw command </summary>
        public void PushParameters(ref CommandBuilder draw)
        {
            if (color.a > .01f)
                draw.PushColor(color);
            if (lineThickness > 0)
                draw.PushLineWidth(lineThickness);
            if (duration > 0)
                draw.PushDuration(duration);
        }

        /// <summary> Pops color, line width and duration for the draw command </summary>
        public void PopParameters(ref CommandBuilder draw)
        {
            if (duration > 0)
                draw.PopDuration();
            if (lineThickness > 0)
                draw.PopLineWidth();
            if (color.a > .01f)
                draw.PopColor();
        }
    }
}
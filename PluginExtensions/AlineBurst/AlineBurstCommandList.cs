using Drawing;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VLib.Aline
{
    internal struct AlineBurstCommandList : IAllocating
    {
        UnsafeList<AlineBurstDrawCommand> drawCommands;
        BurstSpinLock spinLock;

        public bool IsCreated => drawCommands.IsCreated;

        public AlineBurstCommandList(int capacity)
        {
            drawCommands = new UnsafeList<AlineBurstDrawCommand>(capacity, Allocator.Persistent);
            spinLock = new BurstSpinLock(Allocator.Persistent);
        }

        public void Dispose()
        {
            using (spinLock.Scoped())
            {
                drawCommands.Dispose();
            }
            spinLock.DisposeRefToDefault();
        }
        
        public void Add(in AlineBurstDrawCommand drawCommand)
        {
            using (spinLock.Scoped())
            {
                drawCommands.Add(drawCommand);
            }
        }
        
        public void DrawAll(ref CommandBuilder draw)
        {
            using (spinLock.Scoped())
            {
                for (int i = 0; i < drawCommands.Length; i++)
                    drawCommands.ElementAt(i).Draw(ref draw);
                drawCommands.Clear();
            }
        }
    }
}
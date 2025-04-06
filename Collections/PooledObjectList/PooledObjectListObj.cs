using System.Buffers;
using System.Threading;

namespace VLib.Collections
{
    internal sealed class PooledObjectListObj
    {
        internal long id;
        internal object[] objects;
        internal int count;
        
        public object[] Objects => objects;
        public int Count => count;

        public void Initialize(long id, object[] objects)
        {
            this.id = id;
            this.objects = objects;
            count = 0;
        }

        public bool TryDispose()
        {
            // Exchange the value atomically to guard against double-disposal from two or more threads, shouldn't happen but this ensures no race condition
            var originalValue = Interlocked.Exchange(ref id, 0);
            if (originalValue == 0)
                return false;
            
            ArrayPool<object>.Shared.Return(objects, true);
            objects = null;
            count = 0;
            return true;
        }
    }
}
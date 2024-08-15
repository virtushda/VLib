using System.Buffers;

namespace VLib.Libraries.VLib.Collections
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

        public void Dispose()
        {
            id = 0;
            ArrayPool<object>.Shared.Return(objects, true);
            objects = null;
            count = 0;
        }
    }
}
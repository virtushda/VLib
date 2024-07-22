using System.Collections.Generic;

namespace VLib
{
    public class SimpleListPool<T>
    {
        int initCapacity = 8;
        List<List<T>> pooledItems;

        public SimpleListPool(int poolCapacity, int listsInitCapacity)
        {
            pooledItems = new List<List<T>>(poolCapacity);
            this.initCapacity = listsInitCapacity;
        }

        public List<T> Fetch()
        {
            if (pooledItems.Count >= 1)
            {
                int index = pooledItems.Count - 1;
                var pooledItem = pooledItems[index];
                pooledItems.RemoveAt(index);
                return pooledItem;
            }
            else
            {
                return new List<T>(initCapacity);
            }
        }

        public void Repool(List<T> objToPool)
        {
            objToPool.Clear();
            pooledItems.Add(objToPool);
        }
    }
}
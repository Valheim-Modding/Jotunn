using System;
using System.Collections.Generic;

namespace JotunnLib.Utils
{
    public class WeightedList<T, ItemType> where T : WeightedItem<ItemType>
    {
        public readonly List<T> List;

        public WeightedList()
        {
            List = new List<T>();
        }

        public void Add(T item) => List.Add(item);

        public bool Remove(T item) => List.Remove(item);

        public ItemType GetRandomItem(List<T> list = null)
        {
            if (list == null) list = List;

            var sumOfWeight = 0f;
            foreach (var item in list)
            {
                sumOfWeight += item.Weight;
            }

            var randomNumber = UnityEngine.Random.Range(0, sumOfWeight - 1);

            foreach (var item in list)
            {
                if (randomNumber < item.Weight)
                {
                    return item.Item;
                }

                randomNumber -= item.Weight;
            }

            throw new Exception("No item in weighted list");
        }
    }

    public class WeightedItem<T>
    {
        public T Item { get; private set; }

        public virtual float Weight { get; set; }

        public WeightedItem(T item, float weight = 0)
        {
            Item = item;

            if (weight != 0)
            {
                Weight = weight;
            }
        }
    }
}

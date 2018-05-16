using System;
using System.Runtime.CompilerServices;

namespace TestParser.Sys
{
    public static class HybridSort<T> where T : IComparable<T>, IEquatable<T>
    {
        private const int _maxCapacityForShell = 4000;

        /// <summary>
        /// Последовательность Марцина Циура A102549. Применима для сортировки Шелла массивов емкостью до 4000 элементов
        /// </summary>
        private static readonly int[] _a102549 = new[] { 1, 4, 10, 23, 57, 132, 301, 701, 1750 };

        public static void Sort(IContainerItem<T>[] array)
        {
            Guard.ArgumentNotNull(array, "array");

            int length = array.Length;
            if (length <= 1)
            {
                return;
            }

            if (length <= _maxCapacityForShell)
            {
                ShellSort(ref array, 0, length);
            }
            else
            {
                const int chunkSize = 512; //число должно быть меньше чем _maxCapacityForShell

                for (int i = 0; i < length; i += chunkSize)
                {
                    ShellSort(ref array, i, Math.Min(chunkSize, length - i));
                }

                var buffer = new IContainerItem<T>[length];
                for (int size = 1; size < length; size = size + size)
                {
                    for (int index = 0; index < length - size; index += size + size)
                    {
                        Merge(ref buffer, ref array, index, index + size - 1, Math.Min(index + size + size - 1, length - 1));
                    }
                }
                buffer = null;
            }
        }

        private static void ShellSort(ref IContainerItem<T>[] array, int startIndex, int total)
        {
            //Выбор значения index
            int index = 0;
            for (int i = _a102549.Length - 1; i > 0; i--)
            {
                if (_a102549[i] < (total / 2))
                {
                    index = i;
                    break;
                }
            }

            int magicNumber = 0;
            for (int k = index; k >= 0; k--)
            {
                magicNumber = _a102549[k];
                for (int i = magicNumber; i < total; i++)
                {
                    for (int j = i; j >= magicNumber; j -= magicNumber)
                    {
                        if (Compare(array[j + startIndex].Value, array[j + startIndex - magicNumber].Value))
                        {
                            Swap(ref array, j + startIndex, j + startIndex - magicNumber);
                        }
                    }
                }
            }
        }

        private static void Merge(ref IContainerItem<T>[] buffer, ref IContainerItem<T>[] array, int leftIndex, int medianIndex, int rightIndex)
        {
            int i = leftIndex, j = medianIndex + 1;
            Array.Copy(array, leftIndex, buffer, leftIndex, rightIndex + 1 - leftIndex);

            for (int k = leftIndex; k <= rightIndex; k++)
            {
                if (i > medianIndex)
                {
                    array[k] = buffer[j++];
                }
                else if (j > rightIndex)
                {
                    array[k] = buffer[i++];
                }
                else if (Compare(buffer[j].Value, buffer[i].Value))
                {
                    array[k] = buffer[j++];
                }
                else
                {
                    array[k] = buffer[i++];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref IContainerItem<T>[] array, int i, int j)
        {
            IContainerItem<T> tmp = array[i];

            array[i] = array[j];
            array[j] = tmp;

            tmp = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Compare(T first, T second)
        {
            return !Equals(first, default(T)) ?
                first.CompareTo(second) < 0 :
                false;
        }
    }
}

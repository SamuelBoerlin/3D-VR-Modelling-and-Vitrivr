using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Sculpting
{
    public struct NativeArray3D<T> : IDisposable, IEnumerable<T>, IEquatable<NativeArray<T>>, IEnumerable where T : struct
    {
        private NativeArray<T> data;

        private readonly int xSize, ySize, zSize, stride;

        public NativeArray3D(int xSize, int ySize, int zSize, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            data = new NativeArray<T>(xSize * ySize * zSize, allocator, options);
            this.xSize = xSize;
            this.ySize = ySize;
            this.zSize = zSize;
            stride = xSize * ySize;
        }

        public T this[int x, int y, int z]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return data[x + y * xSize + z * stride];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                data[x + y * xSize + z * stride] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Length(int dim)
        {
            switch (dim)
            {
                case 0: return xSize;
                case 1: return ySize;
                case 2: return zSize;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void CopyTo(T[] array)
        {
            data.CopyTo(array);
        }

        public void CopyTo(NativeArray<T> array)
        {
            data.CopyTo(array);
        }

        public void CopyTo(NativeArray3D<T> array)
        {
            data.CopyTo(array.data);
        }

        public void CopyFrom(T[] array)
        {
            data.CopyFrom(array);
        }

        public void CopyFrom(NativeArray<T> array)
        {
            data.CopyFrom(array);
        }

        public void CopyFrom(NativeArray3D<T> array)
        {
            data.CopyFrom(array.data);
        }

        public void Dispose()
        {
            data.Dispose();
        }

        public bool Equals(NativeArray<T> other)
        {
            return data.Equals(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }
}
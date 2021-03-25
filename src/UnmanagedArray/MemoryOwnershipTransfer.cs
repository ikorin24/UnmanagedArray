/*
 MIT License

Copyright (c) 2021 ikorin24

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

#nullable enable

namespace UnmanageUtility
{
    /// <summary>Provides methods for memory ownership transfer.</summary>
    public static unsafe class MemoryOwnershipTransfer
    {
        /// <summary>Create <see cref="UnmanagedArray{T}"/> from <see cref="UnmanagedList{T}"/> by passing its memory ownership.</summary>
        /// <remarks><paramref name="list"/> gets disposed.</remarks>
        /// <typeparam name="T">type of items</typeparam>
        /// <param name="list">source list</param>
        /// <returns>created <see cref="UnmanagedArray{T}"/> instance.</returns>
        public static UnmanagedArray<T> ToUnmanagedArray<T>(UnmanagedList<T> list) where T : unmanaged
        {
            list.TransferInnerMemoryOwnership(out var ptr, out _, out var length);
            return UnmanagedArray<T>.DirectCreateWithoutCopy((T*)ptr, length);
        }

        /// <summary>Create <see cref="UnmanagedArray{T}"/> from <see cref="UnmanagedList{T}"/> by passing its memory ownership.</summary>
        /// <remarks><paramref name="list"/> gets disposed.</remarks>
        /// <typeparam name="T">type of items</typeparam>
        /// <param name="list">source list</param>
        /// <param name="transferLength">length of transferred memory</param>
        /// <returns>created <see cref="UnmanagedArray{T}"/> instance.</returns>
        public static UnmanagedArray<T> ToUnmanagedArray<T>(UnmanagedList<T> list, MemoryTransferLength transferLength) where T : unmanaged
        {
            list.TransferInnerMemoryOwnership(out var ptr, out var capacity, out var length);
            var arrayLength = transferLength switch
            {
                MemoryTransferLength.ItemCount => length,
                MemoryTransferLength.FullCapacity => capacity,
                _ => length,
            };
            return UnmanagedArray<T>.DirectCreateWithoutCopy((T*)ptr, arrayLength);
        }
    }

    /// <summary>Length of transferred Memory</summary>
    public enum MemoryTransferLength
    {
        /// <summary>transfer the memory for the number of items.</summary>
        ItemCount,
        /// <summary>transfer the memory for the full capacity it has.</summary>
        FullCapacity,
    }
}

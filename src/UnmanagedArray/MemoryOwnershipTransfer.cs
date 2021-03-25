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

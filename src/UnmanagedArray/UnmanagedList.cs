/*
 MIT License

Copyright (c) 2020 ikorin24

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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace UnmanageUtility
{
    /// <summary>
    /// List class which is allocated in unmanaged memory.<para/>
    /// Only for unmanaged types. (e.g. int, float, recursive-unmanaged struct, and so on.)
    /// </summary>
    /// <typeparam name="T">type of list</typeparam>
    [DebuggerTypeProxy(typeof(UnmanagedListDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnmanagedList<{typeof(T).Name}>[{_length}]")]
    public sealed unsafe class UnmanagedList<T> : IList<T>, IList, IReadOnlyList<T>, IDisposable
        where T : unmanaged
    {
        private const int DefaultCapacity = 4;  // DO NOT change into 0

        private RawArray _array;
        private int _length;

        /// <summary>Get count of elements in the list.</summary>
        public int Count => _length;

        /// <summary>Get capacity of current inner array.</summary>
        public int Capacity => _array.Length;

        /// <summary>Get pointer of innner array. (Returns <see cref="IntPtr.Zero"/> if <see cref="Count"/> == 0).</summary>
        public IntPtr Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (IntPtr)Unsafe.AsPointer(ref GetReference());
        }

        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> instance with default capacity.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedList()
        {
            _array = new RawArray(DefaultCapacity);
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> instance with specified capacity.</summary>
        /// <param name="capacity">internal capacity of <see cref="UnmanagedList{T}"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedList(int capacity)
        {
            if(capacity < 0) { ThrowHelper.ArgumentOutOfRange(nameof(capacity)); }
            if(capacity != 0) {
                _array = new RawArray(capacity);
            }
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> instance initialized by specified array.</summary>
        /// <param name="items">items copied to <see cref="UnmanagedList{T}"/></param>
        public UnmanagedList(T[] items)
        {
            if(items is null) { ThrowHelper.ArgumentNull(nameof(items)); }
            if(items!.Length == 0) { return; }
            _array = new RawArray(items!.Length);
            items.CopyTo(_array.AsSpan());
            _length = items.Length;
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> instance initialized by specified <see cref="ReadOnlySpan{T}"/>.</summary>
        /// <param name="items">items copied to <see cref="UnmanagedList{T}"/></param>
        public UnmanagedList(ReadOnlySpan<T> items)
        {
            if(items.IsEmpty) { return; }
            _array = new RawArray(items.Length);
            items.CopyTo(_array.AsSpan());
            _length = items.Length;
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> instance initialized by specified <see cref="IEnumerable{T}"/>.</summary>
        /// <param name="items">items copied to <see cref="UnmanagedList{T}"/></param>
        public UnmanagedList(IEnumerable<T> items) : this(capacity: 0)
        {
            AddRange(items);
        }

        /// <summary>Finalize <see cref="UnmanagedList{T}"/> instance</summary>
        ~UnmanagedList() => DisposePrivate();

        /// <summary>Get or set item of type <typeparamref name="T"/> with specified index.</summary>
        /// <param name="index">index to get or set</param>
        /// <returns>item of specified index.</returns>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetReference(index);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => GetReference(index) = value;
        }

        /// <summary>Get reference to head item (Returns ref to null if empty)</summary>
        /// <returns>reference to head item</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetReference()
        {
            return ref Unsafe.AsRef<T>(_length == 0 ? (T*)null : (T*)_array.Ptr);
        }

        /// <summary>Get reference to item of specified index</summary>
        /// <param name="index">index to get reference</param>
        /// <returns>reference to item of specified index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetReference(int index)
        {
            if((uint)index >= (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(index)); }
            return ref _array[index];
        }

        /// <summary>Add an item of type <typeparamref name="T"/></summary>
        /// <param name="item">an item to add to list</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if(_array.Length > _length) {
                _array[_length++] = item;
            }
            else {
                // Here is uncommon path.
                // Don't inline resizing method, to minimize size of IL in common path.
                AddWithResize(item);
            }
        }

        /// <summary>Insert a specified item to specified index</summary>
        /// <param name="index">index of list to insert item</param>
        /// <param name="item">item to insert</param>
        public void Insert(int index, T item)
        {
            // Inserting to end of the list is legal.
            if((uint)index > (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(index)); }
            if(_array.Length == _length) {
                EnsureCapacity(_array.Length + 1);
            }

            if(index < _length) {
                var moveSpan = _array.AsSpan(index, _length - index);
                var dest = _array.AsSpan(index + 1, moveSpan.Length);
                moveSpan.CopyTo(dest);
            }
            _array[index] = item;
            _length++;
        }

        /// <summary>Add items of type <typeparamref name="T"/></summary>
        /// <param name="items">items to add to list</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(T[] items)
        {
            if(items is null) { ThrowHelper.ArgumentNull(nameof(items)); }
            AddRange(items.AsSpan());
        }

        /// <summary>Add items of type <typeparamref name="T"/></summary>
        /// <param name="items">items to add to list</param>
        public void AddRange(ReadOnlySpan<T> items)
        {
            if(items.IsEmpty) { return; }

            // Check whether items are part of self.
            if(SpanContainsMemory(_array.AsSpan(), ref MemoryMarshal.GetReference(items))) {

                // Ensure capacity without disposing old array
                // because it contains added items.
                if(EnsureCapacityWithoutDisposingOld(_length + items.Length, out var old)) {

                    // Dispose old array after copying.
                    using(old) {
                        items.CopyTo(_array.AsSpan(_length));
                    }
                }
                else {
                    // In the case that no swapping inner array happend when ensure capacity.
                    Debug.Assert(old.Length == 0 && old.Ptr == default);
                    items.CopyTo(_array.AsSpan(_length));
                }
            }
            else {
                EnsureCapacity(_length + items.Length);
                items.CopyTo(_array.AsSpan(_length));
            }
            _length += items.Length;
        }

        /// <summary>Add items of type <typeparamref name="T"/></summary>
        /// <param name="items">items to add to list</param>
        public void AddRange(IEnumerable<T> items) => InsertRange(_length, items);

        /// <summary>Insert items to specified index in the list</summary>
        /// <param name="index">index to insert</param>
        /// <param name="items">items to insert</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertRange(int index, T[] items)
        {
            if(items is null) { ThrowHelper.ArgumentNull(nameof(items)); }
            InsertRange(index, items.AsSpan());
        }

        /// <summary>Insert items to specified index in the list</summary>
        /// <param name="index">index to insert</param>
        /// <param name="items">items to insert</param>
        public void InsertRange(int index, ReadOnlySpan<T> items)
        {
            // Inserting to end of the list is legal.
            if((uint)index > (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(index)); }

            if(items.IsEmpty) { return; }

            ref var itemsHead = ref MemoryMarshal.GetReference(items);
            // Check whether items are part of self.
            if(SpanContainsMemory(_array.AsSpan(), ref itemsHead)) {

                // Ensure capacity without disposing old array
                // because it contains added items.
                if(EnsureCapacityWithoutCopy(_length + items.Length, out var old)) {
                    // `items` are in `old`.
                    using(old) {
                        var size1 = index * sizeof(T);
                        var size2 = (_length - index) * sizeof(T);
                        Buffer.MemoryCopy((void*)old.Ptr,
                                          (void*)_array.Ptr,
                                          size1,
                                          size1);
                        Buffer.MemoryCopy((T*)old.Ptr + index,
                                          (T*)_array.Ptr + index + items.Length,
                                          size2,
                                          size2);
                        items.CopyTo(_array.AsSpan(index));
                    }
                }
                else {
                    Debug.Assert(old.Length == 0 && old.Ptr == default);
                    // Capacity was not changed, so `items` are in `_array`

                    //                             index (inserting point)
                    // ------------------------------↓-------------------------------------------------
                    // | _array[0] | _array[1] | ... | _array[index] | ... | _array[_array.Length - 1] |
                    // |          firstHalf          |                   secondHalf                    |
                    // ------------------------------+--------------------------------------------------
                    //                               |
                    // [case 1]                      |
                    //             | <-- items --> | |
                    // [case 2]                      |
                    //                         | <----  items  ----> |
                    // [case 3]                      |
                    //                               | <----  items  ----> |

                    var firstHalf = _array.AsSpan(0, index);
                    var secondHalf = _array.AsSpan(index, _length - index);
                    ref var itemsTail = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(items), (IntPtr)((items.Length - 1) * sizeof(T)));

                    var isItemsHeadInFirst = SpanContainsMemory(firstHalf, ref itemsHead);
                    var isItemsTailInFirst = SpanContainsMemory(firstHalf, ref itemsTail);
                    if(isItemsHeadInFirst && isItemsTailInFirst) {
                        // case 1
                        secondHalf.CopyTo(_array.AsSpan(index + items.Length));
                        items.CopyTo(_array.AsSpan(index, items.Length));
                    }
                    else if(!isItemsHeadInFirst && !isItemsTailInFirst) {
                        // case 3
                        var offsetFromInsertion = Unsafe.ByteOffset(ref MemoryMarshal.GetReference(secondHalf), ref itemsHead);
                        var secondHalfAfterMove = _array.AsSpan(index + items.Length, secondHalf.Length);
                        secondHalf.CopyTo(secondHalfAfterMove);
                        ref var itemsHeadAfterMove = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(secondHalfAfterMove), offsetFromInsertion);

                        // `items` are on unmanaged memory in this path,
                        // so it is no problem to create Span<T> from void* pointer.
                        // (in the runtime of slow span like net48 or netcoreapp2.0)
                        var itemsAfterMove = new Span<T>(Unsafe.AsPointer(ref itemsHeadAfterMove), items.Length);
                        itemsAfterMove.CopyTo(_array.AsSpan(index, items.Length));
                    }
                    else {
                        // case 2
                        Debug.Assert(isItemsHeadInFirst != isItemsTailInFirst);

                        var itemsByteLenInFirst = Unsafe.ByteOffset(ref itemsHead, ref MemoryMarshal.GetReference(secondHalf));
                        var secondHalfAfterMove = _array.AsSpan(index + items.Length, secondHalf.Length);
                        secondHalf.CopyTo(secondHalfAfterMove);
                        var itemsPartInFirst = MemoryMarshal.Cast<T, byte>(items)
                                                .Slice(0, itemsByteLenInFirst.ToInt32());
                        var insertionSpan = MemoryMarshal.Cast<T, byte>(_array.AsSpan(index, items.Length));
                        itemsPartInFirst.CopyTo(insertionSpan);
                        var itemsPartInSecondAfterMove = MemoryMarshal.Cast<T, byte>(secondHalfAfterMove)
                                                            .Slice(0, items.Length * sizeof(T) - itemsByteLenInFirst.ToInt32());
                        itemsPartInSecondAfterMove.CopyTo(insertionSpan.Slice(itemsByteLenInFirst.ToInt32()));
                    }
                }
            }
            else {
                EnsureCapacity(_length + items.Length);
                if(index < _length) {
                    var moveSpan = _array.AsSpan(index, _length - index);
                    var dest = _array.AsSpan(index + items.Length, moveSpan.Length);
                    moveSpan.CopyTo(dest);
                }
                items.CopyTo(_array.AsSpan(index));
            }
            _length += items.Length;
        }

        /// <summary>Insert items to specified index in the list</summary>
        /// <param name="index">index to insert</param>
        /// <param name="items">items to insert</param>
        public void InsertRange(int index, IEnumerable<T> items)
        {
            // Inserting to end of the list is legal.
            if((uint)index > (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(index)); }
            if(items is null) { ThrowHelper.ArgumentNull(nameof(items)); }

            if(items is UnmanagedList<T> ul) {
                InsertRange(index, ul.AsSpan());
            }
            else if(items is T[] a) {
                InsertRange(index, a.AsSpan());
            }
            else if(items is UnmanagedArray<T> ua) {
                InsertRange(index, ua.AsSpan());
            }
            // For future version in .NET 5
            //else if(items is List<T> l) {
            //    InsertRange(index, CollectionsMarshal.AsSpan(l));
            //}
            else if(items is ICollection<T> c) {
                // ICollection<T>.CopyTo needs T[] argument.
                // Copy to a buffer array from ArrayPool<T>.Shared and re-copy to this._array.
                // This is faster than each-item-iterating insertion.

                // items must not be `this` because EnsureCapacity may free memory of items.
                Debug.Assert(items != this);
                var count = c.Count;
                if(count > 0) {
                    EnsureCapacity(_length + count);
                    if(index < _length) {
                        var moveSpan = _array.AsSpan(index, _length - index);
                        var dest = _array.AsSpan(index + count, moveSpan.Length);
                        moveSpan.CopyTo(dest);
                    }
                    var buf = ArrayPool<T>.Shared.Rent(count);
                    try {
                        c.CopyTo(buf, 0);
                        buf.AsSpan(0, count).CopyTo(_array.AsSpan(index));
                    }
                    finally {
                        ArrayPool<T>.Shared.Return(buf);
                    }
                    _length += count;
                }
            }
            else {
                foreach(var item in items!) {
                    Insert(index++, item);
                }
            }
        }

        // No inlining because this is uncommon path.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            int length = _length;
            EnsureCapacity(length + 1);
            _length = length + 1;
            _array[length] = item;
        }

        private void EnsureCapacity(int min)
        {
            if(EnsureCapacityWithoutDisposingOld(min, out var old)) {
                old.Dispose();
            }
        }

        private bool EnsureCapacityWithoutDisposingOld(int min, out RawArray old)
        {
            if(EnsureCapacityWithoutCopy(min, out old)) {
                if(_length > 0) {
                    Buffer.MemoryCopy((void*)old.Ptr, (void*)_array.Ptr, _array.GetSizeInBytes(), _length * sizeof(T));
                }
                return true;
            }
            else {
                return false;
            }
        }

        private bool EnsureCapacityWithoutCopy(int min, out RawArray old)
        {
            if(_array.Length < min) {
                int newCapacity = _array.Length;
                do {
                    // throw exception if newCapacity is overflow
                    checked {
                        newCapacity = newCapacity == 0 ? DefaultCapacity : newCapacity * 2;
                    }
                } while(newCapacity < min);

                var newArray = new RawArray(newCapacity);
                old = _array;
                _array = newArray;
                return true;
            }
            else {
                old = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SpanContainsMemory(Span<T> span, ref T target)
        {
            // [memory space]
            // ← small address                      big address →
            //       ... |  head  | ... |  tail   | ...
            //
            //       ... | ////////////////////// | ...
            //       ... |      memory range      | ...
            //       ... | ////////////////////// | ...
            //
            //       ... | !LessThan(ref target, ref head)
            // !GreaterThan(ref target, ref tail) | ...

            return span.Length > 0 &&
                   !Unsafe.IsAddressLessThan(ref target, ref MemoryMarshal.GetReference(span)) &&
                   !Unsafe.IsAddressGreaterThan(ref target, ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(span), (IntPtr)((span.Length - 1) * sizeof(T))));
        }

        /// <summary>
        /// Dispose this instance and release unmanaged memory.<para/>
        /// If already disposed, do nothing.<para/>
        /// </summary>
        public void Dispose()
        {
            DisposePrivate();
            GC.SuppressFinalize(this);
        }

        /// <summary>Get inner memory as <see cref="Span{T}"/>.</summary>
        /// <returns>inner memory as <see cref="Span{T}"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return new Span<T>((T*)_array.Ptr, _length);
        }

        /// <summary>Get inner memory as <see cref="Span{T}"/> with specified start index.</summary>
        /// <param name="start">start index of inner memory</param>
        /// <returns>inner memory as <see cref="Span{T}"/> with specified start index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start)
        {
            if((uint)start >= (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(start)); }
            return _array.AsSpan(start, _length - start);
        }

        /// <summary>Get inner memory as <see cref="Span{T}"/> with specified start index and specified length.</summary>
        /// <param name="start">start index of inner memory</param>
        /// <param name="length">length of inner memory</param>
        /// <returns>inner memory as <see cref="Span{T}"/> with specified start index and specified length.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length)
        {
            if((uint)start >= (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(start)); }
            if((uint)length > (uint)(_length - start)) { ThrowHelper.ArgumentOutOfRange(nameof(length)); }

            return _array.AsSpan(start, length);
        }

        private void DisposePrivate()
        {
            if(_array.Ptr != IntPtr.Zero) {
                _array.Dispose();
                _array = default;
                _length = 0;
            }
        }


        /// <summary>Get index of specified item. Returns -1 if not contains <paramref name="item"/>.</summary>
        /// <param name="item">item to get index</param>
        /// <returns>index in the list, or -1 if not contains <paramref name="item"/></returns>
        public int IndexOf(T item)
        {
            for(int i = 0; i < _length; i++) {
                if(EqualityComparer<T>.Default.Equals(_array[i], item)) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Remove item of specified index</summary>
        /// <param name="index">index to remove from list</param>
        public void RemoveAt(int index)
        {
            if((uint)index >= (uint)_length) { ThrowHelper.ArgumentOutOfRange(nameof(index)); }
            _length--;
            if(index < _length) {
                _array.AsSpan(index + 1).CopyTo(_array.AsSpan(index));
            }
        }

        /// <summary>Clear items in the list.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _length = 0;
        }

        /// <summary>Get whether specified item is in the list.</summary>
        /// <param name="item">target item to check</param>
        /// <returns>true if contains, false if not contain</returns>
        public bool Contains(T item)
        {
            for(int i = 0; i < _length; i++) {
                if(EqualityComparer<T>.Default.Equals(_array[i], item)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Copy items to specified array of specified start index.</summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if(_array.Length == 0) { return; }
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        /// <summary>Remove specified item from list. Returns true if item is removed, false if not contains item in the list.</summary>
        /// <param name="item">an item to remove from list</param>
        /// <returns>true if item is removed, false if not contains item in the list</returns>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if(index >= 0) {
                RemoveAt(index);
                return true;
            }
            else {
                return false;
            }
        }

        /// <summary>Get enumerator of the list</summary>
        /// <returns>enumerator of the list</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new EnumeratorClass(this);

        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(this);

        int IList.Add(object value)
        {
            Add((T)value);
            return _length;
        }

        bool IList.Contains(object value) => Contains((T)value);

        int IList.IndexOf(object value) => IndexOf((T)value);

        void IList.Insert(int index, object value) => Insert(index, (T)value);

        void IList.Remove(object value) => Remove((T)value);

        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);



        /// <summary>Enumerator struct of <see cref="UnmanagedList{T}"/></summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly UnmanagedList<T> _list;
            private T _current;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(UnmanagedList<T> list)
            {
                _list = list;
                _index = 0;
                _current = default;
            }

            /// <summary>Get current item</summary>
            public T Current => _current;

            object IEnumerator.Current => _current;

            /// <summary>Dispose this enumerator (but do nothing)</summary>
            public void Dispose() { }   // nop

            /// <summary>Move to next item</summary>
            /// <returns>true if continue, false to end</returns>
            public bool MoveNext()
            {
                if(_index < _list._length) {
                    _current = _list[_index];
                    _index++;
                    return true;
                }
                else {
                    return false;
                }
            }

            /// <summary>Reset enumerator</summary>
            public void Reset()
            {
                _index = 0;
                _current = default;
            }
        }

        /// <summary>Enumerator class of <see cref="UnmanagedList{T}"/></summary>
        public class EnumeratorClass : IEnumerator<T>
        {
            private readonly UnmanagedList<T> _list;
            private T _current;
            private int _index;

            internal EnumeratorClass(UnmanagedList<T> list)
            {
                _list = list;
                _index = 0;
                _current = default;
            }

            /// <summary>Get current item</summary>
            public T Current => _current;

            object IEnumerator.Current => _current;

            /// <summary>Dispose this enumerator (but do nothing)</summary>
            public void Dispose() { }   // nop

            /// <summary>Move to next item</summary>
            /// <returns>true if continue, false to end</returns>
            public bool MoveNext()
            {
                if(_index < _list._length) {
                    _current = _list[_index];
                    _index++;
                    return true;
                }
                else {
                    return false;
                }
            }

            /// <summary>Reset enumerator</summary>
            public void Reset()
            {
                _index = 0;
                _current = default;
            }
        }


        /// <summary>
        /// Raw array struct. This struct checks no index boundary and any other safety.
        /// </summary>
        private readonly struct RawArray : IDisposable
        {
            /// <summary>Raw array pointer</summary>
            public readonly IntPtr Ptr;
            /// <summary>Raw array length (this is NOT size in bytes)</summary>
            public readonly int Length;

            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref ((T*)Ptr)[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RawArray(int length)
            {
                // Not clear by zero for performance.
                // This is no problem because T is unmanaged type.

                Ptr = Marshal.AllocHGlobal(length * sizeof(T));
                Length = length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetSizeInBytes() => Length * sizeof(T);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan() => new Span<T>((T*)Ptr, Length);

            // No boundary checking. Be careful !!
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan(int start) => new Span<T>(((T*)Ptr) + start, Length - start);

            // No boundary checking. Be careful !!
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan(int start, int length) => new Span<T>(((T*)Ptr) + start, length);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                // Clear memory if debug for easy detecting invalid memory access.
                // (e.g. Accessing to memory after free)
#if DEBUG
                AsSpan().Clear();
#endif
                Marshal.FreeHGlobal(Ptr);
            }
        }
    }

    /// <summary>Define extension methods of <see cref="UnmanagedList{T}"/></summary>
    public static class UnmanagedListExtension
    {
        /// <summary>Create new <see cref="UnmanagedList{T}"/> from array of type <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">type of elements</typeparam>
        /// <param name="source">source array to initialize <see cref="UnmanagedList{T}"/></param>
        /// <returns>new <see cref="UnmanagedList{T}"/> initialized by <paramref name="source"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedList<T> ToUnmanagedList<T>(this T[] source) where T : unmanaged
        {
            return new UnmanagedList<T>(source);
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> from <see cref="Span{T}"/>.</summary>
        /// <typeparam name="T">type of elements</typeparam>
        /// <param name="source">source <see cref="Span{T}"/> to initialize <see cref="UnmanagedList{T}"/></param>
        /// <returns>new <see cref="UnmanagedList{T}"/> initialized by <paramref name="source"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedList<T> ToUnmanagedList<T>(this Span<T> source) where T : unmanaged
        {
            return new UnmanagedList<T>(source);
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> from <see cref="IEnumerable{T}"/>.</summary>
        /// <typeparam name="T">type of elements</typeparam>
        /// <param name="source">source <see cref="IEnumerable{T}"/> to initialize <see cref="UnmanagedList{T}"/></param>
        /// <returns>new <see cref="UnmanagedList{T}"/> initialized by <paramref name="source"/></returns>
        public static UnmanagedList<T> ToUnmanagedList<T>(this IEnumerable<T> source) where T : unmanaged
        {
            return new UnmanagedList<T>(source);
        }

        /// <summary>Create new <see cref="UnmanagedList{T}"/> from <see cref="ReadOnlySpan{T}"/>.</summary>
        /// <typeparam name="T">type of elements</typeparam>
        /// <param name="source">source <see cref="ReadOnlySpan{T}"/> to initialize <see cref="UnmanagedList{T}"/></param>
        /// <returns>new <see cref="UnmanagedList{T}"/> initialized by <paramref name="source"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedList<T> ToUnmanagedList<T>(this ReadOnlySpan<T> source) where T : unmanaged
        {
            return new UnmanagedList<T>(source);
        }
    }

    internal class UnmanagedListDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnmanagedList<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[_entity.Count];
                _entity.CopyTo(items, 0);
                return items;
            }
        }

        public UnmanagedListDebuggerTypeProxy(UnmanagedList<T> entity) => _entity = entity;
    }
}

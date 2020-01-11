﻿/*
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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// Array class which is allocated in unmanaged memory.<para/>
    /// Only for unmanaged types. (e.g. int, float, recursive-unmanaged struct, and so on.)
    /// </summary>
    /// <typeparam name="T">type of array</typeparam>
    [DebuggerTypeProxy(typeof(UnmanagedArrayDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnmanagedArray<{typeof(T).Name}>[{_length}]")]
    public sealed class UnmanagedArray<T> : IList<T>, IReadOnlyList<T>, IList, IReadOnlyCollection<T>, IDisposable
        where T : unmanaged
    {
        private readonly int _length;
        private int _version;
        private bool _disposed;
        private readonly IntPtr _array;

        /// <summary>Get pointer address of this array.</summary>
        public IntPtr Ptr { get { ThrowIfDisposed(); return _array; } }

        /// <summary>Get <see cref="UnmanagedArray{T}"/> is disposed.</summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// [CAUTION] This is only for performance in case of internal access.<para/>
        /// This may cause violational access to memory!!!!!<para/>
        /// Access to private fields directly without any checking.<para/>
        /// </summary>
        internal InternalDirectAccessor DirectAccessor => new InternalDirectAccessor(this);

        /// <summary>Get the specific item of specific index.</summary>
        /// <param name="i">index</param>
        /// <returns>The item of specific index</returns>
        public unsafe T this[int i]
        {
            get
            {
                if((uint)i >= (uint)_length) { throw new IndexOutOfRangeException(); }
                ThrowIfDisposed();
                return ((T*)_array)[i];
            }
            set
            {
                if((uint)i >= (uint)_length) { throw new IndexOutOfRangeException(); }
                ThrowIfDisposed();
                ((T*)_array)[i] = value;
                _version++;
            }
        }

        /// <summary>Get length of this array</summary>
        public int Length { get { ThrowIfDisposed(); return _length; } }


        // *** NOTICE ***
        // T[] a = new T[10];
        // (a as ICollection<T>).IsReadOnly   ----> true
        // (a as IList).IsReadOnly   ----> false
        // 
        // ↓ I copied thier values of the properties.

        bool ICollection<T>.IsReadOnly => true;

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => true;

        int ICollection<T>.Count { get { ThrowIfDisposed(); return _length; } }

        int IReadOnlyCollection<T>.Count { get { ThrowIfDisposed(); return _length; } }

        int ICollection.Count { get { ThrowIfDisposed(); return _length; } }

        object ICollection.SyncRoot => _syncRoot ?? (_syncRoot = new object());
        private object? _syncRoot;

        bool ICollection.IsSynchronized => false;

        object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

        /// <summary>UnmanagedArray Constructor</summary>
        /// <param name="length">Length of array</param>
        public unsafe UnmanagedArray(int length)
        {
            if(length < 0) { throw new ArgumentOutOfRangeException(); }
            var objsize = sizeof(T);
            _array = Marshal.AllocHGlobal(length * objsize);
            _length = length;

            // initialize all bytes as zero
            var array = (byte*)_array;
            for(int i = 0; i < objsize * length; i++) {
                array[i] = 0x00;
            }
        }

        /// <summary>Create new <see cref="UnmanagedArray{T}"/> filled by specified element.</summary>
        /// <param name="length">length of array</param>
        /// <param name="fill">element that fills array</param>
        public unsafe UnmanagedArray(int length, T fill)
        {
            if(length < 0) { throw new ArgumentOutOfRangeException(); }
            var objsize = sizeof(T);
            _array = Marshal.AllocHGlobal(length * objsize);
            _length = length;

            // fill
            var array = (T*)_array;
            for(int i = 0; i < length; i++) {
                array[i] = fill;
            }
        }

        /// <summary>Create new <see cref="UnmanagedArray{T}"/>, those elements are copied from <see cref="ReadOnlySpan{T}"/>.</summary>
        /// <param name="span">Elements of the <see cref="UnmanagedArray{T}"/> are initialized by this <see cref="ReadOnlySpan{T}"/>.</param>
        public unsafe UnmanagedArray(ReadOnlySpan<T> span)
        {
            var objsize = sizeof(T);
            _array = Marshal.AllocHGlobal(span.Length * objsize);
            _length = span.Length;

            // initialize all bytes as zero
            for(int i = 0; i < span.Length * objsize; i++) {
                Marshal.WriteByte(_array + i, 0x00);
            }

            var array = (T*)_array;
            for(int i = 0; i < _length; i++) {
                array[i] = span[i];
            }
        }

        ~UnmanagedArray() => Dispose(false);

        /// <summary>Get enumerator instance.</summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            ThrowIfDisposed();
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            ThrowIfDisposed();
            // Avoid boxing by using class enumerator.
            return new EnumeratorClass(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();
            // Avoid boxing by using class enumerator.
            return new EnumeratorClass(this);
        }

        /// <summary>Get index of the item</summary>
        /// <param name="item">target item</param>
        /// <returns>index (if not contain, value is -1)</returns>
        public int IndexOf(T item)
        {
            ThrowIfDisposed();
            for(int i = 0; i < _length; i++) {
                if(item.Equals(this[i])) { return i; }
            }
            return -1;
        }

        /// <summary>Get whether this instance contains the item.</summary>
        /// <param name="item">target item</param>
        /// <returns>true: This array contains the target item. false: not contain</returns>
        public bool Contains(T item)
        {
            ThrowIfDisposed();
            for(int i = 0; i < _length; i++) {
                if(item.Equals(this[i])) { return true; }
            }
            return false;
        }

        /// <summary>Copy to managed memory</summary>
        /// <param name="array">managed memory array</param>
        /// <param name="arrayIndex">start index of destination array</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            if(array == null) { throw new ArgumentNullException(nameof(array)); }
            if((uint)arrayIndex >= (uint)array.Length) { throw new ArgumentOutOfRangeException(nameof(arrayIndex)); }
            if(arrayIndex + _length > array.Length) { throw new ArgumentException("There is not enouph length of destination array"); }
            unsafe {
                var objsize = sizeof(T);
                fixed(T* arrayPtr = array) {
                    var byteLen = (long)(_length * objsize);
                    var dest = new IntPtr(arrayPtr) + arrayIndex * objsize;
                    Buffer.MemoryCopy((void*)_array, (void*)dest, byteLen, byteLen);
                }
            }
        }

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        void ICollection<T>.Clear() => throw new NotSupportedException();

        public unsafe IntPtr GetPtrIndexOf(int index)
        {
            ThrowIfDisposed();
            if((uint)index >= (uint)_length) { throw new IndexOutOfRangeException(); }
            return new IntPtr((T*)_array + index);
        }

        /// <summary>Copy fron <see cref="UnmanagedArray{T}"/>.</summary>
        /// <param name="array">source array of type <see cref="UnmanagedArray{T}"/></param>
        public void CopyFrom(UnmanagedArray<T> array) => CopyFrom(array.Ptr, 0, array.Length);

        /// <summary>Copy from <see cref="ReadOnlySpan{T}"/> to this <see cref="UnmanagedArray{T}"/> of index 0.</summary>
        /// <param name="source"><see cref="ReadOnlySpan{T}"/> object.</param>
        public void CopyFrom(ReadOnlySpan<T> span) => CopyFrom(span, 0);

        /// <summary>Copy from <see cref="ReadOnlySpan{T}"/> to this <see cref="UnmanagedArray{T}"/> of specified index.</summary>
        /// <param name="source"><see cref="ReadOnlySpan{T}"/> object.</param>
        /// <param name="start">start index of destination. (destination is this <see cref="UnmanagedArray{T}"/>.)</param>
        public unsafe void CopyFrom(ReadOnlySpan<T> source, int start)
        {
            ThrowIfDisposed();
            if(start < 0) { throw new ArgumentOutOfRangeException(); }
            if(start + source.Length > _length) { throw new ArgumentOutOfRangeException(); }
            var objsize = sizeof(T);
            fixed(T* ptr = source) {
                var byteLen = (long)(source.Length * objsize);
                Buffer.MemoryCopy(ptr, (void*)(_array + start * objsize), byteLen, byteLen);
                _version++;
            }
        }

        /// <summary>Copy from unmanaged.</summary>
        /// <param name="source">unmanaged source pointer</param>
        /// <param name="start">start index of destination. (destination is this <see cref="UnmanagedArray{T}"/>.)</param>
        /// <param name="length">count of copied item. (NOT length of bytes.)</param>
        public unsafe void CopyFrom(IntPtr source, int start, int length)
        {
            ThrowIfDisposed();
            if(length == 0) { return; }
            if(source == IntPtr.Zero) { throw new ArgumentNullException("source is null"); }
            if(start < 0 || length < 0) { throw new ArgumentOutOfRangeException(); }
            if(start + length > _length) { throw new ArgumentOutOfRangeException(); }
            var objsize = sizeof(T);
            var byteLen = (long)(length * objsize);
            Buffer.MemoryCopy((void*)source, (void*)(_array + start * objsize), byteLen, byteLen);
            _version++;
        }

        /// <summary>Return <see cref="Span{T}"/> of this <see cref="UnmanagedArray{T}"/>.</summary>
        /// <returns><see cref="Span{T}"/></returns>
        public unsafe Span<T> AsSpan()
        {
            ThrowIfDisposed();
            return new Span<T>((T*)_array, _length);
        }

        /// <summary>Create new <see cref="UnmanagedArray{T}"/> whose values are initialized by memory layout of specified structure.</summary>
        /// <typeparam name="TStruct">type of source structure</typeparam>
        /// <param name="obj">source structure</param>
        /// <returns>instance of <see cref="UnmanagedArray{T}"/> whose values are initialized by <paramref name="obj"/></returns>
        public static unsafe UnmanagedArray<T> CreateFromStruct<TStruct>(ref TStruct obj) where TStruct : unmanaged
        {
            var structSize = sizeof(TStruct);
            var itemSize = sizeof(T);
            var arrayLen = structSize / itemSize + (structSize % itemSize > 0 ? 1 : 0);
            var array = new UnmanagedArray<T>(arrayLen);
            fixed(TStruct* ptr = &obj) {
                Buffer.MemoryCopy(ptr, (void*)array._array, structSize, structSize);
            }
            return array;
        }

        /// <summary>
        /// Dispose this instance and release unmanaged memory.<para/>
        /// If already disposed, do nothing.<para/>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }
            if(disposing) {
                // relase managed resource here.
            }
            Marshal.FreeHGlobal(_array);
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if(_disposed) { throw new ObjectDisposedException(nameof(UnmanagedArray<T>), "Memory of array is already free."); }
        }

        int IList.Add(object value) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        void IList.Insert(int index, object value) => throw new NotSupportedException();
        void IList.Remove(object value) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
        bool IList.Contains(object value) => (value is T v) ? Contains(v) : false;
        int IList.IndexOf(object value) => (value is T v) ? IndexOf(v) : -1;
        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly UnmanagedArray<T> _array;
            private readonly int _version;
            private int _index;
            public T Current { get; private set; }

            internal Enumerator(UnmanagedArray<T> array)
            {
                _array = array;
                _index = 0;
                _version = _array._version;
                Current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var localArray = _array;
                if(_version == localArray._version && ((uint)_index < (uint)localArray._length)) {
                    Current = localArray[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                var localArray = _array;
                if(_version != localArray._version) {
                    throw new InvalidOperationException();
                }
                _index = localArray._length + 1;
                Current = default;
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if(_index == 0 || _index == _array._length + 1) {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if(_version != _array._version) {
                    throw new InvalidOperationException();
                }
                _index = 0;
                Current = default;
            }
        }

        public class EnumeratorClass : IEnumerator<T>, IEnumerator
        {
            private readonly UnmanagedArray<T> _array;
            private readonly int _version;
            private int _index;
            public T Current { get; private set; }

            internal EnumeratorClass(UnmanagedArray<T> array)
            {
                _array = array;
                _index = 0;
                _version = _array._version;
                Current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var localArray = _array;
                if(_version == localArray._version && ((uint)_index < (uint)localArray._length)) {
                    Current = localArray[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                var localArray = _array;
                if(_version != localArray._version) {
                    throw new InvalidOperationException();
                }
                _index = localArray._length + 1;
                Current = default;
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if(_index == 0 || _index == _array._length + 1) {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if(_version != _array._version) {
                    throw new InvalidOperationException();
                }
                _index = 0;
                Current = default;
            }
        }

        /// <summary>
        /// [CAUTION] This struct is only for performance in case of internal access.<para/>
        /// This may cause violational access to memory!!!!!<para/>
        /// Access to private fields directly without any checking.<para/>
        /// </summary>
        internal struct InternalDirectAccessor
        {
            private readonly UnmanagedArray<T> _instance;
            internal int Length => _instance._length;
            internal IntPtr Ptr => _instance._array;

            internal unsafe T GetItem(int i) => ((T*)_instance._array)[i];
            internal unsafe void SetItem(int i, T value)
            {
                ((T*)_instance._array)[i] = value;
                _instance._version++;
            }

            internal unsafe void CopyFrom(IntPtr source, int start, int length)
            {
                var objsize = sizeof(T);
                var byteLen = (long)(length * objsize);
                Buffer.MemoryCopy((void*)source, (void*)(_instance._array + start * objsize), byteLen, byteLen);
                _instance._version++;
            }
            internal InternalDirectAccessor(UnmanagedArray<T> instance) => _instance = instance;
        }
    }

    public static class UnmanagedArrayExtension
    {
        /// <summary>Create a new instance of <see cref="UnmanagedArray{T}"/> initialized by source.</summary>
        /// <typeparam name="T">Type of item in array</typeparam>
        /// <param name="source">source which initializes new array.</param>
        /// <returns>instance of <see cref="UnmanagedArray{T}"/></returns>
        public static UnmanagedArray<T> ToUnmanagedArray<T>(this IEnumerable<T> source) where T : unmanaged
        {
            if(source == null) { throw new ArgumentNullException(nameof(source)); }
            if(source is T[] managedArray) {
                var array = new UnmanagedArray<T>(managedArray.Length);
                unsafe {
                    fixed(T* ptr = managedArray) {
                        array.DirectAccessor.CopyFrom((IntPtr)ptr, 0, managedArray.Length);
                    }
                }
                return array;
            }
            else if (source is ICollection<T> collection) {
                var array = new UnmanagedArray<T>(collection.Count);
                int i = 0;
                foreach(var item in collection) {
                    array.DirectAccessor.SetItem(i++, item);
                }
                return array;
            }
            else {
                const int initialLen = 4;
                var array = new UnmanagedArray<T>(initialLen);
                int i = 0;
                foreach(var item in source) {
                    if(i >= array.DirectAccessor.Length) {
                        // Expand length of the array.
                        var newArray = new UnmanagedArray<T>(array.DirectAccessor.Length * 2);
                        newArray.DirectAccessor.CopyFrom(array.DirectAccessor.Ptr, 0, array.DirectAccessor.Length);
                        array.Dispose();
                        array = newArray;
                    }
                    array.DirectAccessor.SetItem(i, item);
                    i++;
                }
                if(array.DirectAccessor.Length != i) {
                    // Shrink length of the array.
                    var newArray = new UnmanagedArray<T>(i);
                    newArray.CopyFrom(array.DirectAccessor.Ptr, 0, i);
                    array.Dispose();
                    array = newArray;
                }
                return array;
            }
        }
    }

    internal class UnmanagedArrayDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnmanagedArray<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var items = new T[_entity.Length];
                _entity.CopyTo(items, 0);
                return items;
            }
        }

        public UnmanagedArrayDebuggerTypeProxy(UnmanagedArray<T> entity) => _entity = entity;
    }
}
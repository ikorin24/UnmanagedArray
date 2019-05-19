using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elffy.Effective
{
    #region class UnmanagedArray<T>
    /// <summary>
    /// Array class which is allocated in unmanaged memory.<para/>
    /// Only for unmanaged type. (e.g. int, float, recursive-unmanaged struct, and so on.)
    /// </summary>
    /// <typeparam name="T">type of array</typeparam>
    [DebuggerTypeProxy(typeof(UnmanagedArrayDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnmanagedArray<{Type.Name}>[{Length}]")]
    public sealed class UnmanagedArray<T> : IList<T>, IReadOnlyList<T>, IDisposable
        where T : unmanaged
    {
        #region private member
        private readonly object _syncRoot;
        private int _length;
        private int _version;
        private bool _disposed;
        private bool _isFree;
        private readonly IntPtr _array;
        private readonly int _objsize;
        #endregion private member

        /// <summary>Get wheater this instance is thread-safe-access-suppoted.</summary>
        public bool IsThreadSafe { get; }

        /// <summary>Get the type of an item in this array.</summary>
        public Type Type => typeof(T);

        /// <summary>Get the specific item of specific index.</summary>
        /// <param name="i">index</param>
        /// <returns>The item of specific index</returns>
        public T this[int i]
        {
            get
            {
                if(IsThreadSafe) {
                    lock(_syncRoot) {
                        ThrowIfFree();
                        unsafe {
                            return *(T*)(_array + i * _objsize);
                        }
                    }
                }
                else {
                    ThrowIfFree();
                    unsafe {
                        return *(T*)(_array + i * _objsize);
                    }
                }
            }
            set
            {
                if(IsThreadSafe) {
                    lock(_syncRoot) {
                        ThrowIfFree();
                        var ptr = _array + i * _objsize;
                        Marshal.StructureToPtr<T>(value, ptr, true);
                        _version++;
                    }
                }
                else {
                    ThrowIfFree();
                    var ptr = _array + i * _objsize;
                    Marshal.StructureToPtr<T>(value, ptr, true);
                    _version++;
                }
            }
        }

        /// <summary>Length of this array</summary>
        public int Length => _length;

        /// <summary>Length of this array (ICollection implementation)</summary>
        public int Count => _length;

        /// <summary>Get wheater this array is readonly.</summary>
        public bool IsReadOnly => false;

        #region constructor
        public UnmanagedArray(int length) : this(length, false) { }

        /// <summary>UnmanagedArray Constructor</summary>
        /// <param name="length">Length of array</param>
        public UnmanagedArray(int length, bool threadSafe)
        {
            if(length < 0) { throw new InvalidOperationException(); }
            IsThreadSafe = threadSafe;
            if(threadSafe) {
                _syncRoot = new object();
            }
            _objsize = Marshal.SizeOf<T>();
            _array = Marshal.AllocHGlobal(length * _objsize);
            _length = length;

            // initialize all block as zero
            for(int i = 0; i < _objsize * length; i++) {
                Marshal.WriteByte(_array + i, 0x00);
            }
        }

        ~UnmanagedArray() => Dispose(false);
        #endregion

        #region Free
        /// <summary>
        /// Free the allocated memory of this instance. <para/>
        /// If already free, do nothing.<para/>
        /// </summary>
        public void Free()
        {
            if(IsThreadSafe) {
                lock(_syncRoot) {
                    if(!_isFree) {
                        Marshal.FreeHGlobal(_array);
                        _isFree = true;
                    }
                }
            }
            else {
                if(!_isFree) {
                    Marshal.FreeHGlobal(_array);
                    _isFree = true;
                }
            }
        }
        #endregion

        #region IList implementation
        /// <summary>Get index of the item</summary>
        /// <param name="item">target item</param>
        /// <returns>index (if not contain, value is -1)</returns>
        public int IndexOf(T item)
        {
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
            if(array == null) { throw new ArgumentNullException(nameof(array)); }
            if(arrayIndex + _length > array.Length) { throw new ArgumentException("There is not enouph length of destination array"); }
            unsafe {
                for(int i = 0; i < _length; i++) {
                    array[i + arrayIndex] = this[i];
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <summary>Not Supported in this class.</summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        [Obsolete("This method is not supported.", true)]
        public void Insert(int index, T item) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        /// <param name="index"></param>
        [Obsolete("This method is not supported.", true)]
        public void RemoveAt(int index) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        /// <param name="item"></param>
        [Obsolete("This method is not supported.", true)]
        public void Add(T item) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        [Obsolete("This method is not supported.", true)]
        public bool Remove(T item) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        [Obsolete("This method is not supported.", true)]
        public void Clear() => throw new NotSupportedException();
        #endregion

        #region Dispose
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
            Free();
            _disposed = true;
        }
        #endregion

        private void ThrowIfFree()
        {
            if(_isFree) { throw new InvalidOperationException("Memory of Array is already free."); }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region struct Enumerator
        [Serializable]
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
                var localList = _array;
                if(_version == localList._version && ((uint)_index < (uint)localList._length)) {
                    Current = localList[_index];
                    _index++;
                    return true;
                }

                if(_version != _array._version) {
                    throw new InvalidOperationException();
                }
                _index = _array._length + 1;
                Current = default;
                return false;
            }

            object IEnumerator.Current
            {
                get {
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
        #endregion struct Enumerator
    }
    #endregion class UnmanagedArray<T>

    public static class UnmanagedArrayExtension
    {
        /// <summary>Create a new instance of <see cref="UnmanagedArray{T}"/> initialized by source.</summary>
        /// <typeparam name="T">Type of item in array</typeparam>
        /// <param name="source">source which initializes new array.</param>
        /// <returns>instance of <see cref="UnmanagedArray{T}"/></returns>
        public static UnmanagedArray<T> ToUnmanagedArray<T>(this IEnumerable<T> source) where T : unmanaged 
            => ToUnmanagedArray(source, false);

        /// <summary>Create a new instance of <see cref="UnmanagedArray{T}"/> initialized by source.</summary>
        /// <typeparam name="T">Type of item in array</typeparam>
        /// <param name="source">source which initializes new array.</param>
        /// <param name="threadSafe">thread safety of <see cref="UnmanagedArray{T}"/>.</param>
        /// <returns>instance of <see cref="UnmanagedArray{T}"/></returns>
        public static UnmanagedArray<T> ToUnmanagedArray<T>(this IEnumerable<T> source, bool threadSafe) where T : unmanaged
        {
            if(source == null) { throw new ArgumentNullException(); }
            var len = source.Count();
            var array = new UnmanagedArray<T>(len, threadSafe);
            var i = 0;
            foreach (var item in source)
            {
                array[i++] = item;
            }
            return array;
        }
    }

    #region class UnmanagedArrayDebuggerTypeProxy<T>
    internal class UnmanagedArrayDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private UnmanagedArray<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get {
                var items = new T[_entity.Length];
                _entity.CopyTo(items, 0);
                return items;
            }
        }

        public bool IsThreadSafe => _entity.IsThreadSafe;

        public bool IsReadOnly => _entity.IsReadOnly;

        public UnmanagedArrayDebuggerTypeProxy(UnmanagedArray<T> entity) => _entity = entity;
    }
    #endregion class UnmanagedArrayDebuggerTypeProxy<T>
}

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
using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections;
using UnmanageUtility;

namespace Test
{
    public class UnmanagedArrayTest
    {
        [Fact]
        public unsafe void Exception()
        {
            using(var array = new UnmanagedArray<int>(10000)) {
                Assert.Throws<IndexOutOfRangeException>(() => array[-1] = 4);
                Assert.Throws<IndexOutOfRangeException>(() => array[-8]);
                Assert.Throws<IndexOutOfRangeException>(() => array[array.Length] = 9);
                Assert.Throws<IndexOutOfRangeException>(() => array[array.Length]);

                // CopyTo
                Assert.Throws<ArgumentNullException>(() => array.CopyTo(null!, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyTo(new int[array.Length], -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyTo(new int[array.Length], array.Length));
                Assert.Throws<ArgumentException>(() => array.CopyTo(new int[array.Length], 10));        // not enough length of destination
                Assert.Throws<ArgumentException>(() => array.CopyTo(new int[100], 0));                  // not enough length of destination

                // CopyFrom
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyFrom(new ReadOnlySpan<int>(), -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyFrom(new int[10000], 5));
                Assert.Throws<ArgumentNullException>(() => array.CopyFrom(IntPtr.Zero, 0, 4));
                var ptr = stackalloc int[10];
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyFrom((IntPtr)ptr, -1, 5));
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyFrom((IntPtr)ptr, 5, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyFrom((IntPtr)ptr, 9999, 4));

                // NotSupported methods
                Assert.Throws<NotSupportedException>(() => (array as IList<int>).Insert(0, 0));
                Assert.Throws<NotSupportedException>(() => (array as IList<int>).RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => (array as ICollection<int>).Add(0));
                Assert.Throws<NotSupportedException>(() => (array as ICollection<int>).Remove(0));
                Assert.Throws<NotSupportedException>(() => (array as ICollection<int>).Clear());
                Assert.Throws<NotSupportedException>(() => (array as IList).Add(0));
                Assert.Throws<NotSupportedException>(() => (array as IList).Clear());
                Assert.Throws<NotSupportedException>(() => (array as IList).Insert(0, 0));
                Assert.Throws<NotSupportedException>(() => (array as IList).Remove(0));
                Assert.Throws<NotSupportedException>(() => (array as IList).RemoveAt(0));
            }
            Assert.Throws<ArgumentNullException>(() => (null as int[])!.ToUnmanagedArray());
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedArray<bool>(-4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedArray<bool>(-5, true));
        }

        [Fact]
        public void Ptr()
        {
            using(var array = new UnmanagedArray<double>(10)) {
                Assert.NotEqual(array.Ptr, IntPtr.Zero);
            }
        }

        [Fact]
        public void Length()
        {
            for(int i = 0; i < 1000; i++) {
                using(var array = new UnmanagedArray<ulong>(i)) {
                    Assert.Equal(array.Length, i);
                }
            }
        }

        [Fact]
        public void Empty()
        {
            var array = UnmanagedArray<int>.Empty;
            Assert.Equal(0, array.Length);
            Assert.True(array.IsDisposed);          // empty array is disposed already
            Assert.Equal(IntPtr.Zero, array.Ptr);   // pointer of empty array is always null.

            var array2 = UnmanagedArray<int>.Empty;
            Assert.True(ReferenceEquals(array, array2));    // empty arrays are same instance.
            
            // Nothing happens if dispose empty array
            array.Dispose();
            array.Dispose();

            Assert.Equal(0, array.Length);
            Assert.True(array.IsDisposed);
            Assert.Equal(IntPtr.Zero, array.Ptr);
        }

        [Fact]
        public void Indexer()
        {
            using(var array = new UnmanagedArray<int>(100)) {
                for(int i = 0; i < array.Length; i++) {
                    array[i] = i * i;
                }
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(array[i], i * i);
                }
            }

            using(var array = new UnmanagedArray<int>(10)) {
                for(int i = 0; i < array.Length; i++) {
                    array[i] = i;
                }
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(i, (array as IList)[i]);
                }
            }
        }

        [Fact]
        public void Constructor()
        {
            using var array = new UnmanagedArray<int>(10);
            Assert.Equal(10, array.Length);
            Span<ushort> span = stackalloc ushort[10];
            using var array2 = new UnmanagedArray<ushort>(span);
            Assert.Equal(10, array2.Length);
            using var array3 = new UnmanagedArray<long>(30, 300L);
            for(int i = 0; i < array3.Length; i++) {
                Assert.Equal(300L, array3[i]);
            }
        }

        [Fact]
        public void OtherProperties()
        {
            using(var array = new UnmanagedArray<decimal>(30)) {
                Assert.True((array as ICollection<decimal>).IsReadOnly);
                Assert.False((array as IList).IsReadOnly);
                Assert.True((array as IList).IsFixedSize);
                Assert.Equal(30, (array as ICollection<decimal>).Count);
                Assert.Equal(30, (array as IReadOnlyCollection<decimal>).Count);
                Assert.Equal(30, (array as ICollection).Count);
                Assert.NotNull((array as ICollection).SyncRoot);
                Assert.False((array as ICollection).IsSynchronized);
            }
        }

        [Fact]
        public void ArrayDispose()
        {
            var array = new UnmanagedArray<float>(10);
            array.Dispose();
            Assert.True(array.IsDisposed);
            Assert.Equal(IntPtr.Zero, array.Ptr);
            Assert.Equal(0, array.Length);
            Assert.True(array.AsSpan().IsEmpty);
            Assert.Throws<IndexOutOfRangeException>(() => array[0] = 34f);
            Assert.Throws<IndexOutOfRangeException>(() => array[3]);
            Assert.Throws<NullReferenceException>(() => array.GetReference() = 3f);
            Assert.Throws<NullReferenceException>(() => array.GetReference());
            Assert.Throws<IndexOutOfRangeException>(() => array.GetReference(2) = 3f);
            Assert.Throws<IndexOutOfRangeException>(() => array.GetReference(5));
            //Assert.Throws<ObjectDisposedException>(() => ((ICollection<float>)array).Count);
            //Assert.Throws<ObjectDisposedException>(() => ((IReadOnlyCollection<float>)array).Count);
            //Assert.Throws<ObjectDisposedException>(() => ((ICollection)array).Count);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList)array)[0]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList)array)[0] = 0f);
            Assert.Throws<ObjectDisposedException>(() => array.GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => ((IEnumerable)array).GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => ((IEnumerable<float>)array).GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => array.IndexOf(0f));
            Assert.Throws<ObjectDisposedException>(() => array.Contains(0f));
            Assert.Throws<ObjectDisposedException>(() => array.CopyTo(new float[10], 0));
#pragma warning disable CS0618 // warning for obsolete
            Assert.Throws<ObjectDisposedException>(() => array.GetPtrIndexOf(0));
#pragma warning restore CS0618 // warning for obsolete
            Assert.Throws<ObjectDisposedException>(() =>
            {
                using var array2 = new UnmanagedArray<float>(10);
                array.CopyFrom(array2.Ptr, 0, array2.Length);
            });
            Assert.Throws<ObjectDisposedException>(() => array.CopyFrom(new UnmanagedArray<float>(10)));
            Assert.Throws<ObjectDisposedException>(() =>
            {
                var span = new Span<float>();
                array.CopyFrom(span, 0);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(0, array.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(1, array.Length));


            array.Dispose();        // No exception although already disposed
        }

        [Fact]
        public void GetReference()
        {
            using(var array = new UnmanagedArray<int>(10)) {
                array[0] = 31;
                array[8] = 40;
                ref var head = ref array.GetReference();
                Assert.Equal(31, head);
                head = 20;
                Assert.Equal(20, head);

                ref var item = ref array.GetReference(8);
                Assert.Equal(40, item);
                item = 600;
                Assert.Equal(600, item);

                Assert.Throws<IndexOutOfRangeException>(() => array.GetReference(30));
                Assert.Throws<IndexOutOfRangeException>(() => array.GetReference(50) = 100);
                Assert.Throws<IndexOutOfRangeException>(() => array.GetReference(-1));
                Assert.Throws<IndexOutOfRangeException>(() => array.GetReference(-3) = 100);
            }
        }

        [Fact]
        public void ToUnmanagedArray()
        {
            var rand = new Random(12345678);
            var origin = Enumerable.Range(0, 100).Select(i => rand.Next()).ToArray();

            // from Array
            using(var array = origin.ToUnmanagedArray()) {
                Assert.Equal(origin.Length, array.Length);
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(array[i], origin[i]);
                }
            }

            // From ICollection<T>
            var list = origin.ToList();
            using(var array = list.ToUnmanagedArray()) {
                Assert.Equal(list.Count, array.Length);
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(array[i], list[i]);
                }
            }

            // From IEnumerable<T>
            using(var array = Enumerable.Range(0, 10).ToUnmanagedArray()) {
                Assert.Equal(10, array.Length);
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(array[i], i);
                }
            }

            // From Empty IEnumerable<T>
            using(var array = Enumerable.Empty<int>().ToUnmanagedArray()) {
                Assert.Equal(0, array.Length);
            }
        }

        [Fact]
        public void Enumerate()
        {
            using(var array = new UnmanagedArray<bool>(100)) {
                foreach(var item in array) {
                    Assert.False(item);
                }
                for(int i = 0; i < array.Length; i++) {
                    array[i] = true;
                }
                foreach(var item in array) {
                    Assert.True(item);
                }
            }
        }

        [Fact]
        public void Linq()
        {
            using(var array = new UnmanagedArray<bool>(100)) {
                array.All(x => !x);
                for(int i = 0; i < array.Length; i++) {
                    array[i] = true;
                }
                array.All(x => x);
                var rand1 = new Random(1234);
                var rand2 = new Random(1234);
                var seq1 = array.Select(x => rand1.Next());
                var seq2 = array.Select(x => rand2.Next());
                Assert.True(seq1.SequenceEqual(seq2));
            }
        }

        [Fact]
        public void IndexOf()
        {
            using(var array = Enumerable.Range(10, 10).ToUnmanagedArray()) {
                Assert.Equal(4, array.IndexOf(14));
                Assert.Equal(-1, array.IndexOf(120));
                Assert.Equal(5, ((IList)array).IndexOf(15));
                Assert.Equal(-1, ((IList)array).IndexOf(140));
                Assert.Equal(-1, ((IList)array).IndexOf(new object()));
                Assert.Equal(-1, ((IList)array).IndexOf(null!));
            }
        }

        [Fact]
        public void Contains()
        {
            using(var array = Enumerable.Range(10, 10).ToUnmanagedArray())
            using(var array2 = new UnmanagedArray<int>(array.Length)) {
                var tmp = array.Contains(179);
                Assert.False(tmp);
                var tmp2 = array.Contains(16);
                Assert.True(tmp2);
            }
        }

        [Fact]
        public void CopyTo()
        {
            using(var array = new UnmanagedArray<int>(10)) {
                var dest = new int[array.Length + 5];
                array.CopyTo(dest, 5);
                Assert.True(dest.Skip(5).SequenceEqual(array));
            }

            using(var array = new UnmanagedArray<float>(20)) {
                var dest = new float[array.Length + 8];
                ((ICollection)array).CopyTo(dest, 8);
                Assert.True(dest.Skip(8).SequenceEqual(array));
            }
        }

        [Fact]
        [Obsolete]
        public unsafe void GetPtr()
        {
            using(var array = new UnmanagedArray<long>(10)) {
                Assert.Equal(array.Ptr, array.GetPtrIndexOf(0));
                for(int i = 0; i < array.Length; i++) {
                    var p1 = array.Ptr + sizeof(long) * i;
                    var p2 = array.GetPtrIndexOf(i);
                    var p3 = (IntPtr)(&((long*)array.Ptr)[i]);
                    Assert.Equal(p1, p2);
                    Assert.Equal(p1, p3);
                }
            }
        }

        [Fact]
        public void AsSpan()
        {
            using(var array = Enumerable.Range(0, 100).ToUnmanagedArray()) {
                var answer = Enumerable.Range(0, 100).Sum();
                var span = array.AsSpan();
                var sum = 0;
                foreach(var item in span) {
                    sum += item;
                }
                Assert.Equal(answer, sum);

                for(int i = 0; i < span.Length; i++) {
                    span[i] = 30;
                }
                Assert.True(array.All(x => x == 30));
            }

            using(var array = Enumerable.Range(0, 100).ToUnmanagedArray()) {
                var ans = Enumerable.Range(0, 100).ToArray();
                Assert.True(array.AsSpan().SequenceEqual(ans));
                Assert.True(array.AsSpan(0).SequenceEqual(ans.AsSpan(0)));
                Assert.True(array.AsSpan(100).SequenceEqual(ans.AsSpan(100)));
                Assert.True(array.AsSpan(0, 100).SequenceEqual(ans.AsSpan(0, 100)));
                Assert.True(array.AsSpan(0, 0).SequenceEqual(ans.AsSpan(0, 0)));
                Assert.True(array.AsSpan(0, 0).SequenceEqual(ans.AsSpan(0, 0)));
                Assert.True(array.AsSpan(10, 20).SequenceEqual(ans.AsSpan(10, 20)));


                Assert.True(array.AsSpan().SequenceEqual(array.AsSpan(0, array.Length)));
            }

            using(var array = UnmanagedArray<int>.Empty) {
                Assert.True(array.AsSpan().IsEmpty);
                Assert.True(array.AsSpan(0).IsEmpty);
                Assert.True(array.AsSpan(0, 0).IsEmpty);
            }
        }

        [Fact]
        public void CopyFrom()
        {
            Span<bool> span = stackalloc bool[20];
            for(int i = 0; i < span.Length; i++) {
                span[i] = true;
            }

            using(var array = new UnmanagedArray<bool>(span.Length)) {
                array.CopyFrom(span);
                for(int i = 0; i < array.Length; i++) {
                    Assert.True(array[i]);
                }
            }
            using(var array = new UnmanagedArray<bool>(span.Length + 20)) {
                array.CopyFrom(span, 5);
                for(int i = 0; i < array.Length; i++) {
                    if(i < 5 || i >= 5 + span.Length) {
                        Assert.False(array[i]);
                    }
                    else {
                        Assert.True(array[i]);
                    }
                }
            }

            using(var array = new UnmanagedArray<bool>(span))
            using(var array2 = new UnmanagedArray<bool>(array.Length + 20))
            using(var array3 = new UnmanagedArray<bool>(array.Length + 20)) {
                array2.CopyFrom(array);
                for(int i = 0; i < array2.Length; i++) {
                    Assert.Equal(i < array.Length, array2[i]);
                }

                array3.CopyFrom(array.Ptr, 4, array.Length);
                for(int i = 0; i < array3.Length; i++) {

                    if(i < 4 || i >= 4 + array.Length) {
                        Assert.False(array3[i]);
                    }
                    else {
                        Assert.True(array3[i]);
                    }
                }
            }

            // Copy from Empty
            using(var array = new UnmanagedArray<short>(10)) {
                var empty = Enumerable.Empty<short>().ToArray();
                unsafe {
                    fixed(short* ptr = empty) {
                        array.CopyFrom((IntPtr)ptr, 0, empty.Length);
                    }
                }
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(0, array[i]);
                }
            }
        }

        [Fact]
        public void CreateFromStruct()
        {
            var data = new TestStruct()
            {
                A = 10,
                B = 5,
                C = 90,
                D = new TestSubStruct()
                {
                    A = 32,
                    B = 50,
                    C = 0xAABBCCDDEEFF0011,
                }
            };
            using(var array = UnmanagedArray<uint>.CreateFromStruct(ref data)) {
                Assert.Equal<uint>(10, array[0]);
                Assert.Equal<uint>(5, array[1]);
                Assert.Equal<uint>(90, array[2]);
                Assert.Equal<uint>(32, array[3]);
                Assert.Equal<uint>(50, array[4]);
                Assert.Equal<uint>(0xEEFF0011, array[5]);
                Assert.Equal<uint>(0xAABBCCDD, array[6]);
            }

            var empty = new EmptyStruct();
            using(var array = UnmanagedArray<byte>.CreateFromStruct(ref empty)) {

            }
        }


        [StructLayout(LayoutKind.Explicit)]
        struct TestStruct
        {
            [FieldOffset(0)]
            public int A;
            [FieldOffset(4)]
            public int B;
            [FieldOffset(8)]
            public int C;
            [FieldOffset(12)]
            public TestSubStruct D;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct TestSubStruct
        {
            [FieldOffset(0)]
            public int A;
            [FieldOffset(4)]
            public int B;
            [FieldOffset(8)]
            public ulong C;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct EmptyStruct
        {

        }
    }
}

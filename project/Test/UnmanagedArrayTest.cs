#nullable enable
using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections;

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
        }

        [Fact]
        public void Ptr()
        {
            using(var array = new UnmanagedArray<short>(0)) {
                Assert.NotEqual(array.Ptr, IntPtr.Zero);
            }
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
            Assert.Throws<ObjectDisposedException>(() => array[0] = 34f);
            Assert.Throws<ObjectDisposedException>(() => array[3]);
            Assert.Throws<ObjectDisposedException>(() => array.Ptr);
            Assert.Throws<ObjectDisposedException>(() => ((ICollection<float>)array).Count);
            Assert.Throws<ObjectDisposedException>(() => ((IReadOnlyCollection<float>)array).Count);
            Assert.Throws<ObjectDisposedException>(() => ((ICollection)array).Count);
            Assert.Throws<ObjectDisposedException>(() => ((IList)array)[0]);
            Assert.Throws<ObjectDisposedException>(() => ((IList)array)[0] = 0f);
            Assert.Throws<ObjectDisposedException>(() => array.GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => ((IEnumerable)array).GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => ((IEnumerable<float>)array).GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => array.IndexOf(0f));
            Assert.Throws<ObjectDisposedException>(() => array.Contains(0f));
            Assert.Throws<ObjectDisposedException>(() => array.CopyTo(new float[10], 0));
            Assert.Throws<ObjectDisposedException>(() => array.GetPtrIndexOf(0));
            Assert.Throws<ObjectDisposedException>(() =>
            {
                using var array2 = new UnmanagedArray<float>(array.Length);
                array.CopyFrom(array2.Ptr, 0, array2.Length);
            });
            Assert.Throws<ObjectDisposedException>(() => array.CopyFrom(new UnmanagedArray<float>(10)));
            Assert.Throws<ObjectDisposedException>(() =>
            {
                var span = new Span<float>();
                array.CopyFrom(span, 0);
            });
            Assert.Throws<ObjectDisposedException>(() => array.AsSpan());


            array.Dispose();        // No exception although already disposed
        }

        [Fact]
        public void NormalArrayToUnmanaged()
        {
            var rand = new Random(12345678);
            var origin = Enumerable.Range(0, 100).Select(i => rand.Next()).ToArray();
            using(var array = origin.ToUnmanagedArray()) {
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(array[i], origin[i]);
                }
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
                Assert.False(array.Contains(179));
                Assert.True(array.Contains(16));

                var copy = new int[array.Length + 5];
                array.CopyTo(copy, 5);
                Assert.True(copy.Skip(5).SequenceEqual(array));

                array2.CopyFrom(array.Ptr, 2, 8);
                Assert.True(array2.Skip(2).SequenceEqual(array.Take(8)));
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

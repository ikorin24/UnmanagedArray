#nullable enable
using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace Test
{
    public class UnmanagedArrayTest
    {
        [Fact]
        public void ReadWrite()
        {
            var array = new UnmanagedArray<int>(100);
            for(int i = 0; i < array.Length; i++) {
                array[i] = i * i;
            }
            for(int i = 0; i < array.Length; i++) {
                Assert.Equal(array[i], i * i);
            }
            array.Dispose();
        }

        [Fact]
        public void Exception()
        {
            using(var array = new UnmanagedArray<int>(10000)) {
                Assert.Throws<IndexOutOfRangeException>(() => array[-1] = 4);
                Assert.Throws<IndexOutOfRangeException>(() => array[-8]);
                Assert.Throws<IndexOutOfRangeException>(() => array[array.Length] = 9);
                Assert.Throws<IndexOutOfRangeException>(() => array[array.Length]);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedArray<bool>(-4));
        }

        [Fact]
        public void ArrayLen()
        {
            for(int i = 0; i < 50; i++) {
                using(var array = new UnmanagedArray<short>(i)) {
                    Assert.Equal(array.Length, i);
                }
            }
        }

        [Fact]
        public void ArrayDispose()
        {
            var array = new UnmanagedArray<float>(10);
            array.Dispose();
            Assert.Throws<ObjectDisposedException>(() => array[0] = 34f);
            Assert.Throws<ObjectDisposedException>(() => array[3]);
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
        public void Methods1()
        {
            using(var array = Enumerable.Range(10, 10).ToUnmanagedArray())
            using(var array2 = new UnmanagedArray<int>(array.Length)) {
                Assert.Equal(4, array.IndexOf(14));
                Assert.DoesNotContain(179, array);
                Assert.Contains(16, array);

                var copy = new int[array.Length + 5];
                array.CopyTo(copy, 5);
                Assert.True(copy.Skip(5).SequenceEqual(array));

                array2.CopyFrom(array.Ptr, 2, 8);
                Assert.True(array2.Skip(2).SequenceEqual(array.Take(8)));
            }
        }

        [Fact]
        public void Method2()
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
                Assert.Equal<uint>(5 , array[1]);
                Assert.Equal<uint>(90, array[2]);
                Assert.Equal<uint>(32, array[3]);
                Assert.Equal<uint>(50, array[4]);
                Assert.Equal<uint>(0xEEFF0011,array[5]);
                Assert.Equal<uint>(0xAABBCCDD, array[6]);
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
    }
}

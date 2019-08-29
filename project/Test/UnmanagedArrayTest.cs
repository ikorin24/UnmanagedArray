using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Elffy.Effective;
using System.Linq;
using static Test.TestHelper;

namespace Test
{
    [TestClass]
    public class UnmanagedArrayTest
    {
        [TestMethod]
        public void ReadWrite()
        {
            var len = 100;

            var array = new UnmanagedArray<int>(len);
            for(int i = 0; i < array.Length; i++) {
                array[i] = i * i;
            }
            for(int i = 0; i < len; i++) {
                if(array[i] != i * i) { throw new Exception(); }
            }
            array.Free();
        }

        [TestMethod]
        public void BasicAPI()
        {
            // UnmanagedArrayの提供するpublicなAPIを網羅的にテストします

            // プロパティ
            using(var array = new UnmanagedArray<short>(10000)) {
                Assert(array.Type == typeof(short));
                Assert(array.IsReadOnly == false);
                Assert(array.IsThreadSafe == false);
                AssertException<IndexOutOfRangeException>(() => array[-1] = 4);
                AssertException<IndexOutOfRangeException, int>(() => array[-8]);
                AssertException<IndexOutOfRangeException>(() => array[array.Length] = 4);
                AssertException<IndexOutOfRangeException, int>(() => array[array.Length]);
            }
            using(var array = new UnmanagedArray<uint>(2, true)) {
                Assert(array.IsThreadSafe);
            }

            // 要素数
            for(int i = 0; i < 10; i++) {
                using(var array = new UnmanagedArray<double>(i)) {
                    Assert(array.Length == i);
                }
            }

            // 手動解放/二重解放防止
            {
                var array = new UnmanagedArray<float>(10);
                array.Free();
                AssertException<InvalidOperationException>(() => array[0] = 3);
                AssertException<InvalidOperationException, float>(() => array[7]);
                array.Free();
            }

            // 配列との相互変換
            {
                var rand = new Random(12345678);
                var origin = Enumerable.Range(0, 100).Select(i => rand.Next()).ToArray();
                using(var array = origin.ToUnmanagedArray()) {
                    for(int i = 0; i < array.Length; i++) {
                        Assert(array[i] == origin[i]);
                    }
                    var copy = origin.ToArray();
                    for(int i = 0; i < copy.Length; i++) {
                        Assert(array[i] == origin[i]);
                    }
                }
            }

            // 列挙/LINQ
            using(var array = new UnmanagedArray<bool>(100)) {
                foreach(var item in array) {
                    Assert(item == false);
                }
                for(int i = 0; i < array.Length; i++) {
                    array[i] = true;
                }
                Assert(array.All(x => x));
                var rand1 = new Random(1234);
                var rand2 = new Random(1234);
                var seq1 = array.Select(x => rand1.Next());
                var seq2 = Enumerable.Range(0, array.Length).Select(x => rand2.Next());
                Assert(seq1.SequenceEqual(seq2));
            }

            // その他メソッド
            AssertException<ArgumentException>(() => new UnmanagedArray<ulong>(-4));
            using(var array = Enumerable.Range(10, 10).ToUnmanagedArray()) {
                Assert(array.IndexOf(14) == 4);
                Assert(array.Contains(179) == false);
                Assert(array.Contains(16) == true);
                var copy = new int[array.Length + 5];
                array.CopyTo(copy, 5);
                Assert(copy.Skip(5).SequenceEqual(array));
                using(var array2 = new UnmanagedArray<int>(array.Length)) {
                    array2.CopyFrom(array.Ptr, 2, 8);
                    Assert(array2.Skip(2).SequenceEqual(array.Take(8)));
                }
            }
        }
    }
}

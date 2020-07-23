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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnmanageUtility;
using Xunit;

namespace Test
{
    public unsafe class UnmanagedListTest
    {
        private static readonly Type[] TargetTypes = new Type[]
        {
            typeof(bool), typeof(decimal),
            typeof(sbyte), typeof(byte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
            typeof(IntPtr), typeof(TestData),
        };

        [Fact]
        public void Ctor()
        {
            static void Ctor_<T>() where T : unmanaged
            {
                using(var list = new UnmanagedList<T>()) {
                    Assert.True(list.Capacity > 0);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr != IntPtr.Zero);
                }

                using(var list = new UnmanagedList<T>(10)) {
                    Assert.True(list.Capacity == 10);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr != IntPtr.Zero);
                }

                using(var list = new UnmanagedList<T>(0)) {
                    Assert.True(list.Capacity == 0);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr == IntPtr.Zero);
                }

                using(var list = new UnmanagedList<T>(10)) {
                    Assert.True(list.Capacity == 10);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr != IntPtr.Zero);
                }

                Assert.Throws<ArgumentOutOfRangeException>(() => new UnmanagedList<T>(-1));
            }

            Ctor_<bool>();
            Ctor_<decimal>();
            Ctor_<sbyte>();
            Ctor_<byte>();
            Ctor_<short>();
            Ctor_<ushort>();
            Ctor_<int>();
            Ctor_<uint>();
            Ctor_<long>();
            Ctor_<ulong>();
            Ctor_<float>();
            Ctor_<double>();
            Ctor_<IntPtr>();
            Ctor_<TestData>();
        }

        [Fact]
        public void ListDispose()
        {
            static void ListDispose_<T>() where T : unmanaged
            {
                var list = new UnmanagedList<T>();
                Assert.True(list.Ptr != IntPtr.Zero);
                list.Dispose();

                // After disposing, those properties may be cleared.
                Assert.True(list.Ptr == IntPtr.Zero);
                Assert.True(list.Count == 0);
                Assert.True(list.Capacity == 0);

                // No exceptions would be thown on re-disposing.
                list.Dispose();
            }

            ListDispose_<int>();
            ListDispose_<double>();
            ListDispose_<bool>();
        }

        [Fact]
        public void Add()
        {
            // Case of default capacity, of type int
            using(var list = new UnmanagedList<int>()) {
                var len = 100;
                for(int i = 0; i < len; i++) {
                    list.Add(i);
                    Assert.True(list.Count == i + 1);
                    Assert.True(list[i] == i);
                }

                Assert.True(list.Count == len);
                Assert.True(list.Capacity >= list.Count);
            }

            // Case of default capacity, of type long
            using(var list = new UnmanagedList<long>()) {
                var len = 100;
                for(int i = 0; i < len; i++) {
                    list.Add(i);
                    Assert.True(list.Count == i + 1);
                    Assert.True(list[i] == i);
                }

                Assert.True(list.Count == len);
                Assert.True(list.Capacity >= list.Count);
            }

            // Case of default capacity, of type bool
            using(var list = new UnmanagedList<bool>()) {
                var len = 100;
                for(int i = 0; i < len; i++) {
                    var value = i % 3 == 0;
                    list.Add(value);
                    Assert.True(list.Count == i + 1);
                    Assert.True(list[i] == value);
                }

                Assert.True(list.Count == len);
                Assert.True(list.Capacity >= list.Count);
            }



            // Case of empty capacity, of type int
            using(var list = new UnmanagedList<int>(0)) {
                var len = 20;
                for(int i = 0; i < len; i++) {
                    list.Add(i);
                    Assert.True(list.Count == i + 1);
                    Assert.True(list[i] == i);
                }

                Assert.True(list.Count == len);
                Assert.True(list.Capacity >= list.Count);
            }

            // Case of specified capacity, of type bool
            using(var list = new UnmanagedList<bool>(10)) {
                var len = 100;
                for(int i = 0; i < len; i++) {
                    var value = i % 3 == 0;
                    list.Add(value);
                    Assert.True(list.Count == i + 1);
                    Assert.True(list[i] == value);
                }

                Assert.True(list.Count == len);
                Assert.True(list.Capacity >= list.Count);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TestData
        {
            public bool Bool;
            public float Float;
            public double Double;
            public int Int;
        }
    }
}

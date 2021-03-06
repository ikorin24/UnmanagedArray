﻿/*
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnmanageUtility;
using Xunit;

namespace Test
{
    public unsafe class UnmanagedListTest
    {
        [Fact]
        public void Ctor()
        {
            static void Ctor_<T>() where T : unmanaged
            {
                // default ctor
                using(var list = new UnmanagedList<T>()) {
                    Assert.True(list.Capacity > 0);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr == IntPtr.Zero);
                }

                // ctor of setting capacity
                using(var list = new UnmanagedList<T>(10)) {
                    Assert.True(list.Capacity == 10);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr == IntPtr.Zero);
                }

                // ctor of setting empty capacity
                using(var list = new UnmanagedList<T>(0)) {
                    Assert.True(list.Capacity == 0);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr == IntPtr.Zero);
                }

                // ctor of initializing list from T[]
                using(var list = new UnmanagedList<T>(new T[10])) {
                    Assert.True(list.Capacity == 10);
                    Assert.True(list.Count == 10);
                    Assert.True(list.Ptr != IntPtr.Zero);
                }

                // ctor of initializing list from ReadOnlySpan<T>
                using(var list = new UnmanagedList<T>(new T[10].AsSpan())) {
                    Assert.True(list.Capacity == 10);
                    Assert.True(list.Count == 10);
                    Assert.True(list.Ptr != IntPtr.Zero);
                }

                // ctor of initializing list from empty T[]
                using(var list = new UnmanagedList<T>(new T[0])) {
                    Assert.True(list.Capacity == 0);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr == IntPtr.Zero);
                }

                // ctor of initializing list from empty ReadOnlySpan<T>
                using(var list = new UnmanagedList<T>(ReadOnlySpan<T>.Empty)) {
                    Assert.True(list.Capacity == 0);
                    Assert.True(list.Count == 0);
                    Assert.True(list.Ptr == IntPtr.Zero);
                }

                // ctor of initializing list from IEnumerable<T>
                using(var list = new UnmanagedList<T>(Enumerable.Repeat(default(T), 10))) {
                    Assert.True(list.Count == 10);
                    Assert.True(list.Capacity >= 0);
                    Assert.True(list.Ptr != IntPtr.Zero);
                }

                Assert.Throws<ArgumentNullException>(() => new UnmanagedList<T>((T[])null!));

                // Negative value capacity throws an exception.
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
        public void Ptr()
        {
            var list = new UnmanagedList<int>();

            // Ptr is IntPtr.Zero when Count == 0
            Assert.True(list.Count == 0);
            Assert.Equal(IntPtr.Zero, list.Ptr);

            for(int i = 0; i < 10; i++) {
                list.Add(i);
                Assert.NotEqual(IntPtr.Zero, list.Ptr);
            }

            unsafe {
                var ptr = (int*)list.Ptr;
                for(int i = 0; i < 10; i++) {
                    Assert.Equal(i, ptr[i]);
                }
            }

            list.Dispose();

            // Ptr is IntPtr.Zero after Dispose()
            Assert.True(list.Count == 0);
            Assert.Equal(IntPtr.Zero, list.Ptr);
        }

        [Fact]
        public void Capacity()
        {
            using(var list = new UnmanagedList<int>(10)) {
                Assert.Equal(10, list.Capacity);
                Assert.True(list.Count == 0);

                list.Capacity = 20;
                Assert.Equal(20, list.Capacity);
                Assert.True(list.Count == 0);

                list.Capacity = 0;
                Assert.Equal(0, list.Capacity);
                Assert.True(list.Count == 0);
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 10))) {
                Assert.Equal(10, list.Count);
                Assert.True(list.Capacity >= 10);
                for(int i = 0; i < 10; i++) {
                    Assert.Equal(i, list[i]);
                }

                list.Capacity = 20;

                Assert.Equal(10, list.Count);
                Assert.Equal(20, list.Capacity);
                for(int i = 0; i < 10; i++) {
                    Assert.Equal(i, list[i]);
                }
            }

            using(var list = new UnmanagedList<int>(10)) {
                Assert.True(list.Count == 0);
                Assert.Equal(10, list.Capacity);

                list.Capacity = 0;
                Assert.True(list.Count == 0);
                Assert.True(list.Capacity == 0);
            }
        }

        [Fact]
        public void Indexer()
        {
            using(var list = new UnmanagedList<int>()) {
                Assert.Throws<ArgumentOutOfRangeException>(() => list[0] = default);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[1] = default);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = default);
            }

            var items = Enumerable.Range(0, 10).ToArray();
            using(var list = new UnmanagedList<int>(items.AsSpan())) {
                Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = default);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[10] = default);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[20] = default);

                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(i, list[i]);
                }

                var firstItem = list[0];
                for(int i = 0; i < list.Count - 1; i++) {
                    list[i] = list[i + 1];
                }
                list[list.Count - 1] = firstItem;

                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal((i + 1) % list.Count, list[i]);
                }
            }
        }

        [Fact]
        public void GetReference()
        {
            ref var nullRef = ref Unsafe.AsRef<int>((void*)null);
            
            using(var list = new UnmanagedList<int>()) {

                // GetReference() throws no exceptions when list is empty.
                // It returns reference to null.
                Assert.True(Unsafe.AreSame(ref list.GetReference(), ref nullRef));

                // GetReference(int) throws ArgumentOutOfRangeException
                Assert.Throws<ArgumentOutOfRangeException>(() => list.GetReference(0));

                list.Add(10);
                Assert.True(list.Count == 1);
                Assert.Equal(10, list.GetReference());
                Assert.Equal(10, list.GetReference(0));

                list.GetReference() = 20;
                Assert.Equal(20, list[0]);

                list.GetReference(0) = 0;
                Assert.Equal(0, list[0]);


                list.Add(1);
                list.Add(2);
                list.Add(3);
                list.Add(4);
                Assert.True(list.Count == 5);
                Assert.Equal(0, list.GetReference());
                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(i, list.GetReference(i));
                }
                Assert.Throws<ArgumentOutOfRangeException>(() => list.GetReference(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.GetReference(5));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.GetReference(10));
            }
        }

        [Fact]
        public void ListDispose()
        {
            static void ListDispose_<T>() where T : unmanaged
            {
                var list = new UnmanagedList<T>(new T[5].AsSpan());
                Assert.True(list.Count == 5);
                Assert.True(list.Capacity >= 5);
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

        [Fact]
        public void AddRange()
        {
            var itemsCount = 10;
            var itemsEnumerable = Enumerable.Range(0, itemsCount);
            var items = itemsEnumerable.ToArray();

            static void TestForSpan(ReadOnlySpan<int> span)
            {
                using(var list = new UnmanagedList<int>()) {
                    var len = 10;
                    for(int i = 0; i < len; i++) {
                        list.AddRange(span);
                        Assert.True(list.Count == span.Length * (i + 1));
                        Assert.True(list.Capacity >= list.Count);
                    }
                    for(int i = 0; i < list.Count; i++) {
                        Assert.True(list[i] == (i % len));
                    }
                }
            }

            static void TestForIEnumerable(IEnumerable<int> ienumerable, int count)
            {
                using(var list = new UnmanagedList<int>()) {
                    var len = 10;
                    for(int i = 0; i < len; i++) {
                        list.AddRange(ienumerable);
                        Assert.True(list.Count == count * (i + 1));
                        Assert.True(list.Capacity >= list.Count);
                    }
                    for(int i = 0; i < list.Count; i++) {
                        Assert.True(list[i] == (i % len));
                    }
                }
            }

            static void TestForSelf_AsSpan(ReadOnlySpan<int> initialItems)
            {
                using(var list = new UnmanagedList<int>(initialItems)) {
                    var len = 10;
                    var count = initialItems.Length;
                    for(int i = 0; i < len; i++) {
                        list.AddRange(list.AsSpan());
                        count += count;
                        Assert.True(list.Count == count);
                        Assert.True(list.Capacity >= list.Count);
                    }
                    for(int i = 0; i < list.Count; i++) {
                        Assert.True(list[i] == (i % len));
                    }
                }
            }

            static void TestForSelf_AsIEnumerable(ReadOnlySpan<int> initialItems)
            {
                using(var list = new UnmanagedList<int>(initialItems)) {
                    var len = 10;
                    var count = initialItems.Length;
                    for(int i = 0; i < len; i++) {
                        list.AddRange(list.AsEnumerable());
                        count += count;
                        Assert.True(list.Count == count);
                        Assert.True(list.Capacity >= list.Count);
                    }
                    for(int i = 0; i < list.Count; i++) {
                        Assert.True(list[i] == (i % len));
                    }
                }
            }

            // AddRange T[] as ReadOnlySpan<T>
            TestForSpan(items);

            // AddRange T[] as IEnumerable<T>
            TestForIEnumerable(items, items.Length);

            // AddRange List<T> as IEnumerable<T>
            TestForIEnumerable(items.ToList(), items.Length);

            // AddRange ReadOnlyCollection<T> as IEnumerable<T>
            TestForIEnumerable(items.ToList().AsReadOnly(), items.Length);

            // AddRange IEnumerable<T>
            TestForIEnumerable(itemsEnumerable, itemsCount);

            // AddRange self as ReadOnlySpan<T>
            TestForSelf_AsSpan(items.AsSpan());

            // AddRange self as IEnumerable<T>
            TestForSelf_AsIEnumerable(items.AsSpan());
        }

        [Fact]
        public void Extend()
        {
            foreach(var zeroFill in stackalloc[] { true, false }) {
                using(var list = new UnmanagedList<int>(0)) {
                    TestExtend(list, 10, zeroFill);
                }
                using(var list = new UnmanagedList<int>(10)) {
                    TestExtend(list, 5, zeroFill);
                    TestExtend(list, 5, zeroFill);
                    TestExtend(list, 5, zeroFill);
                }
                using(var list = new UnmanagedList<int>(30)) {
                    TestExtend(list, 50, zeroFill);
                }
            }

            static void TestExtend(UnmanagedList<int> list, int exCount, bool zeroFill)
            {
                var count = list.Count;
                var span = list.Extend(exCount, zeroFill);
                Assert.True(list.Capacity >= exCount + count);
                Assert.True(list.Count == exCount + count);
                Assert.True(span.Length == exCount);
                if(zeroFill) {
                    foreach(var item in span) {
                        Assert.True(item == default);
                    }
                }
                for(int i = 0; i < span.Length; i++) {
                    span[i] = i;
                }
                for(int i = 0; i < span.Length; i++) {
                    Assert.True(span[i] == list[count + i]);
                }
            }
        }

        [Fact]
        public void CopyTo()
        {
            var items = Enumerable.Range(0, 100).ToArray();
            var boolItems = items.Select(x => x % 3 == 0).ToArray();
            
            // basic copy to array
            using(var list = new UnmanagedList<int>(items.AsSpan())) {
                var copy = new int[items.Length];
                list.CopyTo(copy, 0);
                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(copy[i], list[i]);
                }
            }

            // copy to array with offset
            using(var list = new UnmanagedList<bool>(boolItems.AsSpan())) {
                var offset = 13;
                var copy = new bool[boolItems.Length + offset];
                list.CopyTo(copy, offset);
                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(copy[i + offset], list[i]);
                }
            }

            // in case of empty list
            using(var list = new UnmanagedList<double>()) {
                var copy = new double[0];
                
                // no exceptions thrown
                list.CopyTo(copy, 0);
            }
        }

        [Fact]
        public void AsSpan()
        {
            using(var list = new UnmanagedList<int>()) {
                Assert.True(list.AsSpan().IsEmpty);
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                var span = list.AsSpan();
                for(int i = 0; i < span.Length; i++) {
                    Assert.Equal(span[i], list[i]);
                    Assert.True(Unsafe.AreSame(ref span[i], ref list.GetReference(i)));
                }
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                var span = list.AsSpan(20);
                for(int i = 0; i < span.Length - 20; i++) {
                    Assert.Equal(span[i], list[i + 20]);
                    Assert.True(Unsafe.AreSame(ref span[i], ref list.GetReference(i + 20)));
                }
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                var span = list.AsSpan(20, 80);
                for(int i = 0; i < 80; i++) {
                    Assert.Equal(span[i], list[i + 20]);
                    Assert.True(Unsafe.AreSame(ref span[i], ref list.GetReference(i + 20)));
                }

                Assert.True(list.AsSpan(20, 0).IsEmpty);
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 10).ToArray().AsSpan())) {
                Assert.True(list.Count == 10);
                var span = list.AsSpan();
                Assert.True(list.AsSpan(10).IsEmpty);
                Assert.True(list.AsSpan(10, 0).IsEmpty);
                Assert.True(list.AsSpan(0).SequenceEqual(span));
                Assert.True(list.AsSpan(0, 10).SequenceEqual(span));
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.AsSpan(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.AsSpan(20, 81));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.AsSpan(20, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.AsSpan(-1, 100));
            }

            {
                var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan());
                list.Dispose();
                // AsSpan() throws no exceptions after Dispose().
                Assert.True(list.AsSpan().IsEmpty);
            }
        }

        [Fact]
        public void IndexOf()
        {
            using(var list = new UnmanagedList<int>(Enumerable.Range(10, 100).ToArray().AsSpan())) {
                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(i, list.IndexOf(i + 10));
                }
                Assert.Equal(-1, list.IndexOf(3));
                Assert.Equal(-1, list.IndexOf(200));

                list.Clear();

                for(int i = 0; i < 100; i++) {
                    Assert.Equal(-1, list.IndexOf(i + 10));
                }
            }
        }

        [Fact]
        public void RemoveAt()
        {
            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                for(int i = 0; i < 100; i++) {
                    Assert.Equal(i, list[0]);
                    list.RemoveAt(0);
                    Assert.Equal(100 - i - 1, list.Count);
                }

                // list is empty

                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(1));
            }

            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(100));

                Assert.Equal(100, list.Count);
                Assert.True(list.Capacity >= list.Count);

                for(int i = 0; i < 30; i++) {
                    Assert.Equal(70 + i, list[70]);
                    list.RemoveAt(70);
                    Assert.Equal(100 - i - 1, list.Count);
                }

                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(i, list[i]);
                }
            }
        }

        [Fact]
        public void Clear()
        {
            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                var capacity = list.Capacity;
                Assert.True(list.Count == 100);
                list.Clear();
                Assert.True(list.Count == 0);
                Assert.Equal(capacity, list.Capacity);
            }
        }

        [Fact]
        public void Contains()
        {
            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {
                Assert.Equal(100, list.Count);
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(list.Contains(i) == true);
                }

                Assert.True(list.Contains(-1) == false);
                Assert.True(list.Contains(-10) == false);
                Assert.True(list.Contains(100) == false);
                Assert.True(list.Contains(1000) == false);

                list.Clear();

                for(int i = 0; i < 100; i++) {
                    Assert.True(list.Contains(i) == false);
                }
            }
        }

        [Fact]
        public void Remove()
        {
            using(var list = new UnmanagedList<int>()) {
                list.Add(5);
                Assert.True(list.Count == 1);
                Assert.True(list[0] == 5);

                Assert.True(list.Remove(5));
                Assert.True(list.Count == 0);
            }

            using(var list = new UnmanagedList<int>()) {
                for(int i = 0; i < 50; i++) {
                    list.Add(i);
                }
                for(int i = 0; i < 50; i++) {
                    Assert.True(list.Remove(i));
                    Assert.True(list.Count == 50 - i - 1);
                }
            }

            using(var list = new UnmanagedList<int>()) {
                list.Add(0);
                list.Add(1);
                list.Add(2);
                list.Add(3);
                list.Add(4);
                list.Add(5);
                Assert.True(list.Count == 6);

                Assert.True(list.Remove(4));
                Assert.True(list.Count == 5);
                Assert.True(list[0] == 0);
                Assert.True(list[1] == 1);
                Assert.True(list[2] == 2);
                Assert.True(list[3] == 3);
                Assert.True(list[4] == 5);

                Assert.True(list.Remove(2));
                Assert.True(list.Count == 4);
                Assert.True(list[0] == 0);
                Assert.True(list[1] == 1);
                Assert.True(list[2] == 3);
                Assert.True(list[3] == 5);

                // false if not exist
                Assert.False(list.Remove(2));

                Assert.True(list.Remove(5));
                Assert.True(list.Count == 3);
                Assert.True(list[0] == 0);
                Assert.True(list[1] == 1);
                Assert.True(list[2] == 3);

                Assert.True(list.Remove(0));
                Assert.True(list.Count == 2);
                Assert.True(list[0] == 1);
                Assert.True(list[1] == 3);

                Assert.True(list.Remove(1));
                Assert.True(list.Count == 1);
                Assert.True(list[0] == 3);

                Assert.True(list.Remove(3));
                Assert.True(list.Count == 0);

                // false if not exist
                Assert.False(list.Remove(5));
            }
        }

        [Fact]
        public void Enumerate()
        {
            // Iterate in three ways
            using(var list = new UnmanagedList<int>(Enumerable.Range(0, 100).ToArray().AsSpan())) {

                // Iterate by foreach
                {
                    var i = 0;
                    foreach(var item in list) {
                        Assert.Equal(i, item);
                        i++;
                    }
                }

                // Iterate by foreach as IEnumerable<T>
                {
                    var i = 0;
                    foreach(var item in list.AsEnumerable()) {
                        Assert.Equal(i, item);
                        i++;
                    }
                }

                // Iterate by foreach as IEnumerable
                {
                    var i = 0;
                    foreach(var item in (System.Collections.IEnumerable)list) {
                        Assert.Equal(i, (int)item!);
                        i++;
                    }
                }
            }


            // Iterate empty list in three ways
            using(var list = new UnmanagedList<int>()) {
                Assert.True(list.Count == 0);

                // Iterate by foreach
                foreach(var item in list) {
                    throw new Exception("invalid");
                }

                // Iterate by foreach as IEnumerable<T>
                foreach(var item in list.AsEnumerable()) {
                    throw new Exception("invalid");
                }

                // Iterate by foreach as IEnumerable
                foreach(var item in (System.Collections.IEnumerable)list) {
                    throw new Exception("invalid");
                }
            }
        }

        [Fact]
        public void Insert()
        {
            using(var list = new UnmanagedList<int>()) {
                list.Insert(0, 1);
                list.Insert(0, 2);
                list.Insert(0, 3);
                list.Insert(0, 4);
                list.Insert(2, 5);
                list.Insert(5, 6);

                // list : [4, 3, 5, 2, 1, 6]
                Assert.Equal(6, list.Count);
                Assert.Equal(4, list[0]);
                Assert.Equal(3, list[1]);
                Assert.Equal(5, list[2]);
                Assert.Equal(2, list[3]);
                Assert.Equal(1, list[4]);
                Assert.Equal(6, list[5]);

            }
        }

        [Fact]
        public void InsertRange()
        {
            var initial = Enumerable.Range(10, 20).ToArray();

            var itemsCount = 10;
            var itemsEnumerable = Enumerable.Range(0, itemsCount);
            var items = itemsEnumerable.ToArray();

            {
                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = 0;
                    list.InsertRange(insert, items);
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, items.Length).SequenceEqual(items));
                    Assert.True(list.AsSpan(insert + items.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = 5;
                    list.InsertRange(insert, items);
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, items.Length).SequenceEqual(items));
                    Assert.True(list.AsSpan(insert + items.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = initial.Length;
                    list.InsertRange(insert, items);
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert).SequenceEqual(items));
                }
            }

            {
                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = 0;
                    list.InsertRange(insert, itemsEnumerable);
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, items.Length).SequenceEqual(items));
                    Assert.True(list.AsSpan(insert + items.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = 5;
                    list.InsertRange(insert, itemsEnumerable);
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, items.Length).SequenceEqual(items));
                    Assert.True(list.AsSpan(insert + items.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = initial.Length;
                    list.InsertRange(insert, itemsEnumerable);
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert).SequenceEqual(items));
                }
            }

            {
                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = 0;
                    list.InsertRange(insert, items.AsSpan());
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, items.Length).SequenceEqual(items));
                    Assert.True(list.AsSpan(insert + items.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = 5;
                    list.InsertRange(insert, items.AsSpan());
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, items.Length).SequenceEqual(items));
                    Assert.True(list.AsSpan(insert + items.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var insert = initial.Length;
                    list.InsertRange(insert, items.AsSpan());
                    Assert.True(list.Count == initial.Length + items.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert).SequenceEqual(items));
                }
            }

            // Test for inserting to self 
            {
                using(var list = new UnmanagedList<int>(initial)) {
                    var c = list.Count;
                    list.InsertRange(0, list.AsSpan());
                    Assert.True(list.Count == initial.Length * 2);
                    Assert.True(list.AsSpan(0, c).SequenceEqual(list.AsSpan(c, c)));
                }

                using(var list = new UnmanagedList<int>(initial)) {
                    var copy = list.AsSpan(0, 10).ToArray();
                    var insert = 5;
                    list.InsertRange(insert, list.AsSpan(0, 10));
                    Assert.True(list.Count == initial.Length + copy.Length);
                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, copy.Length).SequenceEqual(copy));
                    Assert.True(list.AsSpan(insert + copy.Length).SequenceEqual(initial.AsSpan(insert)));
                }



                using(var list = new UnmanagedList<int>(initial.Length * 2 + 10)) {
                    list.AddRange(initial);

                    var span = list.AsSpan(8, 10);
                    var copy = span.ToArray();
                    var insert = 5;
                    list.InsertRange(insert, span);
                    Assert.True(list.Count == initial.Length + copy.Length);

                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, copy.Length).SequenceEqual(copy));
                    Assert.True(list.AsSpan(insert + copy.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial.Length * 2 + 10)) {
                    list.AddRange(initial);

                    var span = list.AsSpan(3, 10);
                    var copy = span.ToArray();
                    var insert = 15;
                    list.InsertRange(insert, span);
                    Assert.True(list.Count == initial.Length + copy.Length);

                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, copy.Length).SequenceEqual(copy));
                    Assert.True(list.AsSpan(insert + copy.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial.Length * 2 + 10)) {
                    list.AddRange(initial);

                    var span = list.AsSpan(3, 10);
                    var copy = span.ToArray();
                    var insert = 6;
                    list.InsertRange(insert, span);
                    Assert.True(list.Count == initial.Length + copy.Length);

                    Assert.True(list.AsSpan(0, insert).SequenceEqual(initial.AsSpan(0, insert)));
                    Assert.True(list.AsSpan(insert, copy.Length).SequenceEqual(copy));
                    Assert.True(list.AsSpan(insert + copy.Length).SequenceEqual(initial.AsSpan(insert)));
                }

                using(var list = new UnmanagedList<int>(initial.Length * 2 + 10)) {
                    list.AddRange(initial);

                    var span = list.AsSpan(3, 0);
                    var copy = span.ToArray();
                    list.InsertRange(6, span);
                    Assert.True(list.AsSpan().SequenceEqual(initial));
                }
            }
        }

        [Fact]
        public void Linq()
        {
            var items = Enumerable.Range(0, 10).ToArray();

            using(var list = Enumerable.Range(0, 10).ToUnmanagedList()) {
                Assert.True(list.AsSpan().SequenceEqual(items));

                Assert.True(
                    list.Where(x => x % 2 == 0)
                        .Select(x => x * 3)
                        .SequenceEqual(
                    items.Where(x => x % 2 == 0)
                         .Select(x => x * 3)));
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

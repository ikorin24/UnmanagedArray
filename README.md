# Unmanaged Array

[![GitHub license](https://img.shields.io/github/license/ikorin24/UnmanagedArray?color=FA77FF)](https://github.com/ikorin24/UnmanagedArray/blob/master/LICENSE)
[![nuget](https://img.shields.io/badge/nuget-v2.1.3-FA77FF)](https://www.nuget.org/packages/UnmanagedArray)

An Effective tool for unmanaged array in C#.

## About

Array in C# is allocated in managed memory.

```UnmanagedArray<T>``` in this library is allocated in unmanaged memory.

In other words, items in `UnmanagedArray<T>` is not collected by Garbage Collection.

### Supported Types

The only type of item `UnmanagedArray<T>` supports is `unmanaged` type.

`unmanaged` type is `int`, `float`, recursive-unmanaged struct, and so on.

`string`, and other types which are `class` are NOT SUPPORTED.

(because reference types are allocated in managed memory on C#.)

## Building from Source

```sh
$ git clone https://github.com/ikorin24/UnmanagedArray.git
$ cd UnmanagedArray
$ dotnet build src/UnmanagedArray/UnmanagedArray.csproj -c Release
```

## Installation

Install from Nuget by package manager console (in Visual Studio).

https://www.nuget.org/packages/UnmanagedArray

```
PM> Install-Package UnmanagedArray
```

## How to Use

The way of use is similar to normal array.

Unmanaged resources are release when an instance goes through the scope.

```cs
using UnmanageUtility;

// UnmanagedArray releases its memories when it goes through the scope.
using(var array = new UnmanagedArray<int>(10))
{
    for(int i = 0;i < array.Length;i++)
    {
        array[i] = i;
    }
}
```

If not use the `using` scope, you can release the memories by `Dispose()` method.

```cs
var array = new UnmanagedArray<int>(10);
array[3] = 100;
array.Dispose();       // The memories allocated in unmanaged is released here.
```

Of cource, LINQ is supported.

```cs
using(var array = Enumerable.Range(0, 10).ToUnmanagedArray())
using(var array2 = array.Where(x => x >= 5).ToUnmanagedArray()) {
    for(int i = 0; i < array2.Length; i++) {
        Console.WriteLine(array2[i]);       // 5, 6, 7, 8, 9
    }
}
```

***NOTICE***

`UnmanagedArray<T>` has Finalizer and releases its unmanaged resources automatically when you forget releasing that.

However, you have to release them explicitly ( by `using` scope or `Dispose()` ).

### New Feature of ver 2.1.0

`UnmanagedList<T>` is available, which the way of use is similar to `List<T>`.

```cs
using(var list = new UnmanagedList<int>())
{
    list.Add(4);
    list.Add(9);
    foreach(var num in list)
    {
        Console.WriteLine(num);
    }
}
```

## License and Credits

This is under [MIT license](https://github.com/ikorin24/UnmanagedArray/blob/master/LICENSE).

This software includes the work that is distributed in the Apache License 2.0.

Apache License 2.0 (http://www.apache.org/licenses/LICENSE-2.0)

## Author

[github: ikorin24](https://github.com/ikorin24)

## Release Note

### 2020/01/11 v1.0.0

[![nuget](https://img.shields.io/badge/nuget-v1.0.0-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/1.0.0)

- First release

### 2020/01/12 v1.0.1

[![nuget](https://img.shields.io/badge/nuget-v1.0.1-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/1.0.1)

- Performance improvement of iteration by `foreach`, that is as faster as T[] (normal array).

### 2020/01/15 v1.0.2

[![nuget](https://img.shields.io/badge/nuget-v1.0.2-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/1.0.2)

- Great performance improvement of accessing to the item by index. (`array[i]`)

### 2020/04/30 v1.0.3

[![nuget](https://img.shields.io/badge/nuget-v1.0.3-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/1.0.3)

- Performance improvement.
- Add `GC.AddMemoryPressure` in constructor and `GC.RemoveMemoryPressure` in destructor.

### 2020/04/30 v2.0.0

[![nuget](https://img.shields.io/badge/nuget-v2.0.0-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.0.0)

- Change namespace into `UnmanageUtility`.

### 2020/06/07 v2.0.1

[![nuget](https://img.shields.io/badge/nuget-v2.0.1-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.0.1)

### 2020/07/27 v2.1.0-rc

[![nuget](https://img.shields.io/badge/nuget-v2.1.0_rc-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.1.0-rc)

- Add `UnmanagedList<T>`.

### 2020/10/05 v2.1.0

[![nuget](https://img.shields.io/badge/nuget-v2.1.0-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.1.0)

- Performance improvement.
- Add some methods.

### 2020/11/26 v2.1.1

[![nuget](https://img.shields.io/badge/nuget-v2.1.1-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.1.1)

- Add `UnmanagedArray<T>.Empty` static property.
- Add property setter of `UnmanagedList<T>.Capacity`.

### 2021/01/05 v2.1.2

[![nuget](https://img.shields.io/badge/nuget-v2.1.2-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.1.2)

- Add `UnmanagedArray<T>.AsSpan` overload methods.
- Package for multi target frameworks. (net48, netcoreapp3.1, net5.0, netstandard2.0, netstandard2.1)
- Fix small bugs.

### 2021/02/10 v2.1.3

[![nuget](https://img.shields.io/badge/nuget-v2.1.3-FA77FF)](https://www.nuget.org/packages/UnmanagedArray/2.1.3)

- Add `UnmanagedList<T>.Extend` method.

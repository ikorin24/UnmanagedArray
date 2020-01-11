# Unmanaged Array

[![GitHub license](https://img.shields.io/github/license/ikorin24/UnmanagedArray?color=FA77FF)](https://github.com/ikorin24/UnmanagedArray/blob/master/LICENSE)
[![nuget](https://img.shields.io/badge/nuget-v1.0.1-FA77FF)](https://www.nuget.org/packages/UnmanagedArray)

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

## Requirements and Dependencies (On Building)

- .NET Standard 2.0
- C# 8.0

## Building from Source

```sh
$ git clone https://github.com/ikorin24/UnmanagedArray.git
$ cd UnmanagedArray
$ dotnet build src/UnmanagedArray/UnmanagedArray.csproj -c Release

# ----> src/UnmanagedArray/bin/Release/netstandard2.0/UnmanagedArray.dll
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

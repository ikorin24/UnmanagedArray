# Unmanaged Array

This is the library by which you can use unmanaged array in C#.

## About

Array in C# is allocated in managed memory.

```UnmanagedArray<T>``` whith this library supports are allocated in unmanaged memory.

In other words, items in ```UnmanagedArray<T>``` is not collected by Garbage Collection.

### Supported Types

The only type of item this library supported is ```unmanaged``` type.

```unmanaged``` type is '```int```', '```float```', recursive-unmanaged struct, and so on. 

'```string```', and other types which are ```class``` are NOT SUPPORTED.

(because reference types are allocated in managed memory on C#.)

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

If not use the ```using``` scope, you can release the memories by ```Free()``` method.

```cs
var array = new UnmanagedArray<int>(10);
array[3] = 100;
array.Free();       // The memories allocated in unmanaged is released here.
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

```UnmanagedArray<T>``` has Finalizer and releases its unmanaged resources automatically when you forget releasing that.

However, you have to release them explicitly ( by ```using``` scope or ```Free()``` ).


## Author

[github: ikorin24](https://github.com/ikorin24)

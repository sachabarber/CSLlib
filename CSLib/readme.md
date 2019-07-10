# Overview

# CSLib
This is a standard .NET framework 4.7.2 Class Library. 

## Threading
I have made the assumtion that the code would need to be multithreaded. You could do this lots of ways, such as using 

``` csharp
private object _synclock = new Object();

lock(_synlock) 
{
  ...
}
```

The problem with this and this example spec is that we are asked to call event handlers in the `ReceiveTick()` method. 
Event handlers are foreign code, and the class with the events can make NO assumption at all about what the consumer of the events may put in the event
handler code. As such you need to be careful of locks around event handler invocation. 

As such I went for a simple solution of 

- finer grained lock statements. Dont lock the whole method
- Interlocked.Exchange

## Logging
I also added a simple `ILoggerService` which in reality would probably be one of the off the shelf ones like **SeriLog** or **Log4Net** abstractions

## Tests
The enclosed image [TestsRunning.png](TestsRunning.png) show the tests running. I used
- NUnit
- Fluent Assertions
- FakeItEasy
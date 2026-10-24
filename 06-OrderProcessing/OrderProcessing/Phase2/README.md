## Phase 2 Questions

### 1. What is the difference between starting a task and awaiting it?

Starting a task means calling the async method. It begins running right
away, synchronously, until it reaches its first `await` on something that
is not finished, and then it returns a `Task` to me. Awaiting is waiting
for that task to finish and getting its result or its exception.

My own log shows it. When I started all 15 processing tasks in a loop,
all 15 "ProductRepository -> Opening connection..." lines were printed
within about 3 ms (19:47:33.349 to 19:47:33.351), before any of them had
finished. The waiting only happened later in the foreach with `await`.
Starting all of them first and awaiting after is what made the total time
9.6 s instead of running 15 orders one after another.

### 2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?

No. A Task is only an object that represents work that will finish in the
future. Most of my tasks are waiting for Task.Delay or for file I/O, and
no thread is dedicated to them. Task.Delay uses a timer: while it waits,
no thread is blocked, and when the timer fires the rest of the method is
scheduled on a free ThreadPool thread.

Evidence: after `await Task.Delay(1000)` I printed the status of every
task and they were all `WaitingForActivation`, not `Running`. They were
suspended at an await, not occupying a thread. Only Task.Run (the invoice
rendering) puts work on a ThreadPool thread.

### 3. What happens if you call .Result or .Wait() on an unfinished task?

The calling thread is blocked until the task finishes. `await` frees the
thread, while `.Result` / `.Wait()` hold it. I tested it in
BlockingDemo.cs, the only place in the project allowed to use them:

    [20:12:23.218] [Thread 18] Before .Result: this thread is about to block
    [20:12:24.723] [Thread 18] After .Result: got 42 after 1504 ms, on the SAME thread (it was blocked the whole time)
    [20:12:25.059] [Thread 18] .Wait() threw AggregateException; the real error is inside: "Something failed inside the task"

Thread 18 did nothing for the whole 1504 ms. With `await`, it would have
gone back to the ThreadPool and could have run other work.

It is dangerous for two reasons. First, blocked threads are wasted: with
many requests at the same time, the ThreadPool can run out of free
threads. Second, in apps with a synchronization context (UI apps, old
ASP.NET) it can cause a deadlock: the thread waits for the task, while
the task waits for that same thread to continue.

Also, `.Wait()` wraps the real exception inside an AggregateException (as
my last log line shows), while `await` throws the original exception.

### 4. What happens to an exception thrown inside a task that is never awaited?

The exception is stored inside the Task object. If nobody observes it (no
await, no Wait(), and nobody reads Task.Exception), it is not shown where
it happened and my program keeps running as if nothing went wrong.

According to Microsoft's documentation, when such a task is cleaned up
the UnobservedTaskException event is raised, but by default the process is
not terminated, and the exception is ignored afterwards even if no event
handler observes it. When this happens depends on garbage collection, so
the error can appear late or never be noticed. In .NET Framework 4 the
default was different: the process was terminated.

That is why every task in my pipeline is awaited. In Phase 2 I awaited
them one by one inside try/catch, so the failure of order #7 ("Shipping
provider unavailable") was seen, logged and handled instead of being lost.

Sources:
- https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler.unobservedtaskexception
- https://learn.microsoft.com/dotnet/framework/configure-apps/file-schema/runtime/throwunobservedtaskexceptions-element

### 5. When should you use Task.Run(), and when should you avoid it?

Use it for CPU-bound work, where a thread is really busy computing. In my
project I used it once, in InvoiceGenerator, for the 20,000,000-iteration
loop that simulates rendering a PDF. Task.Run moves that loop to a
ThreadPool thread so the calling thread is not blocked.

Avoid it for I/O-bound work (database calls, files, HTTP, Task.Delay). The
time there is spent waiting, not computing, so `await` alone already frees
the thread. Wrapping it in Task.Run would just take a pool thread to wait.
Also avoid it just to make a method "look async".

### 6. What is the difference between Task and Task<T>?

Task represents an asynchronous operation that finishes without a result
(for example `SaveAsync`). Task<T> represents one that finishes with a
value of type T (for example `Task<bool> ProcessValidatedOrderAsync` or
`Task<decimal> CalculateSubTotalAsync`). With `await` on a Task<T> I get
the T; with `await` on a Task I get nothing. Both carry the status and
any exception.

### 7. Why can a lock block not contain an await?

The compiler rejects it with error CS1996 ("Cannot await in the body of a
lock statement"). Microsoft's documentation explains that async code
inside a lock is hard to make reliable and risks deadlocks, so the
compiler does not allow it. The problem is that the lock stays held while
the method is suspended at the await. Other code that needs the same lock
has to wait for an unknown time, and the method may resume later on a
different thread. My own logs show this: the thread id often changes after
an await (for example thread 14 before and thread 12 after in order #1).

The fix is to keep awaits outside the lock block. In my JsonFileStore I
needed to hold exclusive access across awaits (read file -> modify ->
write file), so I used SemaphoreSlim with WaitAsync() instead of lock, one
semaphore per file. Waiting on it does not block a thread, so it works
with await.

Sources:
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs1996
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/lock-semantics

### 8. Why did the unprotected stock reservation oversell, and how did you prevent it?

ReserveStockUnsafeAsync does read stock -> check -> wait -> write as
separate steps. Both orders read stock = 1 before either of them wrote,
both passed the check, and both wrote 0. My output:

    Order #10 got the keyboard: True
    Order #11 got the keyboard: True
    Keyboard stock in products.json: 0

Two orders got the last keyboard, and the file ended with 0 as if only one
was sold (a lost update). The file store itself was not corrupted; the
business rule "check then take" was not atomic.

The fix (ReserveStockAsync) does the check and the subtraction inside
JsonFileStore.UpdateAsync, which holds a per-file SemaphoreSlim for the
whole read -> modify -> write. After the fix one order got True, the other
False, and the stock was 0.
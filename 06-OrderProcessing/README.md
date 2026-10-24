## Phase 1 Questions

### 1. Why are the sync and async times almost the same when orders are awaited one after another?

Because await only frees the thread it does not make the work run in
parallel My loop awaits each order before starting the next one so every
delay still happens one after another. An order takes the same total
delay time in both versions

In my run the sync time was 34.7 s and the async time was 53.7 s. The
difference of about 19 s is not a speed difference: the async run saves
every order to orders.json (about 0.9 s per save x 20 orders), while the
sync run does not save anything, as the assignment requires. Async only
becomes faster when independent operations are started together (Phase 2)

### 2. Is the thread ID before an await always the same as after it?

No. In my async run, order #1:

    [14:33:16.829] [Thread 14] CustomerRepository -> Opening connection...
    [14:33:17.032] [Thread 12] CustomerRepository -> Executing query for Customer #1...
    [14:33:17.454] [Thread 14] CustomerRepository -> Customer #1 found (Ahmed Ali)

The code before await Task.Delay(200) ran on thread 14 and the code
after it continued on thread 12. When the delay finishes, the rest of the
method (the continuation) is scheduled on whichever ThreadPool thread is
free, not necessarily the original one. In the sync run every line is on
[Thread 8], because Thread.Sleep never lets the thread go.

(It can also stay the same, since the pool may pick the same free thread.
So the ID is not guaranteed either way.)

### 3. What does the thread do while Task.Delay() / Thread.Sleep() is waiting?

- Task.Delay: no thread waits. The method returns to its caller at the
  `await`, the thread goes back to the ThreadPool and can run other work,
  and a timer resumes the method later.
- Thread.Sleep: the thread is blocked and does nothing for the whole time.
  It cannot be used by anyone else. This is why the sync version needs one
  thread per order in Phase 3.

### 4. Why should a repository method return Task<T> instead of T?

A database or file call takes time. `Task<T>` represents a result that
will exist later, so the method can return immediately and the caller
decides when to await it. Returning plain `T` would force the method to
block the thread until the result is ready. A Task also lets the caller
await it, start several together, pass a CancellationToken, and get
exceptions through the task.

### 5. File.ReadAllText vs File.ReadAllTextAsync

ReadAllText blocks the calling thread until the whole file is read.
ReadAllTextAsync returns a Task<string>, so the caller can await it and
the calling thread is free to do other work while the read is pending.

Does the async version occupy a thread while the disk is reading? The
calling thread is not blocked. According to Microsoft's documentation,
the underlying implementation may use operating system async I/O or
thread pool threads depending on the platform and configuration, so I
cannot say that no thread is involved at all. For finer control, the docs
recommend FileStream with the async option, which does async I/O at the
OS level and avoids blocking a thread pool thread in many cases.

Sources:
- https://learn.microsoft.com/en-us/dotnet/api/system.io.file.readalltextasync
- https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/using-async-for-file-access
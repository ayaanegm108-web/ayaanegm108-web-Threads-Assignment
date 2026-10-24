Student name: Aya Negm
Group / ID: 1

## 1. How to run, and project structure

### Requirements
.NET 10 SDK. The project targets `net10.0`.

### Run commands
From the repository root:

    dotnet run --project 06-OrderProcessing/OrderProcessing -- phase1
    dotnet run --project 06-OrderProcessing/OrderProcessing -- phase2
    dotnet run --project 06-OrderProcessing/OrderProcessing -- phase3
    dotnet run --project 06-OrderProcessing/OrderProcessing -- phase4
    dotnet run --project 06-OrderProcessing/OrderProcessing -- all
    dotnet run --project 06-OrderProcessing/OrderProcessing -- fireforget-exit

With no argument, a numbered menu is shown. Add `--cancel-after <ms>` to cancel a run automatically,
for example `-- phase3 --cancel-after 4000`. Ctrl+C also cancels the current run instead of killing the process.

Phase 3 also runs the sync-vs-async comparison (Task 9.6) and the fire-and-forget demo (Task 9.7)
before the 20-order checkout. They are skipped when `--cancel-after` is used.

### Where the Data folder is created
`AppContext.BaseDirectory/Data`, which after `dotnet run` is
`06-OrderProcessing/OrderProcessing/bin/Debug/net10.0/Data`. Every phase starts with
`DataSeeder.ResetAsync()`, which deletes and recreates this folder. It is not committed to the repository;
the final files of each phase are copied to `sample-output/`.

### Project structure and the role of each class

    OrderProcessing/
    ├── Program.cs                 run modes, menu, Ctrl+C and --cancel-after
    ├── Shared/
    │   ├── Models/                plain data classes and enums
    │   ├── Storage/
    │   │   ├── DataFiles.cs       file names (const) and folder paths
    │   │   ├── DataSeeder.cs      seed data and ResetAsync
    │   │   └── JsonFileStore.cs   the only class that reads or writes the JSON files
    │   ├── Logging/Log.cs         thread-safe coloured logging, tracks distinct thread ids
    │   └── Exceptions/            the six custom exceptions
    ├── Phase1/                    repositories, OrderValidator, their Sync versions, Phase1Runner
    ├── Phase2/                    PricingService, OrderPricer, RaceConditionDemo, OrderProcessorV2,
    │                              InvoiceGenerator, ReportGenerator, BlockingDemo, Phase2Runner
    ├── Phase3/                    CustomerService, InventoryService, PaymentGateway, EmailService,
    │                              OrderProcessor, Sync* versions, SyncVsAsyncComparison,
    │                              FireAndForgetDemo, Phase3Runner
    └── Phase4/                    (Phase 4 — TODO)

- **JsonFileStore**: reads and writes the JSON files (async and sync versions). No delays inside.
- **Repositories** (Customer, Product, Coupon, Order, IncomingOrder): simulate DB latency, then use the store.
- **OrderValidator**: the six validation checks in the required order.
- **PricingService / OrderPricer**: price rules; the discount and shipping calls run at the same time.
- **InventoryService**: reserves all items of an order or none, and releases stock.
- **PaymentGateway** (`IPaymentGateway`) and **EmailService** (`IEmailService`): external services with
  failures injected through constructors.
- **OrderProcessor**: the six-step checkout with compensation (refund, release stock) and cancellation.

## 2. Storage design

### Files
`customers.json`, `products.json`, `coupons.json`, `incoming_orders.json` (seeded),
`orders.json`, `payments.json`, `emails.json`, `email_retry.json` (start as `[]`),
plus `invoices/` and `reports/` folders. File names are `const string` fields in `DataFiles`.

### JSON format
Each file is one JSON array, written with `System.Text.Json` and one shared static
`JsonSerializerOptions`: indented, camelCase, and `JsonStringEnumConverter` so enums are stored as text
(`"Completed"`). UTF-8 without BOM. An empty collection is written as `[]`. A missing file reads as an
empty list; invalid JSON throws `DataFileCorruptedException` with the file name.

### How the files stay correct under concurrent access
- **One `SemaphoreSlim(1,1)` per file name.** Every read, write and update takes the file's semaphore with
  `WaitAsync(ct)`. I used a semaphore and not `lock` because a `lock` block cannot contain `await`.
- **`UpdateAsync(file, mutate, ct)`** does read → change → write while holding the semaphore, so two tasks can
  never both read the old content and overwrite each other (no lost update). Check-then-change rules such as
  "reserve stock only if stock >= qty" run inside the `mutate` delegate for the same reason.
- **Atomic writes.** The JSON is written to `file.json.tmp` first, then `File.Move(temp, path, overwrite: true)`
  replaces the real file in one step. A crash in the middle of a write leaves the old complete file, never a
  half-written one. If a write fails or is cancelled, the temp file is deleted.
- **Cancellation:** the token is checked just before the replace, so a cancelled run never leaves a broken file.

## 3. Performance table

| Test | Time | Distinct threads | ThreadPool.ThreadCount |
|---|---|---|---|
| Phase 1 sync (20 orders, one by one, not saved) | 34.7 s | TODO | TODO |
| Phase 1 async (20 orders, one by one, saved) | 53.7 s | TODO | TODO |
| Phase 2 pricing, order #1, sequential | TODO | TODO | TODO |
| Phase 2 pricing, order #1, concurrent | TODO | TODO | TODO |
| Phase 2 concurrent processing (15 tasks) | 9.6 s | TODO | TODO |
| Phase 3 sync, orders #1–#10 (10 `Thread`s) | 11.5 s | 10 | 4 |
| Phase 3 async, orders #1–#10 (10 tasks) | 9.7 s | 7 | 7 |
| Phase 3 full checkout, 20 orders | 13.7 s | TODO | TODO |
| Phase 4 customer dashboard | TODO | TODO | TODO |
| Phase 4 shipping quotes | TODO | TODO | TODO |
| Phase 4 fraud scoring, sequential | TODO | TODO | TODO |
| Phase 4 fraud scoring, parallel | TODO | TODO | TODO |
| Phase 4 email re-send | TODO | TODO | TODO |

Note: in the Phase 3 sync run `ThreadPool.ThreadCount` is only 4 because those 10 workers were created with
`new Thread(...)` and do not come from the pool. The meaningful number for the sync version is
"distinct threads = 10".

## 4. Screenshots

All screenshots are in `screenshots/`:

| Item | File |
|---|---|
| Race demo (unsafe vs safe) | TODO |
| Faulted task (order #7, shipping unavailable) | TODO |
| Payment timeout (order #5) | TODO |
| Refund (order #13) | TODO |
| Email retry (orders #3 and #17) | TODO |
| Fire-and-forget demo, before and after the fix | TODO |
| Fire-and-forget, process exits (`emails.json` empty) | TODO |
| Sync vs async summary table | TODO |
| Phase 4 tasks 10.1 – 10.4 | TODO |
| One cancelled run (`--cancel-after`) | TODO |

## 5. Problems I found while building it, and how I solved them

1. **Lost updates in the JSON files.** Two tasks reading the same file and then writing it can overwrite
   each other. Solved with one `SemaphoreSlim` per file and an `UpdateAsync` that does read → change → write
   under it. A `lock` would not compile around the awaits.
2. **Half-written files.** A crash during `WriteAllText` would leave a broken file. Solved by writing a `.tmp`
   file and replacing the real one with `File.Move(..., overwrite: true)`.
3. **Overselling the last keyboard.** `ReserveStockUnsafeAsync` reads the stock, waits, then writes, so both
   orders passed the check. The safe version does the check and the subtraction inside `UpdateAsync`.
4. **Telling a payment timeout from a cancelled run.** Both end as `OperationCanceledException`. The payment
   uses a linked `CancellationTokenSource` with `CancelAfter(3000)`, and the catch uses
   `when (!ct.IsCancellationRequested)`: if the run was not cancelled, it was the timeout (PaymentFailed);
   otherwise it is a cancellation (Pending / "Cancelled").
5. **Cleanup must survive cancellation.** If the run is cancelled, a refund or a stock release using the same
   token would be cancelled too. The cleanup code (refund, release stock, save the final status) uses
   `CancellationToken.None`.
6. **Cancelled in the middle of a stock reservation.** The stock could already be subtracted in the file while
   the caller only sees the exception and never releases it. `InventoryService.ReserveStockAsync` catches the
   cancellation during the commit step, adds the quantities back, and then rethrows.
7. **Two concurrent calls, one fails.** In `PricingService.CalculateTotalAsync` the discount and shipping calls
   run together. If the first one throws, the second one is still awaited inside a try/catch, so its exception
   is never left unobserved.
8. **Mixed console colours.** Several threads writing at once could mix colours. `Log` writes the line and
   resets the colour inside one `lock`.
9. **Sync versions need their own store methods.** For Task 9.6 I added a synchronous `Update<T>` to
   `JsonFileStore`, so the sync services behave exactly like the async ones.
10. **Fire-and-forget demo signature.** The assignment shows `ProcessOrderAsync(Order, Customer)` without a
    token, but rule 6.6 says every async method takes a `CancellationToken` last, so I added `ct` and kept the
    rest of the broken code exactly as given.

TODO: add the Phase 4 problems here.

## 6. Answers to the questions

### Phase 1

PASTE here the five answers from your current `Phase1/README.md`, unchanged.

### Phase 2

PASTE here the eight answers from your current `Phase2/README.md`, unchanged.
Also add one sentence comparing the total time of Phase 2 (9.6 s) with Phase 1 (53.7 s), as Task 8.4 asks.

### Phase 3 — Task 9.6: sync vs async (orders #1–#10, failures disabled)

| Version | Time | Distinct threads | ThreadPool.ThreadCount | Completed | Rejected |
|---|---|---|---|---|---|
| Sync (10 threads) | 11.5 s | 10 | 4 | 8 | 2 |
| Async (10 tasks) | 9.7 s | 7 | 7 | 8 | 2 |

**1. Does async make one order's workflow faster?**
Mostly no. An order still goes through the same delays: customer lookup, stock, pricing, payment
(800 + 500 + 700 ms), save and email. `await` does not shorten any of them; it only frees the thread while
waiting. The only part that gets faster is where two independent calls start together: the discount and
shipping APIs run at the same time in pricing. That is why async finished in 9.7 s and sync in 11.5 s,
although the sync version also had all 10 orders running at once on 10 threads.

**2. Why does async scale better for many orders?**
In the sync run each order was tied to its own thread for its whole life (all log lines of Order #2 are on
Thread 13), so 10 orders used 10 threads, mostly sleeping. In the async run the same 10 orders were served by
7 threads, and the threads were shared between orders: when an order reaches `await Task.Delay`, its thread
goes back to the pool and takes work from another order. The number of threads follows the work that is
actually running, not the number of waiting orders.

**3. What happens to the sync version with 1,000 orders?**
It needs 1,000 threads. Each reserves its own stack (about 1 MB by default on Windows), so around 1 GB for
threads that are mostly asleep in `Thread.Sleep`. Creating them is slow and the OS spends more time switching
between them. My `JsonFileStore` also allows one access per file at a time, so many of them would be blocked
waiting for it too. The program would get slower and could run out of memory. I did not run 1,000 orders; this
is based on my 10-thread run and how threads work.

**4. Why is async essential for apps that call databases, files and APIs?**
These calls are I/O-bound: the program waits for something outside the CPU for almost the whole time. A blocked
thread during that wait does nothing but still costs memory. With async the wait costs no thread, so a few
threads can serve many requests. In my run, 10 orders full of simulated DB, API, payment and SMTP waits were
handled by 7 threads. Async also made the 3-second payment timeout simple, with a linked
`CancellationTokenSource`.

### Phase 3 — Task 9.7: fire-and-forget investigation

The broken method saves the order, calls `SendConfirmationAsync` without `await`, and prints
"Order processing finished". The demo is in `Phase3/FireAndForgetDemo.cs`.

**1. The problem.** The email task is started but nobody waits for it. The method says "finished" before the
email is sent, and nobody checks whether it worked (the compiler warns with CS4014).

**2. The email is still being sent when the method returns / the app exits.**
TODO: paste the output of section A (emails.json had 0 records right after the method returned and 1
record 1.5 s later) and of `fireforget-exit` (the process ended and `emails.json` was empty).

**3. The email throws.**
TODO: paste the output of section B. The exception is stored inside the unawaited task. The caller received
no exception and believed everything worked, and the email was lost. The exception appeared only through
`TaskScheduler.UnobservedTaskException` when the task was garbage collected, and by default that event does
not terminate the process, so the error can show up late or never.

**4. The fix.** `await` the email inside a try/catch (the order is already saved and paid, so a failed email
must not break it). TODO: paste the output of sections C and D (the email was in `emails.json` right after
the method returned; when the email failed, the failure was logged and the order stayed Completed).

**When is `await` necessary?** When I need the result, when I must know that the work finished or failed,
when the next step depends on it, or when its failure must be handled.

**When is fire-and-forget acceptable?** Only for side work that is not important if it fails, when the task
handles and logs its own exceptions, and the process will still be alive until it finishes. For real work such
as emails, a background queue is a better solution.

**Why is `async void` dangerous?** It returns no `Task`, so the caller cannot await it or catch its exception.
An exception thrown in it is raised on the synchronization context or the thread pool and can crash the whole
process. It is also hard to test.

**Why is it acceptable for event handlers?** The delegate type of an event (for example `Button.Click`)
returns `void`, and the framework is the caller, so `async void` is the only way to use `await` inside it.
I would still put a try/catch inside so that exceptions are handled there.
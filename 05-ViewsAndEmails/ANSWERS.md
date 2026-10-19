# Assignment 05 — Answers

Student name:  
Group / ID:

## Failures you observed
adding key that already existed in the dictionary
-

## What you changed
locking on the condition 
many threads passes the condition at the same time one of them add the key and the other make an exception but they all shouldn't pass the condition just one should be passed
-

## Collection safety vs business-rule safety

A collection can be safe on its own but the business rule built on top of it
can still be unsafe. For example, a Dictionary guarantees that a key is unique,
but that does not make the "count views" rule safe=> Counting is an atomic operation
(read -> modify -> write sequence) so two threads can read the same value and
write the same result and one view is lost 

The fix is to make lock the same problem applies to the email queue: checking
Count > 0 and then calling Dequeue are two steps so another worker can
take the job in between

 before the fix :
 Excepton System.InvalidOperationException: 'Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.'
After:
Views expected=50000, actual=50000
Email jobs placed=20, unique sent=20, total sends=20
-

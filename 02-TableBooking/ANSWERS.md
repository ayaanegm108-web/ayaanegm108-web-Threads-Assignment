# Assignment 02 — Answers

Student name:  Aya Negm
Group / ID: 1

## What is wrong
more than 1 can book the same table(more than one thread pass the condition at the same time and all seeing the old data so they all update the left at the same time)

        _________________                                      |->Th1 update left from 5 to 4
Th1 => |                 |  Th1 ,Th2                           |->Th2 update left from 5 to 4
Th2 => |                 |  ==========> updating left value  __|->Th3 update left from 5 to 4
Th3 => | TablesLeft <= 0 |  Th3 ,Th4                           |->Th4 update left from 5 to 4
Th4 => |_________________|
-
as shown there are lost updates (left here sould be 1 not 4)


## How you fixed it
by locking the condition using lock (allowing only one thread to enter at a time)
-
 
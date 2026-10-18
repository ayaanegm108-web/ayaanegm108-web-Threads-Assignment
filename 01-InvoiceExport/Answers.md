## Original run (before the fix)
```
Starting invoice export...

Export finished in 6.0 s
```


## Problems with the current design
```
1. thread.sleep => prevent the thread from returning to the thread pool
2. a lot of database calls per order => can lead to performance issues and increased latency
3. too slow to run(toke 6 seconds)
4. no cancellation token => cannot cancel the operation 
```

## ## What I changed
```
1. Removed thread.sleep and replaced it with async/await
2.exception handling
3. Added cancellation token to allow the operation to be cancelled
```

## After the fix

```
Starting invoice export...


Export finished in 2.3 s
```


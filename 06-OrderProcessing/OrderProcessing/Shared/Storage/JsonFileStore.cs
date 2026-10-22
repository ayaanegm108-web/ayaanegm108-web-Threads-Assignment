using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Storage
{
    public class JsonFileStore
    {
        public Task<List<T>> ReadAllAsync<T>(string fileName, CancellationToken ct)
            => throw new NotImplementedException();

        public Task WriteAllAsync<T>(string fileName, List<T> items, CancellationToken ct)
            => throw new NotImplementedException();

        public Task UpdateAsync<T>(string fileName, Action<List<T>> mutate, CancellationToken ct)
            => throw new NotImplementedException();

        public List<T> ReadAll<T>(string fileName)
            => throw new NotImplementedException();

        public void WriteAll<T>(string fileName, List<T> items)
            => throw new NotImplementedException();
    }
}

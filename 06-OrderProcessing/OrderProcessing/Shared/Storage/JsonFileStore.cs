using OrderProcessing.Shared.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderProcessing.Shared.Storage
{
    public class JsonFileStore
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly UTF8Encoding Utf8NoBom = new(false);

        

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

        private static SemaphoreSlim LockFor(string fileName)
            => Locks.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));

        private static string PathOf(string fileName)
            => Path.Combine(DataFiles.DataFolder, fileName);


        //  Async 
        public async Task<List<T>> ReadAllAsync<T>(string fileName, CancellationToken ct)
        {
            var gate = LockFor(fileName);
            await gate.WaitAsync(ct);
            try
            {
                return await ReadCoreAsync<T>(fileName, ct);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task WriteAllAsync<T>(string fileName, List<T> items, CancellationToken ct)
        {
            var gate = LockFor(fileName);
            await gate.WaitAsync(ct);
            try
            {
                await WriteCoreAsync(fileName, items, ct);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task UpdateAsync<T>(string fileName, Action<List<T>> mutate, CancellationToken ct)
        {
            var gate = LockFor(fileName);
            await gate.WaitAsync(ct);
            try
            {
                var items = await ReadCoreAsync<T>(fileName, ct);
                mutate(items);
                await WriteCoreAsync(fileName, items, ct);
            }
            finally
            {
                gate.Release();
            }
        }

        // Sync (للمقارنة بس)

        public List<T> ReadAll<T>(string fileName)
        {
            var gate = LockFor(fileName);
            gate.Wait();
            try
            {
                string path = PathOf(fileName);
                if (!File.Exists(path)) return new List<T>();

                string json = File.ReadAllText(path, Encoding.UTF8);
                return Deserialize<T>(fileName, json);
            }
            finally
            {
                gate.Release();
            }
        }

        public void WriteAll<T>(string fileName, List<T> items)
        {
            var gate = LockFor(fileName);
            gate.Wait();
            try
            {
                string path = PathOf(fileName);
                string temp = path + ".tmp";
                Directory.CreateDirectory(DataFiles.DataFolder);

                File.WriteAllText(temp, JsonSerializer.Serialize(items, Options), Utf8NoBom);
                File.Move(temp, path, overwrite: true);
            }
            finally
            {
                gate.Release();
            }
        }

        
        private static async Task<List<T>> ReadCoreAsync<T>(string fileName, CancellationToken ct)
        {
            string path = PathOf(fileName);
            if (!File.Exists(path)) return new List<T>();

            string json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            return Deserialize<T>(fileName, json);
        }

        private static async Task WriteCoreAsync<T>(string fileName, List<T> items, CancellationToken ct)
        {
            string path = PathOf(fileName);
            string temp = path + ".tmp";
            Directory.CreateDirectory(DataFiles.DataFolder);

            try
            {
                string json = JsonSerializer.Serialize(items, Options);
                await File.WriteAllTextAsync(temp, json, Utf8NoBom, ct);

                ct.ThrowIfCancellationRequested();     // آخر فرصة للإلغاء قبل الاستبدال
                File.Move(temp, path, overwrite: true); // الاستبدال في خطوة واحدة
            }
            catch
            {
                if (File.Exists(temp)) File.Delete(temp);  // نضّف الملف المؤقت
                throw;
            }
        }

        private static List<T> Deserialize<T>(string fileName, string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);
            }
        }
    }
}

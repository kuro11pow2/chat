using System.Text.Json;

namespace Chat
{
    public class Object2File<T>
    {
        private readonly string FilePath;
        private readonly JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true, };

        public Object2File(string filePath)
        {
            FilePath = filePath;
        }

        public async Task Save(T obj)
        {
            using FileStream createStream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(createStream, obj, Options);
            await createStream.DisposeAsync();
        }

        public async Task<T> Load()
        {
            using FileStream openStream = File.OpenRead(FilePath);
            T? obj = await JsonSerializer.DeserializeAsync<T>(openStream);
            if (obj == null)
                throw new FileLoadException();
            return obj;
        }
    }
}
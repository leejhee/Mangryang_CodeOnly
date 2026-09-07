using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Scripts.GameSave.IO
{
    /// <summary>
    /// 슬롯 데이터 저장 및 로드 시의 비동기 작업을 담당하는 정적 클래스
    /// </summary>
    public static class SlotIO
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks
            = new(StringComparer.OrdinalIgnoreCase);

        private static SemaphoreSlim GetLock(string path)
            => Locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

        private static string userDataRoot;
        
        /// <summary>
        /// 정돈해주고, 추상타입 받으며, 국제표준형식 시간표현
        /// </summary>
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };
        
        /// <summary>
        /// 파일명만으로 userData에 저장 가능하도록 함.
        /// </summary>
        /// <param name="pathOrFileName">파일 이름만 넣어도 되도록 한다.</param>
        /// <returns>userdata 이하의 경로 반환</returns>
        private static string GetFullPath(string pathOrFileName)
        {
            if (string.IsNullOrEmpty(userDataRoot))
                throw new InvalidOperationException("SlotIO.InitUserRoot가 먼저 호출되어야 합니다.");

            if (Path.IsPathRooted(pathOrFileName)) return pathOrFileName;
            string file = Path.HasExtension(pathOrFileName) ? pathOrFileName : pathOrFileName + ".json";
            return Path.Combine(userDataRoot, file);
        }

        public static void InitUserRoot(string userRoot)
        {
            userDataRoot = userRoot ?? throw new ArgumentNullException(nameof(userRoot));
            Directory.CreateDirectory(userDataRoot);
        }
        
        public static async Task<GameSlotData> LoadAsync(string path, CancellationToken ct)
        {
            string final = GetFullPath(path);
            var gate = GetLock(final);
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!File.Exists(final)) return null;
                await using var fs = new FileStream(
                    final, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096,
                    FileOptions.Asynchronous);
                using var sr = new StreamReader(fs, Encoding.UTF8);
                string json = await sr.ReadToEndAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<GameSlotData>(json, JsonSettings);
            }
            finally { gate.Release(); }
        }

        public static async Task SaveAsync(string path, GameSlotData slot, CancellationToken ct)
        {
            string final = GetFullPath(path);
            string dir = Path.GetDirectoryName(final);
            Directory.CreateDirectory(dir!);
            string tmp = Path.Combine(dir!, Path.GetRandomFileName());
            string json = JsonConvert.SerializeObject(slot, JsonSettings);
            
            var gate = GetLock(final);
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using (var fs = new FileStream(
                                 tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                                 FileOptions.Asynchronous | FileOptions.WriteThrough))
                await using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    await sw.WriteAsync(json.AsMemory(), ct).ConfigureAwait(false);
                    await sw.FlushAsync().ConfigureAwait(false);
                    await fs.FlushAsync(ct).ConfigureAwait(false);
                }

                const int maxTry = 5;
                for (int i = 0; i < maxTry; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (File.Exists(final)) File.Replace(tmp, final, null);
                        else                    File.Move(tmp, final);
                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(8 * (i + 1), ct).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                gate.Release();
                TryDelete(tmp);
            }
        }
        
        static void TryDelete(string p)
        {
            try { if (File.Exists(p)) File.Delete(p); } catch { /* Silence */ }
        }
    }
}
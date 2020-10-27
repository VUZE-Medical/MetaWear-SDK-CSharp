using MbientLab.MetaWear.Impl.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLEMetawear
{
    public class IO : ILibraryIO
    {
        private readonly string MacAddr;
        public static string CacheRoot { get; set; } = ".metawear";
        public IO(string macAddr)
        {
            MacAddr = macAddr.Replace(":", "");
        }

        public async Task<Stream> LocalLoadAsync(string key)
        {
            return await Task.FromResult(File.Open(Path.Combine(Directory.GetCurrentDirectory(), CacheRoot, MacAddr, key), FileMode.Open));
        }

        public Task LocalSaveAsync(string key, byte[] data)
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), CacheRoot, MacAddr);
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            using (Stream outs = File.Open(Path.Combine(root, key), FileMode.Create))
            {
                outs.Write(data, 0, data.Length);
            }
            return Task.CompletedTask;
        }
    }
}

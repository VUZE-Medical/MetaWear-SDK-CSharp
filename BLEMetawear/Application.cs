using MbientLab.MetaWear;
using MbientLab.MetaWear.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace BLEMetawear
{
    public class Application
    {

        private static bool StartedTimer; 
        private static string FormatUid(ulong address)
        {
            var toSplit = String.Format("{0:X}", address);
            var result = "";
            for (int i = 0; i < toSplit.Length; i += 2)
            {
                var sub = toSplit.Substring(i, 2);
                if (i == 0)
                {
                    result = sub;
                }
                else
                {
                    result += ":" + sub;
                }
            }
            return result;
        }
        public async static Task<IMetaWearBoard> GetMetaWearBoard(string mac, string hci = null)
        {
            if (!StartedTimer)
            {
                FastScanner.StartScanner(0, 29, 29);
            }
            StartedTimer = true;
            //var b = await SearchMac(mac, 2000); 
            var succeed = await SearchMac(mac, 2000);
            //m_Watcher.AdvertisementFilter = new BluetoothLEAdvertisementFilter
            //{
            //    Advertisement= new BluetoothLEAdvertisement
            //};
           

            if (succeed)
            {
                var gatt = new Gatt(mac);
                var io = new IO(mac);
                return new MetaWearBoard(gatt, io);
            }
            else
            {
                return null;
            }
        }

        private static async Task<bool> SearchMac(string mac,int timeout)
        {
            var watcher = new BluetoothLEAdvertisementWatcher();
            watcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(100);
            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2500);
            watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(Constants.METAWEAR_GATT_SERVICE);
            var source = new TaskCompletionSource<bool>();
            watcher.Received += (o, e) =>
            {
                if (e.Advertisement.LocalName == "MetaWear")
                {
                    var macRecieved = FormatUid(e.BluetoothAddress);
                    Console.WriteLine($"recieved {macRecieved} {e.Advertisement.LocalName}");
                    if (macRecieved == mac)
                    {
                        Console.WriteLine($"found mac {mac}");
                        source.SetResult(true);
                    }
                   
                }
            };
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Start();
            var succeed = await Task.WhenAny(source.Task, Task.Delay(timeout)) == source.Task;
            watcher.Stop();
            return succeed;

        }
    }
}

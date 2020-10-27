using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using MbientLab.MetaWear;
using MbientLab.MetaWear.Impl.Platform;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace BLEMetawear
{
    public class Gatt : IBluetoothLeGatt
    {
        private BluetoothLEDevice m_Device;

        public Gatt(string mac)
        {
            string addressHex = mac.Replace(":", "");
            BluetoothAddress = ulong.Parse(addressHex, System.Globalization.NumberStyles.HexNumber);
        }
        public ulong BluetoothAddress { get; set; }

        public Action OnDisconnect { get; set; }
        private Dictionary<Guid, GattCharacteristic> Characteristics { get; set; } = new Dictionary<Guid, GattCharacteristic>();
        public Task DisconnectAsync()
        {
            Console.WriteLine($"DisconnectAsync");
            this.Characteristics.Clear();
            m_Device.ConnectionStatusChanged -= OnConnectionChanged;
            m_Device.GattServicesChanged -= OnGattServicesChanged;

            var device = this.m_Device;
            this.m_Device = null;
            this.OnDisconnect?.Invoke();
            device.Dispose();
            return Task.CompletedTask;
        }
        private static void PrintInfo(Windows.Devices.Enumeration.DeviceInformation info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Id:" + info.Id);
            sb.AppendLine("EnclosureLocation:" + info.EnclosureLocation);
            sb.AppendLine("IsDefault:" + info.IsDefault);
            sb.AppendLine("IsEnabled:" + info.IsEnabled);
            sb.AppendLine("Kind:" + info.Kind);
            sb.AppendLine("IsPaired:" + info.Pairing.IsPaired);
            info.Properties.ToList().ForEach(p =>
            {
                sb.AppendLine("\t" + p.Key + ":" + p.Value); ;
            });
            Console.WriteLine(sb);
        }
        public async Task DiscoverServicesAsync()
        {
            m_Device = await BluetoothLEDevice.FromBluetoothAddressAsync(BluetoothAddress);
            var info = m_Device.DeviceInformation;
            PrintInfo(info);
            m_Device.ConnectionStatusChanged += OnConnectionChanged;
            m_Device.GattServicesChanged += OnGattServicesChanged;
            var access = await m_Device.RequestAccessAsync();
            if (access != Windows.Devices.Enumeration.DeviceAccessStatus.Allowed)
            {
                throw new Exception("could not access BLE");
            }
            await Task.Delay(2000);
            await FillServices();

        }


        private async Task FillServices()
        {
            var result = await m_Device.GetGattServicesAsync(BluetoothCacheMode.Cached);
            Console.WriteLine($"GetGattServicesAsync result status {result.Status} ");
            //if (result.Status == GattCommunicationStatus.Success)
            {
                foreach (var service in result.Services)
                {
                    var res = await service.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
                    //if (res.Status == GattCommunicationStatus.Success)
                    {

                        res.Characteristics.ToList().ForEach(c =>
                        {
                            if (!Characteristics.ContainsKey(c.Uuid))
                            {
                                Characteristics.Add(c.Uuid, c);
                            }
                        });
                        //Characteristics.AddRange(res.Characteristics);

                    }
                }
                Console.WriteLine($"filled  chars - {Characteristics.Count}");
            }
            if (Characteristics.Count < 1)
            {
                Console.WriteLine("could not fill chars - ");
            }
        }
        private async Task<GattCharacteristic> GetChar(Guid uuid)
        {
            if (Characteristics.ContainsKey(uuid))
            {
                return Characteristics[uuid];
            }
            if (this.m_Device == null ||
                m_Device.ConnectionStatus != BluetoothConnectionStatus.Connected)
            {
                Console.WriteLine("GetChar device not connected");
                return null;
            }
            
            var result = await m_Device.GetGattServicesAsync(BluetoothCacheMode.Cached);
            //Console.WriteLine($"GetGattServicesAsync result status {result.Status} ");
            bool found = false;
            //if (result.Status == GattCommunicationStatus.Success)
            {
                foreach (var service in result.Services)
                {
                    var res = await service.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
                    //if (res.Status == GattCommunicationStatus.Success)
                    {
                        res.Characteristics.ToList().ForEach(ch =>
                        {
                            if (!Characteristics.ContainsKey(ch.Uuid))
                            {

                                Characteristics.Add(ch.Uuid, ch);
                            }
                            if (ch.Uuid == uuid)
                            {
                                found = true;
                            }
                        });
                        //var c = res.Characteristics.FirstOrDefault(ch => ch.Uuid == uuid);

                        //if (c!= null)
                        //{
                        //    //Characteristics.Add(c.Uuid,c);
                        //    Console.WriteLine("found!");
                        //    found = true;
                        //}

                        //Characteristics.AddRange(res.Characteristics);

                    }
                }
                if (found)
                {
                    Console.WriteLine($"found {uuid}");
                    return Characteristics[uuid];
                }
                else
                {
                    Console.WriteLine($"not found {uuid}");
                    return null;
                    //throw new InvalidOperationException("uuid not found");
                }

            }

        }
        private void OnGattServicesChanged(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine("OnGattServicesChanged");
            //await FillServices();
        }

        private void OnConnectionChanged(BluetoothLEDevice sender, object args)
        {
            Console.WriteLine($"connection changed {sender.ConnectionStatus}");
            //if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            //{
            //    FillServices().Wait();
            //}
        }

        public async Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler)
        {
            if (this.m_Device == null)
            {
                Console.WriteLine($"EnableNotificationsAsync leave no device");
                return;
            }
            //GattDeviceServicesResult result = await m_Device.GetGattServicesForUuidAsync(gattChar.Item1);
            //if (result.Services.Count > 0)
            //if (this.Characteristics.Count == 0)
            //{
            //    await FillServices();
            //}
            {


                //var service = result.Services[0];
                //var chars = await service.GetCharacteristicsForUuidAsync(gattChar.Item2);
                //if (chars.Characteristics.Count > 0)
                {
                    GattCharacteristic characteristic = await GetChar(gattChar.Item2); 

                    if (characteristic != null)
                    {
                        try
                        {
                            var status = await characteristic
                                           .WriteClientCharacteristicConfigurationDescriptorAsync(
                                            GattClientCharacteristicConfigurationDescriptorValue.Notify);

                            //if (status != GattCommunicationStatus.Success)
                            //{
                            //    Console.WriteLine("could not set notification");
                            //    throw new IOException("could not set notification");
                            //}
                            //else
                            {
                                characteristic.ValueChanged += ((o, e) =>
                                {
                                    var bytes = e.CharacteristicValue.ToArray();
                                    if (m_Device != null && m_Device.ConnectionStatus == BluetoothConnectionStatus.Connected)
                                    {
                                        handler(bytes);
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine(ex.Message);
                            throw;
                        }
                    }
                    else
                    {
                        Console.WriteLine("error registering handler");
                        throw new InvalidOperationException($"could not find gattchar {gattChar.Item2}");
                    }
                }
            }
        }

        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar)
        {
            if (this.m_Device == null)
            {
                Console.WriteLine("ReadCharacteristicAsync device null");
                return null;
            }
            //if (this.Characteristics.Count == 0)
            //{
            //    await FillServices();
            //}
            //GattDeviceServicesResult result = await m_Device.GetGattServicesForUuidAsync(gattChar.Item1);
            //if ( result.Services.Count > 0)
            {
                //var service = result.Services[0];
                //var chars = await service.GetCharacteristicsAsync();
                var characteristic = await GetChar(gattChar.Item2);
                if (characteristic != null)
                {
                    var properties = characteristic.CharacteristicProperties;
                    if (properties.HasFlag(GattCharacteristicProperties.Read))
                    {
                        GattReadResult resRead = await characteristic.ReadValueAsync();
                        var reader = DataReader.FromBuffer(resRead.Value);
                        byte[] input = new byte[reader.UnconsumedBufferLength];
                        reader.ReadBytes(input);
                        return input;
                    }
                    else
                    {
                        Console.WriteLine($"Could not read from {gattChar.Item2}");
                    }
                }
                else
                {
                    Console.Write("could not read value for ");
                }
            }
            Console.Write("could not read value");
            //return null;
            throw new IOException("could not read value");
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceGuid)
        {
            if (m_Device != null && m_Device.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {

                GattDeviceServicesResult result = await m_Device.GetGattServicesForUuidAsync(serviceGuid);
                var services = result.Services.ToList();
                foreach (var s in services)
                {
                    Console.WriteLine($"{s.Uuid}");
                    var sub = await s.GetIncludedServicesAsync();
                    foreach (var s1 in sub.Services)
                    {
                        Console.WriteLine($"sub:{s1.Uuid}");
                    }
                }
                return services.FirstOrDefault(s => s.Uuid == serviceGuid) != null;
            }
            else
            {
                return false;
            }
        }

        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value)
        {
            if (this.m_Device == null) return;
            //if (this.Characteristics.Count == 0)
            //{
            //    await FillServices();
            //}
            //GattDeviceServicesResult result = await m_Device.GetGattServicesForUuidAsync(gattChar.Item1);
            //if (result.Services.Count > 0)
            {
                //var service = result.Services[0];
                //var chars = await service.GetCharacteristicsForUuidAsync(gattChar.Item2);
                //if (chars.Characteristics.Count > 0)
                {
                    var characteristic = await GetChar(gattChar.Item2);
                    if (characteristic != null)
                    {
                        var properties = characteristic.CharacteristicProperties;
                        if (properties.HasFlag(GattCharacteristicProperties.Write))
                        {
                            var writer = new DataWriter();
                            writer.WriteBytes(value);
                            var opt = writeType == GattCharWriteType.WRITE_WITH_RESPONSE ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse;
                            await characteristic.WriteValueAsync(writer.DetachBuffer(), opt);
                            Console.WriteLine($"writen to {characteristic}");
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"Could not write to {characteristic}");
                        }
                    }
                }
            }

            //Console.WriteLine("could not write to characteristic",char)
            throw new IOException("could not write to characteristic");
        }
    }
}

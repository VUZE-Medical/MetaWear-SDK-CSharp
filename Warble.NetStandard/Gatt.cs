﻿using static MbientLab.Warble.Bindings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MbientLab.Warble {
    /// <summary>
    /// Wrapper around the WarbleGatt C struct
    /// </summary>
    public class Gatt {
        /// <summary>
        /// Handler that listens for disconnect events
        /// </summary>
        public Action<int> OnDisconnect { get; set; }
        /// <summary>
        /// True if currently connected to the remote device
        /// </summary>
        public bool IsConnected { get => warble_gatt_is_connected(WarbleGatt) != 0; }

        private readonly IntPtr WarbleGatt;
        private readonly Dictionary<string, GattChar> Characteristics = new Dictionary<string, GattChar>();
        private readonly FnVoid_IntPtr_WarbleGattP_Int DcHandler;
        private GCHandle m_DcHandlerPin;
        private FnVoid_IntPtr_WarbleGattP_CharP m_handler;
        private GCHandle m_handlerPin;
        /// <summary>
        /// Creates a C# Warble Gatt object
        /// </summary>
        /// <param name="mac">Mac address of the board to connect to e.g. E8:C9:8F:52:7B:07</param>
        /// <param name="hci">Mac address of the hci device to use, only applicable on Linux</param>
        /// <param name="addrType">Ble address type, defaults to random</param>
        public Gatt(string mac, string hci = null, string addrType = null) {
            Option[] options = new Option[3];
            byte actualSize = 1;

            options[0].key = "mac";
            options[0].value = mac;
            if (hci != null && RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                options[actualSize].key = "hci";
                options[actualSize].value = hci;
                actualSize++;
            }
            if (addrType != null) {
                options[actualSize].key = "address-type";
                options[actualSize].value = addrType;
                actualSize++;
            }

            WarbleGatt = warble_gatt_create_with_options(actualSize, options);

            DcHandler = new FnVoid_IntPtr_WarbleGattP_Int((ctx, caller, status) => {
                Characteristics.Clear();
                OnDisconnect?.Invoke(status);
            });
            //m_DcHandlerPin = GCHandle.Alloc(DcHandler, GCHandleType.Pinned);
            warble_gatt_on_disconnect(WarbleGatt, IntPtr.Zero, DcHandler);
        }

        /// <summary>
        /// Establishes a connection to the remote device
        /// </summary>
        /// <returns>Null when connection is established</returns>
        /// <exception cref="WarbleException">If device cannot be found or cannot connect within a time range</exception>
        public async Task ConnectAsync() {
            TaskCompletionSource<bool> warbleTaskSrc = new TaskCompletionSource<bool>();
            m_handler = new FnVoid_IntPtr_WarbleGattP_CharP((ctx, caller, err) => {
                if (err != null) {
                    warbleTaskSrc.SetException(new WarbleException(err));
                } else {
                    warbleTaskSrc.SetResult(true);
                }
            });
            //m_handlerPin = GCHandle.Alloc(m_handler, GCHandleType.Pinned);
            warble_gatt_connect_async(WarbleGatt, IntPtr.Zero, m_handler);

            await warbleTaskSrc.Task;
        }
        /// <summary>
        /// Disconnects from the remote device
        /// </summary>
        public void Disconnect() {
            warble_gatt_disconnect(WarbleGatt);
            //m_handlerPin.Free();
        }

        /// <summary>
        /// Find the GATT characteristic corresponding to the uuid value 
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns>Object representing gatt characteristic, null if it does not exist</returns>
        public GattChar FindCharacteristic(string uuid) {
            if (!Characteristics.TryGetValue(uuid, out var gattchar)) {
                IntPtr pointer = warble_gatt_find_characteristic(WarbleGatt, uuid);

                gattchar = pointer == IntPtr.Zero ? null : new GattChar(pointer);
                if (gattchar != null) {
                    Characteristics.Add(uuid, gattchar);
                }
            }

            return gattchar;
        }

        /// <summary>
        /// Check if a GATT service with the corresponding UUID exists on the device
        /// </summary>
        /// <param name="uuid">128-bit UUID string to lookup</param>
        /// <returns>True if GATT service exists, false otherwise</returns>
        public bool ServiceExists(string uuid) {
            return warble_gatt_has_service(WarbleGatt, uuid) != 0;
        }
    }
}

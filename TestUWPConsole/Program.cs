using System;
using System.Threading.Tasks;
using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Peripheral;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// This example code shows how you could implement the required main function for a 
// Console UWP Application. You can replace all the code inside Main with your own custom code.

// You should also change the Alias value in the AppExecutionAlias Extension in the 
// Package.appxmanifest to a value that you define. To edit this file manually, right-click
// it in Solution Explorer and select View Code, or open it with the XML Editor.

namespace TestUWPConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IMetaWearBoard board = null;
            bool succeed = false;
            int retries = 5;
            //while (!succeed && retries > 0)
            {
                //try
                {
                    board = MbientLab.MetaWear.NetStandard.Application.GetMetaWearBoard(args[0]);
                    //var board = BLEMetawear.Application.GetMetaWearBoard(args[0]);
                    board.TimeForResponse = 100000;
                    board.OnUnexpectedDisconnect += OnDisconneted;
                    await board.InitializeAsync();
                    succeed = true;
                }
                //catch
                {
                    retries--;
                }
            }

            ILed led = null;
            ISensorFusionBosch sensor = null;
            if (board.IsConnected)
            {
                led = board.GetModule<ILed>();
                led.EditPattern(MbientLab.MetaWear.Peripheral.Led.Color.Green, MbientLab.MetaWear.Peripheral.Led.Pattern.Solid);
                led.Play();

                sensor = board.GetModule<ISensorFusionBosch>();
                sensor.Configure();

                var rout = await sensor.EulerAngles.AddRouteAsync(source =>
                {
                    try
                    {
                        source.Stream(data =>
                        {

                            var value = data.Value<EulerAngles>();
                            var AngularRead = (int)value.Roll;
                            var OrbitalRead = (int)value.Pitch;
                            //Console.Clear();
                            Console.Write($"\rroll: {value.Roll} pitch:{value.Pitch} Yaw:{value.Yaw}      ");



                            //Rotate(-value.Pitch, 0 , -value.Yaw);

                        });
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                        //LogException(LoggerCategory, ex, "Could not initialize IMU stream callback");
                        throw;
                    }
                });
                sensor.EulerAngles.Start();
                sensor.Start();
            }
            ConsoleKeyInfo key = new ConsoleKeyInfo();
            while (key.Key != ConsoleKey.Q)
            {
                key = Console.ReadKey();
            }
            if (board.IsConnected)
            {

                if (led != null)
                {
                    led.Stop(true);
                }
                if (sensor != null)
                {
                    sensor.EulerAngles.Stop();
                    sensor.Stop();
                }

                await board.DisconnectAsync();
                //board.TearDown();
            }
        }

        private static void OnDisconneted()
        {
            Console.WriteLine("disconnected");
            //throw new NotImplementedException();
        }
    }
}

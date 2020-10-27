using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Peripheral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestNet
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            bool reconnect = true;
            
            Console.WriteLine($"connecting to {args[0]}");
            //Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            ConsoleKeyInfo key = new ConsoleKeyInfo();
            IMetaWearBoard board = null;
            bool succeed = false;
            int retries = 5;
            while (!succeed && retries > 0)
            {
                try
                {   
                    //board = MbientLab.MetaWear.NetStandard.Application.GetMetaWearBoard(args[0]);
                    
                    board =await  BLEMetawear.Application.GetMetaWearBoard(args[0]);
                    if (board != null)
                    {
                        board.TimeForResponse = 100000;
                        board.OnUnexpectedDisconnect += ()=>
                        {
                            key = new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false);
                        };
                        await board.InitializeAsync();
                        succeed = true;
                    }
                    else
                    {
                        retries--;
                    }
                }
                catch(Exception ex)
                {
                    retries--;
                }
            }

            ILed led = null;
            ISensorFusionBosch sensor= null;
            if (board!= null && board.IsConnected)
            {
                led = board.GetModule<ILed>();
                led.EditPattern(MbientLab.MetaWear.Peripheral.Led.Color.Green, MbientLab.MetaWear.Peripheral.Led.Pattern.Solid);
                led.Play();

                sensor  = board.GetModule<ISensorFusionBosch>();
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
            
            while (key.Key != ConsoleKey.Q)
            {
                key = Console.ReadKey();
            }
            if (board.IsConnected)
            {
                //board.OnUnexpectedDisconnect -= OnDisconneted;
                if (led != null)
                {
                    led.Stop(true);
                }
                if (sensor!= null)
                {
                    sensor.EulerAngles.Stop();
                    sensor.Stop();
                }
                
                //await board.GetModule<IDebug>().DisconnectAsync();
                board.TearDown();
                await board.DisconnectAsync();
                //board.TearDown();
            }
        }

        private static void OnDisconneted()
        {
            Console.WriteLine("disconnected");
        }
    }
}

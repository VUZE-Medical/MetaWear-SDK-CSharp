using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Sensor;
//using MbientLab.Warble;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor.GyroBmi160;
using System.Windows.Media.Media3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using HelixToolkit.Wpf;
using HelixUtils;
using Windows.Devices.Bluetooth.Advertisement;
using System.Diagnostics;
using MbientLab.MetaWear.Core.SensorFusionBosch;
using Windows.Devices.Power;
using System.ComponentModel;
using MbientLab.MetaWear.Peripheral;

namespace TestMegaWearWpf
{
    public class BLEDevice: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name { get; set; }
        public string Mac { get; set; }

        public bool CanConnect { get; set; }
        public bool IsConnected { get; set; }
        public byte BatteryLevel { get; set; }

        public override string ToString()
        {
            return $"{Name} {Mac} {BatteryLevel}";
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<BLEDevice>  devices = new List<BLEDevice>();
        private IMetaWearBoard m_Metaware;
        private bool m_Connected;
        
        private IAccelerometer m_Accelerometer;
        private IGyroBmi160 m_Gyro;
        private ISensorFusionBosch m_SensorFusion;
        private BluetoothLEAdvertisementWatcher m_Watcher;

        private bool NeedsReloadingModel { get; set; }
        public bool IsCompresedModel { get; set; } = false;
        public MainWindow()
        {
           
            //MbientLab.Warble.Library.Init("trace");
            InitializeComponent();
            //Scanner.OnResultReceived += OnReceived;
            //Scanner.Start();
           
        }

        private async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.Advertisement.ServiceUuids.Count > 0 && (!string.IsNullOrEmpty(args.Advertisement.LocalName)))
            {
                try
                {
                   
                    //bool canConnect = args.IsScannable;
                    await OnReceived(new BLEDevice
                    {
                        Mac = FormatUid(args.BluetoothAddress),
                        Name = args.Advertisement.LocalName,
                        //CanConnect = canConnect
                    });
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
           
        }

        private async Task OnReceived(BLEDevice device)
        {
            if (device != null)
            {
                //if (device.HasServiceUuid(Constants.METAWEAR_GATT_SERVICE.ToString()))
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        
                        if (devices.FirstOrDefault(o => device.Mac == o.Mac ) != null)
                        {
                            return;
                        }
                        devices.Add(device);
                        lst.Items.Add(device);

                    });
                }
            }
            
        }
        private async Task Disconnect()
        {
            if (m_Metaware != null)
            {
                var led = m_Metaware.GetModule<ILed>();
                led.Stop(true);
                await m_Metaware.DisconnectAsync();
                m_Metaware.TearDown();
                m_Metaware = null;
                m_Connected = false;
            }
           
            //Scanner.Start();
            btnConnect.Content = "Connect";
        }
        private async void OnConnect(object sender, RoutedEventArgs e)
        {
            var item = lst.SelectedItem as BLEDevice;
            if (item!= null)
            {
                if (m_Metaware == null)
                {
                    await MbientLab.MetaWear.NetStandard.Application.ClearDeviceCacheAsync(item.Mac);
                    m_Metaware = MbientLab.MetaWear.NetStandard.Application.GetMetaWearBoard(item.Mac);
                    m_Metaware.TimeForResponse = 100000;
                }
                if (m_Metaware.IsConnected)
                {
                    await Disconnect();
                }
                else
                {
                    try
                    {
                        int retries = 5;
                        do
                        {
                            try
                            {
                                btnConnect.Content = "Connecting...";
                                btnConnect.IsEnabled = false;
                                m_Metaware.OnUnexpectedDisconnect += OnDisconneted;
                                await m_Metaware.InitializeAsync();
                                retries = -1;
                            }
                            catch
                            {
                                retries--;
                            }
                        } while (retries > 0);
                        btnConnect.IsEnabled = true;
                        if (m_Metaware.IsConnected)
                        {
                            var batteryLevel = await m_Metaware.ReadBatteryLevelAsync();
                            var led = m_Metaware.GetModule<ILed>();
                            led.EditPattern(MbientLab.MetaWear.Peripheral.Led.Color.Green, MbientLab.MetaWear.Peripheral.Led.Pattern.Solid);
                            led.Play();
                            item.BatteryLevel = batteryLevel;
                            BatteryText.Text = $" Battery:{batteryLevel}";
                            btnConnect.Content = "Disconnect";
                            m_Connected = true;

                            //m_Metaware.GetModule<IMagnetometerBmm150>();
                            //m_Gyro = m_Metaware.GetModule<IGyroBmi160>();
                            //m_Gyro.Configure(OutputDataRate._100Hz);
                            //m_Accelerometer = m_Metaware.GetModule<IAccelerometer>();
                            //m_Accelerometer.Configure(odr: 100f, range: 8f);
                            m_SensorFusion = m_Metaware.GetModule<ISensorFusionBosch>();
                            m_SensorFusion.Configure();
                           
                            var rout = await m_SensorFusion.EulerAngles.AddRouteAsync(source =>
                            {
                                try
                                {
                                    source.Stream(async data =>
                                    {
                                       await Dispatcher.InvokeAsync(() =>
                                       {
                                                var value = data.Value<EulerAngles>();
                                                AngleText.Text = value.ToString();
                                                Rotate(-value.Roll, -value.Pitch, -value.Yaw);
                                            //Rotate(-value.Pitch, 0 , -value.Yaw);
                                        });
                                    });
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            });
                            if ( !rout.Valid)
                            {
                                MessageBox.Show("rout invalid");
                            }
                            await m_SensorFusion.Gravity.AddRouteAsync(source =>
                            {
                                source.Stream(async data =>
                                {
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        var value = data.Value<Acceleration>();
                                        GravityText.Text = value.ToString();
                                        //Rotate(-value.Roll, -value.Pitch, -value.Yaw);
                                        //Rotate(-value.Pitch, 0 , -value.Yaw);
                                    });
                                });
                            });
                            //var calib = await m_SensorFusion.ReadCalibrationStateAsync();
                            m_SensorFusion.Gravity.Start();
                            m_SensorFusion.EulerAngles.Start();
                            m_SensorFusion.Start();
                            
                            
                            //Scanner.Stop();
                        }
                        else
                        {
                            throw new InvalidOperationException("Could not connect");
                        }
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        btnConnect.Content = "Connect";
                        m_Connected = false;
                    }
                }

            }
        }

        
        
        private async void OnDisconneted()
        {
            
            //await this.Dispatcher.InvokeAsync( () =>
            //{
            //    btnConnect.Content = "disconnected!";
               
                
                
            //});
            //await m_Metaware.DisconnectAsync();
            //m_Metaware.TearDown();
            await this.Dispatcher.InvokeAsync( () =>
            {
                btnConnect.Content = "Connect";
                m_Connected = false;

            });
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_Metaware != null)
            {
                if (m_Metaware.IsConnected)
                {
                    var led = m_Metaware.GetModule<ILed>();
                    led.Stop(true);
                    m_Metaware.DisconnectAsync().Wait();
                }
                m_Metaware.TearDown();
            }
        }



        private void viewPort3d_StylusSystemGesture(object sender, StylusSystemGestureEventArgs e)
        {

        }
        public static readonly DependencyProperty ModelProperty =
                     DependencyProperty.Register("Model",
                     typeof(Model3D), typeof(MainWindow), new FrameworkPropertyMetadata(
                         null, FrameworkPropertyMetadataOptions.AffectsRender,
                         new PropertyChangedCallback(OnModelChanged)));
        public Model3D Model
        {
            get
            {
                return (Model3D)GetValue(ModelProperty);
            }
            set
            {
                SetValue(ModelProperty, value);
            }
        }


        private void Rotate( double pitch,double yaw , double roll)
        {
            this.Yaw.Rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), yaw);
            this.Pitch.Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), pitch);
            this.Roll.Rotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), roll);

        }
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainWindow view = d as MainWindow;

            view.ModelVisual.Content = e.NewValue as Model3D;
            
            view.ResetView();
            //view.viewPort3d.ZoomExtents();
            view.ZoomExtent();

        }

         

        private void ResetView()
        {
            viewPort3d.Camera.Reset();
            viewPort3d.Camera.UpDirection = new Vector3D(0, 0, 1);
            viewPort3d.Camera.Position = new Point3D(0, -0.1, 0);
            viewPort3d.Camera.LookDirection = new Vector3D(0, 1, 0);
            //
        }

        private void ZoomExtent()
        {
            var viewport = viewPort3d.Viewport;
            var bounds = Visual3DHelper.FindBounds(viewport.Children);
            var diagonal = new Vector3D(bounds.SizeX, bounds.SizeY, bounds.SizeZ);

            if (bounds.IsEmpty || diagonal.LengthSquared < double.Epsilon)
            {
                return;
            }

            var infbounds = Inflate(bounds, 0.9f);
            CameraHelper.ZoomExtents(this.viewPort3d.Camera, viewport, infbounds);
        }


        private Rect3D Inflate(Rect3D rect, float amount)
        {
            Vector3D center = new Vector3D(rect.X + 0.5 * rect.SizeX,
                                            rect.Y + 0.5 * rect.SizeY,
                                            rect.Z + 0.5 * rect.SizeZ);

            Size3D newSize = new Size3D(rect.SizeX * amount,
                                        rect.SizeY * amount,
                                        rect.SizeZ * amount);
            Point3D newLocation = new Point3D(center.X - 0.5 * newSize.X,
                                                center.Y - 0.5 * newSize.Y,
                                                center.Z - 0.5 * newSize.Z);
            Rect3D newRect = new Rect3D(newLocation, newSize);
            return newRect;

        }

        private string FormatUid(ulong address)
        {
            var toSplit = String.Format("{0:X}", address);
            var result = "";
            for ( int i =0; i < toSplit.Length;i+=2)
            {
                var sub = toSplit.Substring(i, 2);
                if (i == 0)
                {
                    result = sub ;
                }
                else
                {
                    result += ":" + sub;
                }
            }
            return result;
        }
        private void LoadControlModel(string fileName)
        {
            ModelImporter import = new ModelImporter();
            ObjReader2 reader = new ObjReader2();
            if (string.IsNullOrEmpty(fileName))
            {
                Model = null;
                viewPort3d.Visibility = Visibility.Hidden;
            }
            if (File.Exists(fileName) && !string.IsNullOrEmpty(fileName))
            {
                try
                {
                    viewPort3d.Visibility = Visibility.Hidden;
                    
                    if (IsCompresedModel)
                    {
                        Model = reader.ReadZ(fileName);
                    }
                    else
                    {
                        Model = reader.Read(fileName);//import.Load(fileName);
                    }
                    viewPort3d.Visibility = Visibility.Visible;
                }
                catch

                {

                }
            }
            else
            {
                viewPort3d.Visibility = Visibility.Hidden;
                
            }
            NeedsReloadingModel = false;
        }

        private void viewPort3d_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {

            //e.Cancel();
        }

        private void viewPort3d_Loaded(object sender, RoutedEventArgs e)
        {
            //if (NeedsReloadingModel)
            //{
            //    LoadControlModel(this.ModelPath);
            //}
            //this.ResetView();
            //this.ZoomExtent();

        }

        private async void OnLoad(object sender, RoutedEventArgs e)
        {
            LoadControlModel(@"c:\temp\cube\Cube.obj");
            m_Watcher = new BluetoothLEAdvertisementWatcher();
            m_Watcher.Received += Watcher_Received;
            m_Watcher.ScanningMode = BluetoothLEScanningMode.Active;
            await Task.Delay(2000);
            m_Watcher.Start();
            
        }
    }
}

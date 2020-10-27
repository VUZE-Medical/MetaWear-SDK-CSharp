using OxyPlot;
using OxyPlot.Series;
using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.GyroBmi160;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using OxyPlot.Axes;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RealTimeGraph {
    public class MainViewModel {
        public const int MAX_DATA_SAMPLES = 960;
        public MainViewModel() {
            MyModel = new PlotModel {
                Title = "Euler",
                IsLegendVisible = true
            };
            MyModel.Series.Add(new LineSeries {
                BrokenLineStyle = LineStyle.Solid,
                MarkerStroke = OxyColor.FromRgb(1, 0, 0),
                LineStyle = LineStyle.Solid,
                Title = "pitch"
            });
            MyModel.Series.Add(new LineSeries {
                MarkerStroke = OxyColor.FromRgb(0, 1, 0),
                LineStyle = LineStyle.Solid,
                Title = "roll"
            });
            MyModel.Series.Add(new LineSeries {
                MarkerStroke = OxyColor.FromRgb(0, 0, 1),
                LineStyle = LineStyle.Solid,
                Title = "yaw"
            });
            MyModel.Axes.Add(new LinearAxis {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = -360f,
                AbsoluteMaximum = 360f,
                Minimum = -360f,
                Maximum = 360f,
                Title = "Value"
            });
            MyModel.Axes.Add(new LinearAxis {
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                AbsoluteMinimum = 0,
                Minimum = 0,
                Maximum = MAX_DATA_SAMPLES
            });
        }

        public PlotModel MyModel { get; private set; }
        public string Angles { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LineGraph : Page {

        private IMetaWearBoard metawear;
        private IAccelerometer accelerometer;
        private IGyroBmi160 gyro;
        private ISensorFusionBosch m_SensorFusion;

        public LineGraph() {
            InitializeComponent();
        }


        protected async  void OnNavigatedTo2(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var samples = 0;
            var model = (DataContext as MainViewModel).MyModel;

            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
            //accelerometer = metawear.GetModule<IAccelerometer>();
            //gyro = metawear.GetModule<IGyroBmi160>();
            //gyro.Configure();

            //accelerometer.Configure(odr: 100f, range: 8f);
            m_SensorFusion = metawear.GetModule<ISensorFusionBosch>();
            gyro = metawear.GetModule<IGyroBmi160>();
            gyro.Configure(OutputDataRate._100Hz);
            accelerometer = metawear.GetModule<IAccelerometer>();
            accelerometer.Configure(odr: 100f, range: 8f);

            m_SensorFusion.Configure();
            await m_SensorFusion.EulerAngles.AddRouteAsync(source =>
            {
                source.Stream(async data =>
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var value = data.Value<EulerAngles>();
                        (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.Pitch));
                        (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Roll));
                        (model.Series[2] as LineSeries).Points.Add(new DataPoint(samples, value.Yaw));
                        samples++;

                        model.InvalidatePlot(true);
                        if (samples > MainViewModel.MAX_DATA_SAMPLES)
                        {
                            model.Axes[1].Reset();
                            model.Axes[1].Maximum = samples;
                            model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
                            model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);
                        }
                        GyroText.Text = value.ToString();
                    });
                });
            });

            m_SensorFusion.Start();
            m_SensorFusion.EulerAngles.Start();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            var samples = 0;
            var model = (DataContext as MainViewModel).MyModel;

            metawear = MbientLab.MetaWear.Win10.Application.GetMetaWearBoard(e.Parameter as BluetoothLEDevice);
            //accelerometer = metawear.GetModule<IAccelerometer>();
            //gyro = metawear.GetModule<IGyroBmi160>();
            //gyro.Configure();
            
            //accelerometer.Configure(odr: 100f, range: 8f);
            m_SensorFusion = metawear.GetModule<ISensorFusionBosch>();
            m_SensorFusion.Configure();
            await m_SensorFusion.EulerAngles.AddRouteAsync(source =>
            {
                source.Stream(async data =>
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        var value = data.Value<EulerAngles>();
                        (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.Pitch));
                        (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Roll));
                        (model.Series[2] as LineSeries).Points.Add(new DataPoint(samples, value.Yaw));
                        samples++;

                        model.InvalidatePlot(true);
                        if (samples > MainViewModel.MAX_DATA_SAMPLES)
                        {
                            model.Axes[1].Reset();
                            model.Axes[1].Maximum = samples;
                            model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
                            model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);
                        }
                        
                    });
                });
            });
            //await accelerometer.PackedAcceleration.AddRouteAsync(source => source.Stream(async data =>
            //{
            //    var value = data.Value<Acceleration>();
            //    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            //    {

            //        (model.Series[0] as LineSeries).Points.Add(new DataPoint(samples, value.X));
            //        (model.Series[1] as LineSeries).Points.Add(new DataPoint(samples, value.Y));
            //        (model.Series[2] as LineSeries).Points.Add(new DataPoint(samples, value.Z));
            //        samples++;

            //        model.InvalidatePlot(true);
            //        if (samples > MainViewModel.MAX_DATA_SAMPLES)
            //        {
            //            model.Axes[1].Reset();
            //            model.Axes[1].Maximum = samples;
            //            model.Axes[1].Minimum = (samples - MainViewModel.MAX_DATA_SAMPLES);
            //            model.Axes[1].Zoom(model.Axes[1].Minimum, model.Axes[1].Maximum);
            //        }
            //    });
            //}));

            //await gyro.AngularVelocity.AddRouteAsync(source =>
            //        {
            //            source.Stream(async data=>
            //            {
            //                var dt = data.Value<AngularVelocity>();
            //                 await Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            //                 {
            //                     GyroText.Text = dt.ToString();//=$" X = {dt.X}";
            //                 });
            //            }
            //            );
            //        });
            //await gyro.PullConfigAsync();
            //gyro.Start();
            //gyro.AngularVelocity.Start();
            m_SensorFusion.Start();
            m_SensorFusion.EulerAngles.Start();
        }

        private async void back_Click(object sender, RoutedEventArgs e) {
            if (!metawear.InMetaBootMode) {
                metawear.TearDown();
                await metawear.GetModule<IDebug>().DisconnectAsync();
            }
            Frame.GoBack();
        }

        private void streamSwitch_Toggled(object sender, RoutedEventArgs e) {
            if (streamSwitch.IsOn) {
                m_SensorFusion.EulerAngles.Start();
                m_SensorFusion.Start();
            } else {
                m_SensorFusion.Stop();
                m_SensorFusion.EulerAngles.Stop();
            }
        }
    }
}

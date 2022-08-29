using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Hompus.VideoInputDevices;


namespace VideoCaptureWPF_NF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private VideoCapture capture;
        //private readonly CascadeClassifier cascadeClassifier;
        private BackgroundWorker bkgWorker;

        public MainWindow()
        {
            InitializeComponent();
            var sde = new SystemDeviceEnumerator();
            System.Collections.Generic.IReadOnlyDictionary<int, string> devices = sde.ListVideoInputDevice();

            cboDevice.ItemsSource = devices;

            capture = new VideoCapture();                      

            //cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");

          

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //capture.Open(0, VideoCaptureAPIs.ANY);
            //if (!capture.IsOpened())
            //{
            //    Close();
            //    return;
            //}

            //bkgWorker.RunWorkerAsync();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            bkgWorker.CancelAsync();

            capture.Dispose();
            //cascadeClassifier.Dispose();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (var frameMat = capture.RetrieveMat())
                {
                    //var rects = cascadeClassifier.DetectMultiScale(frameMat, 1.1, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));

                    //foreach (var rect in rects)
                    //{
                    //    Cv2.Rectangle(frameMat, rect, Scalar.Red);
                    //}

                    // Must create and use WriteableBitmap in the same thread(UI Thread).
                    Dispatcher.Invoke(() =>
                    {
                        FrameImage.Source = frameMat.ToWriteableBitmap();
                    });
                }

                Thread.Sleep(30);
            }
        }

        private void cboDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboDevice.SelectedItem != null)
            {
                var wSelectedDevice = (System.Collections.Generic.KeyValuePair<int, string>)cboDevice.SelectedItem;
                if (wSelectedDevice is System.Collections.Generic.KeyValuePair<int, string>)
                {
                    if (capture.IsOpened())
                    {
                        bkgWorker.CancelAsync();
                        bkgWorker.DoWork -= Worker_DoWork;
                        bkgWorker.Dispose();
                        capture.Dispose();
                        Thread.Sleep(30);                        
                        capture = new VideoCapture();
                    }

                    capture.Open(wSelectedDevice.Key, VideoCaptureAPIs.DSHOW);
                    if (!capture.IsOpened())
                    {
                        capture = new VideoCapture();
                        return;
                    }

                    bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
                    bkgWorker.DoWork += Worker_DoWork;
                    bkgWorker.RunWorkerAsync();
                }
            }
        }
    }




}

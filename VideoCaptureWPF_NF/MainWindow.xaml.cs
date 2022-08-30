using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Hompus.VideoInputDevices;
using LibUsbDotNet.Main;
using System.Net.NetworkInformation;


namespace VideoCaptureWPF_NF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        VideoCapture capture;
        //private readonly CascadeClassifier cascadeClassifier;
        BackgroundWorker bkgWorker;

        internal System.Windows.Threading.DispatcherTimer drcTimer;
        tic m_tic;

        bool m_saveimage = false;
        bool m_connected = false;
        bool m_captureImages = false;
        bool m_movestage = false;
        bool m_processing = false;
        int m_stepmode = 0;
        int m_currentImageCount = 0;

        private int stepInterval = 3;
        private int stepDelay = 1000;
        private int stepLimit = 1000;
        private int stepSpeed = 5000000;
        private bool stepDirection = false;

        string ticStatus;
        string ticVariables;

        int ImageIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            m_tic = new tic();
            var sde = new SystemDeviceEnumerator();
            System.Collections.Generic.IReadOnlyDictionary<int, string> devices = sde.ListVideoInputDevice();

            cboDevice.ItemsSource = devices;

            capture = new VideoCapture();                      

            //cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");

          

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        void ConnectUSB()
        {
            try
            {
                if (!m_connected)
                {
                    drcTimer = new System.Windows.Threading.DispatcherTimer();
                    drcTimer.Tick += DrcTimer_Tick;
                    drcTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                    drcTimer.Start();
                                       

                    m_connected = m_tic.open(tic.PRODUCT_ID.T36V4);
                    m_tic.clear_driver_error();
                    SetStepMode();
                    m_tic.energize();
                    m_tic.exit_safe_start();

                    //m_tic.set_max_accel(100000);
                    //m_tic.set_max_decel(100000);
                    //m_tic.set_max_speed(50000000);
                    //m_tic.set_starting_speed(2000000);                   
                    //tic.wait_for_device_ready();

                    m_tic.halt_and_set_position(0);
                    m_tic.set_target_position(0);
                    m_tic.set_step_mode(m_stepmode);

                    if (m_connected) txtTicStatus.Text ="Connected to tic.PRODUCT_ID.T36V4";
                }
                //RefreshTicInfo();
            }
            catch (Exception ex)
            {
                m_connected = false;
                txtError.Text = ex.Message;
            }
        }

        void DisconnectUSB()
        {
            try
            {
                if (drcTimer != null)
                {
                    drcTimer.Stop();
                    drcTimer.Tick -= DrcTimer_Tick;
                }
                m_tic.deenergize();
                m_tic.close();
                m_connected = false;
                if (!m_connected) txtTicStatus.Text="Disconnected from tic.PRODUCT_ID.T36V4";
                txtActivity.Text = "";
            }
            catch (Exception ex)
            {
                txtError.Text = ex.Message;
            }
        }

        void RefreshTicInfo()
        {
            if (m_connected)
            {
                m_tic.reset_command_timeout();
                m_tic.get_variables();
                m_tic.get_status_variables(true);
               
                var sb = new StringBuilder();
                foreach (var prop in m_tic.status_vars.GetType().GetProperties())
                {

                    var wObject = prop.GetValue(m_tic.status_vars, null);
                    if (wObject != null)
                    {
                        sb.AppendLine(string.Format("{0}={1}", prop.Name, wObject));
                    }
                }
                ticStatus = sb.ToString();
                txtTicStatus.Text = string.Format("Op Status: {0} Err: {1} Step Mode: {2}", m_tic.status_vars.operation_state, m_tic.status_vars.string_error_status, m_tic.vars.step_mode);

                sb = new StringBuilder();
                foreach (var prop in m_tic.vars.GetType().GetProperties())
                {
                    var wObject = prop.GetValue(m_tic.vars, null);
                    if (wObject != null)
                    {
                        sb.AppendLine(string.Format("{0}={1}", prop.Name, wObject));
                    }
                }
                ticVariables = sb.ToString();                
            }

        }

        void StartImageCapture()
        {
            try
            {
                if (capture != null && capture.IsOpened())
                {
                    m_currentImageCount = 0;
                    if (m_connected)
                    {
                        DisconnectUSB();
                        ConnectUSB();

                        //m_param = param;
                        m_tic.halt_and_hold();
                        m_tic.halt_and_set_position(0);
                        m_tic.set_target_position(0);
                        m_tic.set_max_speed((int)UpSpeed.Value);
                        m_captureImages = true;
                        m_processing = false;
                        m_saveimage = true;
                        ImageIndex = 0;
                    }
                    else
                    {
                       txtTicStatus.Text ="USB TIC controller not connected.";
                    }
                }
                else
                {
                  txtStatus.Text= "No camera connected.";
                }
            }
            catch (Exception ex)
            {
                txtError.Text = ex.Message;
            }
        }

        void StopImageCapture()
        {
            try
            {
                if (m_connected) m_tic.halt_and_hold();
                m_captureImages = false;
                //drcTimer.Stop();
                //drcTimer.Tick -= DrcTimer_Tick;
            }
            catch (Exception ex)
            {
                txtError.Text = ex.Message;
            }
        }

        void Saveshot()
        {
            try
            {
                string imagefilePath = @"C:\Users\KindelG\Documents\AmScope\";
                ImageIndex++;
                if (capture != null && capture.IsOpened())
                {
                    
                    var fileName = string.Format("{0}.jpg", $"{ImageIndex:0000}");
                    var fullpath = System.IO.Path.Combine(imagefilePath, fileName);

                    using (var fileStream = new FileStream(fullpath, FileMode.Create))
                    {
                        MemoryStream ms = new MemoryStream();
                        var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                        encoder.QualityLevel = 100;
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(FrameImage.Source as System.Windows.Media.Imaging.BitmapSource));
                        encoder.Save(ms);
                        fileStream.Position = 0;
                        ms.CopyTo(fileStream);
                        ms.WriteTo(fileStream);
                        ms.Flush();
                        fileStream.Flush();
                    }
                    //ImageHelper.CreateDRCEXIF(fullpath);

                    //OnPropertyChanged("Tabs");
                    //UIHelper.UpdateMemoryUsage();
                    txtActivity.Text = String.Format("Captured Image #{0}", $"{ImageIndex:0000}");
                }
            }
            catch (Exception ex)
            {
                txtError.Text = ex.Message;
                throw;
            }
        }

        void SetStepMode()
        {
            foreach (Control control in this.spControls.Children)
            {
                if ((control is RadioButton))
                {
                    if ((control as RadioButton).IsChecked.HasValue && ((bool)(control as RadioButton).IsChecked))
                    {
                        m_stepmode = Convert.ToInt32((control as RadioButton).Tag);
                        if (m_tic != null && m_connected)
                        {
                            m_tic.set_step_mode(m_stepmode);
                        }
                    }
                }
            }
        }

        private void DrcTimer_Tick(object sender, EventArgs e)
        {
            // Current and target are not matching.
            // NEED TO REWORK

            // Initial state: m_captureImages true  m_processing false


            if (m_connected && m_tic != null)
            {
                if (m_captureImages)
                {
                    if (!string.IsNullOrEmpty(m_tic.get_error_status())) m_tic.clear_driver_error();

                    if (!m_processing)
                    {
                        m_currentImageCount++;
                        m_processing = true;
                        m_tic.set_target_position(m_tic.vars.current_position + Convert.ToInt32(UpSteps.Value));
                        m_tic.wait_for_move_complete();
                        if (capture != null && capture.IsOpened())
                        {
                            if (m_saveimage)
                            {
                                Saveshot();
                            }
                            else
                            {
                                //Takeshot(m_param);
                            }
                        }
                        //Task.Delay(StepDelay).Wait();
                        m_processing = false;
                    }
                    if (Math.Abs(m_tic.vars.current_position) >= UpStop.Value)
                    {
                        m_captureImages = false;
                    }
                    txtActivity.Text = string.Format("count: {0} current {1}: target: {2} Limit: {3} {4}", m_currentImageCount, m_tic.vars.current_position, m_tic.vars.target_position, UpStop.Value, System.DateTime.Now.ToString("HH:mm:ss:ffff"));
                }
                else
                {
                    //txtActivity.Text = string.Format("current {0}: target: {1} Limit: {2} {3}", m_tic.vars.current_position, m_tic.vars.target_position, MoveStageInternal, System.DateTime.Now.ToString("HH:mm:ss:ffff"));
                }
            }
            RefreshTicInfo();
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
            if (m_connected) DisconnectUSB();
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

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            StartImageCapture();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopImageCapture();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            ConnectUSB();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnectUSB();
        }
    }




}

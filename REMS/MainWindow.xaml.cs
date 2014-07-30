using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using REMS.classes;
using REMS.drivers;
using REMS.popups;
using REMS.data;
using NationalInstruments.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;

namespace REMS
{
    //Binding datagrids
    //http://www.codeproject.com/Articles/165368/WPF-MVVM-Quick-Start-Tutorial

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window //maybe should try 'implement' REMSProperties
    {
        // Calibration Variables
        private Point[] mImageCalibrationPoints = new Point[2]; // where actually on the image is the calibration points
        private Point[] mCanvasCalibrationPoints = new Point[2]; // where the user clicked on the canvas
        private int mCalibrationPoints = 0;

        // Scan area variables
        private Point[] mCanvasScanArea = new Point[2]; // the selected are to scan selected by the user
        private Point[] mMotorScanArea = new Point[2]; // the bounds for the motor to travel
        private double mCanvasXYStepSize; // Number of pixels for every mm
        private Boolean mScanAreaIsSet = false;
        private double mMotorXPos, mMotorYPos, mMotorZPos;
        private int mXScanPoints, mYScanPoints = 0;
        private int mTotalScanPoints = 0;
        private Boolean mHomeMotors = false;
        private Boolean mMoveMotors = false;

        // Saved Setting Variables
        private double mMotorXTravelDistance;
        private double mMotorYTravelDistance;
        private double mMotorZTravelDistance;
        //private double mSAMinFrequency; // MHz
        //private double mSAMaxFrequency; // MHz
        private Point[] mSABaseLine;
        private int mXYStepSize, mZStepSize, mZMin, mZMax;

        // Tread to control the 3rd party devices
        private readonly BackgroundWorker mWorkerThread = new BackgroundWorker();

        // Mouse variables
        private bool mMouseDown = false; // Set to 'true' when mouse is held down.
        private Point mCanvasMouseDownPos; // The point where the mouse button was clicked down on the image.
        private Point mCanvasMouseUpPos; // the point where the mouse button was released on the image

        // File Names
        private string mLogFileName;

        // Connected Devices
        private MotorDriver mMotor;
        private agn934xni mScope;

        // Binded Variables
        private ObservableCollection<ThresholdViewModel> mThresholds;
        private ObservableCollection<ScanLevel> mScans = new ObservableCollection<ScanLevel>();
        private ThresholdViewModel mSelectedThreshold;
        private ScanLevel mSelectedScanPoint;

        // Others
        private Constants.state mCurrentState, mPreviousState;
        private int mProbeChangeState = 0;
        private Constants.direction mVerticalScanDirection, mHorizontalScanDirection;
        private Boolean mScanFinished = false;
        private Boolean mScanStarted = false;
        private CountdownTimer mCDTimer;
        private ImageSource mLoadedImage;
        private int mCurrentZScanIndex = 0; // Index for the Z scan level that is selected
        private Boolean mHeatMapPixelClicked = false;
        private Boolean mSelectedThresholdChanged = false;
        private int mDUTHeight = 0;
        private string mScanMode;
        private int mProbeNum;

        public MainWindow()
        {
            InitializeComponent();

            loadUserPreferences();

            // Populate the treshold table from file
            mThresholds = new ObservableCollection<ThresholdViewModel>(ExternalData.GetThresholds());
            dgThresholds.ItemsSource = mThresholds;
            dgThresholds.SelectedIndex = 0;
            drawThreshold((ThresholdViewModel)dgThresholds.SelectedItem);

            // Setup our worker to move the motors and collect data
            mWorkerThread.DoWork += worker_DoWork;
            mWorkerThread.RunWorkerCompleted += worker_RunWorkerCompleted;
            mWorkerThread.WorkerSupportsCancellation = true;

            // Try to connect to the SA and motors
            establishConnections();

            // Start at the initial state
            transitionToState(Constants.state.Overview);
        }

        private void establishConnections()
        {
            // Load the motor drivers and connect to the motors
            if (mMotor == null)
                mMotor = new MotorDriver();

            mMotor.Connect(Convert.ToString(Properties.Settings.Default.MotorCommPort));
            if (mMotor.isOnline())
            {
                lblMotorStatus.Content = "Connected";
                lblMotorStatus.Background = Brushes.Green;
            }
            else
            {
                lblMotorStatus.Content = "Not Connected";
                lblMotorStatus.Background = Brushes.Red;
            }

            // Connect to the spectrum analyzer
            try
            {
                mScope = new agn934xni(Properties.Settings.Default.SAConnectionString, false, false);
                setSAFreqRange();

                if (mScope != null)
                {
                    lblSAStatus.Content = "Connected";
                    lblSAStatus.Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    lblSAStatus.Content = "Not Connected";
                    lblSAStatus.Background = new SolidColorBrush(Colors.Red);
                }
            }
            catch (Exception) { }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker lWorker = sender as BackgroundWorker;

            // run all background tasks here
            if (mHomeMotors)
            {
                double lPrevXPos = mMotorXPos;
                double lPrevYPos = mMotorYPos;
                double lPrevZPos = mMotorZPos;

                mHomeMotors = false;
                moveMotors(0, 0, mMotorZTravelDistance, true);

                if (mCurrentState == Constants.state.Stopped)
                {
                    mMotorXPos = lPrevXPos;
                    mMotorYPos = lPrevYPos;
                    mMotorZPos = lPrevZPos;
                }
            }
            else if (mMoveMotors)
            {
                mMoveMotors = false;
                moveMotors(mMotorXPos, mMotorYPos, mMotorZPos);
            }
            else if (mCurrentState == Constants.state.Scanning)
            {
                while (!mScanFinished && !lWorker.CancellationPending)
                {
                    moveMotors(mMotorXPos, mMotorYPos, mMotorZPos);

                    if (lWorker.CancellationPending)
                        break;

                    getDataAtCurrentLocation();

                    determineNextScanPoint();

                    mCDTimer.pointScanned();
                }

                // If we have finished the scan, home the motors
                if (mScanFinished)
                {
                    moveMotors(0, 0, mMotorZTravelDistance);
                }
            }
            else if (mCurrentState == Constants.state.ProbeChange)
            {
                if (mProbeChangeState == 0)
                {
                    if (moveMotors((mMotorXTravelDistance / 2), mMotorYTravelDistance, 20, true))
                    {
                        mProbeChangeState = 1;
                        MessageBox.Show("1) Disconnect currently connected probe\n2) Connect new probe\n3) Move probe up in cord grip as high as possible. DO NOT TIGHTEN PROBE!\n4) Press OK to move motors further down.",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                if (mProbeChangeState == 1)
                {
                    if (moveMotors((mMotorXTravelDistance / 2), mMotorYTravelDistance, 0))
                    {
                        mProbeChangeState = 2;
                        MessageBox.Show("1) Adjust probe to be 1 mm above table and tighten cord grip\n2) Click OK when ready to return motors to home position.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                if (mProbeChangeState == 2)
                {
                    if (moveMotors(0, 0, mMotorZTravelDistance, true))
                    {
                        mProbeChangeState = 0;
                    }
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (mCurrentState == Constants.state.Scanning && mScanFinished)
            {
                transitionToState(Constants.state.Overview);

                string messageBoxText = "Scan Finished!";
                string caption = "Information";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
            else if (mCurrentState == Constants.state.Scanning)
            {
                transitionToState(Constants.state.Stopped);
            }
            else if (mCurrentState == Constants.state.Stopped)
            {
                // only enable the resume button when this thread has finished
                btnAccept.IsEnabled = true;
                btnCancel.IsEnabled = true;
            }
            else if (mCurrentState == Constants.state.ProbeChange)
            {
                transitionToState(Constants.state.Initial);
            }
        }

        // select image to set scan area and zoom in on picture
        //http://www.codeproject.com/Articles/148503/Simple-Drag-Selection-in-WPF

        private void populateGraph(int aRow, int aCol, int aZPos)
        {
            string[] lLine = Logger.getLineFromFile(mLogFileName + ".csv", Convert.ToString(aRow),
                                                    Convert.ToString(aCol), Convert.ToString(aZPos));
            if (lLine != null)
            {
                int numberOfPoints = lLine.Length - 3;

                Point[] data = new Point[numberOfPoints];
                string[] lStrPoint;

                for (int i = 0; i < numberOfPoints; i++)
                {
                    lStrPoint = lLine[i + 3].Split('|');
                    data[i] = new Point(Convert.ToDouble(lStrPoint[0]), Convert.ToDouble(lStrPoint[1]));

                }

                graph1.Data[1] = data;

                // Only show the section of the treshold that applies to the data collected
                ThresholdViewModel lDerivedThreshold = Utilities.getDerivedThreshold(mSelectedThreshold, (double)(((ScanLevel)dgZScanPoints.SelectedItem).ZPos) - mDUTHeight);
                ThresholdViewModel lThresholdSection = new ThresholdViewModel();
                lThresholdSection.Name = "Trimmed Threshold";
                ThresholdLimitViewModel lPrevLimit = null;

                foreach (ThresholdLimitViewModel lLimit in lDerivedThreshold.Limits)
                {
                    if (lPrevLimit == null)
                        lPrevLimit = lLimit;

                    if (lPrevLimit.Frequency <= data[0].X && lLimit.Frequency >= data[0].X)
                    {
                        lThresholdSection.Limits.Add(new ThresholdLimitViewModel(data[0].X, lPrevLimit.Amplitude));
                    }

                    if (lPrevLimit.Frequency <= data[numberOfPoints - 1].X && lLimit.Frequency >= data[numberOfPoints - 1].X)
                    {
                        lThresholdSection.Limits.Add(new ThresholdLimitViewModel(data[numberOfPoints - 1].X, lPrevLimit.Amplitude));
                    }

                    if (lLimit.Frequency >= data[0].X && lLimit.Frequency <= data[numberOfPoints - 1].X)
                    {
                        lThresholdSection.Limits.Add(lLimit);
                    }

                    lPrevLimit = lLimit;
                }

                drawThreshold(lThresholdSection);
            }
            else
            {
                MessageBox.Show("Still Writing data to file. Please try again.");
            }
        }

        private void drawThreshold(ThresholdViewModel aThreshold)
        {
            try
            {
                if (aThreshold != null && mScanMode == Constants.EField)
                {
                    if (aThreshold.Limits.Count != 0)
                    {
                        Point[] lThresholdLine = new Point[aThreshold.Limits.Count * 2 - 1];
                        int freq = Convert.ToInt32(aThreshold.Limits[0].Frequency);
                        int amp = Convert.ToInt32(aThreshold.Limits[0].Amplitude);
                        int pos = 0;
                        for (int i = 1; i < aThreshold.Limits.Count * 2; i++)
                        {
                            if (i % 2 == 0)
                                freq = Convert.ToInt32(aThreshold.Limits[i / 2].Frequency);
                            else
                                amp = Convert.ToInt32(aThreshold.Limits[i / 2].Amplitude);

                            lThresholdLine[pos] = new Point(freq, amp);
                            pos++;
                        }
                        graph1.Data[0] = lThresholdLine;
                    }
                }
                else
                {
                    graph1.Data[0] = null;
                }
            }
            catch (Exception)
            { }
        }

        private void transitionToState(Constants.state aState)
        {
            mPreviousState = mCurrentState;
            mCurrentState = aState;

            switch (aState)
            {
                case Constants.state.Initial:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = false;

                    btnAccept.Content = "Accept";
                    btnAccept.Background = null;
                    btnAccept.IsEnabled = false;

                    btnHomeMotors.IsEnabled = true;
                    btnChangeProbe.IsEnabled = true;
                    btnMoveTo.IsEnabled = true;
                    btnPreferences.IsEnabled = true;

                    // Enable all GUI Controls
                    setGUIControlIsEnabled(true);

                    // Only remove the image if we are starting a new scan
                    if (mPreviousState == Constants.state.Overview)
                    {
                        // Remmove captured image and heat map
                        mLoadedImage = null;
                        imageCaptured.Source = null;
                    }

                    mHeatMap.Clear(IntensityColorKeyGrid);
                    dgZScanPoints.ItemsSource = null;

                    // Update Status
                    if (mLoadedImage == null)
                        lblStatus.Text = Constants.StatusInitial1;
                    else
                        lblStatus.Text = Constants.StatusInitial2;

                    // Show captured image
                    imageCaptured.Source = mLoadedImage;

                    // Redraw the table area on the image after calibration
                    redrawSelectionObjects();

                    // Only show the canvas if we have an image loaded
                    if (imageCaptured.Source == null)
                    {
                        canvasDrawing.Visibility = Visibility.Collapsed;
                        btnCalibrate.IsEnabled = false;
                    }
                    else
                    {
                        canvasDrawing.Visibility = Visibility.Visible;
                        btnCalibrate.IsEnabled = true;
                    }

                    // Restore the mode to whatever probe is installed
                    mProbeNum = Properties.Settings.Default.ProbeNum;
                    mScanMode = Properties.Settings.Default.ScanMode;

                    updateTitles();

                    setSAFreqRange();

                    break;

                case Constants.state.Calibration:
                    btnHomeMotors.IsEnabled = false;
                    btnChangeProbe.IsEnabled = false;
                    btnMoveTo.IsEnabled = false;
                    btnCancel.IsEnabled = true;

                    // Update Status
                    lblStatus.Text = Constants.StatusCalibration1;

                    // Reset the number of calibration points clicked
                    mCalibrationPoints = 0;

                    redrawSelectionObjects();
                    break;

                case Constants.state.Ready:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    //rbEField.IsEnabled = false;
                    //rbHField.IsEnabled = false;
                    btnHomeMotors.IsEnabled = false;
                    //btnBaseLine.IsEnabled = false;
                    btnChangeProbe.IsEnabled = false;
                    btnMoveTo.IsEnabled = false;
                    btnPreferences.IsEnabled = false;
                    btnCalibrate.IsEnabled = false;

                    // Disable all GUI Controls
                    setGUIControlIsEnabled(false);

                    mScanFinished = false;
                    mScanStarted = false;

                    // Update Status
                    lblStatus.Text = Constants.StatusReady;

                    // Hide drawing canvas
                    canvasDrawing.Visibility = Visibility.Collapsed;

                    // Set the area of where to scan
                    setScanArea();

                    // Show reminder to SA
                    MessageBox.Show("Reminder: Please finalize all spectrum analyzer settings now. (Press 'Enter' on the spectrum analyzer to release remote control)",
                        "Reminder", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case Constants.state.Scanning:
                    // Update Buttons
                    btnCancel.Content = "Stop";
                    btnCancel.Background = Brushes.Red;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = false;

                    btnHomeMotors.IsEnabled = false;

                    // Update Status
                    lblStatus.Text = Constants.StatusScanning;

                    // Initialize the file
                    if (!mScanStarted)
                    {
                        Logger.initializeFile(mLogFileName + ".csv", mXScanPoints, mYScanPoints, mScans.Count, mDUTHeight, Properties.Settings.Default.ScanMode);
                        mScanStarted = true;
                    }

                    // Just in case the user accidently changed the frequency on the SA
                    setSAFreqRange();

                    // Start moving the motors
                    try
                    {
                        mWorkerThread.RunWorkerAsync();
                    }
                    catch (Exception)
                    {
                        string messageBoxText = "Unable to start scanning due to another process that is already running. Please try again.";
                        string caption = "Process already running";
                        MessageBoxButton button = MessageBoxButton.OK;
                        MessageBoxImage icon = MessageBoxImage.Error;
                        MessageBoxResult rsltMessageBox = MessageBox.Show(messageBoxText, caption, button, icon);

                        transitionToState(Constants.state.Stopped);
                    }

                    mCDTimer.Start();

                    break;

                case Constants.state.Stopped:
                    // Update Buttons
                    btnCancel.Content = "Reset";
                    btnCancel.Background = null;

                    btnAccept.Content = "Resume";
                    btnAccept.Background = Brushes.Green;

                    btnHomeMotors.IsEnabled = true;

                    // Update Status
                    lblStatus.Text = Constants.StatusStopped;

                    // Stop the simulator
                    if (mWorkerThread.IsBusy)
                    {
                        btnCancel.IsEnabled = false;
                        btnAccept.IsEnabled = false;
                        mWorkerThread.CancelAsync();
                    }
                    else
                    {
                        btnCancel.IsEnabled = true;
                        btnAccept.IsEnabled = true;
                    }

                    // Stop the count down timer
                    mCDTimer.Stop();

                    break;

                case Constants.state.Overview:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = false;

                    btnAccept.Content = "Accept";
                    btnAccept.Background = null;
                    btnAccept.IsEnabled = false;

                    // Update Status
                    lblStatus.Text = Constants.StatusOverview;

                    btnHomeMotors.IsEnabled = false;
                    btnChangeProbe.IsEnabled = false;
                    btnMoveTo.IsEnabled = false;
                    btnPreferences.IsEnabled = true;

                    // Disable main gui buttons
                    setGUIControlIsEnabled(false);

                    btnChangeProbe.IsEnabled = false;

                    // Disable Menu Items
                    btnCalibrate.IsEnabled = false;

                    if (mLoadedImage == null)
                        btnSaveHeatMap.IsEnabled = false;
                    else
                        btnSaveHeatMap.IsEnabled = true;

                    mVerticalScanDirection = Constants.direction.South;
                    mHorizontalScanDirection = Constants.direction.East;

                    canvasDrawing.Visibility = Visibility.Collapsed;

                    updateTitles();
                    break;

                case Constants.state.ProbeChange:

                    if (!mWorkerThread.IsBusy)
                        mWorkerThread.RunWorkerAsync();

                    break;
            }
        }

        /**
         * 
         * 
         * Button Functions
         * 
         * 
         **/

        /// <summary>
        /// Called on "Calibrate" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_calibrate(object sender, RoutedEventArgs e)
        {
            transitionToState(Constants.state.Calibration);
        }

        /// <summary>
        /// Called on "Cancel" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_cancel(object sender, RoutedEventArgs e)
        {
            if (mMotor.isMoving)
            {
                mMotor.stop();

                if (mWorkerThread.IsBusy)
                {
                    mWorkerThread.CancelAsync();
                }
            }
            else if (mCurrentState == Constants.state.Scanning)
            {
                transitionToState(Constants.state.Stopped);
            }
            else if (mCurrentState == Constants.state.Calibration)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (mCurrentState == Constants.state.Ready)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (mCurrentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Overview);

                mHomeMotors = true;
                mWorkerThread.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Called on "Accept" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_accept(object sender, RoutedEventArgs e)
        {
            if (mCurrentState == Constants.state.Initial && mScanAreaIsSet)
            {
                MessageBoxResult rsltMessageBox = MessageBoxResult.Yes;
                if (mDUTHeight == 0)
                {
                    rsltMessageBox = MessageBox.Show("You have entered a DUT height of 0mm. Are you sure you want to continue?",
                        "Do you want to proceed?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                }

                switch (rsltMessageBox)
                {
                    case MessageBoxResult.Yes:
                        transitionToState(Constants.state.Ready);
                        break;

                    case MessageBoxResult.No:
                        /* Do Nothing */
                        break;
                }
            }
            else if (mCurrentState == Constants.state.Ready)
            {
                // Save the image to be referenced later
                ImagingMinipulation.SaveToJPG((BitmapSource)imageCaptured.Source, mLogFileName + ".jpg");

                transitionToState(Constants.state.Scanning);
            }
            else if (mCurrentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Scanning);
            }
            else if (mCurrentState == Constants.state.Calibration)
            {
                // Only want to save our calibration values if the user clicks
                // the 'Accept' button in the calibration state
                mCanvasCalibrationPoints = Utilities.determineOpositeCorners(mCanvasCalibrationPoints[0], mCanvasCalibrationPoints[1]);

                mImageCalibrationPoints[0] = ImagingMinipulation.getAspectRatioPosition(mCanvasCalibrationPoints[0], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                mImageCalibrationPoints[1] = ImagingMinipulation.getAspectRatioPosition(mCanvasCalibrationPoints[1], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                transitionToState(Constants.state.Initial);
            }
        }

        private void click_newScan(object sender, RoutedEventArgs e)
        {
            String lFileName = string.Format("Scan_{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now);

            if (DialogBox.save(out lFileName, "LOG", lFileName))
            {
                String[] tokens = lFileName.Split('.');
                mLogFileName = tokens[0];
                transitionToState(Constants.state.Initial);
            }
        }

        /// <summary>
        /// Called on "Open" menu item click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_open(object sender, RoutedEventArgs e)
        {
            transitionToState(Constants.state.Overview);
            String lScanMode;
            string lFileName = "";

            if (DialogBox.open(out lFileName, "LOG"))
            {
                String[] tokens = lFileName.Split('.');
                mLogFileName = tokens[0];

                // Read In the Log file
                LogReader.openReport(mLogFileName + ".csv", mHeatMap, IntensityColorKeyGrid, out mDUTHeight, out mScans, out lScanMode);

                dgZScanPoints.ItemsSource = mScans;
                mScanMode = lScanMode;

                updateTitles();

                // Try to read in the scan image with the same file name
                try
                {
                    mLoadedImage = ImagingMinipulation.openImageSource(mLogFileName + ".jpg");
                    imageCaptured.Source = mLoadedImage;

                    btnSaveHeatMap.IsEnabled = true;
                }
                catch (Exception)
                {
                    string messageBoxText = "No image file was found! To correlate the heat map to a scanned component, the image file is needed. Would you like to locate the file?";
                    string caption = "Information";
                    MessageBoxButton button = MessageBoxButton.YesNo;
                    MessageBoxImage icon = MessageBoxImage.Information;
                    MessageBoxResult rsltMessageBox = MessageBox.Show(messageBoxText, caption, button, icon);

                    switch (rsltMessageBox)
                    {
                        case MessageBoxResult.Yes:
                            prompt_ScannedImage();
                            break;

                        case MessageBoxResult.No:
                            /* Do Nothing */
                            break;
                    }
                }
            }
        }

        private void prompt_ScannedImage()
        {
            string lFilePath = "";
            if (DialogBox.open(out lFilePath, "IMAGE"))
            {
                try
                {
                    mLoadedImage = ImagingMinipulation.openImageSource(lFilePath);
                    imageCaptured.Source = mLoadedImage;
                }
                catch (Exception) { }
            }
        }

        private void click_capture_image(object sender, RoutedEventArgs e)
        {
            string lFileName = "";
            if (DialogBox.open(out lFileName, "IMAGE"))
            {
                try
                {
                    mLoadedImage = ImagingMinipulation.openImageSource(lFileName);
                    imageCaptured.Source = mLoadedImage;
                    canvasDrawing.Visibility = Visibility.Visible;
                    btnCalibrate.IsEnabled = true;
                    lblStatus.Text = Constants.StatusInitial2;
                }
                catch (Exception) { }
            }
        }

        private void click_preferences(object sender, RoutedEventArgs e)
        {
            PrefPopup popup = new PrefPopup();
            popup.ShowDialog();

            mMotorXTravelDistance = Properties.Settings.Default.motorXTravelDistance;
            mMotorYTravelDistance = Properties.Settings.Default.motorYTravelDistance;
            mMotorZTravelDistance = Properties.Settings.Default.motorZTravelDistance;

            mThresholds = new ObservableCollection<ThresholdViewModel>(ExternalData.GetThresholds());
            dgThresholds.ItemsSource = mThresholds;
        }

        private void click_captureImage(object sender, RoutedEventArgs e)
        {
            try
            {
                ImageCapturePopup popup = new ImageCapturePopup(gotImage_callback);
                popup.ShowDialog();
            }
            catch (Exception)
            {
                string messageBoxText = "An error occured while trying to load the camera driver. Please confirm driver is installed correctly and try again.";
                string caption = "Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        private void probeChange_callback(String aScanMode, int aProbeNum, int aProbeOffset)
        {
            Properties.Settings.Default.ScanMode = aScanMode;
            Properties.Settings.Default.ProbeNum = aProbeNum;
            Properties.Settings.Default.ProbeOffset = aProbeOffset;
            Properties.Settings.Default.Save();

            updateTitles();

            // Lets now put in the new probe
            transitionToState(Constants.state.ProbeChange);
        }

        private void gotImage_callback(System.Drawing.Bitmap aBitmap)
        {
            if (aBitmap != null)
            {
                try
                {
                    Analyzer lAnalyzer = new Analyzer();
                    System.Drawing.Bitmap lBitmap = lAnalyzer.RemoveFisheye(ref aBitmap, 2500.0);
                    mLoadedImage = ImagingMinipulation.openBitmap(lBitmap);
                    imageCaptured.Source = mLoadedImage;
                    canvasDrawing.Visibility = Visibility.Visible;
                    btnCalibrate.IsEnabled = true;
                    lblStatus.Text = Constants.StatusInitial2;
                }
                catch (Exception) { }
            }
        }

        private void click_MoveTo(object sender, RoutedEventArgs e)
        {
            MoveToPopup popup = new MoveToPopup(moveTo_callback);
            popup.ShowDialog();
        }

        private void moveTo_callback(double X, double Y, double Z)
        {
            mMotorXPos = X;
            mMotorYPos = Y;
            mMotorZPos = Z;
            mMoveMotors = true;
            mWorkerThread.RunWorkerAsync();
        }

        private void click_heatMapMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // for double-click, remove this condition if only want single click
            {
                Point cell = mHeatMap.getClickedCell(sender, e);
                populateGraph((int)cell.X, (int)cell.Y, ((ScanLevel)dgZScanPoints.SelectedItem).ZPos);
                mHeatMapPixelClicked = true;
            }
        }

        private void click_heatMapMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mHeatMapPixelClicked)
            {
                AnalyzeTab.IsSelected = true;
                mHeatMapPixelClicked = false;
            }
        }

        private void click_saveHeatMapImage(object sender, RoutedEventArgs e)
        {
            string lFilePath;

            if (DialogBox.save(out lFilePath, "IMAGE"))
                ImagingMinipulation.SaveToJPG(gridResultsTab, lFilePath);
        }

        private void motorJogRelease_Click(object sender, MouseButtonEventArgs e)
        {
            //mMotor.decelerate();
            //mMotor.getPosition();
        }

        private void motorJogUp_Click(object sender, RoutedEventArgs e)
        {
            //mMotor.sendCommand("F,C,I2M-0,R");
        }

        private void motorJogDown_Click(object sender, RoutedEventArgs e)
        {
            //mMotor.sendCommand("F,C,I2M0,R");
        }

        private void motorJogLeft_Click(object sender, RoutedEventArgs e)
        {

        }

        private void motorJogRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private void dataGridThreshold_MouseUp(object sender, MouseButtonEventArgs e)
        {
            drawThreshold(((ThresholdViewModel)dgThresholds.SelectedItem));
        }

        private void dataGridScanLevel_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        /**
         * 
         * 
         * GUI functions
         * 
         * 
         **/

        // test

        /// <summary>
        /// Called when the mouse button is pressed over the Results Tab grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mCanvasMouseDownPos = e.GetPosition(canvasDrawing);
            mCanvasMouseUpPos = mCanvasMouseDownPos;
            double lx = (mCanvasMouseDownPos.X - mCanvasCalibrationPoints[0].X) / mCanvasXYStepSize;
            double ly = (mCanvasMouseDownPos.Y - mCanvasCalibrationPoints[0].Y) / mCanvasXYStepSize;
            Console.WriteLine("Position: " + lx + "," + ly);

            //Console.WriteLine("X: " + canvasMouseDownPos.X + " Y: " + canvasMouseDownPos.Y);
            if (imageCaptured.Source != null)
            {
                if (mCurrentState == Constants.state.Initial)
                {
                    // Only draw the scan area if we are within the table
                    // calibration area
                    if (mCanvasMouseDownPos.X >= mCanvasCalibrationPoints[0].X &&
                        mCanvasMouseDownPos.Y >= mCanvasCalibrationPoints[0].Y &&
                        mCanvasMouseDownPos.X <= mCanvasCalibrationPoints[1].X &&
                        mCanvasMouseDownPos.Y <= mCanvasCalibrationPoints[1].Y)
                    {
                        mMouseDown = true;

                        // The area selection will always be in position 1
                        if (canvasDrawing.Children.Count > 1)
                        {
                            canvasDrawing.Children.RemoveAt(1);
                        }

                        Draw.Rectangle(canvasDrawing, 0, 0, 0, 0, Brushes.Red, true);
                    }
                }
                else if (mCurrentState == Constants.state.Calibration)
                {
                    mCalibrationPoints++;

                    if (mCalibrationPoints <= 2)
                    {
                        Draw.Circle(canvasDrawing, e.GetPosition(canvasDrawing));
                    }

                    if (mCalibrationPoints == 1)
                    {
                        mCanvasCalibrationPoints[0] = e.GetPosition(imageCaptured);

                        lblStatus.Text = "Now click on the top right corner of the table";

                        btnCancel.IsEnabled = true;
                        btnAccept.IsEnabled = false;
                    }
                    else if (mCalibrationPoints == 2)
                    {
                        mCanvasCalibrationPoints[1] = e.GetPosition(imageCaptured);

                        lblStatus.Text = "Click 'Accept' if rectangle matches table";
                        btnAccept.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the mouse button is released on the Results Tab grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Only do stuff if the user clicked down in the
            // canvas area first.
            if (mMouseDown)
            {
                // Get the selection area
                if (imageCaptured.Source != null && mCurrentState == Constants.state.Initial)
                {
                    mMouseDown = false;

                    if (Math.Ceiling(Math.Abs(mCanvasMouseUpPos.X - mCanvasMouseDownPos.X)) >= (mXYStepSize * mCanvasXYStepSize) &&
                        Math.Ceiling(Math.Abs(mCanvasMouseUpPos.Y - mCanvasMouseDownPos.Y)) >= (mXYStepSize * mCanvasXYStepSize))
                    {
                        mScanAreaIsSet = true;

                        if (!mWorkerThread.IsBusy)
                            btnAccept.IsEnabled = true;

                        lblStatus.Text = Constants.StatusInitial3;
                    }
                    else
                    {
                        mScanAreaIsSet = false;
                        btnAccept.IsEnabled = false;
                        lblStatus.Text = Constants.StatusInitial2;
                    }

                }
            }
        }

        /// <summary>
        /// Called when the mouse is moved over the Results Tab grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// http://stackoverflow.com/questions/1838163/click-and-drag-selection-box-in-wpf
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (mMouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.
                Point lCanvasMousePos = e.GetPosition(canvasDrawing);

                Rectangle selectionBox = (Rectangle)canvasDrawing.Children[1];

                // Restrict movement to image of PCB
                if (lCanvasMousePos.X < mCanvasCalibrationPoints[0].X) lCanvasMousePos.X = mCanvasCalibrationPoints[0].X;
                if (lCanvasMousePos.X > mCanvasCalibrationPoints[1].X) lCanvasMousePos.X = mCanvasCalibrationPoints[1].X;
                if (lCanvasMousePos.Y < mCanvasCalibrationPoints[0].Y) lCanvasMousePos.Y = mCanvasCalibrationPoints[0].Y;
                if (lCanvasMousePos.Y > mCanvasCalibrationPoints[1].Y) lCanvasMousePos.Y = mCanvasCalibrationPoints[1].Y;

                int lXDistance = (int)(Math.Abs(lCanvasMousePos.X - mCanvasMouseDownPos.X) / mCanvasXYStepSize); // in mm
                int lYDistance = (int)(Math.Abs(lCanvasMousePos.Y - mCanvasMouseDownPos.Y) / mCanvasXYStepSize); // in mm

                int lNextXDistance = lXDistance + mXYStepSize - (lXDistance % mXYStepSize);
                int lNextYDistance = lYDistance + mXYStepSize - (lYDistance % mXYStepSize);

                // Position box
                if (mCanvasMouseDownPos.X < lCanvasMousePos.X)
                {
                    Canvas.SetLeft(selectionBox, mCanvasMouseDownPos.X);
                    selectionBox.Width = lNextXDistance * mCanvasXYStepSize;
                    mCanvasMouseUpPos.X = mCanvasMouseDownPos.X + lNextXDistance * mCanvasXYStepSize;
                    Console.WriteLine("Width: " + (mCanvasMouseUpPos.X - mCanvasMouseDownPos.X));
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mCanvasMouseDownPos.X - (lNextXDistance * mCanvasXYStepSize));
                    selectionBox.Width = lNextXDistance * mCanvasXYStepSize;
                    mCanvasMouseUpPos.X = mCanvasMouseDownPos.X - lNextXDistance * mCanvasXYStepSize;
                    Console.WriteLine("Width: " + (mCanvasMouseUpPos.X - mCanvasMouseDownPos.X));
                }

                if (mCanvasMouseDownPos.Y < lCanvasMousePos.Y)
                {
                    Canvas.SetTop(selectionBox, mCanvasMouseDownPos.Y);
                    selectionBox.Height = lNextYDistance * mCanvasXYStepSize;
                    mCanvasMouseUpPos.Y = mCanvasMouseDownPos.Y + lNextYDistance * mCanvasXYStepSize;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mCanvasMouseDownPos.Y - (lNextYDistance * mCanvasXYStepSize));
                    selectionBox.Height = lNextYDistance * mCanvasXYStepSize;
                    mCanvasMouseUpPos.Y = mCanvasMouseDownPos.Y - lNextYDistance * mCanvasXYStepSize;
                }
            }
        }

        /// <summary>
        /// Resizes selection items (i.e. table calibration/scan area) when the window resizes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void redrawSelectionObjects()
        {
            // Only resize stuff if an image has been loaded
            if (imageCaptured.Source != null)
            {
                // Convert from image location to canvas location
                mCanvasCalibrationPoints[0] = ImagingMinipulation.getAspectRatioPosition(mImageCalibrationPoints[0],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                mCanvasCalibrationPoints[1] = ImagingMinipulation.getAspectRatioPosition(mImageCalibrationPoints[1],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                // Delete everything in the selectorCanvas
                canvasDrawing.Children.Clear();

                mCanvasXYStepSize = (mCanvasCalibrationPoints[1].X - mCanvasCalibrationPoints[0].X) / mMotorXTravelDistance;

                // Draw the table area
                if (mCurrentState != Constants.state.Calibration)
                    drawMotorTravelArea();
            }
        }

        private void imageResized(object sender, SizeChangedEventArgs e)
        {
            redrawSelectionObjects();
        }

        /// <summary>
        /// Sets the area for where the probe will need to scan
        /// </summary>
        private void setScanArea()
        {
            // Crop down the image to the selected region
            mCanvasScanArea = Utilities.determineOpositeCorners(mCanvasMouseDownPos, mCanvasMouseUpPos);

            // Zoom in on the scan area
            ImageSource lImage = ImagingMinipulation.cropImage(imageCaptured, mCanvasScanArea[0], mCanvasScanArea[1]);
            imageCaptured.Source = lImage;

            // Convert the bounds to an actual motor positions
            mMotorScanArea[0].X = Math.Round((mCanvasScanArea[0].X - mCanvasCalibrationPoints[0].X) / mCanvasXYStepSize, 0, MidpointRounding.ToEven);
            mMotorScanArea[0].Y = Math.Round((mCanvasScanArea[0].Y - mCanvasCalibrationPoints[0].Y) / mCanvasXYStepSize, 0, MidpointRounding.ToEven);
            mMotorScanArea[1].X = Math.Round((mCanvasScanArea[1].X - mCanvasCalibrationPoints[0].X) / mCanvasXYStepSize, 0, MidpointRounding.ToEven);
            mMotorScanArea[1].Y = Math.Round((mCanvasScanArea[1].Y - mCanvasCalibrationPoints[0].Y) / mCanvasXYStepSize, 0, MidpointRounding.ToEven);

            // Give the motors a starting position
            mMotorXPos = mMotorScanArea[0].X + (double)(mXYStepSize / 2);
            mMotorYPos = mMotorScanArea[0].Y + (double)(mXYStepSize / 2);
            mMotorZPos = mZMin;

            // Update the scan area width and length
            mXScanPoints = (int)Math.Round((mMotorScanArea[1].X - mMotorScanArea[0].X) / mXYStepSize);
            mYScanPoints = (int)Math.Round((mMotorScanArea[1].Y - mMotorScanArea[0].Y) / mXYStepSize);

            // Calculate how long it will take
            int numXYPlanes = (int)Math.Ceiling((double)(mZMax - mZMin) / (double)mZStepSize) + 1;

            mScans.Clear();
            for (int i = 0; i < numXYPlanes; i++)
            {
                int temp = (mZMin + (mZStepSize * i));
                if (temp > mZMax) temp = mZMax;
                ScanLevel lScan = new ScanLevel(temp);
                mScans.Add(lScan);
            }
            dgZScanPoints.ItemsSource = mScans;
            dgZScanPoints.SelectedIndex = 0;
            mCurrentZScanIndex = 0;

            mTotalScanPoints = Convert.ToInt32(mXScanPoints * mYScanPoints * numXYPlanes);

            mCDTimer = null;
            mCDTimer = new CountdownTimer(lblCDTimer, mTotalScanPoints);

            // Initialize the heatmap
            mHeatMap.Create((int)mXScanPoints, (int)mYScanPoints, IntensityColorKeyGrid);
        }

        private void updateMotorPositionLabels()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                lblXPosition.Text = mMotorXPos.ToString();
                lblYPosition.Text = mMotorYPos.ToString();
                lblZPosition.Text = mMotorZPos.ToString();
            }));
        }

        /// <summary>
        /// Saves all of the application settings for furture use when application closes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //srLog.Close(); // close stream to log file
            //disconnectMotors
            mMotor.Disconnect();

            // Save User Preferences
            saveUserPreferences();
        }


        /**
         * 
         * 
         * Helper Functions
         * 
         * 
         **/

        private void drawMotorTravelArea()
        {
            // Draw the table area
            try
            {
                Draw.Rectangle(canvasDrawing,
                    mCanvasCalibrationPoints[0].X,
                    mCanvasCalibrationPoints[0].Y,
                    mCanvasCalibrationPoints[1].X - mCanvasCalibrationPoints[0].X,
                    mCanvasCalibrationPoints[1].Y - mCanvasCalibrationPoints[0].Y,
                    Brushes.Blue);
            }
            catch (Exception) { }
        }

        private void getDataAtCurrentLocation()
        {
            int lCurrentCol = 0;
            int lCurrentRow = 0;

            lCurrentCol = (int)Math.Round((mMotorXPos - ((double)mXYStepSize / 2) - mMotorScanArea[0].X) / mXYStepSize);
            lCurrentRow = (int)Math.Round((mMotorYPos - ((double)mXYStepSize / 2) - mMotorScanArea[0].Y) / mXYStepSize);

            double lMaxDifference = 0;
            Boolean lPassed;

            mScans.ElementAt<ScanLevel>(mCurrentZScanIndex).ScanState = "In Progress";

            Point[] lFinalData = getSAData();

            if (Properties.Settings.Default.ScanMode == Constants.EField)
            {
                Utilities.analyzeScannedData(lFinalData, mThresholds, mSelectedThreshold, mMotorZPos - mDUTHeight, out lPassed, out lMaxDifference);

                mScans.ElementAt<ScanLevel>(mCurrentZScanIndex).pfState = lPassed ? "Passed" : "Failed";
            }
            else
            {
                Utilities.getMaxAmplitude(lFinalData, out lMaxDifference);
            }

            // Save collected data
            Logger.writeToFile(mLogFileName + ".csv", lCurrentCol, lCurrentRow, (int)mMotorZPos, (int)mMotorXPos, (int)mMotorYPos, lFinalData);

            // draw heat map pixel
            if (mMotorZPos == mSelectedScanPoint.ZPos)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    mHeatMap.addIntensityPixel(lCurrentCol, lCurrentRow, Convert.ToInt32(lMaxDifference));
                    mHeatMap.updateIntensityKey(IntensityColorKeyGrid);
                }));
            }
        }

        private Point[] getSAData(Boolean aRemoveNoise = true, Boolean aConvert = true)
        {
            double[] lData = new double[461];
            int lDataLen;
            Point[] lFinalData = new Point[461];
            
            double lMinFreq = (Properties.Settings.Default.ScanMode == Constants.EField ? Properties.Settings.Default.SAEMinFreq : Properties.Settings.Default.SAHMinFreq) * 1000000;
            double lMaxFreq = (Properties.Settings.Default.ScanMode == Constants.EField ? Properties.Settings.Default.SAEMaxFreq : Properties.Settings.Default.SAHMaxFreq) * 1000000;

            double lStepSize = (lMaxFreq - lMinFreq) / 461;
            double lDistance = (mMotorZPos - mDUTHeight) / 1000;

            if (mScope != null)
            {
                // Determine when Acquisition is complete
                int acqStatus;
                mScope.AcquisitionStatus(out acqStatus);
                mScope.AcquisitionStatus(out acqStatus);
                mScope.FetchYTrace("TRACE1", 461, out lDataLen, lData);
                
                if(aRemoveNoise)
                    Utilities.removeNoise(lData, mSABaseLine);

                lFinalData = Utilities.convertArray2Points(lData, lMinFreq, lStepSize);

                if(aConvert)
                    Utilities.convertScannedData(lFinalData, lDistance, Properties.Settings.Default.ScanMode, Properties.Settings.Default.ProbeNum);    
            }

            return lFinalData;
        }

        private void determineNextScanPoint()
        {
            // Move the motors to the next location
            double nextX = mMotorXPos;
            double nextY = mMotorYPos;
            double nextZ = mMotorZPos;

            // Determine the next scan position
            nextY += (mVerticalScanDirection == Constants.direction.South) ? mXYStepSize : -mXYStepSize;

            if (nextY > mMotorScanArea[1].Y || nextY < mMotorScanArea[0].Y)
            {
                mVerticalScanDirection = Utilities.reverseDirection(mVerticalScanDirection);
                nextX += (mHorizontalScanDirection == Constants.direction.East) ? mXYStepSize : -mXYStepSize;
            }

            if (nextX > mMotorScanArea[1].X || nextX < mMotorScanArea[0].X)
            {
                mScans.ElementAt<ScanLevel>(mCurrentZScanIndex).ScanState = "Complete";

                if (nextZ == mZMax)
                {
                    mScanFinished = true;

                    // Stop the count down timer
                    mCDTimer.Stop();
                }
                else
                {
                    nextZ = (nextZ + mZStepSize > mZMax) ? mZMax : nextZ + mZStepSize;

                    mCurrentZScanIndex++;

                    mHorizontalScanDirection = Utilities.reverseDirection(mHorizontalScanDirection);
                }
            }

            // Make sure nextY stays in bounds
            if (nextY > mMotorScanArea[1].Y)
                nextY += -mXYStepSize;
            else if (nextY < mMotorScanArea[0].Y)
                nextY += mXYStepSize;

            // Make sure nextX stays in bounds
            if (nextX > mMotorScanArea[1].X)
                nextX += -mXYStepSize;
            else if (nextX < mMotorScanArea[0].X)
                nextX += mXYStepSize;

            mMotorXPos = nextX;
            mMotorYPos = nextY;
            mMotorZPos = nextZ;
        }

        private int[] getScannedData(int aPoints)
        {
            Random rNum = new Random();
            int[] data = new int[aPoints];

            for (int i = 0; i < aPoints; i++)
            {
                int num = rNum.Next(110) - rNum.Next(150);
                if (num < 0)
                    num = -(num / 2);

                data[i] = num;
                if (i > 420)
                    data[i] += rNum.Next(60) - rNum.Next(20);
            }

            return data;
        }

        private void loadUserPreferences()
        {
            // Load user/application settings
            mImageCalibrationPoints[0] = Properties.Settings.Default.TableBL;
            mImageCalibrationPoints[1] = Properties.Settings.Default.TableTR;

            mMotorXTravelDistance = Properties.Settings.Default.motorXTravelDistance;
            mMotorYTravelDistance = Properties.Settings.Default.motorYTravelDistance;
            mMotorZTravelDistance = Properties.Settings.Default.motorZTravelDistance;

            sHeatMapOpacity.Value = Properties.Settings.Default.heatMapOpacity;

            nsXYStepSize.Value = Properties.Settings.Default.nsXYStepSize;
            nsZStepSize.Value = Properties.Settings.Default.nsZStepSize;

            mScanMode = Properties.Settings.Default.ScanMode;
            mProbeNum = Properties.Settings.Default.ProbeNum;

            mZMin = Properties.Settings.Default.nsZMin;
            mZMax = Properties.Settings.Default.nsZMax;
            nsZMin.Value = mZMin;
            nsZMax.Value = mZMax;

            updateTitles();
        }

        private void saveUserPreferences()
        {
            Properties.Settings.Default["TableBL"] = mImageCalibrationPoints[0];
            Properties.Settings.Default["TableTR"] = mImageCalibrationPoints[1];

            Properties.Settings.Default.motorXTravelDistance = mMotorXTravelDistance;
            Properties.Settings.Default.motorYTravelDistance = mMotorYTravelDistance;
            Properties.Settings.Default.motorZTravelDistance = mMotorZTravelDistance;

            Properties.Settings.Default.nsXYStepSize = mXYStepSize;
            Properties.Settings.Default.nsZStepSize = mZStepSize;
            Properties.Settings.Default.nsZMin = mZMin;
            Properties.Settings.Default.nsZMax = mZMax;

            Properties.Settings.Default.heatMapOpacity = sHeatMapOpacity.Value;

            Properties.Settings.Default.Save();
        }

        private void heatMapOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            mHeatMap.setPixelOpacity(slider.Value);
        }

        private void setGUIControlIsEnabled(Boolean aIsEnabled)
        {
            btnCaptureImage.IsEnabled = aIsEnabled;
            nsXYStepSize.IsEnabled = aIsEnabled;
            nsZStepSize.IsEnabled = aIsEnabled;
            tbDUTHeight.IsEnabled = aIsEnabled;
            nsZMax.IsEnabled = aIsEnabled;
            nsZMin.IsEnabled = aIsEnabled;
            btnDataCollector.IsEnabled = aIsEnabled;
            btnBaseLine.IsEnabled = aIsEnabled;
        }

        public void drawHeatMapFromFile(string aFileName, String aZPos, int aZIndex)
        {
            mHeatMap.ClearPixels();
            string[] lLine = null;
            Boolean lFoundData = false;
            Boolean lFirstLine = true;
            double lValue;
            Boolean lPassed;
            try
            {
                using (StreamReader srLog = new StreamReader(aFileName))
                {
                    // Read the log file
                    while (srLog.Peek() >= 0)
                    {
                        // Skip the header
                        if (lFirstLine)
                        {
                            lFirstLine = false;
                            continue;
                        }

                        // Put the line into an array
                        lLine = srLog.ReadLine().Split(',');

                        // if this line is the Z level we are interested in...
                        if (lLine[2] == aZPos)
                        {
                            lFoundData = true;
                            // Extract the amplitude values from this line
                            Point[] lData = new Point[lLine.Count() - 3];
                            string[] lStrPoint;
                            for (int i = 0; i < lLine.Count() - 3; i++)
                            {
                                lStrPoint = lLine[i + 3].Split('|');
                                lData[i] = new Point(Convert.ToDouble(lStrPoint[0]), Convert.ToDouble(lStrPoint[1]));
                            }

                            if (mScanMode == Constants.EField)
                            {
                                Utilities.analyzeScannedData(lData, mThresholds, mSelectedThreshold, (double)(((ScanLevel)dgZScanPoints.SelectedItem).ZPos) - mDUTHeight, out lPassed, out lValue);

                                mScans.ElementAt<ScanLevel>(aZIndex).pfState = lPassed ? "Passed" : "Failed";
                            }
                            else
                            {
                                Utilities.getMaxAmplitude(lData, out lValue);
                            }

                            mHeatMap.addIntensityPixel(Convert.ToInt32(lLine[0]), Convert.ToInt32(lLine[1]), Convert.ToInt32(lValue));
                        }
                        else if (lFoundData)
                            break;
                    }
                    mHeatMap.updateIntensityKey(IntensityColorKeyGrid);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("File not found: " + aFileName);
            }
        }

        private void dgZScanPoints_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dgZScanPoints.SelectedItem != null)
            {
                mSelectedScanPoint = ((ScanLevel)dgZScanPoints.SelectedItem);
                drawHeatMapFromFile(mLogFileName + ".csv", Convert.ToString(mSelectedScanPoint.ZPos), dgZScanPoints.SelectedIndex);
            }
        }

        private void analyzeGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dgThresholds.SelectedItem != null)
            {
                mSelectedThresholdChanged = true;
                mSelectedThreshold = ((ThresholdViewModel)dgThresholds.SelectedItem);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // Clear all, then transition to the overview state
            mLoadedImage = null;
            imageCaptured.Source = null;
            mHeatMap.Clear(IntensityColorKeyGrid);
            dgZScanPoints.ItemsSource = null;

            transitionToState(Constants.state.Overview);
        }

        /// <summary>
        /// Ensures that the values entered into the numeric steppers are valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StepSizeValidator(object sender, ValueChangedEventArgs<int> e)
        {
            NumericTextBoxInt32 numericStepper = sender as NumericTextBoxInt32;

            // Step size cannot be less than 1
            if (e.NewValue < 1)
                numericStepper.Value = 1;

            DependencyObject ldpObj = sender as DependencyObject;
            string lComponentName = ldpObj.GetValue(FrameworkElement.NameProperty) as string;
            if (lComponentName == "nsXYStepSize")
            {
                if (e.NewValue > mMotorXTravelDistance)
                    numericStepper.Value = (int)mMotorXTravelDistance;

                mXYStepSize = numericStepper.Value;
            }
            else if (lComponentName == "nsZStepSize")
            {
                if (e.NewValue > mMotorZTravelDistance)
                    numericStepper.Value = (int)mMotorZTravelDistance;

                mZStepSize = numericStepper.Value;
            }
        }

        private void click_reconnect(object sender, RoutedEventArgs e)
        {
            establishConnections();
        }

        private void click_changeProbe(object sender, RoutedEventArgs e)
        {
            ProbeChangePopup popup = new ProbeChangePopup(probeChange_callback);
            popup.ShowDialog();
        }

        private void btnHomeMotors_Click(object sender, RoutedEventArgs e)
        {
            mHomeMotors = true;
            mWorkerThread.RunWorkerAsync();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private Boolean moveMotors(double X, double Y, double Z, Boolean aFast = false)
        {
            string lCancelContent = "Error";
            Brush lCancelBackground = Brushes.Red;
            Boolean lCancelIsEnabled = false;
            Boolean lHomeMotorsIsEnabled = false;
            Boolean lMoveMotorsIsEnabled = false;
            Boolean lAcceptIsEnabled = false;
            Boolean lChangeProbeIsEnabled = false;
            Boolean lSuccess = false;

            // Save button states and update values
            this.Dispatcher.Invoke((Action)(() =>
            {
                lCancelContent = Convert.ToString(btnCancel.Content);
                lCancelBackground = btnCancel.Background;
                lCancelIsEnabled = btnCancel.IsEnabled;

                lHomeMotorsIsEnabled = btnHomeMotors.IsEnabled;
                lMoveMotorsIsEnabled = btnMoveTo.IsEnabled;
                lAcceptIsEnabled = btnAccept.IsEnabled;
                lChangeProbeIsEnabled = btnChangeProbe.IsEnabled;

                btnCancel.Content = "Stop";
                btnCancel.Background = Brushes.Red;
                btnCancel.IsEnabled = true;

                btnMoveTo.IsEnabled = false;
                btnHomeMotors.IsEnabled = false;
                btnAccept.IsEnabled = false;
                btnChangeProbe.IsEnabled = false;
                btnReconnect.IsEnabled = false;
            }));

            mMotorXPos = X;
            mMotorYPos = Y;
            mMotorZPos = Z;
            updateMotorPositionLabels();

            // Ready to move the motors
            if (mMotor.isOnline())
            {
                // Account for probe offset (if necessary)
                int lX = (int)X - Properties.Settings.Default.ProbeOffset < 0 ? 0 : (int)X - Properties.Settings.Default.ProbeOffset;

                lSuccess = mMotor.move(lX, (int)Y, (int)(mMotorZTravelDistance - Z), aFast);
            }
            else
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    lblMotorStatus.Content = "Not Connected";
                    lblMotorStatus.Background = Brushes.Red;
                }));
            }

            // Restore buttons to previous state
            this.Dispatcher.Invoke((Action)(() =>
            {
                btnCancel.Content = lCancelContent;
                btnCancel.Background = lCancelBackground;
                btnCancel.IsEnabled = lCancelIsEnabled;
                btnHomeMotors.IsEnabled = lHomeMotorsIsEnabled;
                btnMoveTo.IsEnabled = lMoveMotorsIsEnabled;
                btnReconnect.IsEnabled = true;

                if (mCurrentState == Constants.state.Initial)
                {
                    if (mScanAreaIsSet)
                        btnAccept.IsEnabled = true;
                    else
                        btnAccept.IsEnabled = false;
                }
                else
                    btnAccept.IsEnabled = lAcceptIsEnabled;

                btnChangeProbe.IsEnabled = lChangeProbeIsEnabled;
            }));

            return lSuccess;
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (ResultsTab.IsSelected && mLogFileName != null && mSelectedScanPoint != null && mSelectedThresholdChanged)
                {
                    mSelectedThresholdChanged = false;
                    drawHeatMapFromFile(mLogFileName + ".csv", Convert.ToString(mSelectedScanPoint.ZPos), dgZScanPoints.SelectedIndex);
                }
            }
        }

        private void updateTitles()
        {
            if (mScanMode == Constants.EField)
            {
                lblAnalyzeGraphTitle.Content = "dBuV/m vs MHz";
                lblHeatMapUnits.Content = "dBuV/m";

                lblMode.Text = "E-Field";
            }
            else if (mScanMode == Constants.HField)
            {
                lblAnalyzeGraphTitle.Content = "A/m vs MHz";
                lblHeatMapUnits.Content = "A/m";

                lblMode.Text = "H-Field " + getProbeName(mProbeNum);
            }
        }

        private string getProbeName(int aProbeNum)
        {
            string lResult = "";
            switch (aProbeNum)
            {
                case 1:
                    lResult = "6 cm Probe";
                    break;

                case 2:
                    lResult = "3 cm Probe";
                    break;

                case 3:
                    lResult = "1 cm Probe";
                    break;
            }
            return lResult;
        }

        public int DUTHeight
        {
            get { return mDUTHeight; }
            set
            {
                mDUTHeight = value;

                if (mZMax < mDUTHeight)
                {
                    mZMax = (int)mDUTHeight + 1;
                }

                if (mZMin < mDUTHeight)
                {
                    mZMin = (int)mDUTHeight + 1;
                }

                nsZMax.Value = mZMax;
                nsZMin.Value = mZMin;
            }
        }

        public int ZMin
        {
            get { return mZMin; }
            set
            {
                mZMin = value;

                if (mZMin < mDUTHeight + 1)
                    mZMin = (int)mDUTHeight + 1;

                else if (mZMin > mZMax)
                    mZMin = mZMax;
            }
        }

        public int ZMax
        {
            get { return mZMax; }
            set
            {
                mZMax = value;

                if (mZMax < mDUTHeight + 1)
                    mZMax = (int)mDUTHeight + 1;

                else if (mZMax < mZMin)
                    mZMax = mZMin;
            }
        }

        private void btnDataCollector_Click(object sender, RoutedEventArgs e)
        {
            Point[] lFinalData = getSAData(true, true);
            graph1.Data[1] = lFinalData;
            AnalyzeTab.IsSelected = true;
        }

        private void btnGetBaseLine_Click(object sender, RoutedEventArgs e)
        {
            Point[] lFinalData = getSAData(false, false);

            double lSum = 0;
            foreach (Point lPoint in lFinalData)
            {
                lSum += lPoint.Y;
            }
            double lAverage = lSum / lFinalData.Length;

            for (int i = 0; i < lFinalData.Length; i++)
            {
                lFinalData[i].Y = lAverage - lFinalData[i].Y;
            }

            mSABaseLine = lFinalData;
        }

        private void setSAFreqRange()
        {
            if (mScope != null)
            {
                double lMinFreq = (Properties.Settings.Default.ScanMode == Constants.EField ? Properties.Settings.Default.SAEMinFreq : Properties.Settings.Default.SAHMinFreq) * 1000000;
                double lMaxFreq = (Properties.Settings.Default.ScanMode == Constants.EField ? Properties.Settings.Default.SAEMaxFreq : Properties.Settings.Default.SAHMaxFreq) * 1000000;

                mScope.ConfigureFrequencyStartStop(lMinFreq, lMaxFreq);
            }
        }
    }
}
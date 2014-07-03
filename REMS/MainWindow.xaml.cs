using System;
using System.Diagnostics;
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
using NationalInstruments;
using REMS.classes;
using REMS.popups;
using REMS.data;
using System.Windows.Threading;
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
        //private Boolean mCalibrationSet = false;
        private int mCalibrationPoints = 0;

        // Scan area variables
        private Point[] mCanvasScanArea = new Point[2]; // the selected are to scan selected by the user
        private Point[] mMotorScanAreaPoints = new Point[2]; // the bounds for the motor to travel
        private Boolean mScanAreaIsSet = false;
        private int motorXPos, motorYPos, motorZPos;
        private int mXScanPoints, mYScanPoints = 0;
        private int mTotalScanPoints = 0;
        
        // Saved Setting Variables
        private double mMotorXTravelDistance;
        private double mMotorYTravelDistance;
        private double mMotorZTravelDistance;
        private int mSAMinFrequency;
        private int mSAMaxFrequency;

        // Tread to control the 3rd party devices
        private readonly BackgroundWorker worker = new BackgroundWorker();

        // Mouse variables
        private bool mMouseDown = false; // Set to 'true' when mouse is held down.
        private Point mCanvasMouseDownPos; // The point where the mouse button was clicked down on the image.
        private Point mCanvasMouseUpPos; // the point where the mouse button was released on the image
        
        // File Names
        private string mLogFileName;

        // Connected Devices
        private Motor mMotor;

        // Binded Variables
        private ObservableCollection<ThresholdViewModel> mThresholds;
        private ObservableCollection<ScanLevel> mScans = new ObservableCollection<ScanLevel>();
        private ThresholdViewModel mSelectedThreshold;
        private ScanLevel mSelectedScanPoint;

        // Others
        private Constants.state mCurrentState, mPreviousState;
        private Constants.direction mVerticalScanDirection, mHorizontalScanDirection;
        private Boolean mScanFinished = false;
        private CountdownTimer mCDTimer;
        private ImageSource mLoadedImage;
        private int mCurrentZScanIndex = 0; // Index for the Z scan level that is selected
        private Boolean mHeatMapPixelClicked = false;

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
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerSupportsCancellation = true;

            // Load the motor drivers and connect to the motors
            mMotor = new Motor();
            mMotor.connect();

            // Start at the initial state
            transitionToState(Constants.state.Overview);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker wk = sender as BackgroundWorker;

            // run all background tasks here
            if (mCurrentState == Constants.state.Initial)
            {
                mMotor.home();
            }
            else if (mCurrentState == Constants.state.Scanning)
            {
                while (!mScanFinished)
                {
                    if (wk.CancellationPending)
                    {
                        break;
                    }

                    getDataAtCurrentLocation();

                    // Determine the next scan location or if we have
                    // finished the entire scan
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        determineNextScanPoint();
                    }));

                    if (!mScanFinished)
                        mMotor.move(motorXPos, motorYPos, motorZPos);
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
                double stepSize = (double)(mSAMaxFrequency - mSAMinFrequency) / (numberOfPoints);
                double currFreq = mSAMinFrequency;

                Point[] data = new Point[numberOfPoints];

                for (int i = 0; i < numberOfPoints; i++)
                {
                    data[i] = new Point(currFreq, Convert.ToInt32(lLine[i + 3]));

                    currFreq += stepSize;
                }

                graph1.Data[1] = data;
            }
            else
            {
                MessageBox.Show("Still Writing data to file. Please try again.");
            }
        }

        private void drawThreshold(ThresholdViewModel aThreshold)
        {
            if (aThreshold != null)
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

                    btnCalibrate.IsEnabled = true;

                    // Enable all GUI Controls
                    setGUIControlIsEnabled(true);

                    btnCaptureImage.IsEnabled = true;

                    mVerticalScanDirection = Constants.direction.South;
                    mHorizontalScanDirection = Constants.direction.East;
                    dgZScanPoints.ItemsSource = null;

                    if (mPreviousState == Constants.state.Overview)
                    {
                        // Remmove captured image and heat map
                        mLoadedImage = null;
                    }

                    mHeatMap.Clear(IntensityColorKeyGrid);

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
                    break;

                case Constants.state.Calibration:
                    // Update Status
                    lblStatus.Text = Constants.StatusCalibration1;

                    // Reset the number of calibration points clicked
                    mCalibrationPoints = 0;

                    // Delete everything in the selectorCanvas
                    canvasDrawing.Children.Clear();

                    //if (mCalibrationSet)
                    //{
                        // Draw the table area
                        drawMotorTravelArea();
                    //}
                    break;

                case Constants.state.Ready:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    // Disable all GUI Controls
                    setGUIControlIsEnabled(false);

                    btnCalibrate.IsEnabled = false;

                    // Update Status
                    lblStatus.Text = Constants.StatusReady;

                    // Hide drawing canvas
                    canvasDrawing.Visibility = Visibility.Collapsed;

                    // Set the area of where to scan
                    setScanArea();
                    break;

                case Constants.state.Scanning:
                    // Update Buttons
                    btnCancel.Content = "Stop";
                    btnCancel.Background = Brushes.Red;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = false;

                    mScanFinished = false;

                    // Update Status
                    lblStatus.Text = Constants.StatusScanning;

                    // Hide drawing canvas
                    canvasDrawing.Visibility = Visibility.Collapsed;

                    // Start moving the motors
                    worker.RunWorkerAsync();

                    break;

                case Constants.state.Stopped:
                    // Update Buttons
                    btnCancel.Content = "Reset";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Resume";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    // Update Status
                    lblStatus.Text = Constants.StatusStopped;

                    // Stop the simulator
                    //simulatorTimer.Stop();
                    if (worker.IsBusy == true && worker.WorkerSupportsCancellation == true)
                    {
                        worker.CancelAsync();
                    }
                    //mRunScan = false;

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

                    // Disable main gui buttons
                    setGUIControlIsEnabled(false);

                    // Disable Menu Items
                    btnCalibrate.IsEnabled = false;
                    btnCaptureImage.IsEnabled = false;

                    if (mLoadedImage == null)
                        btnSaveHeatMap.IsEnabled = false;
                    else
                        btnSaveHeatMap.IsEnabled = true;

                    canvasDrawing.Visibility = Visibility.Collapsed;

                    // Home the motors
                    worker.RunWorkerAsync();
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
            if (mCurrentState == Constants.state.Calibration)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (mCurrentState == Constants.state.Ready)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (mCurrentState == Constants.state.Scanning)
            {
                transitionToState(Constants.state.Stopped);
            }
            else if (mCurrentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Overview);
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
                transitionToState(Constants.state.Ready);
            }
            else if (mCurrentState == Constants.state.Ready)
            {
                // Save the image to be referenced later
                Imaging.SaveToJPG((BitmapSource)imageCaptured.Source, mLogFileName + ".jpg");

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

                mImageCalibrationPoints[0] = Imaging.getAspectRatioPosition(mCanvasCalibrationPoints[0], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                mImageCalibrationPoints[1] = Imaging.getAspectRatioPosition(mCanvasCalibrationPoints[1], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                //mCalibrationSet = true;

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

            string lFileName = "";

            if (DialogBox.open(out lFileName, "LOG"))
            {
                String[] tokens = lFileName.Split('.');
                mLogFileName = tokens[0];

                // Read In the Log file
                LogReader.openReport(mLogFileName + ".csv", mHeatMap, dgZScanPoints, IntensityColorKeyGrid);

                // Try to read in the scan image with the same file name
                try
                {
                    mLoadedImage = Imaging.openImageSource(mLogFileName + ".jpg");
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
                    mLoadedImage = Imaging.openImageSource(lFilePath);
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
                    mLoadedImage = Imaging.openImageSource(lFileName);
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
            loadUserPreferences();

            mThresholds = new ObservableCollection<ThresholdViewModel>(ExternalData.GetThresholds());
            dgThresholds.ItemsSource = mThresholds;
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
                Imaging.SaveToJPG(gridResultsTab, lFilePath);
        }

        private void motorJogUp_Click(object sender, RoutedEventArgs e)
        {
            //Vxm.DriverTerminalShowState(1, 0);
            //Vxm.PortOpen(1, 9600);
            //Vxm.PortSendCommands("F,C,I1M1000,I1M-1000,R");
            //Vxm.PortClose();
            //Vxm.DriverTerminalShowState(0, 0);
        }

        private void motorJogDown_Click(object sender, RoutedEventArgs e)
        {
            //int status = 0;

            //status = Vxm.PortOpen(3, 9600);
            //Console.WriteLine("Connection to Motor: " + (status == 1 ? "Success" : "Failed"));

            //Vxm.PortSendCommands("F,C,I1M1000,I1M-1000,R");
            //Vxm.PortWaitForChar("^", 0);
            //Vxm.PortClose();
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
                        //DrawScanArea();

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
                // Get the mouse up location
                //canvasMouseUpPos = e.GetPosition(imageCaptured);

                // Get the selection area
                if (imageCaptured.Source != null && mCurrentState == Constants.state.Initial)
                {
                    mMouseDown = false;

                    if (mCanvasMouseDownPos.X != mCanvasMouseUpPos.X && mCanvasMouseDownPos.Y != mCanvasMouseUpPos.Y)
                    {
                        mScanAreaIsSet = true;
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
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (mMouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.
                Point mousePos = e.GetPosition(canvasDrawing);

                Rectangle selectionBox = (Rectangle)canvasDrawing.Children[1];

                // Restrict movement to image of PCB
                if (mousePos.X < mCanvasCalibrationPoints[0].X) mousePos.X = mCanvasCalibrationPoints[0].X;
                if (mousePos.X > mCanvasCalibrationPoints[1].X) mousePos.X = mCanvasCalibrationPoints[1].X;
                if (mousePos.Y < mCanvasCalibrationPoints[0].Y) mousePos.Y = mCanvasCalibrationPoints[0].Y;
                if (mousePos.Y > mCanvasCalibrationPoints[1].Y) mousePos.Y = mCanvasCalibrationPoints[1].Y;

                mCanvasMouseUpPos = mousePos;

                // Position box
                if (mCanvasMouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(canvasDrawing.Children[1], mCanvasMouseDownPos.X);
                    selectionBox.Width = mousePos.X - mCanvasMouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = mCanvasMouseDownPos.X - mousePos.X;
                }

                if (mCanvasMouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, mCanvasMouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - mCanvasMouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = mCanvasMouseDownPos.Y - mousePos.Y;
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
                mCanvasCalibrationPoints[0] = Imaging.getAspectRatioPosition(mImageCalibrationPoints[0],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                mCanvasCalibrationPoints[1] = Imaging.getAspectRatioPosition(mImageCalibrationPoints[1],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                // Delete everything in the selectorCanvas
                canvasDrawing.Children.Clear();

                // Draw the table area
                //if (mCalibrationSet)
                //{
                    drawMotorTravelArea();
                //}
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
            ImageSource lImage = Imaging.cropImage(imageCaptured, mCanvasScanArea[0], mCanvasScanArea[1]);
            imageCaptured.Source = lImage;

            double canvasXStepSize = (mCanvasCalibrationPoints[1].X - mCanvasCalibrationPoints[0].X) / mMotorXTravelDistance;
            double canvasYStepSize = (mCanvasCalibrationPoints[1].Y - mCanvasCalibrationPoints[0].Y) / mMotorYTravelDistance;

            // Convert the bounds to an actual motor positions
            mMotorScanAreaPoints[0].X = Math.Round((mCanvasScanArea[0].X - mCanvasCalibrationPoints[0].X) / canvasXStepSize, 0, MidpointRounding.ToEven);
            mMotorScanAreaPoints[0].Y = Math.Round((mCanvasScanArea[0].Y - mCanvasCalibrationPoints[0].Y) / canvasYStepSize, 0, MidpointRounding.ToEven);
            mMotorScanAreaPoints[1].X = Math.Round((mCanvasScanArea[1].X - mCanvasCalibrationPoints[0].X) / canvasXStepSize, 0, MidpointRounding.ToEven);
            mMotorScanAreaPoints[1].Y = Math.Round((mCanvasScanArea[1].Y - mCanvasCalibrationPoints[0].Y) / canvasYStepSize, 0, MidpointRounding.ToEven);

            // Give the motors a starting position
            motorXPos = Convert.ToInt32(mMotorScanAreaPoints[0].X);
            motorYPos = Convert.ToInt32(mMotorScanAreaPoints[0].Y);
            motorZPos = nsZMin.Value;

            // Update the scan area width and length
            mXScanPoints = (int)((mMotorScanAreaPoints[1].X - mMotorScanAreaPoints[0].X) / nsXStepSize.Value) + 1;
            mYScanPoints = (int)((mMotorScanAreaPoints[1].Y - mMotorScanAreaPoints[0].Y) / nsYStepSize.Value) + 1;

            // Calculate how long it will take
            int numXYPlanes = (int)Math.Ceiling((double)(nsZMax.Value - nsZMin.Value) / (double)nsZStepSize.Value) + 1;

            mScans.Clear();
            for (int i = 0; i < numXYPlanes; i++)
            {
                int temp = (int)(nsZMin.Value + (nsZStepSize.Value * i));
                if (temp > (int)nsZMax.Value) temp = (int)nsZMax.Value;
                ScanLevel lScan = new ScanLevel(temp);
                mScans.Add(lScan);
            }
            dgZScanPoints.ItemsSource = mScans;
            dgZScanPoints.SelectedIndex = 0;

            mTotalScanPoints = Convert.ToInt32(mXScanPoints * mYScanPoints * numXYPlanes);

            mCDTimer = new CountdownTimer(lblCDTimer, mTotalScanPoints);

            // Initialize the heatmap
            mHeatMap.Create((int)mXScanPoints, (int)mYScanPoints, IntensityColorKeyGrid);

            lblXPosition.Text = motorXPos.ToString();
            lblYPosition.Text = motorYPos.ToString();
            lblZPosition.Text = motorZPos.ToString();
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
            mMotor.disconnect();

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

            this.Dispatcher.Invoke((Action)(() =>
            {
                lCurrentCol = (motorXPos - (int)mMotorScanAreaPoints[0].X) / (int)nsXStepSize.Value;
                lCurrentRow = (motorYPos - (int)mMotorScanAreaPoints[0].Y) / (int)nsYStepSize.Value;
            }));

            int lMaxDifference;
            Boolean lPassed;

            mScans.ElementAt<ScanLevel>(mCurrentZScanIndex).State = "In Progress";

            int[] lData = getScannedData(461);
            Utilities.analyzeScannedData(lData, mSAMinFrequency, mSAMaxFrequency, mSelectedThreshold, out lPassed, out lMaxDifference);

            // Save collected data
            Logger.writeToFile(mLogFileName + ".csv", lCurrentCol, lCurrentRow, motorZPos, lData);

            // draw heat map pixel
            if (motorZPos == mSelectedScanPoint.ZPos)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    mHeatMap.addIntensityPixel(lCurrentCol, lCurrentRow, lMaxDifference);
                    mHeatMap.updateIntensityKey(IntensityColorKeyGrid);
                }));
            }
        }

        private void determineNextScanPoint()
        {
            // Move the motors to the next location
            int nextX = motorXPos;
            int nextY = motorYPos;
            int nextZ = motorZPos;

            // Update the motor position text
            lblXPosition.Text = nextX.ToString();
            lblYPosition.Text = nextY.ToString();
            lblZPosition.Text = nextZ.ToString();

            // Determine the next scan position
             
            nextY += (mVerticalScanDirection == Constants.direction.South) ? nsYStepSize.Value : -nsYStepSize.Value;

            if (nextY > mMotorScanAreaPoints[1].Y || nextY < mMotorScanAreaPoints[0].Y)
            {
                mVerticalScanDirection = Utilities.reverseDirection(mVerticalScanDirection);
                nextX += (mHorizontalScanDirection == Constants.direction.East) ? nsXStepSize.Value : -nsXStepSize.Value;
            }

            if (nextX > mMotorScanAreaPoints[1].X || nextX < mMotorScanAreaPoints[0].X)
            {
                mScans.ElementAt<ScanLevel>(mCurrentZScanIndex).State = "Complete";

                if (nextZ == nsZMax.Value)
                {
                    mScanFinished = true;
                }
                else
                {
                    nextZ = (nextZ + nsZStepSize.Value > nsZMax.Value) ? nsZMax.Value : nextZ + nsZStepSize.Value;

                    mCurrentZScanIndex++;

                    mHorizontalScanDirection = Utilities.reverseDirection(mHorizontalScanDirection);
                }
            }

            // Make sure nextY stays in bounds
            if (nextY > mMotorScanAreaPoints[1].Y)
                nextY += -nsYStepSize.Value;
            else if (nextY < mMotorScanAreaPoints[0].Y)
                nextY += nsYStepSize.Value;

            // Make sure nextX stays in bounds
            if (nextX > mMotorScanAreaPoints[1].X)
                nextX += -nsXStepSize.Value;
            else if (nextX < mMotorScanAreaPoints[0].X)
                nextX += nsXStepSize.Value;

            motorXPos = nextX;
            motorYPos = nextY;
            motorZPos = nextZ;
        }

        /// <summary>
        /// Ensures that the values entered into the numeric steppers are valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsValidator(object sender, ValueChangedEventArgs<int> e)
        {
            // Step size cannot be less than 1
            if (e.NewValue < 1)
            {
                NumericTextBoxInt32 numericStepper = sender as NumericTextBoxInt32;
                numericStepper.Value = 1;
            }
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
                //int num = 85;

                data[i] = num;
                if (i > 420)
                    data[i] += rNum.Next(60) - rNum.Next(20);
            }

            return data;
        }

        private void loadUserPreferences()
        {
            // Load user/application settings
            mImageCalibrationPoints[0] = (Point)Properties.Settings.Default["TableBL"];
            mImageCalibrationPoints[1] = (Point)Properties.Settings.Default["TableTR"];

            mMotorXTravelDistance = Properties.Settings.Default.motorXTravelDistance;
            mMotorYTravelDistance = Properties.Settings.Default.motorYTravelDistance;
            mMotorZTravelDistance = Properties.Settings.Default.motorZTravelDistance;

            sHeatMapOpacity.Value = Properties.Settings.Default.heatMapOpacity;

            mSAMinFrequency = Properties.Settings.Default.SAMinFrequency;
            mSAMaxFrequency = Properties.Settings.Default.SAMaxFrequency;

            nsXStepSize.Value = (int)Properties.Settings.Default["nsXStepSize"];
            nsYStepSize.Value = (int)Properties.Settings.Default["nsYStepSize"];
            nsZStepSize.Value = (int)Properties.Settings.Default.nsZStepSize;
            nsZMin.Value = (int)Properties.Settings.Default.nsZMin;
            nsZMax.Value = (int)Properties.Settings.Default.nsZMax;
        }

        private void saveUserPreferences()
        {
            Properties.Settings.Default["TableBL"] = mImageCalibrationPoints[0];
            Properties.Settings.Default["TableTR"] = mImageCalibrationPoints[1];

            Properties.Settings.Default.motorXTravelDistance = mMotorXTravelDistance;
            Properties.Settings.Default.motorYTravelDistance = mMotorYTravelDistance;
            Properties.Settings.Default.motorZTravelDistance = mMotorZTravelDistance;

            Properties.Settings.Default["nsXStepSize"] = nsXStepSize.Value;
            Properties.Settings.Default["nsYStepSize"] = nsYStepSize.Value;
            Properties.Settings.Default.nsZStepSize = nsZStepSize.Value;
            Properties.Settings.Default.nsZMin = nsZMin.Value;
            Properties.Settings.Default.nsZMax = nsZMax.Value;

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
            btnMotorJogUp.IsEnabled = aIsEnabled;
            btnMotorJogDown.IsEnabled = aIsEnabled;
            btnMotorJogLeft.IsEnabled = aIsEnabled;
            btnMotorJogRight.IsEnabled = aIsEnabled;

            nsXStepSize.IsEnabled = aIsEnabled;
            nsYStepSize.IsEnabled = aIsEnabled;
            nsZStepSize.IsEnabled = aIsEnabled;
            nsZMax.IsEnabled = aIsEnabled;
            nsZMin.IsEnabled = aIsEnabled;
        }

        public void drawHeatMapFromFile(string aFileName, String aZPos)
        {
            mHeatMap.ClearPixels();
            string[] lLine = null;
            Boolean lFoundData = false;
            Boolean lPassed;
            int lValue;
            try
            {
                using (StreamReader srLog = new StreamReader(aFileName))
                {
                    // Read the log file
                    while (srLog.Peek() >= 0)
                    {
                        // Put the line into an array
                        lLine = srLog.ReadLine().Split(',');

                        // if this line is the Z level we are interested in...
                        if (lLine[2] == aZPos)
                        {
                            lFoundData = true;
                            // Extract the amplitude values from this line
                            int[] lData = new int[lLine.Count() - 3];
                            for (int i = 0; i < lLine.Count() - 3; i++)
                            {
                                lData[i] = Convert.ToInt32(lLine[i + 3]);
                            }

                            Utilities.analyzeScannedData(lData, mSAMinFrequency, mSAMaxFrequency, mSelectedThreshold, out lPassed, out lValue);                            

                            mHeatMap.addIntensityPixel(Convert.ToInt32(lLine[0]), Convert.ToInt32(lLine[1]), lValue);
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
                drawHeatMapFromFile(mLogFileName + ".csv", Convert.ToString(mSelectedScanPoint.ZPos));
            }
        }

        private void analyzeGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (dgThresholds.SelectedItem != null)
            {
                mSelectedThreshold = ((ThresholdViewModel)dgThresholds.SelectedItem);

            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            transitionToState(Constants.state.Initial);
        }
    }
}
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
using NationalInstruments;
using REMS.classes;
using REMS.popups;
using System.Windows.Threading;
using NationalInstruments.Controls;

namespace REMS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window //maybe should try 'implement' REMSProperties
    {

        // Calibration Variables
        private Point[] imageCalibrationPoints = new Point[2]; // where actually on the image is the calibration points
        private Point[] canvasCalibrationPoints = new Point[2]; // which calibration points have been collected
        private Boolean mCalibrationSet = false;

        private Point[] canvasScanArea = new Point[2];
        private Point[] motorScanAreaPoints = new Point[2]; // the bounds for the motor to travel

        public double motorXTravelDistance;
        public double motorYTravelDistance;
        public double motorZTravelDistance;
        //private int motorZTravelDistance = 100;

        // Timers
        private DispatcherTimer simulatorTimer;
        private CountdownTimer CDTimer;
        
        private double canvasXStepSize, canvasYStepSize;

        //private double heatMapOpacity;
        
        private int numOfCalibrationPoints = 0;

        //private Boolean imageLoaded = false;
        public Boolean scanAreaSet = false;
        private Constants.state currentState = Constants.state.Initial;
        private Constants.state previousState;
        private Constants.direction currentScanDirection = Constants.direction.South;
        
        private ImageSource mLoadedImage;

        

        bool mouseDown = false; // Set to 'true' when mouse is held down.
        Point canvasMouseDownPos; // The point where the mouse button was clicked down on the image.
        Point canvasMouseUpPos;
        int motorXPos, motorYPos, motorZPos;
        
        private double xScanPoints = 0;
        private double yScanPoints = 0;
        //private double zScanPoints = 0;

        //AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(0);
        
        // File Names
        private string mFileName = "";
        private string mLogFileName = "log.csv";
        
        private Boolean mHeatMapPixelClicked = false;

        public MainWindow()
        {
            InitializeComponent();
            
            analyzeGrid.ItemsSource = LoadCollectionData();

            loadUserPreferences();
            transitionToState(Constants.state.Initial);
        }

        // TODO: Show current x,y,z location on main panel
        // TODO: Implement selection area
        // TODO: Add "Take Picture" button somewhere

        // select image to set scan area and zoom in on picture
        //http://www.codeproject.com/Articles/148503/Simple-Drag-Selection-in-WPF

        private List<AnalyzeSynopsisData> LoadCollectionData()
        {

            List<AnalyzeSynopsisData> authors = new List<AnalyzeSynopsisData>();

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 1",
                state = "passed"
            });

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 2",
                state = "failed"
            });

            authors.Add(new AnalyzeSynopsisData()
            {
                threshold = "Test 3",
                state = "passed"
            });


            return authors;
        }

        private void populateGraph(int x, int y, int rows, int columns)
        {
            int numberOfPoints = 461;
            int lineNum = (x * rows) + y + 1;
                
            string line = Logger.getLineFromFile(mLogFileName, lineNum);
            if (line != null)
            {
                //AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(numberOfPoints);

                int[] data = new int[numberOfPoints];
                var temp = line.Split(',');
                for (int i = 3; i < temp.Length; i++)
                {
                    data[i - 3] = Convert.ToInt32(temp[i]);
                }


                Point[] myData = new Point[8];
                myData[0] = new Point(0, 100);
                myData[1] = new Point(82, 100);
                myData[2] = new Point(82, 150);
                myData[3] = new Point(216, 150);
                myData[4] = new Point(216, 200);
                myData[5] = new Point(960, 200);
                myData[6] = new Point(960, 500);
                myData[7] = new Point(1000, 500);
                
                graph1.Data[0] = data;
                graph1.Data[1] = myData;
            }
            else
            {
                MessageBox.Show("No Data at " + x + " " + y);
            }
        }

        private void transitionToState(Constants.state aState)
        {
            previousState = currentState;
            currentState = aState;

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

                    if (previousState == Constants.state.Stopped)
                    {
                        // Remmove captured image and heat map
                        
                        mLoadedImage = null;
                        mHeatMap.Clear();
                    }

                    // Update Status
                    lblStatus.Text = Constants.StatusInitial;

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
                    numOfCalibrationPoints = 0;

                    // Delete everything in the selectorCanvas
                    canvasDrawing.Children.Clear();

                    if (mCalibrationSet)
                    {
                        // Draw the table area
                        drawMotorTravelArea();
                    }
                    break;

                case Constants.state.Ready:
                    // Update Buttons
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

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

                    // Update Status
                    lblStatus.Text = Constants.StatusScanning;

                    // Hide drawing canvas
                    canvasDrawing.Visibility = Visibility.Collapsed;

                    // Initialize the heatmap
                    if(previousState != Constants.state.Stopped)
                        mHeatMap.Create((int)yScanPoints, (int)xScanPoints);
                    
                    // Start the simulator
                    startSimulator();
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
                    simulatorTimer.Stop();

                    // Stop the count down timer
                    CDTimer.Stop();

                    
                    
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
            if (currentState == Constants.state.Calibration)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (currentState == Constants.state.Ready)
            {
                transitionToState(Constants.state.Initial);
            }
            else if (currentState == Constants.state.Scanning)
            {
                transitionToState(Constants.state.Stopped);
            }
            else if (currentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Initial);
            }
        }

        /// <summary>
        /// Called on "Accept" button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_accept(object sender, RoutedEventArgs e)
        {
            if (currentState == Constants.state.Initial && scanAreaSet)
            {
                transitionToState(Constants.state.Ready);
            }
            else if (currentState == Constants.state.Ready)
            {
                transitionToState(Constants.state.Scanning);
            }
            else if (currentState == Constants.state.Stopped)
            {
                transitionToState(Constants.state.Scanning);
            }
            else if (currentState == Constants.state.Calibration)
            {
                // Only want to save our calibration values if the user clicks
                // the 'Accept' button in the calibration state
                canvasCalibrationPoints = Utilities.determineOpositeCorners(canvasCalibrationPoints[0], canvasCalibrationPoints[1]);

                imageCalibrationPoints[0] = Imaging.getAspectRatioPosition(canvasCalibrationPoints[0], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

                imageCalibrationPoints[1] = Imaging.getAspectRatioPosition(canvasCalibrationPoints[1], imageCaptured.ActualWidth, imageCaptured.ActualHeight,
                    imageCaptured.Source.Width, imageCaptured.Source.Height);

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
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".csv";
            //dlg.Filter = "CSV Files (*.csv)|*.csv|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                mFileName = dlg.FileName;
                //textBox1.Text = mFilename;

                String[] tokens = mFileName.Split('.');

                if (tokens[1] == "csv")
                {
                    Logger.read_csv("Test.csv");
                }

                //if (tokens[1] == "jpg" || tokens[1] == "png" || tokens[1] == "jpeg")
                //{
                //    open_image();
                //}
            }
        }

        private void click_capture_image(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".jpg";
            dlg.Filter = "JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                mFileName = dlg.FileName;
                //textBox1.Text = mFilename;

                //String[] tokens = mFileName.Split('.');

                //if (tokens[1] == "jpg" || tokens[1] == "png" || tokens[1] == "jpeg")
                //{


                mLoadedImage = Imaging.openImageSource(mFileName);
                imageCaptured.Source = mLoadedImage;
                //imageLoaded = true;
                canvasDrawing.Visibility = Visibility.Visible;
                btnCalibrate.IsEnabled = true;
                //}
            }
        }

        private void click_preferences(object sender, RoutedEventArgs e)
        {
            PrefPopup popup = new PrefPopup();
            popup.ShowDialog();
            loadUserPreferences();
        }

        private void click_heatMapMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // for double-click, remove this condition if only want single click
            {
                Point cell = mHeatMap.getClickedCell(sender, e);
                populateGraph((int)cell.X, (int)cell.Y, (int)xScanPoints, (int)yScanPoints);
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
            canvasMouseDownPos = e.GetPosition(canvasDrawing);

            //Console.WriteLine("X: " + canvasMouseDownPos.X + " Y: " + canvasMouseDownPos.Y);
            if(imageCaptured.Source != null)
            {
                if (currentState == Constants.state.Initial)
                {
                    // Only draw the scan area if we are within the table
                    // calibration area
                    if (canvasMouseDownPos.X >= canvasCalibrationPoints[0].X &&
                        canvasMouseDownPos.Y >= canvasCalibrationPoints[0].Y &&
                        canvasMouseDownPos.X <= canvasCalibrationPoints[1].X &&
                        canvasMouseDownPos.Y <= canvasCalibrationPoints[1].Y)
                    {
                        mouseDown = true;
                        //DrawScanArea();

                        // The area selection will always be in position 1
                        if (canvasDrawing.Children.Count > 1)
                        {
                            canvasDrawing.Children.RemoveAt(1);
                        }

                        Draw.Rectangle(canvasDrawing, 0, 0, 0, 0, Brushes.Red, true);
                    }
                }
                else if (currentState == Constants.state.Calibration)
                {
                    numOfCalibrationPoints++;

                    if (numOfCalibrationPoints <= 2)
                    {
                        Draw.Circle(canvasDrawing, e.GetPosition(canvasDrawing));
                    }

                    if (numOfCalibrationPoints == 1)
                    {
                        canvasCalibrationPoints[0] = e.GetPosition(imageCaptured);

                        lblStatus.Text = "Now click on the top right corner of the table";

                        btnCancel.IsEnabled = true;
                        btnAccept.IsEnabled = false;
                    }
                    else if (numOfCalibrationPoints == 2)
                    {
                        canvasCalibrationPoints[1] = e.GetPosition(imageCaptured);

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
            if (mouseDown)
            {
                // Get the mouse up location
                canvasMouseUpPos = e.GetPosition(imageCaptured);

                // Get the selection area
                if (imageCaptured.Source != null && currentState == Constants.state.Initial)
                {
                    mouseDown = false;

                    if (canvasMouseDownPos.X != canvasMouseUpPos.X && canvasMouseDownPos.Y != canvasMouseUpPos.Y)
                    {
                        scanAreaSet = true;
                        btnAccept.IsEnabled = true;
                    }
                    else
                    {
                        scanAreaSet = false;
                        btnAccept.IsEnabled = false;
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
            if (mouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.
                Point mousePos = e.GetPosition(canvasDrawing);

                Rectangle selectionBox = (Rectangle)canvasDrawing.Children[1];

                // Restrict movement to image of PCB
                if (mousePos.X < canvasCalibrationPoints[0].X) mousePos.X = canvasCalibrationPoints[0].X;
                if (mousePos.X > canvasCalibrationPoints[1].X) mousePos.X = canvasCalibrationPoints[1].X;
                if (mousePos.Y < canvasCalibrationPoints[0].Y) mousePos.Y = canvasCalibrationPoints[0].Y;
                if (mousePos.Y > canvasCalibrationPoints[1].Y) mousePos.Y = canvasCalibrationPoints[1].Y;

                // Position box
                if (canvasMouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(canvasDrawing.Children[1], canvasMouseDownPos.X);
                    selectionBox.Width = mousePos.X - canvasMouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = canvasMouseDownPos.X - mousePos.X;
                }

                if (canvasMouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, canvasMouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - canvasMouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = canvasMouseDownPos.Y - mousePos.Y;
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
                canvasCalibrationPoints[0] = Imaging.getAspectRatioPosition(imageCalibrationPoints[0],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                canvasCalibrationPoints[1] = Imaging.getAspectRatioPosition(imageCalibrationPoints[1],
                        imageCaptured.Source.Width, imageCaptured.Source.Height, imageCaptured.ActualWidth, imageCaptured.ActualHeight);

                // Delete everything in the selectorCanvas
                canvasDrawing.Children.Clear();

                // Draw the table area
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
            canvasScanArea = Utilities.determineOpositeCorners(canvasMouseDownPos, canvasMouseUpPos);

            // Zoom in on the scan area
            imageCaptured.Source = Imaging.cropImage(imageCaptured, canvasScanArea[0], canvasScanArea[1]);

            canvasXStepSize = (canvasCalibrationPoints[1].X - canvasCalibrationPoints[0].X) / motorXTravelDistance;
            canvasYStepSize = (canvasCalibrationPoints[1].Y - canvasCalibrationPoints[0].Y) / motorYTravelDistance;

            // Convert the bounds to an actual motor positions
            motorScanAreaPoints[0].X = Math.Round((canvasScanArea[0].X - canvasCalibrationPoints[0].X) / canvasXStepSize, 0, MidpointRounding.ToEven);
            motorScanAreaPoints[0].Y = Math.Round((canvasScanArea[0].Y - canvasCalibrationPoints[0].Y) / canvasYStepSize, 0, MidpointRounding.ToEven);
            motorScanAreaPoints[1].X = Math.Round((canvasScanArea[1].X - canvasCalibrationPoints[0].X) / canvasXStepSize, 0, MidpointRounding.ToEven);
            motorScanAreaPoints[1].Y = Math.Round((canvasScanArea[1].Y - canvasCalibrationPoints[0].Y) / canvasYStepSize, 0, MidpointRounding.ToEven);

            // Give the motors a starting position
            motorXPos = Convert.ToInt32(motorScanAreaPoints[0].X);
            motorYPos = Convert.ToInt32(motorScanAreaPoints[0].Y);

            // Update the scan area width and length
            xScanPoints = (motorScanAreaPoints[1].X - motorScanAreaPoints[0].X) / nsXStepSize.Value;
            yScanPoints = (motorScanAreaPoints[1].Y - motorScanAreaPoints[0].Y) / nsYStepSize.Value;

            // Calculate how long it will take
            int scanPoints = Convert.ToInt32(xScanPoints * yScanPoints);
            CDTimer = new CountdownTimer(lblCDTimer, scanPoints);

            lblXPosition.Text = motorXPos.ToString();
            lblYPosition.Text = motorYPos.ToString();
        }


        /// <summary>
        /// Saves all of the application settings for furture use when application closes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //srLog.Close(); // close stream to log file

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
            Draw.Rectangle(canvasDrawing,
                canvasCalibrationPoints[0].X,
                canvasCalibrationPoints[0].Y,
                canvasCalibrationPoints[1].X - canvasCalibrationPoints[0].X,
                canvasCalibrationPoints[1].Y - canvasCalibrationPoints[0].Y,
                Brushes.Blue);
        }

        private void moveMotors_Tick(object sender, EventArgs e)
        {
            // Start the count down timer if it is
            // not already started
            if(!CDTimer.Enabled)
                CDTimer.Start();

            // Move the motors to the next location
            int nextX = motorXPos;
            int nextY = motorYPos;

            Logger.writeToFile(mLogFileName, nextX, nextY, 0, getScannedData(461));

            // Update the motor position text
            lblXPosition.Text = nextX.ToString();
            lblYPosition.Text = nextY.ToString();

            Console.WriteLine("Pixel Pos:: X " + (motorXPos - Convert.ToInt32(motorScanAreaPoints[0].X)) +
                                    " Y " + (motorYPos - Convert.ToInt32(motorScanAreaPoints[0].Y)));

            Console.WriteLine("Motor Pos:: X " + motorXPos +
                                    " Y " + motorYPos);

            int row = (motorXPos - (int)motorScanAreaPoints[0].X) / (int)nsXStepSize.Value;
            int col = (motorYPos - (int)motorScanAreaPoints[0].Y) / (int)nsYStepSize.Value;

            // draw heat map pixel
            //drawHeatMapPixel(row, col, Colors.Blue);
            mHeatMap.drawPixel(row, col, Colors.Blue);

            // Determine the next scan position
            if (currentScanDirection == Constants.direction.South)
            {
                nextY = nextY + nsYStepSize.Value;
                if (nextY > motorScanAreaPoints[1].Y)
                {
                    currentScanDirection = Utilities.reverseDirection(currentScanDirection);
                    nextY = nextY - nsYStepSize.Value;
                    nextX = nextX + nsXStepSize.Value;
                }
            }
            else if (currentScanDirection == Constants.direction.North)
            {
                nextY = nextY - nsYStepSize.Value;
                if (nextY < motorScanAreaPoints[0].Y)
                {
                    currentScanDirection = Utilities.reverseDirection(currentScanDirection);
                    nextY = nextY + nsYStepSize.Value;
                    nextX = nextX + nsXStepSize.Value;
                }
            }

            if (nextX > motorScanAreaPoints[1].X)
                simulatorTimer.Stop();

            motorXPos = nextX;
            motorYPos = nextY;
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
                data[i] = rNum.Next(110) - rNum.Next(40);
                if (i > 82)
                    data[i] += rNum.Next(60) - rNum.Next(20);
                if(i > 216)
                    data[i] += rNum.Next(60) - rNum.Next(20);
            }

            return data;
        }


        private void startSimulator()
        {
            simulatorTimer = new DispatcherTimer();
            simulatorTimer.Tick += new EventHandler(moveMotors_Tick);
            simulatorTimer.Interval = new TimeSpan(0, 0, 1);
            simulatorTimer.Start();
        }

        private void loadUserPreferences()
        {
            // Load user/application settings
            imageCalibrationPoints[0] = (Point)Properties.Settings.Default["TableBL"];
            imageCalibrationPoints[1] = (Point)Properties.Settings.Default["TableTR"];

            motorXTravelDistance = Properties.Settings.Default.motorXTravelDistance;
            motorYTravelDistance = Properties.Settings.Default.motorYTravelDistance;
            motorZTravelDistance = Properties.Settings.Default.motorZTravelDistance;

            sHeatMapOpacity.Value = Properties.Settings.Default.heatMapOpacity;

            nsXStepSize.Value = (int)Properties.Settings.Default["nsXStepSize"];
            nsYStepSize.Value = (int)Properties.Settings.Default["nsYStepSize"];
        }

        private void saveUserPreferences()
        {
            Properties.Settings.Default["TableBL"] = imageCalibrationPoints[0];
            Properties.Settings.Default["TableTR"] = imageCalibrationPoints[1];

            Properties.Settings.Default.motorXTravelDistance = motorXTravelDistance;
            Properties.Settings.Default.motorYTravelDistance = motorYTravelDistance;
            Properties.Settings.Default.motorZTravelDistance = motorZTravelDistance;

            Properties.Settings.Default["nsXStepSize"] = nsXStepSize.Value;
            Properties.Settings.Default["nsYStepSize"] = nsYStepSize.Value;

            Properties.Settings.Default.heatMapOpacity = sHeatMapOpacity.Value;
            
            Properties.Settings.Default.Save();
        }

        private void heatMapOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            mHeatMap.setPixelOpacity(slider.Value);
        }
    }
}

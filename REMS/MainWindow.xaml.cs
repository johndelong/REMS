using System;
using System.IO;
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

namespace REMS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum state : int
        {
            Initial,
            Ready,
            Calibration,
            Stopped,
            Scanning,
            Done
        }

        private Point[] calibrationPoints = new Point[2]; // which calibration points have been collected
        private Point[] imageCalibrationPoints = new Point[2];
        private int numOfCalibrationPoints = 0;
        private Boolean imageLoaded = false;
        public Boolean scanAreaSet = false;
        private state currentState = state.Initial;
        private ImageSource loadedImage;

        AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(0);
        string mFileName;

        public MainWindow()
        {
            InitializeComponent();
            analyzeGrid.ItemsSource = LoadCollectionData();
            //setupHeatMap(50, 50);

            //lblStatus.Text = status.Initial;

            // Load user/application settings
            imageCalibrationPoints[0] = (Point)Properties.Settings.Default["TableBL"];
            imageCalibrationPoints[1] = (Point)Properties.Settings.Default["TableTR"];
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

        // saving user settings (preferences)
        // http://stackoverflow.com/questions/3784477/c-sharp-approach-for-saving-user-settings-in-a-wpf-application


        // charting/plotting information
        // http://zone.ni.com/reference/en-XX/help/372636F-01/mstudiowebhelp/html/wpfgraphplotting/
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();

            int numberOfPoints = 100;
            AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(numberOfPoints);

            for (int i = 0; i < numberOfPoints; i++)
            {
                analogWaveform.Samples[i].Value = random.NextDouble();
            }

            graph1.DataSource = analogWaveform;

            // generate intensity graph
            // http://zone.ni.com/reference/en-XX/help/372636F-01/mstudiowebhelp/html/wpfintensitygraphplotting/


            //intensityGraph1.DataSource = GetData(10);


            colorHeatMap(50, 50);
        }

        //http://www.c-sharpcorner.com/uploadfile/mahesh/grid-in-wpf/
        private void setupHeatMap(int x, int y)
        {
            RowDefinition rowDef;
            ColumnDefinition colDef;

            for (int lRow = 0; lRow < x; lRow++)
            {
                rowDef = new RowDefinition();
                heat_map.RowDefinitions.Insert(heat_map.RowDefinitions.Count, rowDef);
                Console.WriteLine(heat_map.RowDefinitions.IndexOf(rowDef).ToString());
            }

            for (int lCol = 0; lCol < y; lCol++)
            {
                colDef = new ColumnDefinition();
                heat_map.ColumnDefinitions.Insert(heat_map.ColumnDefinitions.Count, colDef);
                Console.WriteLine(heat_map.ColumnDefinitions.IndexOf(colDef).ToString());
            }

        }

        private void colorHeatMap(int x, int y)
        {
            heat_map.Children.Clear();

            Random random = new Random();
            for (int lRow = 0; lRow < x; lRow++)
            {
                for (int lCol = 0; lCol < y; lCol++)
                {

                    Rectangle pixel = new Rectangle();
                    Color temp = new Color();

                    int sel = random.Next(10) % 3;

                    switch (sel)
                    {
                        case 0:
                            temp = Colors.Red;
                            break;
                        case 1:
                            temp = Colors.Blue;
                            break;
                        case 2:
                            temp = Colors.Yellow;
                            break;
                    }

                    SolidColorBrush pixelFill = new SolidColorBrush(temp);
                    pixel.Fill = pixelFill;
                    pixel.Opacity = 0.8;

                    Grid.SetColumn(pixel, lCol);
                    Grid.SetRow(pixel, lRow);

                    heat_map.Children.Add(pixel);
                }
            }
        }


        private void open_file(object sender, RoutedEventArgs e)
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
                    read_csv();
                }

                if (tokens[1] == "jpg" || tokens[1] == "png" || tokens[1] == "jpeg")
                {
                    open_image();
                }
            }
        }

        private void read_csv()
        {
            //var reader = new StreamReader(File.OpenRead(@"C:\Users\delongja\Documents\Visual Studio 2013\Projects\ReadCSV\SampleData.csv"));
            var reader = new StreamReader(File.OpenRead(mFileName));
            List<List<string>> Matrix = new List<List<string>>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                Matrix.Add(values.ToList());
            }

            Console.WriteLine("Here it comes!");
            //Console.WriteLine(Matrix[1][1]);

            foreach (var row in Matrix)
            {
                foreach (var val in row)
                {
                    Console.Write(val);
                }
                Console.Write('\n');
            }

            /*if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pictureBox1.Load(openFileDialog1.FileName);
            }*/
        }

        private void open_image()
        {
            loadedImage = null;
            loadedImage = ConvertBitmapTo96DPI(new BitmapImage(new Uri(mFileName)));
            pcb_image.Source = loadedImage;
            imageLoaded = true;

            
        }

        public static BitmapSource ConvertBitmapTo96DPI(BitmapImage bitmapImage)
        {
            double dpi = 96;
            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            int stride = width * bitmapImage.Format.BitsPerPixel;
            byte[] pixelData = new byte[stride * height];
            bitmapImage.CopyPixels(pixelData, stride, 0);

            return BitmapSource.Create(width, height, dpi, dpi, bitmapImage.Format, null, pixelData, stride);
        }

        private void click_cancel(object sender, RoutedEventArgs e)
        {
            if (currentState == state.Ready)
            {
                currentState = state.Initial;
                updateComponentsByState(currentState);
                pcb_image.Source = loadedImage;
                selectorCanvas.Visibility = Visibility.Visible; 
            }
            else if (currentState == state.Scanning)
            {
                currentState = state.Stopped;
                updateComponentsByState(currentState);
            }
            else if (currentState == state.Stopped)
            {
                currentState = state.Initial;
                updateComponentsByState(currentState);
                pcb_image.Source = null;
                selectorCanvas.Visibility = Visibility.Visible;
            }
        }

        private void click_accept(object sender, RoutedEventArgs e)
        {
            if (currentState == state.Initial && scanAreaSet)
            {
                currentState = state.Ready;
                updateComponentsByState(currentState);

                selectorCanvas.Visibility = Visibility.Collapsed;
                setScanArea();
            }
            else if (currentState == state.Ready)
            {
                currentState = state.Scanning;
                updateComponentsByState(currentState);
            }
            else if (currentState == state.Calibration)
            {
                currentState = state.Initial;
                updateComponentsByState(currentState);
                DrawTableArea();
            }
        }

        private void updateComponentsByState(state aState)
        {
            switch (aState)
            {
                case state.Initial:
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = false;

                    btnAccept.Content = "Accept";
                    btnAccept.Background = null;
                    btnAccept.IsEnabled = false;

                    lblStatus.Text = status.Initial;
                    break;

                case state.Ready:
                    btnCancel.Content = "Cancel";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    lblStatus.Text = status.Ready;
                    break;

                case state.Scanning:
                    btnCancel.Content = "Stop";
                    btnCancel.Background = Brushes.Red;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = false;

                    lblStatus.Text = status.Scanning;
                    break;

                case state.Stopped:
                    btnCancel.Content = "Reset";
                    btnCancel.Background = null;
                    btnCancel.IsEnabled = true;

                    btnAccept.Content = "Start";
                    btnAccept.Background = Brushes.Green;
                    btnAccept.IsEnabled = true;

                    lblStatus.Text = status.Stopped;
                    break;
            }
        }

        private void setScanArea()
        {
            // Crop down the image to the selected region
            pcb_image.Source = cropImage(pcb_image, imageMouseDownPos, imageMouseUpPos);

            // Clear the selection (if there is one)
            //selectionBox.Visibility = Visibility.Collapsed;
            //currentState = state.Initial;
        }

        private Point[] determineOpositeCorners(Point aPoint1, Point aPoint2)
        {
            Point topLeft = new Point();
            Point bottomRight = new Point();
            Point[] corners = new Point[2];

            if (aPoint1.X < aPoint2.X && aPoint1.Y < aPoint2.Y) // top left to bottom right
            {
                topLeft = aPoint1;
                bottomRight = aPoint2;
            }
            else if (aPoint1.X > aPoint2.X && aPoint1.Y > aPoint2.Y) // bottom right to top left
            {
                topLeft = aPoint2;
                bottomRight = aPoint1;
            }
            else if (aPoint1.X > aPoint2.X && aPoint1.Y < aPoint2.Y) // top right to bottom left
            {
                topLeft.X = aPoint2.X;
                topLeft.Y = aPoint1.Y;
                bottomRight.X = aPoint1.X;
                bottomRight.Y = aPoint2.Y;
            }
            else if (aPoint1.X < aPoint2.X && aPoint1.Y > aPoint2.Y) // bottom left to top right
            {
                topLeft.X = aPoint1.X;
                topLeft.Y = aPoint2.Y;
                bottomRight.X = aPoint2.X;
                bottomRight.Y = aPoint1.Y;
            }

            corners[0] = topLeft;
            corners[1] = bottomRight;
            return corners;
        }

        public ImageSource cropImage(Image aImage, Point aMouseDown, Point aMouseUp)
        {
            Point[] corners = determineOpositeCorners(aMouseDown, aMouseUp);

            // Convert selected coordinates to actual image coordinates
            Double Xbegin = (corners[0].X * aImage.Source.Width) / aImage.ActualWidth;
            Double Ybegin = (corners[0].Y * aImage.Source.Height) / aImage.ActualHeight;
            Double Xend = (corners[1].X * aImage.Source.Width) / aImage.ActualWidth;
            Double Yend = (corners[1].Y * aImage.Source.Height) / aImage.ActualHeight;

            // Convert coordinates to integers
            int xPos = Convert.ToInt32(Math.Round(Xbegin, 0, MidpointRounding.ToEven));
            int yPos = Convert.ToInt32(Math.Round(Ybegin, 0, MidpointRounding.ToEven));
            int width = Convert.ToInt32(Math.Round(Xend - Xbegin, 0, MidpointRounding.ToEven));
            int height = Convert.ToInt32(Math.Round(Yend - Ybegin, 0, MidpointRounding.ToEven));

            // Create the cropped image
            var fullImage = aImage.Source;
            var croppedImage = new CroppedBitmap((BitmapSource)fullImage, new Int32Rect(xPos, yPos, width, height));

            return croppedImage;
        }


        bool mouseDown = false; // Set to 'true' when mouse is held down.
        Point imageMouseDownPos; // The point where the mouse button was clicked down on the image.
        Point imageMouseUpPos;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            imageMouseDownPos = e.GetPosition(selectorCanvas);

            if (imageLoaded && currentState == state.Initial)
            {
                if (imageMouseDownPos.X >= calibrationPoints[0].X &&
                    imageMouseDownPos.Y >= calibrationPoints[0].Y &&
                    imageMouseDownPos.X <= calibrationPoints[1].X &&
                    imageMouseDownPos.Y <= calibrationPoints[1].Y)
                {
                    mouseDown = true;
                    DrawScanArea();
                }
            }
            else if (currentState == state.Calibration)
            {
                numOfCalibrationPoints++;

                if (numOfCalibrationPoints <= 2)
                {
                    DrawCircle(e.GetPosition(selectorCanvas));
                }

                if (numOfCalibrationPoints == 1)
                {
                    calibrationPoints[0] = e.GetPosition(pcb_image);
                    
                    lblStatus.Text = "Now click on the top right corner of the table";

                    btnCancel.IsEnabled = true;
                    btnAccept.IsEnabled = false;
                }
                else if (numOfCalibrationPoints == 2)
                {
                    calibrationPoints[1] = e.GetPosition(pcb_image);
                    
                    // Make sure that we always have the bottom left and top right corners
                    Point[] corners = determineOpositeCorners(calibrationPoints[0], calibrationPoints[1]);
                    calibrationPoints[0] = corners[0];
                    calibrationPoints[1] = corners[1];
                    imageCalibrationPoints[0] = getActualImagePixel(pcb_image, calibrationPoints[0]);
                    imageCalibrationPoints[1] = getActualImagePixel(pcb_image, calibrationPoints[1]);
                    
                    lblStatus.Text = "Click 'Accept' if rectangle matches table";
                    btnAccept.IsEnabled = true;
                }
            }
        }

        private Point getActualImagePixel(Image aImage, Point aLocation)
        {
            // Take current location and convert to actual image location
            Double dblXPos = (aLocation.X * aImage.Source.Width) / aImage.ActualWidth;
            Double dblYPos = (aLocation.Y * aImage.Source.Height) / aImage.ActualHeight;

            // Convert coordinates to integers
            int xPos = Convert.ToInt32(Math.Round(dblXPos, 0, MidpointRounding.ToEven));
            int yPos = Convert.ToInt32(Math.Round(dblYPos, 0, MidpointRounding.ToEven));

            return new Point(xPos, yPos);
        }

        private Point getRelativeImagePixel(Image aImage, Point aLocation)
        {
            // Take image pixel location and convert to relative image location
            Double dblXPos = (aLocation.X * aImage.ActualWidth) / aImage.Source.Width;
            Double dblYPos = (aLocation.Y * aImage.ActualHeight) / aImage.Source.Height;

            // Convert coordinates to integers
            int xPos = Convert.ToInt32(Math.Round(dblXPos, 0, MidpointRounding.ToEven));
            int yPos = Convert.ToInt32(Math.Round(dblYPos, 0, MidpointRounding.ToEven));

            return new Point(xPos, yPos);
        }


        private void DrawCircle(Point aLocation)
        {
            Shape Rendershape = new Ellipse() { Height = 20, Width = 20 };
            Rendershape.Fill = Brushes.Blue;
            Canvas.SetLeft(Rendershape, aLocation.X - 10);
            Canvas.SetTop(Rendershape, aLocation.Y - 10);
            selectorCanvas.Children.Add(Rendershape);
        }

        private void DrawTableArea()
        {
            // Delete everything in the selectorCanvas
            selectorCanvas.Children.Clear();

            // Draw the Table Area
            Shape Rendershape = new Rectangle();
            Rendershape.Stroke = Brushes.Blue;
            Rendershape.StrokeThickness = 3;
            Canvas.SetLeft(Rendershape, calibrationPoints[0].X);
            Canvas.SetTop(Rendershape, calibrationPoints[0].Y);
            Rendershape.Width = calibrationPoints[1].X - calibrationPoints[0].X;
            Rendershape.Height = calibrationPoints[1].Y - calibrationPoints[0].Y;
            selectorCanvas.Children.Add(Rendershape);
        }

        private void DrawScanArea()
        {
            // The area selection will always be in position 1
            if (selectorCanvas.Children.Count > 1)
            {
                selectorCanvas.Children.RemoveAt(1);
            }

            Shape Rendershape = new Rectangle();
            Rendershape.Stroke = Brushes.Red;
            Rendershape.StrokeDashArray = new DoubleCollection() { 2, 1 };
            Rendershape.Height = 0;
            Rendershape.Width = 0;
            Rendershape.StrokeThickness = 3;
            selectorCanvas.Children.Add(Rendershape);
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            imageMouseUpPos = e.GetPosition(pcb_image);

            if (imageLoaded && currentState == state.Initial)
            {
                mouseDown = false;
                
                if (imageMouseDownPos.X != imageMouseUpPos.X && imageMouseDownPos.Y != imageMouseUpPos.Y)
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
            else
            {
                //Console.WriteLine("Image Loaded: " + imageLoaded);
                //Console.WriteLine("Current State: " + currentState);
            }
        }


        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.
                Point mousePos = e.GetPosition(selectorCanvas);
               
                Rectangle selectionBox = (Rectangle)selectorCanvas.Children[1];

                // Restrict movement to image of PCB
                if (mousePos.X < calibrationPoints[0].X) mousePos.X = calibrationPoints[0].X;
                if (mousePos.X > calibrationPoints[1].X) mousePos.X = calibrationPoints[1].X;
                if (mousePos.Y < calibrationPoints[0].Y) mousePos.Y = calibrationPoints[0].Y;
                if (mousePos.Y > calibrationPoints[1].Y) mousePos.Y = calibrationPoints[1].Y;

                // Position box
                if (imageMouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectorCanvas.Children[1], imageMouseDownPos.X);
                    selectionBox.Width = mousePos.X - imageMouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = imageMouseDownPos.X - mousePos.X;
                }

                if (imageMouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, imageMouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - imageMouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = imageMouseDownPos.Y - mousePos.Y;
                }
            }
        }

        // Save all of the application settings for future use
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default["TableBL"] = imageCalibrationPoints[0];
            Properties.Settings.Default["TableTR"] = imageCalibrationPoints[1];

            Properties.Settings.Default.Save();
        }

        private void click_calibrate(object sender, RoutedEventArgs e)
        {
            currentState = state.Calibration;
            lblStatus.Text = "Click on the bottom left corner of the table";
            numOfCalibrationPoints = 0;
            selectorCanvas.Children.Clear();
        }

        private void redrawSelectionObjects(object sender, SizeChangedEventArgs e)
        {
            //Console.WriteLine("Size Change!");
            if (pcb_image.Source != null)
            {
                calibrationPoints[0] = getRelativeImagePixel(pcb_image, imageCalibrationPoints[0]);
                calibrationPoints[1] = getRelativeImagePixel(pcb_image, imageCalibrationPoints[1]);
                DrawTableArea();
            }
        }
    }
}

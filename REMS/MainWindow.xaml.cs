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
            Stopped,
            Scanning,
            Done
        }

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
            setupHeatMap(50, 50);
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

                if (tokens[1] == "jpg")
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
            /*ImageBrush brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri(mFileName, UriKind.Relative));
            image_canvas.Background = brush;*/

            loadedImage = new BitmapImage(new Uri(mFileName));
            pcb_image.Source = loadedImage;
            imageLoaded = true;
        }

        private void click_cancel(object sender, RoutedEventArgs e)
        {
            if (currentState == state.Ready)
            {
                currentState = state.Initial;
                btnCancel.IsEnabled = false;

                btnAccept.IsEnabled = false;
                btnAccept.Background = null;
                btnAccept.Content = "Accept";

                scanAreaSet = false;
                pcb_image.Source = loadedImage;
            }
            else if (currentState == state.Scanning)
            {
                currentState = state.Initial;
                lblStatus.Text = "Select a scan area";
            }
        }

        private void click_accept(object sender, RoutedEventArgs e)
        {
            if (currentState == state.Initial && scanAreaSet)
            {
                currentState = state.Ready;
                lblStatus.Text = "Ready to start scanning";

                btnCancel.IsEnabled = true;

                btnAccept.Background = Brushes.Green;
                btnAccept.Content = "Start";

                setScanArea();
            }
            else if (currentState == state.Ready)
            {
                currentState = state.Scanning;
                lblStatus.Text = "Scanning...";

                btnCancel.Background = Brushes.Red;
                btnCancel.Content = "Stop";   
            }
        }

        private void setScanArea()
        {
            // Crop down the image to the selected region
            pcb_image.Source = cropImage(pcb_image, imageMouseDownPos, imageMouseUpPos);

            // Clear the selection (if there is one)
            selectionBox.Visibility = Visibility.Collapsed;
            //currentState = state.Initial;
        }

        public ImageSource cropImage(Image aImage, Point aMouseDown, Point aMouseUp)
        {
            Point topLeft = new Point();
            Point bottomRight = new Point();

            if (aMouseDown.X < aMouseUp.X && aMouseDown.Y < aMouseUp.Y) // top left to bottom right
            {
                topLeft = aMouseDown;
                bottomRight = aMouseUp;
            }
            else if (aMouseDown.X > aMouseUp.X && aMouseDown.Y > aMouseUp.Y) // bottom right to top left
            {
                topLeft = aMouseUp;
                bottomRight = aMouseDown;
            }
            else if (aMouseDown.X > aMouseUp.X && aMouseDown.Y < aMouseUp.Y) // top right to bottom left
            {
                topLeft.X = aMouseUp.X;
                topLeft.Y = aMouseDown.Y;
                bottomRight.X = aMouseDown.X;
                bottomRight.Y = aMouseUp.Y;
            }
            else if (aMouseDown.X < aMouseUp.X && aMouseDown.Y > aMouseUp.Y) // bottom left to top right
            {
                topLeft.X = aMouseDown.X;
                topLeft.Y = aMouseUp.Y;
                bottomRight.X = aMouseUp.X;
                bottomRight.Y = aMouseDown.Y;
            }

            // Convert selected coordinates to actual image coordinates
            Double Xbegin = (topLeft.X * aImage.Source.Width) / aImage.ActualWidth;
            Double Ybegin = (topLeft.Y * aImage.Source.Height) / aImage.ActualHeight;
            Double Xend = (bottomRight.X * aImage.Source.Width) / aImage.ActualWidth;
            Double Yend = (bottomRight.Y * aImage.Source.Height) / aImage.ActualHeight;

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

        //
        //
        // Drag area
        //
        //
        bool mouseDown = false; // Set to 'true' when mouse is held down.
        Point gridMouseDownPos; // The point where the mouse button was clicked down on the grid.
        Point imageMouseDownPos; // The point where the mouse button was clicked down on the image.
        Point gridMouseUpPos;
        Point imageMouseUpPos;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (imageLoaded && currentState == state.Initial)
            {
                mouseDown = true;
                gridMouseDownPos = e.GetPosition(theGrid);
                imageMouseDownPos = e.GetPosition(pcb_image);

                // Initial placement of the drag selection box.         
                Canvas.SetLeft(selectionBox, gridMouseDownPos.X);
                Canvas.SetTop(selectionBox, gridMouseDownPos.Y);
                selectionBox.Width = 0;
                selectionBox.Height = 0;

                //if (mouseMoved)
                //{
                    // Make the drag selection box visible.
                    //selectionBox.Visibility = Visibility.Visible;
                    //currentState = state.AreaSelected;
                //}
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (imageLoaded && currentState == state.Initial)
            {
                mouseDown = false;
                gridMouseUpPos = e.GetPosition(theGrid);
                imageMouseUpPos = e.GetPosition(pcb_image);

                if (gridMouseDownPos.X != gridMouseUpPos.X && gridMouseDownPos.Y != gridMouseUpPos.Y)
                {
                    Console.WriteLine("height: " + selectionBox.ActualHeight);
                    Console.WriteLine("width: " + selectionBox.ActualWidth);
                    scanAreaSet = true;
                    btnAccept.IsEnabled = true;
                }
                else
                {
                    scanAreaSet = false;
                    btnAccept.IsEnabled = false;
                }

                Console.WriteLine("Scan Area Set: " + scanAreaSet);
            }
            else
            {
                Console.WriteLine("Image Loaded: " + imageLoaded);
                Console.WriteLine("Current State: " + currentState);

            }
            
        }

        
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                selectionBox.Visibility = Visibility.Visible;

                // When the mouse is held down, reposition the drag selection box.
                Point mouseImagePos = e.GetPosition(pcb_image);
                Point mousePos = e.GetPosition(theGrid);

                // Restrict movement to image of PCB
                if (mouseImagePos.X < 0) mousePos.X = mousePos.X - mouseImagePos.X;
                if (mouseImagePos.X > pcb_image.ActualWidth) mousePos.X = mousePos.X - (mouseImagePos.X - pcb_image.ActualWidth);
                if (mouseImagePos.Y < 0) mousePos.Y = mousePos.Y - mouseImagePos.Y;
                if (mouseImagePos.Y > pcb_image.ActualHeight) mousePos.Y = mousePos.Y - (mouseImagePos.Y - pcb_image.ActualHeight);

                // Position box
                if (gridMouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectionBox, gridMouseDownPos.X);
                    selectionBox.Width = mousePos.X - gridMouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = gridMouseDownPos.X - mousePos.X;
                }

                if (gridMouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, gridMouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - gridMouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = gridMouseDownPos.Y - mousePos.Y;
                }
            }
        }

        

        /*
         * Obsolete Code
         * 
        
        Rectangle exampleRectangle = new Rectangle();
        exampleRectangle.Width = 300;
        exampleRectangle.Height = 300;

        // Create an ImageBrush and use it to 
        // paint the rectangle.
        ImageBrush myBrush = new ImageBrush();
        //myBrush.ImageSource = new BitmapImage(new Uri(@"C:\Users\delongja\Documents\Visual Studio 2013\Projects\ReadCSV\Audi.jpg", UriKind.Relative));
        myBrush.ImageSource = new BitmapImage(new Uri(mFileName, UriKind.Relative));

        exampleRectangle.Fill = myBrush;

        Canvas.SetRight(exampleRectangle, 0);
        Canvas.SetTop(exampleRectangle, 0);

        image_canvas.Children.Add(exampleRectangle);

        private void draw_heatMap(double size)
        {
          
         
            Random random = new Random();
            int x_pos = 0;
            int y_pos = 0;
            int x_step = (int)(image_canvas.ActualWidth / size);
            int y_step = (int)(image_canvas.ActualHeight / size);

            for (int col = 0; col < size; col++)
            {
                for (int row = 0; row < size; row++)
                {
                    Rectangle pixel = new Rectangle();
                    pixel.Width = x_step;
                    pixel.Height = y_step;

                    Color temp = new Color();
                    //int argb = Color.

                    
                    int sel = random.Next(100) % 3;

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

                    Canvas.SetLeft(pixel, x_pos);
                    Canvas.SetTop(pixel, y_pos);
                    image_canvas.Children.Add(pixel);
                    x_pos = x_pos + x_step;
                }
                x_pos = 0;
                y_pos = y_pos + y_step;
            }
        }
         * */


    }
}

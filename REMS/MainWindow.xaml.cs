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
        AnalogWaveform<double> analogWaveform = new AnalogWaveform<double>(0);
        string mFileName;

        public MainWindow()
        {
            InitializeComponent();
            analyzeGrid.ItemsSource = LoadCollectionData();
        }

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

            //for (int i = 0; i < numberOfPoints; i++)
            //{
                //draw_heatMap(100);
            //}

            /*Rectangle pixel = new Rectangle();
            pixel.Width = 100;
            pixel.Height = 100;

            Color temp = new Color();
            temp = Colors.Red;

            SolidColorBrush pixelFill = new SolidColorBrush(temp);

            pixel.Fill = pixelFill;
            pixel.Opacity = 0.8;

            Canvas.SetLeft(pixel, 0);
            Canvas.SetTop(pixel, 0);
            image_canvas.Children.Add(pixel);*/

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

                String [] tokens = mFileName.Split('.');

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
            /*Rectangle exampleRectangle = new Rectangle();
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

            image_canvas.Children.Add(exampleRectangle);*/

            ImageBrush brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri(mFileName, UriKind.Relative));
            image_canvas.Background = brush;
        }

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
    }
}

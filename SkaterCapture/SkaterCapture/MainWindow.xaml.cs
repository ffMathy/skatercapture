using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using SkaterCapture.Models;
using Color = System.Drawing.Color;
using Encoder = System.Drawing.Imaging.Encoder;

namespace SkaterCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int GracePeriodInSeconds = 1;
        private const int ImagesPerSecond = 10;

        private readonly MotionDetector _detector;

        private DateTime _lastStill;
        private DateTime _lastMovement;

        private Bitmap _backgroundImage;

        private ImageCollection _currentCollection;

        public MainWindow()
        {

            foreach (
                var folder in Directory.GetDirectories(Environment.CurrentDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(folder, true);
            }

            InitializeComponent();

            _detector = new MotionDetector(new TwoFramesDifferenceDetector(), new MotionBorderHighlighting());

            var videoInformation = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var informationList = new List<FilterInfo>();

            foreach (FilterInfo information in videoInformation)
            {
                informationList.Add(information);
            }

            var lastInformation = informationList.FirstOrDefault(i => i.Name.Contains("Front"));
            if (lastInformation != null)
            {
                var captureDevice = new VideoCaptureDevice(lastInformation.MonikerString);

                captureDevice.NewFrame += captureDevice_NewFrame;
                captureDevice.Start();
            }
        }

        void captureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            lock (this)
            {

                Dispatcher.Invoke(delegate()
                {
                    var image = eventArgs.Frame;
                    var originalImage = (Bitmap)image.Clone();

                    if (_detector.ProcessFrame(image) > 0.01d && _backgroundImage != null)
                    {
                        if ((DateTime.Now - _lastMovement).TotalMilliseconds >= 1000d / ImagesPerSecond)
                        {
                            _lastMovement = DateTime.Now;
                            CaptureImage(originalImage);
                        }
                    }
                    else if ((DateTime.Now - _lastStill).TotalSeconds >= GracePeriodInSeconds &&
                             (DateTime.Now - _lastMovement).TotalSeconds >= GracePeriodInSeconds)
                    {
                        _lastStill = DateTime.Now;
                        _backgroundImage = originalImage;
                    }

                    var source = ConvertBitmap(image);
                    Image.Source = source;

                    image.Dispose();

                });

            }
        }

        private Bitmap CaptureImage(Bitmap image)
        {
            var thresholdedDifferenceFilter = new ThresholdedDifference(60);
            thresholdedDifferenceFilter.OverlayImage = _backgroundImage;
            var maskImage = thresholdedDifferenceFilter.Apply(image);

            var grayscaleFilter = new GrayscaleToRGB();
            var rgbMaskImage = grayscaleFilter.Apply(maskImage);

            var intersectFilter = new Intersect(image);
            var finalImage = intersectFilter.Apply(rgbMaskImage);
            finalImage.MakeTransparent(Color.Black);

            var myEncoder = Encoder.Quality;
            var myEncoderParameters = new EncoderParameters(1);

            var myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            myEncoderParameters.Param[0] = myEncoderParameter;

            var jgpEncoder = GetEncoder(ImageFormat.Png);

            var directoryName = _lastStill.Year + "-" + _lastStill.Month + "-" + _lastStill.Day + " " + _lastStill.Hour + _lastStill.Minute + _lastStill.Second;
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);

                if (_currentCollection != null)
                {
                    FileList.Items.Insert(0, _currentCollection);
                    if (FileList.Items.Count >= 25)
                    {
                        FileList.Items.RemoveAt(FileList.Items.Count - 1);
                    }
                }

                _currentCollection = new ImageCollection();
            }

            var fileName = Path.Combine(Environment.CurrentDirectory, directoryName, DateTime.Now.Ticks + ".png");
            _currentCollection.AddFile(fileName);

            finalImage.Save(fileName, jgpEncoder, myEncoderParameters);

            return finalImage;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        private static BitmapSource ConvertBitmap(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        private void ImageCollectionMouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = (FrameworkElement) sender;
            var collection = (ImageCollection) element.DataContext;

            var window = new PickShotWindow(collection);
            window.Show();
        }
    }
}

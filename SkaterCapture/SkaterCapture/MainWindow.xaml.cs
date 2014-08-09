using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
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
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using Color = System.Drawing.Color;
using Encoder = System.Drawing.Imaging.Encoder;

namespace SkaterCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MotionDetector _detector;

        private DateTime _lastStill;
        private DateTime _lastMovement;

        private Bitmap _backgroundImage;

        public MainWindow()
        {

            InitializeComponent();

            _detector = new MotionDetector(new TwoFramesDifferenceDetector(), new BlobCountingObjectsProcessing());

            var videoInformation = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var captureDevice = (VideoCaptureDevice)null;
            foreach (FilterInfo information in videoInformation)
            {
                captureDevice = new VideoCaptureDevice(information.MonikerString);
                break;
            }

            if (captureDevice != null)
            {
                captureDevice.NewFrame += captureDevice_NewFrame;
                captureDevice.Start();
            }
        }

        void captureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            Dispatcher.Invoke(delegate()
            {
                var image = eventArgs.Frame;
                var originalImage = (Bitmap)image.Clone();

                if (_detector.ProcessFrame(image) > 0d && _backgroundImage != null)
                {
                    if ((DateTime.Now - _lastMovement).TotalMilliseconds > 100)
                    {
                        _lastMovement = DateTime.Now;
                        image = CaptureImage(originalImage);
                    }
                }
                else if ((DateTime.Now - _lastStill).TotalSeconds > 3 && (DateTime.Now - _lastMovement).TotalSeconds > 3)
                {
                    _lastStill = DateTime.Now;
                    _backgroundImage = originalImage;
                }

                var source = ConvertBitmap(image);
                Image.Source = source;

                image.Dispose();

            });
        }

        private Bitmap CaptureImage(Bitmap image)
        {
            var thresholdedDifferenceFilter = new ThresholdedDifference(35);
            thresholdedDifferenceFilter.OverlayImage = _backgroundImage;
            var maskImage = thresholdedDifferenceFilter.Apply(image);

            var grayscaleFilter = new GrayscaleToRGB();
            var rgbMaskImage = grayscaleFilter.Apply(maskImage);

            var intersectFilter = new Intersect(image);
            var intersection = intersectFilter.Apply(rgbMaskImage);
            
            var colorFiltering = new EuclideanColorFiltering();
            colorFiltering.CenterColor = new AForge.Imaging.RGB(Color.Black);
            colorFiltering.Radius = 0;
            colorFiltering.FillOutside = false;
            colorFiltering.FillColor = new AForge.Imaging.RGB(Color.White);

            var myEncoder = Encoder.Quality;
            var myEncoderParameters = new EncoderParameters(1);

            var myEncoderParameter = new EncoderParameter(myEncoder, 100L);
    myEncoderParameters.Param[0] = myEncoderParameter;

            var jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            var finalImage = colorFiltering.Apply(intersection);
            finalImage.Save(DateTime.Now.Ticks + ".jpg", jgpEncoder, myEncoderParameters);

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
    }
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using MathNet.Numerics.Interpolation;
using NumSharp;
using System.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

// 8 bit per Pixel
//using PixelFormat = System.Byte;
//using ImageFormat = SixLabors.ImageSharp.PixelFormats.Rgb24;

// 16 bit per Pixel
using PixelFormat = System.UInt16;
using ImageFormat = SixLabors.ImageSharp.PixelFormats.Rgb48;

public class Program
{
    public static float field_of_view = 90 * (float)Math.PI / 180;
    public static int num_beams = 640;
    public static int num_rows = 480;
    public static int num_cols = (int)(Math.Ceiling(Math.Sin((field_of_view) / 2) * num_rows * 2));

    public static IEnumerable<double> beams = Enumerable.Range(0, num_beams).Select(x => (double)x); // list of beam numbers
    public static IEnumerable<double> bearings = beams.Select(x => (x / num_beams - 0.5) * field_of_view); // list of angles for all beams in radians
    public static IInterpolation method = Interpolate.Linear(bearings, beams); // map from angle to beam number, i.e. the x coordinate in the source image


    public static float[] linspace(float startval, float endval, int steps)
    {
        //https://stackoverflow.com/questions/17046293/is-there-a-linspace-like-method-in-math-net
        float interval = (endval / MathF.Abs(endval)) * MathF.Abs(endval - startval) / (steps - 1);
        return (from val in Enumerable.Range(0, steps)
                select startval + (val * interval)).ToArray();
    }
    public static PointF GetMapping(int x, int y)
    {

        float r = (float)Math.Sqrt(Math.Pow(x - num_cols / 2, 2) + Math.Pow(y, 2)); // The radius is the y coordinate in the source image
        double angle = Math.Atan2(x - num_cols / 2, y);
        float b = (float)method.Interpolate(angle); // get bearing number from angle, which corresponds to the x coordinate in the source image
                                                    // Console.WriteLine($"x:{x} y:{y} r:{r} b:{b} (angle: {angle})");
        return new PointF(b, r);
    }
    public static void Main(string[] args)
    {

        using (Image<ImageFormat> input = Image.Load<ImageFormat>("input.jpg"))
        {

            input.Mutate(i => i.Grayscale());


            Console.WriteLine($"Image size: {input.Width}x{input.Height}");
            int inWidth = input.Width;
            int inHeight = input.Height;

            PixelFormat[,] inImgNativeR = new PixelFormat[inWidth, inHeight];
            for (int x = 0; x < inWidth; ++x)
            {
                for (int y = 0; y < inHeight; ++y)
                {
                    inImgNativeR[x, y] = input[x, y].R;
                }
            }


            int outHeight = input.Height;
            int outWidth = num_cols;

            float[,] mapX = new float[outWidth, outHeight];
            float[,] mapY = new float[outWidth, outHeight];
            var sw = new Stopwatch();
            sw.Start();

            for (int x = 0; x < outWidth; ++x)
            {
                for (int y = 0; y < outHeight; ++y)
                {
                    PointF p = GetMapping(x, y);
                    mapX[x, y] = p.X;
                    mapY[x, y] = p.Y;
                }
            }
            var initElapsed = sw.ElapsedMilliseconds;
            Console.WriteLine($"Initialization time: {initElapsed}");

            Image<ImageFormat> outImg = new Image<ImageFormat>(outWidth, outHeight);
            Image<ImageFormat> outImgInterpolated = new Image<ImageFormat>(outWidth, outHeight);

            PixelFormat[,] outImgNativeative = new PixelFormat[outWidth, outHeight];
            PixelFormat[,] outImgNativeInterpolated = new PixelFormat[outWidth, outHeight];

            sw.Restart();
            for (int x = 0; x < outWidth; ++x)
            {
                for (int y = 0; y < outHeight; ++y)
                {
                    float xInterpolated = mapX[x, y];
                    float yInterpolated = mapY[x, y];
                    int xLeft = (int)xInterpolated;
                    int xRight = (int)xInterpolated + 1;
                    int yTop = (int)yInterpolated;
                    int yBottom = (int)yInterpolated + 1;
                    float xFactor = xInterpolated - xLeft;
                    float yFactor = yInterpolated - yTop;

                    if (xLeft < 0 || xRight >= inWidth - 1 || yTop < 0 || yBottom >= inHeight - 1)
                    {
                        outImgNativeative[x, y] = 0;
                    }
                    else
                    {
                        float pixTopLeft = inImgNativeR[xLeft, yTop];
                        float pixTopRight = inImgNativeR[xLeft, yBottom];
                        float pixBottomLeft = inImgNativeR[xRight, yTop];
                        float pixBottomRight = inImgNativeR[xRight, yBottom];
                        float pixTop = pixTopLeft * (1 - xFactor) + pixTopRight * xFactor;
                        float pixBottom = pixBottomLeft * (1 - xFactor) + pixBottomRight * xFactor;
                        float pix = pixTop * (1 - yFactor) + pixBottom * yFactor;
                        //Console.WriteLine($"xFactor:{xFactor}  yFactor:{yFactor} - {pixTopLeft} {pixTopRight} {pixBottomLeft} {pixBottomRight} - {pix}");
                        outImgNativeInterpolated[x, y] = (PixelFormat)pix;
                        outImgNativeative[x, y] = (PixelFormat)pixTopLeft;
                    }
                }

            }
            var remapElapsed = sw.ElapsedMilliseconds;
            Console.WriteLine($"Remap time: {remapElapsed}");
            for (int x = 0; x < outWidth; ++x)
            {
                for (int y = 0; y < outHeight; ++y)
                {
                    PixelFormat val = outImgNativeative[x, y];
                    outImg[x, y] = new ImageFormat(val, val, val);
                    PixelFormat valInterpolated = outImgNativeInterpolated[x, y];
                    outImgInterpolated[x, y] = new ImageFormat(valInterpolated, valInterpolated, valInterpolated);

                }
            }

            outImg.Save("output.png");
            outImgInterpolated.Save("output_smooth.png");

        }
    }
}



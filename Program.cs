using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using NumSharp;
using System.Diagnostics;

// 8 bit per Pixel
//using PixelFormat = System.Byte;
//using ImageFormat = SixLabors.ImageSharp.PixelFormats.Rgb24;

// 16 bit per Pixel
using PixelFormat = System.UInt16;
using ImageFormat = SixLabors.ImageSharp.PixelFormats.Rgb48;

public class Program
{

    public static void Main(string[] args)
    {

        using (Image<ImageFormat> input = Image.Load<ImageFormat>("input.jpg"))
        {

            input.Mutate(i => i.Grayscale());


            Console.WriteLine($"Image size: {input.Width}x{input.Height}");
            int inWidth = input.Width;
            int inHeight = input.Height;

            var npMapX = np.load("map_x.npy");
            var npMapY = np.load("map_y.npy");

            PixelFormat[,] inImgNativeR = new PixelFormat[inWidth, inHeight];


            int outHeight = npMapX.shape[0];
            int outWidth = npMapX.shape[1];

            float[,] mapX = new float[outWidth, outHeight];
            float[,] mapY = new float[outWidth, outHeight];

            for (int x = 0; x < inWidth; ++x)
            {
                for (int y = 0; y < inHeight; ++y)
                {
                    inImgNativeR[x, y] = input[x, y].R;
                }
            }

            for (int x = 0; x < outWidth; ++x)
            {
                for (int y = 0; y < outHeight; ++y)
                {
                    // Numpy arrays are transposed, we fix this here
                    mapX[x, y] = npMapX[y, x];
                    mapY[x, y] = npMapY[y, x];
                }
            }

            Image<ImageFormat> outImg = new Image<ImageFormat>(outWidth, outHeight);
            Image<ImageFormat> outImgInterpolated = new Image<ImageFormat>(outWidth, outHeight);

            PixelFormat[,] outImgNativeative = new PixelFormat[outWidth, outHeight];
            PixelFormat[,] outImgNativeInterpolated = new PixelFormat[outWidth, outHeight];


            var sw = new Stopwatch();
            sw.Start();
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

            Console.WriteLine(sw.ElapsedMilliseconds);
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



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace GitViewer
{
    class TransparentImage
    {
        public static void DrawImageWithAlpha(Graphics graphics, Image image, Point upperLeft, float alpha)
        {
            float[][] matrixItems ={
                            new float[] { 1, 0, 0, 0, 0 },
                            new float[] { 0, 1, 0, 0, 0 },
                            new float[] { 0, 0, 1, 0, 0 },
                            new float[] { 0, 0, 0, alpha, 0 },
                            new float[] { 0, 0, 0, 0, 1 },
                        };
            ColorMatrix colorMatrix = new ColorMatrix(matrixItems);
            var imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(
                colorMatrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap
            );
            graphics.DrawImage(image, new Rectangle(upperLeft.X, upperLeft.Y, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
        }

    }
}

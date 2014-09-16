using MvcApplication1;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Single;
using AForge.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Analysis;
using CoursesTests.Helpers;
using CoursesTests.Models;

namespace CoursesTests.Controllers
{
    
    public class ImageProcessingController : BaseController
    {
        //
        // GET: /ImageProcessing/

        //matrix for Grey level 
        public static byte[,] Grey;
        //public static byte[,] GreyTmp;
        public int[, ,] H3D;
        public byte[,] R;
        public byte[,] G;
        public byte[,] B;
        public DateTime Now;
        public ActionResult ProcessImages()
        {


            ImageProcessionDataViewModel model = new ImageProcessionDataViewModel()
            {
                SelectedPictureName = MyConstants.NO_PICTURE_PATH_AND_NAME,
                GrayStylePictureName = MyConstants.NO_PICTURE_PATH_AND_NAME,
                HistogramName = MyConstants.NO_PICTURE_PATH_AND_NAME,
            };
            return View(model);
        }

        public ActionResult Submit(HttpPostedFileBase file)
        {
            Now = DateTime.Now;
            if (file != null)
            {
                var ext = Path.GetExtension(file.FileName);
                if (allowedFileExtensions.Contains(ext.Substring(1).ToLower()))
                {

                    var selectedFileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var fullPath = Server.MapPath(MyConstants.SELECTED_PICTURE_SAVE_PATH);
                    var selectedFileFullPathPlusName = fullPath + selectedFileName + ext;
                    CreateDirectoryIfDontExist(fullPath);
                    DeleteFileIfExist(selectedFileFullPathPlusName);
                    file.SaveAs(selectedFileFullPathPlusName);

                    Bitmap bitMapPicture = new Bitmap(selectedFileFullPathPlusName);
                    ResizeColorMatrix(bitMapPicture.Height, bitMapPicture.Width);
                    //creates gray style picture and saves it
                    string grayStyleImageFullNameAndPath = Server.MapPath(MyConstants.SELECTED_PICTURE_SAVE_PATH_GRAYSTYLE) + selectedFileName + "-grayStyle" + ext;
                    SaveGrayStylePicture(bitMapPicture, grayStyleImageFullNameAndPath);

                    Matrix translated = KLTransform(DenseMatrix.OfArray(FromByteMatrixToFloat(Grey)));

                    //creates histogram picture and saves it
                    string histogramFullNameAndPath = Server.MapPath(MyConstants.SELECTED_PICTURE_HISTOGRAM_SAVE_PATH) + selectedFileName + "-3d-histogram" + ext;
                    SaveHistrogram(histogramFullNameAndPath, bitMapPicture.Width, bitMapPicture.Height);

                    string displaySelectedPicturePath = MyConstants.SELECTED_PICTURE_DISPLAY_PATH + selectedFileName + ext;
                    string displayGrayStylePicturePath = MyConstants.SELECTED_PICTURE_DISPLAY_PATH_GRAYSTYLE + selectedFileName + "-grayStyle" + ext;
                    string displayHistogramPicturePath = MyConstants.SELECTED_PICTURE_HISTOGRAM_DISPLAY_PATH + selectedFileName + "-3d-histogram" + ext;


                    ImageProcessionDataViewModel model = new ImageProcessionDataViewModel()
                    {
                        SelectedPictureName = displaySelectedPicturePath,
                        GrayStylePictureName = displayGrayStylePicturePath,
                        HistogramName = displayHistogramPicturePath,
                    };

                    return View("ProcessImages", model);
                }
                else
                {

                    return RedirectToAction("ProcessImages");
                }


            }

            return RedirectToAction("ProcessImages");
        }













        #region independable methods

        public Matrix KLTransform(Matrix matrix)
        {
            matrix = (Matrix)matrix.Transpose();

            int columnNumber = matrix.ColumnCount;

            Matrix mean = GetMean(matrix);

            //Don't print it becouse is a lot of values
          //  PrintMatrix(mean, null, " mean matrix"); - 

            float[,] oneD = new float[1, columnNumber];
            for (int i = 0; i < columnNumber; i++)
            {
                oneD[0, i] = 1;
            }

            //so called edinichna matrica
            Matrix onesMatrix = DenseMatrix.OfArray(oneD);

            // center the data
            Matrix xm = (DenseMatrix)matrix.Subtract(mean.Multiply(onesMatrix));
           // PrintMatrix(null, xm.ToArray(), "center the data");

            // Calculate covariance matrix

            DenseMatrix cov = (DenseMatrix)(xm.Multiply(xm.Transpose())).Multiply(1.0f / columnNumber);
          //  PrintMatrix(cov, null, "Calculate covariance matrix");
            PrintText(cov.ColumnCount.ToString() + " Calculate covariance  ColumnCount");
            PrintText(cov.RowCount.ToString() + " Calculate covariance  RowCount");

            //this is from another libraly accord .net 
            EigenvalueDecomposition v = new EigenvalueDecomposition(FromFloatMatrixToDouble(cov.ToArray()));

            double[,] eigVectors = v.Eigenvectors;
            double[,] eidDiagonal = v.DiagonalMatrix;

            Matrix eigVectorsTransposedMatrix = (DenseMatrix)DenseMatrix.OfArray(FromDoubleMatrixToFlaot(eigVectors)).Transpose();
            Matrix eidDiagonalMatrix = DenseMatrix.OfArray(FromDoubleMatrixToFlaot(eidDiagonal));

            int rowsCountDiagonals = eidDiagonal.GetLength(0);
            int maxLambdaIndex = 0;
            for (int i = 0; i < rowsCountDiagonals - 1; i++)
            {
                if (eidDiagonal[i, i] <= eidDiagonal[i + 1, i + 1])
                {
                    maxLambdaIndex = i + 1;
                }
            }

            double maxAlpha = eidDiagonal[maxLambdaIndex, maxLambdaIndex];

            double sumAlpha = 0;
            for (int i = 0; i < rowsCountDiagonals; i++)
            {
                sumAlpha += eidDiagonal[i, i];
            }

            CalculateAndPrintError(maxAlpha, sumAlpha);
           PrintText("Max lambda index " + maxLambdaIndex);

           // PrintMatrix(eidDiagonalMatrix, null, "Eign vals");

            // //PCA

            float[,] arr = new float[1, eigVectorsTransposedMatrix.ColumnCount];
            for (int i = 0; i < eigVectorsTransposedMatrix.ColumnCount; i++)
            {
                arr[0, i] = eigVectorsTransposedMatrix[maxLambdaIndex, i];
            }

            Matrix mainComponentMatrix = DenseMatrix.OfArray(arr);
          //  PrintMatrix(mainComponentMatrix, null, "Main Component");


            Matrix pca = (DenseMatrix)mainComponentMatrix.Multiply(xm);
          //  PrintMatrix(pca, null, "PCA");

            return pca;

        }

        public String CalculateAndPrintError(double maxAlpha, double sumAlpha)
        {
            double error = maxAlpha / sumAlpha;
            int percentage = (int)Math.Ceiling((error) * 100);
            String errStr = "Percenatge Error " + percentage + "%";
            PrintText(errStr + " Mistake");
            return errStr;
        }

        public byte[,] FromByteMatrixToFloat(float[,] matrix)
        {
            byte[,] dMatrix = new byte[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    dMatrix[i, j] = (byte)matrix[i, j];
                }
            }

            return dMatrix;
        }
        public float[,] FromByteMatrixToFloat(byte[,] matrix)
        {
            float[,] dMatrix = new float[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    dMatrix[i, j] = matrix[i, j];
                }
            }

            return dMatrix;
        }

        public double[,] FromFloatMatrixToDouble(float[,] matrix)
        {
            double[,] dMatrix = new double[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    dMatrix[i, j] = matrix[i, j];
                }
            }

            return dMatrix;
        }

        public float[,] FromDoubleMatrixToFlaot(double[,] matrix)
        {
            float[,] fMatrix = new float[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    fMatrix[i, j] = (float)matrix[i, j];
                }
            }

            return fMatrix;
        }

        public void PrintText(string text)
        {
            string path = MyConstants.IMAGE_PROCESSING_TEXT_FILES + "KlTransofmData" + Now.Millisecond.ToString() + ".txt";
            using (StreamWriter sw = System.IO.File.Exists(Server.MapPath(path)) ? System.IO.File.AppendText(Server.MapPath(path)) : System.IO.File.CreateText(Server.MapPath(path)))
            {
                sw.WriteLine("----- text print--->" + text + "-------");
            }

        }


        public void PrintMatrix(Matrix matrix, float[,] matrixAsArray, string message)
        {
            float[,] matrixArr;
            if (matrixAsArray != null)
            {
                matrixArr = (float[,])matrixAsArray.Clone();
            }
            else
            {
                matrixArr = matrix.ToArray();
            }

            string path = MyConstants.IMAGE_PROCESSING_TEXT_FILES + "KlTransofmData" + Now.Millisecond.ToString() + ".txt";
            using (StreamWriter sw = System.IO.File.Exists(Server.MapPath(path)) ? System.IO.File.AppendText(Server.MapPath(path)) : System.IO.File.CreateText(Server.MapPath(path)))
            {
                sw.WriteLine("-----" + message + "-------");
                for (int i = 0; i < matrixArr.GetLength(0); i++)
                {
                    for (int j = 0; j < matrixArr.GetLength(1); j++)
                    {
                        // DeleteFileIfExist(path);
                        sw.Write(" " + matrixArr[i, j]);
                    }
                    sw.WriteLine();
                }
                sw.WriteLine("-----" + message + " end ");
            }



        }
        public Matrix GetMean(Matrix x)
        {
            int colNumber = x.ColumnCount;
            int rowNumber = x.RowCount;

            Matrix mean = new DenseMatrix(rowNumber, 1);

            for (int i = 0; i < rowNumber; i++)
            {
                float avg = 0.0f;
                for (int j = 0; j < colNumber; j++)
                {
                    avg += x[i, j];
                }
                mean[1, 0] = avg / colNumber;

            }
            return mean;
        }
        private void SaveGrayStylePicture(Bitmap loadedPicture, string picturePathAndName)
        {
            int y = loadedPicture.Height;
            int x = loadedPicture.Width;

            //  
            BitmapToMatrix(loadedPicture);

            DeleteFileIfExist(picturePathAndName);
            loadedPicture = new Bitmap(GreyMatrixToBitmap(y, x));
            loadedPicture.Save(picturePathAndName);

        }

        public void SaveHistrogram(string picturePathAndName, int histogramWidth, int histrogramHeight)
        {
            CalculateH3D(histrogramHeight, histogramWidth);

            Save3DHistogramBitMap(picturePathAndName, histogramWidth, histrogramHeight);
        }
        public void Save3DHistogramBitMap(string picturePathAndName, int histogramWidth, int histrogramHeight)
        {

            //Draw Histogram
            Bitmap HistogramBmp = new Bitmap(histogramWidth, histrogramHeight,
                PixelFormat.Format32bppArgb);
            Graphics graphic = Graphics.FromImage(HistogramBmp);

            Pen pen = new Pen(Color.Black);
            Color clr = new Color();

            Point p = new Point();
            Point p1 = new Point();

            double max = H3DMax();
            double H, S, I;
            int R, G, B;
            I = 0.63;
            S = 0.9;

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    p.X = i;
                    p.Y = j;
                    p1.X = i + 1;
                    p1.Y = j + 1;

                    //if (ch3DColor.Checked == false)
                    //{                    //Meke it BW
                    H = 255 * ((double)H3D[0, i, j]) / max;
                    R = (int)H;
                    R = R & 0xFF;
                    clr = Color.FromArgb(R, R, R);
                    //}

                    //if i want to have 3d color picture
                    //if (ch3DColor.Checked == true)
                    //{
                    //    H = 360 - 360.0 * ((double)H3D[0, i, j]) / max;
                    //    //   H =  i * 1.44;
                    //    clr = HSI_RGB(H, S, I);
                    //}

                    pen.Color = clr;
                    graphic.DrawLine(pen, p1, p);
                }

            }

            for (int i = 0; i < 5; i++)
            {
                //Draw string
                // Create string to draw.
                String drawString = (i * 64).ToString();

                // Create font and brush.
                Font drawFont = new Font("Arial", 8);
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                // Create point for upper-left corner of drawing.
                PointF drawPoint = new Point(i * 59, 0);
                // Draw string to screen.
                graphic.DrawString(drawString, drawFont, drawBrush, drawPoint);
            }

            for (int i = 0; i < 5; i++)
            {
                //Draw string
                // Create string to draw.
                String drawString = (i * 64).ToString();

                // Create font and brush.
                Font drawFont = new Font("Arial", 8);
                SolidBrush drawBrush = new SolidBrush(Color.Black);
                // Create point for upper-left corner of drawing.
                PointF drawPoint = new Point(0, i * 59);
                // Draw string to screen.
                graphic.DrawString(drawString, drawFont, drawBrush, drawPoint);
            }

            DeleteFileIfExist(picturePathAndName);

            Bitmap histogram = new Bitmap(HistogramBmp);
            histogram.Save(picturePathAndName);
            // PicHist.Image = new Bitmap(HistogramBmp);
            HistogramBmp.Dispose();
            graphic.Dispose();

        }
        public void CalculateH3D(int y, int x)
        {
            H3D = new int[2, 256, 256];
            H3D.Initialize();

            for (int i = 0; i < y - 1; i++)
            {
                for (int j = 0; j < x - 1; j++)
                {
                    H3D[0, Grey[i, j], Grey[i, j + 1]]++;
                    H3D[0, Grey[i, j], Grey[i + 1, j]]++;

                    H3D[1, Grey[i, j], Grey[i, j + 1]] = Grey[i, j + 1];
                    H3D[1, Grey[i, j], Grey[i + 1, j]] = Grey[i + 1, j];
                }
            }
        }
        public int H3DMax()
        {
            int max = 0;
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    if (max < H3D[0, i, j])
                        max = H3D[0, i, j];
                }
            }
            return max;
        }

        public Bitmap GreyMatrixToBitmap(int y, int x)
        {
            //This function makes access to BMP byte matrix
            //And copy data inside R B B and grey color matrix
            Bitmap returnBmp = new Bitmap(x, y,
                PixelFormat.Format32bppArgb);

            BitmapData bitmapData2 = returnBmp.LockBits(new Rectangle(0, 0,
                returnBmp.Width, returnBmp.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            //	int a = 0;

            unsafe
            {

                byte* imagePointer2 = (byte*)bitmapData2.Scan0;
                //Loop each row
                for (int i = 0; i < bitmapData2.Height; i++)
                {
                    //Scan each pixel inside a row
                    for (int j = 0; j < bitmapData2.Width; j++)
                    {
                        //B color channel
                        imagePointer2[0] = Grey[i, j];
                        //G color channel
                        imagePointer2[1] = Grey[i, j];
                        //R color channel
                        imagePointer2[2] = Grey[i, j];
                        imagePointer2[3] = 255;//imagePointer2[3];
                        //1 pixel is consested by 4 bytes
                        //we increment our pointer position
                        //	imagePointer1 = imagePointer1 + 4;
                        imagePointer2 += 4;
                        //	imagePointer2 += 4;
                    }//end for j
                    //4 bytes per pixel * number of pixels in a row
                    imagePointer2 += bitmapData2.Stride -
                        (bitmapData2.Width * 4);
                }//end for i
            }
            //end unsafe
            returnBmp.UnlockBits(bitmapData2);
            return returnBmp;
        }//end processImage

        public void BitmapToMatrix(Bitmap BMP)
        {
            //This function makes access to BMP byte matrix
            //And copy data inside R B B and grey color matrix
            //	Bitmap returnBmp = new Bitmap(BMP.Width, BMP.Height,
            //		PixelFormat.Format32bppArgb);
            BitmapData bitmapData1 = BMP.LockBits(new Rectangle(0, 0,
                BMP.Width, BMP.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            //	BitmapData bitmapData2 = returnBmp.LockBits(new Rectangle(0, 0,
            //		returnBmp.Width, returnBmp.Height),
            //		ImageLockMode.ReadOnly,
            //		PixelFormat.Format32bppArgb);
            int a = 0;

            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;
                //	byte* imagePointer2 = (byte*)bitmapData2.Scan0;
                //Loop each row
                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    //Scan each pixel inside a row
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        // Find the mean color RGB to grey level convertion
                        R[i, j] = imagePointer1[0];
                        G[i, j] = imagePointer1[1];
                        B[i, j] = imagePointer1[2];

                        a = (byte)(0.229 * (double)imagePointer1[0] + 0.587 * (double)imagePointer1[1] +
                            0.114 * (double)imagePointer1[2]);// / 3;

                        //a = (imagePointer1[0] + imagePointer1[1] +
                        //	imagePointer1[2])/ 3;
                        //Load Grey level in to matrix
                        Grey[i, j] = (byte)a;
                        //B color channel
                        //	imagePointer2[0] = (byte)a;
                        //G color channel
                        //	imagePointer2[1] = (byte)a;
                        //R color channel
                        //	imagePointer2[2] = (byte)a;
                        //	imagePointer2[3] = imagePointer1[3];
                        //1 pixel is consested by 4 bytes
                        //we increment our pointer position
                        //	imagePointer1 = imagePointer1 + 4;
                        imagePointer1 += 4;
                        //	imagePointer2 += 4;
                    }//end for j
                    //4 bytes per pixel * number of pixels in a row
                    imagePointer1 += bitmapData1.Stride -
                        (bitmapData1.Width * 4);
                    //	imagePointer2 += bitmapData1.Stride -
                    //		(bitmapData1.Width * 4);
                }//end for i
            }
            //end unsafe
            //	returnBmp.UnlockBits(bitmapData2);
            BMP.UnlockBits(bitmapData1);
            //	return returnBmp;

        }
        public void ResizeColorMatrix(int y, int x)
        {
            //This code resizes 4 color pixel matrix
            //according to y x dimentions
            Grey = new byte[y, x];
            R = new byte[y, x];
            G = new byte[y, x];
            B = new byte[y, x];
        }

        #endregion
    }
}
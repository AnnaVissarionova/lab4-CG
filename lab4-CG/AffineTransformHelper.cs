using System;
using System.Drawing;
using System.Collections.Generic;

namespace lab4
{
    public static class AffineTransformHelper
    {
        private static float[,] currentTransform = CreateIdentityMatrix();

        public static void ApplyTransform(Polygon polygon, int transformType,
            string dxText, string dyText, string angleText, string scaleText,
            string centerXText, string centerYText)
        {
            currentTransform = CreateIdentityMatrix();

            switch (transformType)
            {
                case 0: // Смещение
                    float dx = float.Parse(dxText);
                    float dy = float.Parse(dyText);
                    ComposeWithTranslation(dx, dy);
                    break;

                case 1: // Поворот вокруг точки - композиция
                    float angle1 = float.Parse(angleText);
                    float centerX1 = float.Parse(centerXText);
                    float centerY1 = float.Parse(centerYText);
                    ComposeWithRotationAroundPoint(angle1, new PointF(centerX1, centerY1));
                    break;

                case 2: // Поворот вокруг центра - композиция
                    float angle2 = float.Parse(angleText);
                    var center = polygon.GetCenter();
                    ComposeWithRotationAroundPoint(angle2, center);
                    break;

                case 3: // Масштабирование от точки - композиция
                    float scale1 = float.Parse(scaleText);
                    float centerX2 = float.Parse(centerXText);
                    float centerY2 = float.Parse(centerYText);
                    ComposeWithScalingAroundPoint(scale1, scale1, new PointF(centerX2, centerY2));
                    break;

                case 4: // Масштабирование от центра - композиция
                    float scale2 = float.Parse(scaleText);
                    var center2 = polygon.GetCenter();
                    ComposeWithScalingAroundPoint(scale2, scale2, center2);
                    break;
            }

            ApplyMatrix(polygon, currentTransform);
        }

        public static void ApplyMatrix(Polygon polygon, float[,] finalMatrix)
        {
            var transformedPoints = new List<PointF>();

            foreach (var point in polygon.WorldPoints)
            {
                // Вектор-строка [x, y, 1] умножается на матрицу преобразования
                // [x' y' 1] = [x y 1] × M
                float x = point.X;
                float y = point.Y;
                float w = 1;

                float newX = x * finalMatrix[0, 0] + y * finalMatrix[1, 0] + w * finalMatrix[2, 0];
                float newY = x * finalMatrix[0, 1] + y * finalMatrix[1, 1] + w * finalMatrix[2, 1];

                transformedPoints.Add(new PointF(newX, newY));
            }

            polygon.WorldPoints.Clear();
            polygon.WorldPoints.AddRange(transformedPoints);
        }



        // Методы для композиции преобразований

        private static void ComposeWithTranslation(float tx, float ty)
        {
            var translationMatrix = CreateTranslationMatrix(-tx, -ty);
            currentTransform = MultiplyMatrices(currentTransform, translationMatrix);
        }

        private static void ComposeWithRotationAroundPoint(float angle, PointF center)
        {
            var translateToOrigin = CreateTranslationMatrix(-center.X, -center.Y);
            var rotate = CreateRotationMatrix(angle);
            var translateBack = CreateTranslationMatrix(center.X, center.Y);

            var rotationAroundPoint = MultiplyMatrices(translateBack,
                                MultiplyMatrices(rotate, translateToOrigin));

            currentTransform = MultiplyMatrices(currentTransform, rotationAroundPoint);
        }

        private static void ComposeWithScalingAroundPoint(float scaleX, float scaleY, PointF center)
        {
            var translateToOrigin = CreateTranslationMatrix(-center.X, -center.Y);
            var scale = CreateScaleMatrix(scaleX, scaleY);
            var translateBack = CreateTranslationMatrix(center.X, center.Y);

            var scalingAroundPoint = MultiplyMatrices(translateBack,
                                MultiplyMatrices(scale, translateToOrigin));

            currentTransform = MultiplyMatrices(currentTransform, scalingAroundPoint);
        }

     
        // [ 1  0  0 ]
        // [ 0  1  0 ]
        // [ tx ty 1 ]
        public static float[,] CreateTranslationMatrix(float tx, float ty)
        {
            return new float[,]
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { -tx, -ty, 1 }
            };
        }


        // [ cosθ  sinθ  0 ]
        // [ -sinθ cosθ  0 ]
        // [  0     0    1 ]
        public static float[,] CreateRotationMatrix(float angle)
        {
            double radians = angle * Math.PI / 180.0;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            return new float[,]
            {
                { (float)cos, (float)sin, 0 },
                { (float)-sin, (float)cos, 0 },
                { 0, 0, 1 }
            };
        }


        // [ sx  0  0 ]
        // [ 0  sy  0 ]
        // [ 0   0  1 ]
        public static float[,] CreateScaleMatrix(float scaleX, float scaleY)
        {
            return new float[,]
            {
                { scaleX, 0, 0 },
                { 0, scaleY, 0 },
                { 0, 0, 1 }
            };
        }

    
        // [ 1  0  0 ]
        // [ 0  1  0 ]
        // [ 0  0  1 ]
        public static float[,] CreateIdentityMatrix()
        {
            return new float[,]
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 }
            };
        }

        public static float[,] MultiplyMatrices(float[,] a, float[,] b)
        {
            if (a.GetLength(0) != 3 || a.GetLength(1) != 3 ||
                b.GetLength(0) != 3 || b.GetLength(1) != 3)
                throw new ArgumentException("Both matrices must be 3x3");

            float[,] result = new float[3, 3];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < 3; k++)
                    {
                        result[i, j] += a[i, k] * b[k, j];
                    }
                }
            }

            return result;
        }

      
    }
}
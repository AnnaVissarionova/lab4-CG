using Editor;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Editor
{
    public static class AffineTransformHelper
    {
        public static void ApplyTransform(Polygon polygon, int transformType,
            string dxText, string dyText, string angleText, string scaleText,
            string centerXText, string centerYText)
        {
            switch (transformType)
            {
                case 0: // Смещение
                    float dx = float.Parse(dxText);
                    float dy = float.Parse(dyText);
                    Translate(polygon, dx, dy);
                    break;

                case 1: // Поворот вокруг точки
                    float angle1 = float.Parse(angleText);
                    float centerX1 = float.Parse(centerXText);
                    float centerY1 = float.Parse(centerYText);
                    Rotate(polygon, angle1, new PointF(centerX1, centerY1));
                    break;

                case 2: // Поворот вокруг центра
                    float angle2 = float.Parse(angleText);
                    RotateAroundCenter(polygon, angle2);
                    break;

                case 3: // Масштабирование от точки
                    float scale1 = float.Parse(scaleText);
                    float centerX2 = float.Parse(centerXText);
                    float centerY2 = float.Parse(centerYText);
                    Scale(polygon, scale1, new PointF(centerX2, centerY2));
                    break;

                case 4: // Масштабирование от центра
                    float scale2 = float.Parse(scaleText);
                    ScaleAroundCenter(polygon, scale2);
                    break;
            }
        }

        public static void Translate(Polygon polygon, float dx, float dy)
        {
            var matrix = new Matrix();
            matrix.Translate(dx, dy);
            TransformPoints(polygon, matrix);
        }

        public static void Rotate(Polygon polygon, float angle, PointF center)
        {
            var matrix = new Matrix();
            matrix.RotateAt(angle, center);
            TransformPoints(polygon, matrix);
        }

        public static void RotateAroundCenter(Polygon polygon, float angle)
        {
            var center = polygon.GetCenter();
            Rotate(polygon, angle, center);
        }

        public static void Scale(Polygon polygon, float scale, PointF center)
        {
            var matrix = new Matrix();
            matrix.Translate(-center.X, -center.Y);
            matrix.Scale(scale, scale);
            matrix.Translate(center.X, center.Y);
            TransformPoints(polygon, matrix);
        }

        public static void ScaleAroundCenter(Polygon polygon, float scale)
        {
            var center = polygon.GetCenter();
            Scale(polygon, scale, center);
        }

        private static void TransformPoints(Polygon polygon, Matrix matrix)
        {
            var points = polygon.Points.ToArray();
            matrix.TransformPoints(points);
            polygon.Points.Clear();
            polygon.Points.AddRange(points);
        }
    }
}
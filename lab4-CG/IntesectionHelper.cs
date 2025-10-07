using System;
using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace lab4
{
    public static class IntersectionHelper
    {
        public static PointF? FindIntersection(Edge edge1, Edge edge2)
        {
            float x1 = edge1.Start.X, y1 = edge1.Start.Y;
            float x2 = edge1.End.X, y2 = edge1.End.Y;
            float x3 = edge2.Start.X, y3 = edge2.Start.Y;
            float x4 = edge2.End.X, y4 = edge2.End.Y;

            float denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denom) < 0.0001) return null;

            float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
            float u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                return new PointF(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
            }

            return null;
        }
    }
}
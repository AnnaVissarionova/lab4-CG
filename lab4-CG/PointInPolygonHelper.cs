using System;
using System.Collections.Generic;
using System.Drawing;

namespace lab4
{
    public static class PointInPolygonHelper
    {
        public static bool IsPointInPolygon(PointF testPoint, List<PointF> polygon)
        {
            if (polygon.Count < 3) return false;

            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > testPoint.Y) != (polygon[j].Y > testPoint.Y)) &&
                    (testPoint.X < (polygon[j].X - polygon[i].X) * (testPoint.Y - polygon[i].Y) /
                    (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
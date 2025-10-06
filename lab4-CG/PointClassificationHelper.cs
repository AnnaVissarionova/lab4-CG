using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace  Editor
{
    public static class PointClassificationHelper
    {
        public static int ClassifyPointRelativeToEdge(PointF point, Edge edge)
        {
            float result = (edge.End.X - edge.Start.X) * (point.Y - edge.Start.Y) -
                          (edge.End.Y - edge.Start.Y) * (point.X - edge.Start.X);
            return Math.Sign(result);
        }
    }
}
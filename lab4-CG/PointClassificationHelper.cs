using System.Drawing;

namespace Editor
{
    public static class PointClassificationHelper
    {
        public static int ClassifyPointRelativeToEdge(PointF point, Edge edge)
        {
            float result = (edge.End.X - edge.Start.X) * (point.Y - edge.Start.Y) -
                          (edge.End.Y - edge.Start.Y) * (point.X - edge.Start.X);
            return Math.Sign(result);
        }

        /*
         ФОРМУЛА ИЗ ЛЕКЦИИ : ( yb * xa ) - ( xb - ya ) > 0   => b слева от OA
                             ( yb * xa ) - ( xb - ya ) < 0   => b справа от OA
         
        где A(xa;ya) - конец ребра (END)
            B(xb;yb) - тестируемая точка
            O(0;0)   - начало координат (START)
         

        edge.End.X - edge.Start.X = xa - 0 = xa
        point.Y - edge.Start.Y = yb - 0 = yb
        edge.End.Y - edge.Start.Y = ya - 0 = ya
        point.X - edge.Start.X = xb - 0 = xb


        по часовой стрелке начиная от стартовой точки 

         */



        public static Color GetPositionColor(int classification)
        {
            if (classification > 0) return Color.Green;
            if (classification < 0) return Color.Orange;
            return Color.Blue;
        }
    }
}
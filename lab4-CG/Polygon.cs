using Editor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Editor
{
    public class Polygon
    {
        public List<PointF> WorldPoints { get; private set; } = new List<PointF>(); // Мировые координаты
        public bool IsSelected { get; set; }

        // Добавляем точку в мировых координатах
        public void AddWorldPoint(PointF worldPoint)
        {
            WorldPoints.Add(worldPoint);
        }

        public void AddPoint(Point screenPoint)
        {
            WorldPoints.Add(new PointF(screenPoint.X, screenPoint.Y));
        }

        public void Draw(Graphics graphics, PointF centerPoint, int gridSize)
        {
            if (WorldPoints.Count == 0) return;

            // Преобразуем мировые координаты в экранные для отрисовки
            var screenPoints = WorldPoints.Select(wp =>
                new PointF(centerPoint.X + wp.X * gridSize, centerPoint.Y - wp.Y * gridSize)
            ).ToArray();

            var pen = IsSelected ? new Pen(Color.Red, 2) : new Pen(Color.Black, 2);
            var brush = IsSelected ? new SolidBrush(Color.FromArgb(50, 255, 100, 100)) :
                                   new SolidBrush(Color.FromArgb(40, 100, 150, 255));

            if (screenPoints.Length == 1)
            {
                DrawVertex(graphics, screenPoints[0]);
            }
            else if (screenPoints.Length == 2)
            {
                graphics.DrawLine(pen, screenPoints[0], screenPoints[1]);
                DrawVertex(graphics, screenPoints[0]);
                DrawVertex(graphics, screenPoints[1]);
            }
            else
            {
                var path = new GraphicsPath();
                path.AddPolygon(screenPoints);
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);

                foreach (var point in screenPoints)
                {
                    DrawVertex(graphics, point);
                }
            }
        }

        private void DrawVertex(Graphics graphics, PointF point)
        {
            graphics.DrawLine(Pens.Blue, point.X - 3, point.Y, point.X + 3, point.Y);
            graphics.DrawLine(Pens.Blue, point.X, point.Y - 3, point.X, point.Y + 3);
            graphics.FillEllipse(Brushes.Blue, point.X - 2, point.Y - 2, 4, 4);
        }

        public List<Edge> GetEdges()
        {
            var edges = new List<Edge>();
            if (WorldPoints.Count < 2) return edges;

            for (int i = 0; i < WorldPoints.Count; i++)
            {
                var next = (i + 1) % WorldPoints.Count;
                edges.Add(new Edge(WorldPoints[i], WorldPoints[next]));
            }
            return edges;
        }

        public bool Contains(Point screenPoint, PointF centerPoint, int gridSize)
        {
            // Преобразуем экранную точку в мировые координаты
            var worldPoint = new PointF(
                (screenPoint.X - centerPoint.X) / gridSize,
                (centerPoint.Y - screenPoint.Y) / gridSize
            );
            return PointInPolygonHelper.IsPointInPolygon(worldPoint, WorldPoints);
        }

        public PointF GetCenter()
        {
            if (WorldPoints.Count == 0) return PointF.Empty;

            float sumX = 0, sumY = 0;
            foreach (var point in WorldPoints)
            {
                sumX += point.X;
                sumY += point.Y;
            }
            return new PointF(sumX / WorldPoints.Count, sumY / WorldPoints.Count);
        }

        // Для обратной совместимости со старым кодом
        public List<PointF> Points => WorldPoints;
    }

    
    public static class GeometryHelper
    {
        public static bool IsPointOnEdge(Point screenPoint, Edge edge, float tolerance, PointF centerPoint, int gridSize)
        {
            // Преобразуем экранную точку в мировые координаты
            var worldPoint = new PointF(
                (screenPoint.X - centerPoint.X) / gridSize,
                (centerPoint.Y - screenPoint.Y) / gridSize
            );

            // Преобразуем tolerance в мировые координаты
            float worldTolerance = tolerance / gridSize;

            float distance = PointToLineDistance(worldPoint, edge.Start, edge.End);
            return distance <= worldTolerance;
        }

        public static bool IsPointOnEdge(Point point, Edge edge, float tolerance)
        {
            // Используем центр (0,0) и gridSize=1 для совместимости со старым кодом
            return IsPointOnEdge(point, edge, tolerance, new PointF(0, 0), 1);
        }

        private static float PointToLineDistance(PointF point, PointF lineStart, PointF lineEnd)
        {
            float A = point.X - lineStart.X;
            float B = point.Y - lineStart.Y;
            float C = lineEnd.X - lineStart.X;
            float D = lineEnd.Y - lineStart.Y;

            float dot = A * C + B * D;
            float lenSq = C * C + D * D;
            float param = (lenSq != 0) ? dot / lenSq : -1;

            float xx, yy;

            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }

            float dx = point.X - xx;
            float dy = point.Y - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}

public class Edge
{
    public PointF Start { get; set; } // Мировые координаты
    public PointF End { get; set; }   // Мировые координаты

    public Edge(PointF start, PointF end)
    {
        Start = start;
        End = end;
    }

    public void Draw(Graphics graphics, Pen pen, PointF centerPoint, int gridSize)
    {
        // Преобразуем в экранные координаты
        var screenStart = new PointF(
            centerPoint.X + Start.X * gridSize,
            centerPoint.Y - Start.Y * gridSize
        );
        var screenEnd = new PointF(
            centerPoint.X + End.X * gridSize,
            centerPoint.Y - End.Y * gridSize
        );

        graphics.DrawLine(pen, screenStart, screenEnd);

        // Вершины
        graphics.FillEllipse(pen.Brush, screenStart.X - 2, screenStart.Y - 2, 4, 4);
        graphics.FillEllipse(pen.Brush, screenEnd.X - 2, screenEnd.Y - 2, 4, 4);
    }

    public void Draw(Graphics graphics, Pen pen)
    {
        // преобразуем мировые координаты в экранные с использованием центра (0,0) и gridSize=1 для совместимости
        var center = new PointF(0, 0);
        int grid = 1;
        Draw(graphics, pen, center, grid);
    }
}
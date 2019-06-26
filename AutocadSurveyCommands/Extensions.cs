using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutocadSurveyCommands
{
    public static class Extensions
    {
        /// <summary>
        ///  Gets the area of a triangle.
        /// </summary>
        /// <param name="p1">1st point2d</param>
        /// <param name="p2">2nd point2d</param>
        /// <param name="p3">3rd point2d</param>
        /// <returns></returns>
        public static double GetArea(Point2d p1, Point2d p2, Point2d p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y)) / 2.0;
        }

        /// <summary>
        /// Gets the area of an arc segment.
        /// </summary>
        /// <param name="arc"></param>
        /// <returns></returns>
        public static double GetArea(this CircularArc2d arc)
        {
            double ang = arc.IsClockWise ?
                arc.StartAngle - arc.EndAngle :
                arc.EndAngle - arc.StartAngle;
            return (ang - Math.Sin(ang)) * arc.Radius * arc.Radius / 2.0;
        }

        /// <summary>
        /// Gets the geometric area of a polyline.
        /// The area will be of negative sign if the polyline is in clockwise direction.
        /// </summary>
        /// <param name="pline"></param>
        /// <returns></returns>
        public static double GetArea(this Polyline pline)
        {
            double area = 0.0;
            int last = pline.NumberOfVertices - 1;
            Point2d p0 = pline.GetPoint2dAt(0);

            if (pline.GetBulgeAt(0) != 0.0)
                area += pline.GetArcSegment2dAt(0).GetArea();
            for (int i = 1; i < last; i++)
            {
                area += GetArea(p0, pline.GetPoint2dAt(i), pline.GetPoint2dAt(i + 1));
                if (pline.GetBulgeAt(i) != 0.0)
                    area += pline.GetArcSegment2dAt(i).GetArea();
            }
            if (pline.GetBulgeAt(last) != 0.0 && pline.Closed)
                area += pline.GetArcSegment2dAt(last).GetArea();
            return area;
        }

        public static bool HasPoint(this Polyline pline, Point3d pt)
        {
            for (int i = 0, last = pline.NumberOfVertices; i < last; i++)
            {
                if (pline.GetPoint3dAt(i) == pt)
                    return true;
            }
            return false;
        }

        public static List<Point2d> GetPolylinePoints(this Polyline pline, int start = 0, int len = -1)
        {
            if (pline == null)
                return null;
            List<Point2d> vertices = new List<Point2d>();
            if (len == -1 || start + len > pline.NumberOfVertices)
            {
                len = pline.NumberOfVertices - start;
            }
            for (int i = start, end = start + len; i < end; i++)
            {
                vertices.Add(pline.GetPoint2dAt(i));
            }
            return vertices;
        }

        public static Point2d GetNextPoint2d(this Polyline pline, Point2d pt)
        {
            var index = pline.GetParameterAtPoint(pt.GetPoint3d());
            index = (index == pline.EndParam - 1) ? 0 : index + 1;
            return pline.GetPointAtParameter(index).GetPoint2d();
        }

        public static Point2d GetPreviousPoint2d(this Polyline pline, Point2d pt)
        {
            var index = pline.GetParameterAtPoint(pt.GetPoint3d());
            index = (index == 0) ? pline.EndParam - 1 : index - 1;
            return pline.GetPointAtParameter(index).GetPoint2d();
        }

        public static Point2d Point2dAfter(this Polyline pline, int index)
        {
            index = (index + pline.NumberOfVertices) % pline.NumberOfVertices;
            index = pline.Closed ?
                (index == pline.EndParam - 1) ? 0 : index + 1
                :
                (index == pline.EndParam) ? 0 : index + 1;
            return pline.GetPoint2dAt(index);
        }

        public static Point2d Point2dBefore(this Polyline pline, int index)
        {
            index = (index + pline.NumberOfVertices) % pline.NumberOfVertices;
            index = pline.Closed ?
                (index == 0) ? (int)pline.EndParam - 1 : index - 1
                :
                (index == 0) ? (int)pline.EndParam : index - 1;
            return pline.GetPoint2dAt(index);
        }

        /// <summary>
        /// Gets a Point2d that is far by a distance and at angle.
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="ang">The angle at which the result Point2d will be layed</param>
        /// <param name="dist">The distance to the new Point2d</param>
        /// <returns></returns>
        public static Point2d Polar(this Point2d basePt, double ang, double dist)
        {
            return new Point2d(
                basePt.X + dist * Math.Cos(ang),
                basePt.Y + dist * Math.Sin(ang));
        }

        public static Point2d Polar(this Point2d p0, Point2d p1, double dist)
        {
            return p0.Polar(p0.AngleTo(p1), dist);
        }

        /// <summary>
        /// returns the angle between X axis and the line connecting this point and pt.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="pt">The point which the angle will be measured to.</param>
        /// <returns></returns>
        public static double AngleTo(this Point2d p0, Point2d pt)
        {
            return p0.GetVectorTo(pt).Angle;
        }

        /// <summary>
        /// Gets a Point3d that is far by a distance and at angle.
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="ang">The angle at which the new Point2d will be layed</param>
        /// <param name="dist">The distance to the new Point2d</param>
        /// <returns></returns>
        public static Point3d Polar(this Point3d basePt, double ang, double dist)
        {
            return new Point3d(
                basePt.X + dist * Math.Cos(ang),
                basePt.Y + dist * Math.Sin(ang),
                basePt.Z);
        }

        /// <summary>
        /// Converts a Point3d to a Point2d.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static Point2d GetPoint2d(this Point3d pt)
        {
            return new Point2d(pt.X, pt.Y);
        }

        public static double GetAngleTo(this Point3d baze, Point3d pt)
        {
            return baze.GetPoint2d().GetVectorTo(pt.GetPoint2d()).Angle;
        }

        public static Point3d GetPoint3d(this Point2d pt)
        {
            return new Point3d(pt.X, pt.Y, 0);
        }
    }
}

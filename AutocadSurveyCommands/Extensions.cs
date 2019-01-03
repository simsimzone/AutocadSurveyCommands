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

        /// <summary>
        /// Gets a Point2d that is far by a distance and at angle.
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="ang">The angle at which the new Point2d will be layed</param>
        /// <param name="dist">The distance to the new Point2d</param>
        /// <returns></returns>
        public static Point2d Polar(this Point2d basePt, double ang, double dist)
        {
            return new Point2d(
                basePt.X + dist * Math.Cos(ang),
                basePt.Y + dist * Math.Sin(ang));
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

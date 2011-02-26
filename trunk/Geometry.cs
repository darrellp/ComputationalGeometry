#if DOUBLEPRECISION
using System.Collections.Generic;
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System;
using System.Linq;
#if NUNIT || DEBUG
using NUnit.Framework;
#endif

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Static class to provide geometric utility functions on points. </summary>
	///
	/// <remarks>	Darrellp, 2/17/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public static class Geometry
	{
		/// Tolerance we use in "near enough" calculations
		public const TPT Tolerance = 1e-10;

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	True if two values are "essentially" equal (i.e., equal within tolerance). </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="a">	First value. </param>
		/// <param name="b">	Second value. </param>
		///
		/// <returns>	True if values are essentially equal, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FCloseEnough(TPT a, TPT b)
		{
			return Math.Abs(a - b) < Tolerance;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	True if two points are "essentially" equal. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		///
		/// <returns>	True if they're "equal", else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FCloseEnough(PT pt1, PT pt2)
		{
			return FNearZero(ManhattanDistance(pt1, pt2));
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Determines if a number is equal to zero within tolerance. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="val">	Value to be checked. </param>
		///
		/// <returns>	True if it's near zero, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FNearZero(TPT val)
		{
			return Math.Abs(val) < Tolerance;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Dot product of two points. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		///
		/// <returns>	Dot product. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static TPT Dot(PT pt1, PT pt2)
		{
			return pt1.X * pt2.X + pt1.Y * pt2.Y;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Gives an index for the quad a point appears in as follows: 
		/// 3 | 0
		/// --+--
		/// 2 | 1. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt">	Point to evaluate. </param>
		///
		/// <returns>	Quadrant index. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static int IQuad(PT pt)
		{
			var iRet = 0;
			if (pt.X < 0)
			{
				iRet += 2;
				if (pt.Y > 0)
				{
					iRet += 1;
				}
			}
			else
			{
				if (pt.Y < 0)
				{
					iRet += 1;
				}
			}
			return iRet;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Midpoint between two other points. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		///
		/// <returns>	Midpoint. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static PT MidPoint(PT pt1, PT pt2)
		{
			return new PT(
				(pt1.X + pt2.X) / 2,
				(pt1.Y + pt2.Y) / 2);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Signed area of the triangle between three points.  This routine is fundamental to a number of
		/// other geometry routines.  It's positive if the three points are in counterclockwise order,
		/// negative otherwise and it's absolute value is the area of the triangle. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		/// <param name="pt3">	Third point. </param>
		///
		/// <returns>	Signed area of the triangle. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static TPT SignedArea(PT pt1, PT pt2, PT pt3)
		{
			return (pt2.X - pt1.X) * (pt3.Y - pt1.Y) -
				(pt3.X - pt1.X) * (pt2.Y - pt1.Y);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Determine if pt1, pt2, pt3 occur in Counter Clockwise order. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		/// <param name="pt3">	Third point. </param>
		///
		/// <returns>	1 if they appear in CCW order, -1 if CW order and 0 if they're linear. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static int ICcw(PT pt1, PT pt2, PT pt3)
		{
			return Math.Sign(SignedArea(pt1, pt2, pt3));
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Area of triangle defined by three points. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		/// <param name="pt3">	Third point. </param>
		///
		/// <returns>	triangle area. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static TPT Area(PT pt1, PT pt2, PT pt3)
		{
			// ReSharper disable RedundantCast
			return (TPT)Math.Abs(SignedArea(pt1, pt2, pt3));
			// ReSharper restore RedundantCast
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Determine if the test point is to the left of the line looking from ptSegmentStart to
		/// ptSegmentEnd. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="ptSegmentStart">	Start point. </param>
		/// <param name="ptSegmentEnd">		End point. </param>
		/// <param name="ptTest">			Test point. </param>
		///
		/// <returns>	true if test point is to the left of the line segment, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FLeft(PT ptSegmentStart, PT ptSegmentEnd, PT ptTest)
		{
			return SignedArea(ptSegmentStart, ptSegmentEnd, ptTest) > 0;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Determine if three points are essentially collinear. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		/// <param name="pt3">	Third point. </param>
		///
		/// <returns>	True if points are (essentially) collinear, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FCollinear(PT pt1, PT pt2, PT pt3)
		{
			return Area(pt1, pt2, pt3) < Tolerance;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Values that represent CrossingType for segment to segment intersection. </summary>
		///
		/// <remarks>	Darrellp, 2/24/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public enum CrossingType
		{
			/// <summary> Segments overlap and are collinear.  </summary>
			Edge,
			/// <summary> The endpoing of one segment lies on the other and they are not collinear.  </summary>
			Vertex,
			/// <summary> Normal crossing.  </summary>
			Normal,
			/// <summary> Segments are parallel and do not cross.  </summary>
			NonCrossing
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Does segment to segment crossings. </summary>
		///
		/// <remarks>	
		/// Based on code from Computational Geometry in C by Joseph O'Rourke.
		/// 
		/// Darrellp, 2/24/2011. 
		/// </remarks>
		///
		/// <param name="seg1Pt1">	The first point of the segment 1. </param>
		/// <param name="seg1Pt2">	The second point of the segment 1. </param>
		/// <param name="seg2Pt1">	The first point of segment 2. </param>
		/// <param name="seg2Pt2">	The second point of segment 2. </param>
		/// <param name="pPt">		[out] The intersection if any. </param>
		///
		/// <returns>	A crossing type as outlined above. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static CrossingType SegSegInt(PT seg1Pt1, PT seg1Pt2, PT seg2Pt1, PT seg2Pt2, out PT pPt)
		{
			var code = (CrossingType)(-1);
			pPt = new PT();

			var denom = seg1Pt1.X*(seg2Pt2.Y - seg2Pt1.Y) +
			               seg1Pt2.X*(seg2Pt1.Y - seg2Pt2.Y) +
			               seg2Pt2.X*(seg1Pt2.Y - seg1Pt1.Y) +
			               seg2Pt1.X*(seg1Pt1.Y - seg1Pt2.Y);

			if (FNearZero(denom))
			{
				return ParallelInt(seg1Pt1, seg1Pt2, seg2Pt1, seg2Pt2, out pPt);
			}

			var num = seg1Pt1.X*(seg2Pt2.Y - seg2Pt1.Y) +
			             seg2Pt1.X*(seg1Pt1.Y - seg2Pt2.Y) +
			             seg2Pt2.X*(seg2Pt1.Y - seg1Pt1.Y);

			if (FNearZero(num) || FCloseEnough(num, denom))
			{
				code = CrossingType.Vertex;
			}
			var tSeg1 = num/denom;

			num = -(seg1Pt1.X * (seg2Pt1.Y - seg1Pt2.Y) +
				   seg1Pt2.X * (seg1Pt1.Y - seg2Pt1.Y) +
				   seg2Pt1.X * (seg1Pt2.Y - seg1Pt1.Y));
			var tSeg2 = num/denom;

			if (FNearZero(num) || FCloseEnough(num, denom))
			{
				code = CrossingType.Vertex;
			}

			if (0 < tSeg1 && tSeg1 < 1 && 0 < tSeg2 && tSeg2 < 1)
			{
				code = CrossingType.Normal;
			}
			else if (0 > tSeg1 || tSeg1 > 1 || 0 > tSeg2 || tSeg2 > 1)
			{
				code = CrossingType.NonCrossing;
			}

			pPt.X = seg1Pt1.X + tSeg1*(seg1Pt2.X - seg1Pt1.X);
			pPt.Y = seg1Pt1.Y + tSeg1*(seg1Pt2.Y - seg1Pt1.Y);

			return code;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Determines if point B lies between points A and C. </summary>
		///
		/// <remarks>	
		/// Based on code from Computational Geometry in C by Joseph O'Rourke.
		/// 
		/// Darrellp, 2/24/2011. 
		/// </remarks>
		///
		/// <param name="ptSegEndpoint1">	a point. </param>
		/// <param name="ptSegmentEndpoint2">	The point. </param>
		/// <param name="ptTest">	The point. </param>
		///
		/// <returns>	true if it succeeds, false if it fails. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool Between(PT ptSegEndpoint1, PT ptSegmentEndpoint2, PT ptTest)
		{
			if (!FCollinear(ptSegEndpoint1, ptSegmentEndpoint2, ptTest))
			{
				return false;
			}

			if (ptSegEndpoint1.X != ptSegmentEndpoint2.X)
			{
				return ptSegEndpoint1.X <= ptTest.X && ptTest.X <= ptSegmentEndpoint2.X ||
				       ptSegEndpoint1.X >= ptTest.X && ptTest.X >= ptSegmentEndpoint2.X;
			}

			return ptSegEndpoint1.Y <= ptTest.Y && ptTest.Y <= ptSegmentEndpoint2.Y ||
			       ptSegEndpoint1.Y >= ptTest.Y && ptTest.Y >= ptSegmentEndpoint2.Y;
		}

		private static CrossingType ParallelInt(PT aPt, PT bPt, PT cPt, PT dPt, out PT pPt)
		{
			pPt = new PT();
			if (!FCollinear(aPt, bPt, cPt))
			{
				return CrossingType.NonCrossing;
			}

			if (Between(aPt, bPt, cPt))
			{
				pPt = cPt;
				return CrossingType.Edge;
			}
			if (Between(aPt, bPt, dPt))
			{
				pPt = dPt;
				return CrossingType.Edge;
			}
			if (Between(cPt, dPt, aPt))
			{
				pPt = aPt;
				return CrossingType.Edge;
			}
			if (Between(cPt, dPt, bPt))
			{
				pPt = cPt;
				return CrossingType.Edge;
			}
			return CrossingType.NonCrossing;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Euclidean distance between two points. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		///
		/// <returns>	Distance between the two points. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static TPT Distance(PT pt1, PT pt2)
		{
			var dx = pt1.X - pt2.X;
			var dy = pt1.Y - pt2.Y;

			return Math.Sqrt(dx * dx + dy * dy);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Euclidean distance between two points. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		///
		/// <returns>	Distance between the two points. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static TPT DistanceSq(PT pt1, PT pt2)
		{
			var dx = pt1.X - pt2.X;
			var dy = pt1.Y - pt2.Y;

			return dx * dx + dy * dy;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Manhattan Distance between two points.  Quicker metric for short distances than the Euclidean
		/// one. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">	First point. </param>
		/// <param name="pt2">	Second point. </param>
		///
		/// <returns>	Manhattan distance. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static TPT ManhattanDistance(PT pt1, PT pt2)
		{
			var dx = pt1.X - pt2.X;
			var dy = pt1.Y - pt2.Y;

			return Math.Abs(dx) + Math.Abs(dy);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Determine the x coordinate where the parabolas with focus at pt1 and pt2 intersect between
		/// the two points. The directrix for both parabolas is the line y = ys.
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <exception cref="InvalidOperationException">	Thrown when the requested operation is
		/// 												invalid. </exception>
		///
		/// <param name="pt1">	First focus. </param>
		/// <param name="pt2">	Second focus. </param>
		/// <param name="ys">	Y coordinate of the directrix. </param>
		///
		/// <returns>	X coordinate of the intersection of the two parabolas. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal static TPT ParabolicCut(PT pt1, PT pt2, TPT ys)
		{
			// If the foci are identical
			if(FCloseEnough(pt1.X, pt2.X) && FCloseEnough(pt1.Y, pt2.Y))
			{
				// Throw an exception
				throw new InvalidOperationException("Identical datapoints are not allowed!");
			}

			// If the focii are at the same y coordinate, the intersection is halfway between them
			if (FCloseEnough(pt1.Y, pt2.Y))
			{
				return (pt1.X + pt2.X) / 2;
			}

			// Handle degenerate vertical lines (y coordinate on the directrix)
			//
			// If one of the focii is on the directrix and the other isn't (if it were, they'd both have
			// the same y coordinate which would have been taken care of in the previous "if"), then
			// it's "parabola" is a vertical line at its X coordinate and the intersection will occur at
			// it's own x coordinate...

			// if pt1 is on the directrix
			if (FCloseEnough(pt1.Y, ys))
			{
				return pt1.X;
			}
			
			// if pt2 is on the directrix
			if (FCloseEnough(pt2.Y, ys))
			{
				return pt2.X;
			}

			// Initialize for the general case
			//
			// The general case is taken care of with this ugly math.  In general, there 
			// will be two places where the parabolas intersect so we have to compute
			// both and pick the one we want.
			//
			var a1 = 1 / (2 * (pt1.Y - ys));
			var a2 = 1 / (2 * (pt2.Y - ys));
			var da = a1 - a2;
			var s1 = 4 * a1 * pt1.X - 4 * a2 * pt2.X;
			var dx = pt1.X - pt2.X;
			var s2 = 2 * Math.Sqrt(2 * (2 * a1 * a2 * dx * dx - da * (pt1.Y - pt2.Y)));
			var m = 0.25 / da;
			var xs1 = m * (s1 + s2);
			var xs2 = m * (s1 - s2);

			// If we need to reorder
			if(xs1 > xs2)
			{
				// Swap xs values
				var h = xs1;
				xs1=xs2;
				xs2=h;
			}

			// Get the solution we're looking for
			return pt1.Y >= pt2.Y ? xs2 : xs1;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Compares absolute clockwise angles from the y axis. </summary>
		///
		/// <remarks>	
		/// This compares the absolute angle of (ptCenter,pt1) with (ptCenter,pt2) measured clockwise
		/// from the positive y axis. 
		/// </remarks>
		///
		/// <param name="ptCenter">	Center of the angle formed. </param>
		/// <param name="pt1">		First point in check. </param>
		/// <param name="pt2">		Second point in check. </param>
		///
		/// <returns>	Comparison of absolute angle. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static int ICompareCw(PT ptCenter, PT pt1, PT pt2)
		{
			// Get values relative to the Center of rotation
			var pt1Rel = new PT(pt1.X - ptCenter.X, pt1.Y - ptCenter.Y);
			var pt2Rel = new PT(pt2.X - ptCenter.X, pt2.Y - ptCenter.Y);

			// Determine quadrants of each point
			var iQuad1 = IQuad(pt1Rel);
			var iQuad2 = IQuad(pt2Rel);

			// If they're in different quadrants
			if (iQuad1 != iQuad2)
			{
				// We only have to compare the quadrants
				return iQuad1.CompareTo(iQuad2);
			}

			// If they're in the same quadrant, use geometry to figure it out...
			return -ICcw(ptCenter, pt2, pt1);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Determines if a point is in the interior of a convex polygon. </summary>
		///
		/// <remarks>	
		/// No check is made for the convexity of the polygon and it must be enumerated in CCW order
		/// 
		/// Darrellp, 2/25/2011. 
		/// </remarks>
		///
		/// <param name="ptTest">	Test point. </param>
		/// <param name="poly">		The points enumerating the polygon in CCW order. </param>
		///
		/// <returns>	true if ptTest is in the polygon, false if it fails. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool PointInConvexPoly(PT ptTest, IEnumerable<PT> poly, bool fIncludeBorders = false)
		{
			// We need to check the last vertex with the first, so add the first at the end for a cycle
			var polyCycle = poly.Concat(new PT[1] {poly.First()});
			return !polyCycle.
			        	// Calculate Signs on pairs of points
			        	Zip(polyCycle.Skip(1), (pt1, pt2) => Math.Sign(SignedArea(ptTest, pt1, pt2) + (fIncludeBorders ? 0.1 : 0))).
			        	// They're +1 for points going CCW around the test point
			        	Where(s => s != 1).
			        	// If any were not 1 then we're not inside.
			        	Any();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Determine the center of a circle passing through three points in the plane.  Most of this is
		/// ugly math generated from Mathematica. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt1">		First point. </param>
		/// <param name="pt2">		Second point. </param>
		/// <param name="pt3">		Third point. </param>
		/// <param name="ptCenter">	[out] out parameter returning the circumcenter. </param>
		///
		/// <returns>	
		/// False if the three points lie on a line in which case the circumcenter is not valid.  True
		/// otherwise and the returned point is the circumcenter. 
		/// </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FFindCircumcenter(PT pt1, PT pt2, PT pt3, out PT ptCenter)
		{
			// Initialize for ugly math to follow
			ptCenter = new PT();
			var d = (pt1.X - pt3.X) * (pt2.Y - pt3.Y) - (pt2.X - pt3.X) * (pt1.Y - pt3.Y);

			// If we've got some points identical to others
			if (Math.Abs(d) <= Tolerance)
			{
				return false;
			}

			ptCenter.X = (((pt1.X - pt3.X) * (pt1.X + pt3.X) + (pt1.Y - pt3.Y) * (pt1.Y + pt3.Y)) / 2 * (pt2.Y - pt3.Y) 
			    -  ((pt2.X - pt3.X) * (pt2.X + pt3.X) + (pt2.Y - pt3.Y) * (pt2.Y + pt3.Y)) / 2 * (pt1.Y - pt3.Y)) 
			    / d;

			ptCenter.Y = (((pt2.X - pt3.X) * (pt2.X + pt3.X) + (pt2.Y - pt3.Y) * (pt2.Y + pt3.Y)) / 2 * (pt1.X - pt3.X)
				- ((pt1.X - pt3.X) * (pt1.X + pt3.X) + (pt1.Y - pt3.Y) * (pt1.Y + pt3.Y)) / 2 * (pt2.X - pt3.X))
				/ d;

			return true;
		}
	}

	#region NUnit
#if NUNIT || DEBUG
	[TestFixture]
	public class TestGeometry
	{
		[Test]
		public void TestPointInConvexPoly()
		{
			List<PT> poly = new List<PointD>()
			                	{
			                		new PointD(-1, -1),
			                		new PointD(1, -1),
			                		new PointD(1, 1),
			                		new PointD(-1, 1)
			                	};
			Assert.IsTrue(Geometry.PointInConvexPoly(new PointD(0,0), poly));
			Assert.IsFalse(Geometry.PointInConvexPoly(new PointD(2, 0), poly));
			poly = new List<PointD>()
			       	{
			       		new PointD(-1689, 9836),
			       		new PointD(-6680, 7107),
			       		new PointD(393.18, 37.905),
			       		new PointD(394.025, 37.825),
			       		new PointD(416, 59)
			       	};
			Assert.IsFalse(Geometry.PointInConvexPoly(new PointD(423, 68), poly));
		}

		[Test]
		public void TestCcw()
		{
			var pt1 = new PT(1, 0);
			var pt2 = new PT(0, 0);
			var pt3 = new PT(1, 1);

			Assert.Less(Geometry.ICcw(pt1, pt2, pt3), 0);
			Assert.Greater(Geometry.ICcw(pt3, pt2, pt1), 0);
		}

		[Test]
		public void TestCircumcenter()
		{
			var pt1 = new PT(0, 0);
			var pt2 = new PT(1, 1);
			var pt3 = new PT(1, -1);
			var pt4 = new PT(2, 2);
			PT ptOut;

			Assert.IsTrue(Geometry.FFindCircumcenter(pt1, pt2, pt3, out ptOut));
			Assert.IsTrue(Geometry.FCloseEnough(ptOut.X, 1));
			Assert.IsTrue(Math.Abs(ptOut.Y) <= Geometry.Tolerance);
			Assert.IsFalse(Geometry.FFindCircumcenter(pt1, pt2, pt4, out ptOut));
			Assert.IsFalse(Geometry.FFindCircumcenter(pt2, pt1, pt1, out ptOut));
			Assert.IsFalse(Geometry.FFindCircumcenter(pt1, pt2, pt1, out ptOut));
			Assert.IsFalse(Geometry.FFindCircumcenter(pt1, pt1, pt2, out ptOut));
			Assert.IsFalse(Geometry.FFindCircumcenter(pt1, pt1, pt1, out ptOut));
		}

		[Test]
		public void TestParabolicCut()
		{
			var pt1 = new PT(0, 0);
			var pt2 = new PT(1, 1);
			Assert.IsTrue(Geometry.FCloseEnough(Geometry.ParabolicCut(pt1, pt2, -1), -3));
			Assert.IsTrue(Geometry.FCloseEnough(Geometry.ParabolicCut(pt2, pt1, -1), 1));

			pt1 = new PT(0, 0);
			pt2 = new PT(8, 4);
			Assert.IsTrue(Geometry.FCloseEnough(Geometry.ParabolicCut(pt1, pt2, -1), -7));
			Assert.IsTrue(Geometry.FCloseEnough(Geometry.ParabolicCut(pt2, pt1, -1), 3));
		}
	}
#endif
	#endregion
}

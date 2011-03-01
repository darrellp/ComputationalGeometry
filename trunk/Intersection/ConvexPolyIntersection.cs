using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Static class to hold the convex poly intersection routine. </summary>
	///
	/// <remarks>	Darrellp, 2/23/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	static public class ConvexPolyIntersection
	{
		private enum InflagVals
		{
			AInterior,
			BInterior,
			Unknown
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Finds the intersection of two convex polygons. </summary>
		///
		/// <remarks>	<para>No check is made for convexity.  The enumerables must yield the points in counterclockwise
		/// order.</para>
		/// 
		/// <para>This code works by looking for intersections between the two polygons.  If there is no intersection
		/// then no points will be returned even if one is wholly contains within the other.  Putting a check in for
		/// this case is often unnecessary and so we leave it out here and plan on incorporating it in a separate
		/// method at a later date.</para>
		/// 
		/// <para>This is based on the code in "Computational Geometry in C" by Joseph O'Rourke.</para>
		/// 
		/// Darrellp, 2/23/2011. </remarks>
		///
		/// <param name="poly1Enum">	The polygon 1 enum. </param>
		/// <param name="poly2Enum">	The polygon 2 enum. </param>
		///
		/// <returns>	An enumeration of the points in the intersection. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static IEnumerable<PointD> FindIntersection(IEnumerable<PointD> poly1Enum, IEnumerable<PointD> poly2Enum)
		{
			// If poly1 is empty, return poly2
			if (!poly1Enum.Any())
			{
				foreach (var poly in poly2Enum)
				{
					yield return poly;
				}
				yield break;
			}

			// If poly2 is empty, return poly1
			if (!poly2Enum.Any())
			{
				foreach (var poly in poly1Enum)
				{
					yield return poly;
				}
				yield break;
			}

			// Initialize

			// Put the two polygons into arrays
			var polyA = poly1Enum.ToArray();
			var polyB = poly2Enum.ToArray();

			// Index of the heads that chase each other around the polygon
			var aCur = 0;
			var bCur = 0;
			var origin = new PointD(0, 0);

			// Tells whether A or B is currently on the "inside".
			var inflag = InflagVals.Unknown;

			// Total number of times we've advanced each of the heads
			var cAdvancesA = 0;
			var cAdvancesB = 0;

			// True after we've found the first point
			var fFoundFirstPoint = false;

			// Counts of vertices in the two polygons
			var cPolyAVertices = polyA.Length;
			var cPolyBVertices = polyB.Length;

			// Last point we output so we don't repeat points
			var ptPrevOutput = new PointD();
			var ptFirstOutput = new PointD();

			// Step through the edges
			do
			{
				// Compute key variables

				// Tail of our current sides
				var aPrev = (aCur + cPolyAVertices - 1)%cPolyAVertices;
				var bPrev = (bCur + cPolyBVertices - 1)%cPolyBVertices;

				// Direction of each side
				var vecA = polyA[aCur] - polyA[aPrev];
				var vecB = polyB[bCur] - polyB[bPrev];

				// cross > 0 means a counterclockwise turn from vector A to vector B
				var cross = Math.Sign(Geometry.SignedArea(origin, vecA, vecB));

				// Whether the left half plane of each vector contains the head of the other vector
				var bHalfPlaneContainsA = Math.Sign(Geometry.SignedArea(polyB[bPrev], polyB[bCur], polyA[aCur]));
				var aHalfPlaneContainsB = Math.Sign(Geometry.SignedArea(polyA[aPrev], polyA[aCur], polyB[bCur]));

				// if A & B intersect
				PointD ptCrossing;
				var code = Geometry.SegSegInt(polyA[aPrev], polyA[aCur], polyB[bPrev], polyB[bCur], out ptCrossing);
				if (code == Geometry.CrossingType.Normal || code == Geometry.CrossingType.Vertex)
				{
					// If this is the first intersection we've seen
					if (inflag == InflagVals.Unknown && !fFoundFirstPoint)
					{
						// Set First Point Found
						fFoundFirstPoint = true;
						ptFirstOutput = ptCrossing;
						cAdvancesA = cAdvancesB = 0;
					}
					// Else if we've looped back on ourselves
					else if (ptCrossing.Equals(ptFirstOutput))
					{
						yield break;
					}

					// update the inflag
					inflag = InOut(inflag, bHalfPlaneContainsA, aHalfPlaneContainsB);
					if (!ptCrossing.Equals(ptPrevOutput))
					{
						yield return ptCrossing;
						ptPrevOutput = ptCrossing;
					}
				}

				// If A and B overlap and are oppositely oriented
				//
				// This means that one edge of the polys meets the edge of the other with the polygons lying on
				// opposite sides so that this overlap is the entirety of the overlap for the polygons.  We've
				// already returned the points so nothing to do here but quit out.
				if (code == Geometry.CrossingType.Edge && Geometry.Dot(vecA, vecB) < 0)
				{
					yield break;
				}
				// else if A and B are parallel and separated
				//
				// The union of the two polygons is empty
				if (cross == 0 && bHalfPlaneContainsA < 0 && aHalfPlaneContainsB < 0)
				{
					// Handle it
					yield break;
				}
				// else if A and B are collinear
				if (cross == 0 && bHalfPlaneContainsA == 0 && aHalfPlaneContainsB == 0)
				{
					// Advance, but don't output point
					if (inflag == InflagVals.AInterior)
					{
						bCur = Advance(bCur, ref cAdvancesB, cPolyBVertices);
					}
					else
					{
						aCur = Advance(aCur, ref cAdvancesA, cPolyAVertices);
					}
				}
				// else if A to B is a CCW turn
				else if (cross >= 0)
				{
					// Is B's head to A's left?
					if (aHalfPlaneContainsB > 0)
					{
						// Is A interior to B?
						if (inflag == InflagVals.AInterior && !polyA[aCur].Equals(ptPrevOutput) && !polyA[aCur].Equals(ptFirstOutput))
						{
							// Yeild A's head
							yield return polyA[aCur];
							ptPrevOutput = polyA[aCur];
						}
						
						// Advance A
						aCur = Advance(aCur, ref cAdvancesA, cPolyAVertices);
					}
					else
					{
						// Is B interior to A?
						if (inflag == InflagVals.BInterior && !polyB[bCur].Equals(ptPrevOutput) && !polyB[bCur].Equals(ptFirstOutput))
						{
							// Yeild B's head
							yield return polyB[bCur];
							ptPrevOutput = polyB[bCur];
						}

						// Advance B
						bCur = Advance(bCur, ref cAdvancesB, cPolyBVertices);
					}
				}
				else
				{
					// Is A's head to B's right?
					if (bHalfPlaneContainsA < 0)
					{
						// Is A interior to B?
						if (inflag == InflagVals.AInterior && !polyA[aCur].Equals(ptPrevOutput) && !polyA[aCur].Equals(ptFirstOutput))
						{
							// Yeild A's head
							yield return polyA[aCur];
							ptPrevOutput = polyA[aCur];
						}

						// Advance A
						aCur = Advance(aCur, ref cAdvancesA, cPolyAVertices);
					}
					else
					{
						// Is B interior to A?
						if (inflag == InflagVals.BInterior && !polyB[bCur].Equals(ptPrevOutput) && !polyB[bCur].Equals(ptFirstOutput))
						{
							// Yeild B's head
							yield return polyB[bCur];
							ptPrevOutput = polyB[bCur];
						}

						// Advance B
						bCur = Advance(bCur, ref cAdvancesB, cPolyBVertices);
					}
				}
			}
			// both indices have cycled or one has cycled twice
			while (
				(cAdvancesA < cPolyAVertices || cAdvancesB < cPolyBVertices) &&
				cAdvancesA < 2*cPolyAVertices && cAdvancesB < 2*cPolyBVertices);

			// If Inflags is unknown then we never intersected and may have one poly wholly contained in the other.
			if (inflag == InflagVals.Unknown)
			{
				// If a point of A is in B
				if (Geometry.PointInConvexPoly(polyA[0], polyB))
				{
					// Yield all of A
					foreach (var pt in polyA)
					{
						yield return pt;
					}
				}
				// else if a point of B is in A
				else if (Geometry.PointInConvexPoly(polyB[0], polyA))
				{
					// Yield all of B
					foreach (var pt in polyB)
					{
						yield return pt;
					}
				}

			}
		}

		private static int Advance(int iHead, ref int cAdvances, int cPolyVertices)
		{
			cAdvances++;
			return (iHead + 1)%cPolyVertices;
		}

		private static InflagVals InOut(InflagVals inflag, int aHalfPlaneContainsB, int bHalfPlaneContainsA)
		{
			if (aHalfPlaneContainsB > 0)
			{
				return InflagVals.AInterior;
			}
			if (bHalfPlaneContainsA > 0)
			{
				return InflagVals.BInterior;
			}
			return inflag;
		}
	}
	
	#region NUNIT
	#if NUNIT || DEBUG

	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Test convex polygon intersection. </summary>
	///
	/// <remarks>	Darrellp, 2/21/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	[TestFixture]
	public class TestConvexIntersect
	{
		private static void Check(List<PointD> poly1, List<PointD> poly2, List<PointD> res)
		{
			var output = ConvexPolyIntersection.FindIntersection(poly1, poly2);
			foreach (var pt in output)
			{
				Assert.IsTrue(res.Contains(pt));
			}
			Assert.AreEqual(output.Count(), res.Count);
		}

		[Test]
		public void TestIntersection()
		{
			var poly1 = new List<PointD>()
				                    {
				                     	new PointD(0, 0),
				                     	new PointD(2, 0),
				                     	new PointD(2, 3),
				                     	new PointD(0, 3)
				                    };
			var poly2 = new List<PointD>()
				                    {
				                     	new PointD(1, 1),
										new PointD(2, 1),
										new PointD(2, 2),
										new PointD(1, 2)
				                    };
			var res = new List<PointD>()
				                {
				                   	new PointD(1, 1),
				                   	new PointD(2, 1),
				                   	new PointD(2, 2),
				                   	new PointD(1, 2)
				                };
			Check(poly1, poly2, res);
			poly1 = new List<PointD>()
				        {
				        	new PointD(107,176),
				        	new PointD(128,370),
				        	new PointD(47,379),
				        	new PointD(26,185),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(-1,257),
				        	new PointD(73,257),
				        	new PointD(73,313),
				        	new PointD(-1,313),
				        };
			var output = ConvexPolyIntersection.FindIntersection(poly1, poly2).ToList();
			Assert.AreEqual(4, output.Count);
			poly1 = new List<PointD>()
				        {
				        	new PointD(1, 0),
				        	new PointD(3, 0),
				        	new PointD(3, 2),
				        	new PointD(1, 2),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(0, 1),
				        	new PointD(1, 0),
				        	new PointD(2, 0),
				        	new PointD(2, 1),
				        };
			res = new List<PointD>()
				        {
				        	new PointD(1, 0),
				        	new PointD(2, 0),
				        	new PointD(2, 1),
				        	new PointD(1, 1),
				        };
			Check(poly1, poly2, res);
			poly1 = new List<PointD>()
				        {
				        	new PointD(0, 0),
				        	new PointD(509, 0),
				        	new PointD(509, 312),
				        	new PointD(0, 312),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(53, 213),
				        	new PointD(110, 240),
				        	new PointD(89, 312),
				        	new PointD(0, 312),
				        	new PointD(0, 233),
				        };
			res = new List<PointD>()
				        {
				        	new PointD(53, 213),
				        	new PointD(110, 240),
				        	new PointD(89, 312),
				        	new PointD(0, 312),
				        	new PointD(0, 233),
				        };
			Check(poly1, poly2, res);

			poly1 = new List<PointD>()
				        {
				        	new PointD(0, 0),
				        	new PointD(4, 0),
				        	new PointD(4, 4),
				        	new PointD(0, 4),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(2, -1),
				        	new PointD(5, 2),
				        	new PointD(2, 5),
				        	new PointD(-1, 2),
				        };
			res = new List<PointD>()
				        {
				        	new PointD(1, 0),
				        	new PointD(3, 0),
				        	new PointD(4, 1),
				        	new PointD(4, 3),
				        	new PointD(3, 4),
				        	new PointD(1, 4),
				        	new PointD(0, 3),
				        	new PointD(0, 1),
				        };
			Check(poly1, poly2, res);

			poly1 = new List<PointD>()
				        {
				        	new PointD(0, 0),
				        	new PointD(3, 0),
				        	new PointD(3, 3),
				        	new PointD(0, 3),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(3, 1),
				        	new PointD(4, 1),
				        	new PointD(4, 2),
				        	new PointD(3, 2),
				        };
			res = new List<PointD>()
				    {
				      	new PointD(3, 1),
				      	new PointD(3, 2),
				    };
			Check(poly1, poly2, res);
			poly1 = new List<PointD>()
				        {
				        	new PointD(0, 0),
				        	new PointD(2, 0),
				        	new PointD(2, 5),
				        	new PointD(0, 5),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(4, 1),
				        	new PointD(1, 4),
				        	new PointD(1, 2),
				        	new PointD(3, 0),
				        };
			res = new List<PointD>()
				    {
				      	new PointD(2, 1),
				      	new PointD(2, 3),
				      	new PointD(1, 4),
				      	new PointD(1, 2),
				    };
			Check(poly1, poly2, res);
			poly1 = new List<PointD>()
				        {
				        	new PointD(0, 0),
				        	new PointD(3, 0),
				        	new PointD(3, 3),
				        	new PointD(0, 3),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(1, 1),
				        	new PointD(1, 2),
				        	new PointD(2, 2),
				        	new PointD(2, 1),
				        };
			res = new List<PointD>()
				    {
				        new PointD(1, 1),
				        new PointD(1, 2),
				        new PointD(2, 2),
				        new PointD(2, 1),
				    };
			Check(poly1, poly2, res);
			poly1 = new List<PointD>()
				        {
				        	new PointD(241, 1090),
				        	new PointD(206, 278),
				        	new PointD(290, 242),
				        };
			poly2 = new List<PointD>()
				        {
				        	new PointD(0, 0),
				        	new PointD(509, 0),
				        	new PointD(509, 312),
				        	new PointD(0, 312),
				        };
			output = ConvexPolyIntersection.FindIntersection(poly1, poly2).ToList();
			Assert.AreEqual(4, output.Count);
		}
	}
	#endif
	#endregion
}

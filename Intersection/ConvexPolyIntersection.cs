using System;
using System.Collections.Generic;
using System.Linq;
#if DOUBLEPRECISION
using NUnit.Framework;
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

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
		/// order.  That is opposite to the way they're returned from the code in fortune so put in a Reverse on
		/// those enumerables.</para>
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
			// Initialize
			var polyA = poly1Enum.ToArray();
			var polyB = poly2Enum.ToArray();
			var aCur = 0;
			var bCur = 0;
			var origin = new PointD(0, 0);
			var inflag = InflagVals.Unknown;
			var cAdvancesA = 0;
			var cAdvancesB = 0;
			var fFoundFirstPoint = true;
			var cPolyAVertices = polyA.Length;
			var cPolyBVertices = polyB.Length;
			var ptPrevOutput = new PointD();

			// Step through the edges
			do
			{
				// Compute key variables
				var aPrev = (aCur + cPolyAVertices - 1)%cPolyAVertices;
				var bPrev = (bCur + cPolyBVertices - 1)%cPolyBVertices;
				var vecA = polyA[aCur] - polyA[aPrev];
				var vecB = polyB[bCur] - polyB[bPrev];
				var cross = Math.Sign(Geometry.SignedArea(origin, vecA, vecB));
				var bHalfPlaneContainsA = Math.Sign(Geometry.SignedArea(polyB[bPrev], polyB[bCur], polyA[aCur]));
				var aHalfPlaneContainsB = Math.Sign(Geometry.SignedArea(polyA[aPrev], polyA[aCur], polyB[bCur]));

				// if A & B intersect
				PT ptCrossing;
				var code = Geometry.SegSegInt(polyA[aPrev], polyA[aCur], polyB[bPrev], polyB[bCur], out ptCrossing);
				if (code == Geometry.CrossingType.Normal || code == Geometry.CrossingType.Vertex)
				{
					// Initialize the first time through
					if (inflag == InflagVals.Unknown && fFoundFirstPoint)
					{
						cAdvancesA = cAdvancesB = 0;
						fFoundFirstPoint = false;
						PT ptFirstCrossing = ptCrossing;
						yield return ptFirstCrossing;
						ptPrevOutput = ptFirstCrossing;
					}

					// update the inflag
					inflag = InOut(inflag, bHalfPlaneContainsA, aHalfPlaneContainsB);
					yield return ptCrossing;
				}

				// Advance one of the indices

				// If A and B overlap and are oppositely oriented
				//
				// This means that one edge of the polys meets the edge of the other with the polygons lying on
				// opposite sides so that this overlap is the entirety of the overlap for the entire polygons
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
					// else if cross > 0
				else if (cross >= 0)
				{
					if (aHalfPlaneContainsB > 0)
					{
						aCur = Advance(aCur, ref cAdvancesA, cPolyAVertices);
						if (inflag == InflagVals.AInterior && !polyA[aCur].Equals(ptPrevOutput))
						{
							yield return polyA[aCur];
							ptPrevOutput = polyA[aCur];
						}
					}
					else
					{
						bCur = Advance(bCur, ref cAdvancesB, cPolyBVertices);
						if (inflag == InflagVals.BInterior && !polyB[bCur].Equals(ptPrevOutput))
						{
							yield return polyB[bCur];
							ptPrevOutput = polyB[bCur];
						}
					}
				}
					// else if cross < 0
				else
				{
					if (bHalfPlaneContainsA < 0)
					{
						aCur = Advance(aCur, ref cAdvancesA, cPolyAVertices);
						if (inflag == InflagVals.AInterior && !polyA[aCur].Equals(ptPrevOutput))
						{
							yield return polyA[aCur];
							ptPrevOutput = polyA[aCur];
						}
					}
					else
					{
						bCur = Advance(bCur, ref cAdvancesB, cPolyBVertices);
						if (inflag == InflagVals.BInterior && !polyB[bCur].Equals(ptPrevOutput))
						{
							yield return polyB[bCur];
							ptPrevOutput = polyB[bCur];
						}
					}
				}
			}
			// both indices have cycled or one has cycled twice
			while (
				(cAdvancesA < cPolyAVertices || cAdvancesB < cPolyAVertices) &&
				(cAdvancesA < 2*cPolyAVertices || cAdvancesB < 2*cPolyBVertices));

			// TODO: If Inflags is unknown then we never intersected and may have one poly wholly contained in the other.
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
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Test convex polygon intersection. </summary>
	///
	/// <remarks>	Darrellp, 2/21/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	[TestFixture]
	public class TestConvexIntersect
	{
		private static void check(List<PT> poly1, List<PT> poly2, List<PT> res)
		{
			var output = ConvexPolyIntersection.FindIntersection(poly1, poly2);
			foreach (var pt in output)
			{
				Assert.IsTrue(res.Contains(pt));
			}
		}

		[Test]
		public void TestGeneratorAdds()
		{
			var poly1 = new List<PT>()
				                    {
				                     	new PT(0, 0),
				                     	new PT(2, 0),
				                     	new PT(2, 3),
				                     	new PT(0, 3)
				                    };
			var poly2 = new List<PT>()
				                    {
				                     	new PT(1, 1),
										new PT(2, 1),
										new PT(2, 2),
										new PT(1, 2)
				                    };
			var res = new List<PT>()
				                {
				                   	new PT(1, 1),
				                   	new PT(2, 1),
				                   	new PT(2, 2),
				                   	new PT(1, 2)
				                };
			check(poly1, poly2, res);

			poly1 = new List<PT>()
				        {
				        	new PT(0, 0),
				        	new PT(4, 0),
				        	new PT(4, 4),
				        	new PT(0, 4),
				        };
			poly2 = new List<PT>()
				        {
				        	new PT(2, -1),
				        	new PT(5, 2),
				        	new PT(2, 5),
				        	new PT(-1, 2),
				        };
			res = new List<PT>()
				        {
				        	new PT(1, 0),
				        	new PT(3, 0),
				        	new PT(4, 1),
				        	new PT(4, 3),
				        	new PT(3, 4),
				        	new PT(1, 4),
				        	new PT(0, 3),
				        	new PT(0, 1),
				        };
			check(poly1, poly2, res);

			poly1 = new List<PT>()
				        {
				        	new PT(0, 0),
				        	new PT(3, 0),
				        	new PT(3, 3),
				        	new PT(0, 3),
				        };
			poly2 = new List<PT>()
				        {
				        	new PT(3, 1),
				        	new PT(4, 1),
				        	new PT(4, 2),
				        	new PT(3, 2),
				        };
			res = new List<PT>()
				    {
				      	new PT(3, 1),
				      	new PT(3, 2),
				    };
			check(poly1, poly2, res);
		}
	}
	#endregion
}

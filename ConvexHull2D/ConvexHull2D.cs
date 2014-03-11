using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DAP.CompGeom
{
	internal static class ConvexHull2D
	{
		#region Graham Scan

		// ReSharper disable once ReturnTypeCanBeEnumerable.Global
		public static List<int> GrahamScan(IEnumerable<PointD> points)
		{
			// Ultimately we care about the indices so set up tuples with the index as first value
			// and the point itself as second value.
			var indexedPoints = points.Select((p, i) => new Tuple<int, PointD>(i, p)).ToList();
			var indices = new List<int>();

			// Get our lower right which everything pivots on
			var iptPivot = LowerRight(indexedPoints);
			var ptPivot = iptPivot.Item2;
			var iPivot = iptPivot.Item1;

			// Lower right is the first point in our convex hull
			indices.Add(iPivot);

			// Sort the rest of the points based on their angle around the pivot point
			var sortedPoints = indexedPoints.Where(ipt => ipt.Item1 != iPivot).ToList();
			sortedPoints.Sort((ipt1, ipt2) =>
			{
				var dpt1 = ipt1.Item2 - ptPivot;
				var dpt2 = ipt2.Item2 - ptPivot;
				// ReSharper disable CompareOfFloatsByEqualityOperator
				var cmp1 = dpt1.Y == 0 ? double.MaxValue * Math.Sign(dpt1.X) : dpt1.X / dpt1.Y;
				var cmp2 = dpt2.Y == 0 ? double.MaxValue * Math.Sign(dpt2.X) : dpt2.X / dpt2.Y;
				// ReSharper restore CompareOfFloatsByEqualityOperator
				return cmp1.CompareTo(cmp2);
			});

			indices.Add(sortedPoints[0].Item1);
			for (var iPt = 1; iPt < sortedPoints.Count - 1; iPt++)
			{
				var iptCur = sortedPoints[iPt];
				var ptOnHull = indexedPoints[indices[indices.Count - 1]].Item2;
				var ptTest = indexedPoints[iptCur.Item1].Item2;
				var ptNext = indexedPoints[sortedPoints[iPt + 1].Item1].Item2;

				if (Geometry.ICcw(ptOnHull, ptTest, ptNext) == -1)
				{
					indices.Add(iptCur.Item1);
				}
			}
			indices.Add(sortedPoints[sortedPoints.Count - 1].Item1);
			return indices;
		}

		private static Tuple<int, PointD> LowerRight(IEnumerable<Tuple<int, PointD>> points)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return points.Aggregate(new Tuple<int, PointD>(0, new PointD(double.MinValue, double.MaxValue)),
				(ip1, ip2) =>
				{
					var p1 = ip1.Item2;
					var p2 = ip2.Item2;
					return p1.Y == p2.Y ? (p1.X < p2.X ? ip2 : ip1) : (p1.Y < p2.Y ? ip1 : ip2);
				});
		}
		#endregion
	}

	#region NUnit
	#if NUNIT || DEBUG

	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Test Graham Scan. </summary>
	///
	/// <remarks>	Darrellp, 3/11/2014. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	[TestFixture]
	public class TestGrahamHull
	{
// ReSharper disable CSharpWarnings::CS1591
// ReSharper disable once UnusedParameter.Local
		private static void Check(IEnumerable<PointD> points, List<int> expected)
		{
			var output = ConvexHull2D.GrahamScan(points);
			Assert.IsTrue(output.Zip(expected, (i1, i2) => i1 == i2).All(t => t));
		}

		[Test]
		public void TestIntersection()
		{
// ReSharper disable JoinDeclarationAndInitializer
			List<PointD> points;
			List<int> expected;
// ReSharper restore JoinDeclarationAndInitializer

			points = new List<PointD>
			{
				new PointD(1, 1),
				new PointD(0, 2),
				new PointD(3, 1),
				new PointD(1, 2),
				new PointD(2, 1),
				new PointD(0, 0),
				new PointD(1, 0),
				new PointD(1, -1),
				new PointD(3, 0),
			};
			expected = new List<int> { 7, 5, 1, 3, 2, 8 };
			Check(points, expected);

			points = new List<PointD>
			{
				new PointD(0, 0),
				new PointD(1, 1),
				new PointD(0, 2),
				new PointD(2, 2),
				new PointD(2, 0)
			};
			expected = new List<int> {4, 0, 2, 3};
			Check(points, expected);
		}
// ReSharper restore CSharpWarnings::CS1591
	}
	#endif
	#endregion
}

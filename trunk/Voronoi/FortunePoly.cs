using System.Collections.Generic;
using NetTrace;
#if NUNIT || DEBUG
using NUnit.Framework;
#endif
#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Fortune polygon. </summary>
	///
	/// <remarks>
	/// <para>I should point out that classic winged edge polygons don't keep a list of edges on each
	/// polygon.  They are enumerable through the Edges property.  Only a single arbitrary "starting"
	/// edge is kept in the polygon structure.  In general, that is the way winged edges work.  In
	/// the fortune case, we kind of come at the edges in an almost random manner so we keep them in
	/// a list.  The Edges enumeration should still work, though it's slower and unnecessary since
	/// you can just retrieve the list through FortunePoly.FortuneEdges.  We could end up with lower
	/// performance and less space by nulling out the array in the fortune polygons at the end of
	/// processing and relying on the Edges enumeration but we'd still need to keep the arrays around
	/// while we're actually creating the structure so I don't know how much real space we'd
	/// save.</para>
	/// 
	/// Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class FortunePoly : WePolygon
	{
		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets a list of edges in Clockwise order. </summary>
		///
		/// <value>	The edges in Clockwise order. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public List<FortuneEdge> FortuneEdges { get; protected set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Adds an edge to the Fortune polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/22/2011. </remarks>
		///
		/// <param name="edge">	The edge to be added. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void AddEdge(FortuneEdge edge)
		{
			FortuneEdges.Add(edge);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the number of vertices. </summary>
		///
		/// <value>	The number of vertices. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public int VertexCount
		{
			get
			{
				return FortuneEdges.Count;
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Indicates that this is the singleton polygon at infinity </summary>
		///
		/// <value>	true if at it's the polygon at infinity. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FAtInfinity { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Tells whether we detected a zero length edge during the fortune processing </summary>
		///
		/// <value>	true if zero length edge is present </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool FZeroLengthEdge { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	The original point which caused this voronoi cell to exist. </summary>
		///
		/// <value>	The point in the original set of data points. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PT VoronoiPoint { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	A generic index to identify this polygon for debugging purposes.. </summary>
		///
		/// <value>	The index. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		internal int Index { get; set; }

		#endregion

		#region Queries

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Return real vertices.  Vertices at infinity will be converted based on the passed ray length. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <param name="rayLength">	Length of the ray. </param>
		///
		/// <returns>	An enumerable of real points representing the polygon. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<PointD> RealVertices(double rayLength)
		{
			// For every edge in the polygon
			foreach (var oe in OrientedEdges)
			{
				// Skip if it's the edge at infinity - we'll pick it up on one of the infinite edges
				if (oe.Edge.FAtInfinity)
				{
					continue;
				}

				// Get start and end vertices
				var vtxStart = oe.Forward ? oe.Edge.VtxStart : oe.Edge.VtxEnd;
				var vtxEnd = oe.Forward ? oe.Edge.VtxEnd : oe.Edge.VtxStart;

				// If the start vtx is at infinity
				if (vtxStart.FAtInfinity)
				{
					// Then return it to count for the edge at infinity
					yield return vtxStart.ConvertToReal(vtxEnd.Pt, rayLength);
				}
				
				// If the end vertex is at infinity
				if (vtxEnd.FAtInfinity)
				{
					// convert it to a real point and return it
					yield return vtxEnd.ConvertToReal(vtxStart.Pt, rayLength);
				}
				else
				{
					// return it normally
					yield return vtxEnd.Pt;
				}
			}
		}
		#endregion

		#region Constructor

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="pt">		The point which creates this polygon. </param>
		/// <param name="index">	The index to identiry this polygon. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal FortunePoly(PT pt, int index)
		{
			FZeroLengthEdge = false;
			FAtInfinity = false;
			Index = index;
			VoronoiPoint = pt;
			FortuneEdges = new List<FortuneEdge>();
		}
		#endregion

		#region Edge operations

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Sort the edges in Clockwise order. </summary>
		///
		/// <remarks>	
		/// We do this partially by knowing that all the polygons in a Voronoi diagram are convex.  That
		/// means we can sort edges by measuring their angle around the generator for the polygon.  We
		/// have to pick the point to measure this angle carefully which is what
		/// WEEdge.PolyOrderingTestPoint() does.  We also have to make a special case for the rare doubly
		/// infinite lines (such as that created with only two generators).
		/// 
		/// Darrellp, 2/18/2011. 
		/// </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void SortEdges()
		{
			// If there are only two edges
			if (FortuneEdges.Count == 2)
			{
				// If they are split
				if (FortuneEdges[0].FSplit)
				{
					// If they need reordering
					//
					// I don't think this is really necessary.  It makes sense to order more than 2
					// edges because they really have an "order" which will be maintained no matter which
					// polygon they're ordered by (though the starting edge may change).  With two oppositely
					// directed rays as in this case, there is no such "order" independent of the polygon that
					// does the ordering.  I'm leaving it in but I don't think it's necessary.
					// TODO: Check on this!
					if (Geometry.ICcw(
						FortuneEdges[0].PolyOrderingTestPoint,
						FortuneEdges[1].PolyOrderingTestPoint,
						VoronoiPoint) < 0)
					{
						// Reorder them
						var edgeT = FortuneEdges[0];
						FortuneEdges[0] = FortuneEdges[1];
						FortuneEdges[1] = edgeT;
					}
				}
				else
				{
					// I think this represents an infinite polygon with only two edges.

					// If not ordered around the single base point properly
					if (Geometry.ICcw(FortuneEdges[0].VtxStart.Pt,
					                  FortuneEdges[0].PolyOrderingTestPoint,
					                  FortuneEdges[1].PolyOrderingTestPoint) > 0)
					{
						// Swap the edges
						var edgeT = FortuneEdges[0];
						FortuneEdges[0] = FortuneEdges[1];
						FortuneEdges[1] = edgeT;
					}
				}
			}
			else
			{
				// More than 3 vertices just get a standard CLR sort
				FortuneEdges.Sort();
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Remove an edge. </summary>
		///
		/// <remarks>	This really only makes much sense for zero length edges. </remarks>
		///
		/// <param name="edge">	Edge to remove. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void DetachEdge(FortuneEdge edge)
		{
			// Remove the zero length edge
			//
			// We do this by removing it's end vertex, reassigning the proper
			// vertex in each of the edges which formerly connected to that vertex and splicing those edges into the
			// the proper spot for the edge list of our start vertex and finally removing it from the edge list of
			// both polygons which it adjoins.
			edge.ReassignVertexEdges();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Sort out zero length edge issues. </summary>
		///
		/// <remarks>	
		/// Zero length edges are a pain and have to be dealt with specially since they don't sort
		/// properly using normal geometrical position nor can "sidedness" be determined solely from
		/// their geometry (a zero length line has no "sides").  Instead, we have to look at the non-zero
		/// length edges around them and determine this information by extrapolating from those edges
		/// topological connection to this edge. 
		/// </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void HandleZeroLengthEdges()
		{
			// If no zero length edges
			if (!FZeroLengthEdge)
			{
				// We're outta here
				return;
			}

			// For every edge in the polygon
			for (var i = 0; i < VertexCount; i++)
			{
				// Retrieve the edge
				var edgeCheck = FortuneEdges[i];

				// If it's zero length
				if (edgeCheck.FZeroLength())
				{
					// Diagnostics
					Tracer.Trace(tv.ZeroLengthEdges, "Fixing zero length edge {0} for poly {1}", edgeCheck, this);

					// Remove the edge from both this polygon and the polygon "across" the zero length edge
					DetachEdge(edgeCheck);
					FortuneEdges.Remove(edgeCheck);
					edgeCheck.OtherPoly(this).FortuneEdges.Remove(edgeCheck);

					// We have to back up one because we deleted edge i
					i--;
				}
			}
		}
		#endregion

		#region NUnit
#if NUNIT || DEBUG
		[TestFixture]
		public class TestFortunePoly
		{
			[Test]
			public void TestEdgeSort()
			{
				//var poly1 = new FortunePoly(new PT(0, 0), 0);
				//var poly2 = new FortunePoly(new PT(0, 2), 1);
				//var poly3 = new FortunePoly(new PT(2, 0), 2);
				//var poly4 = new FortunePoly(new PT(0, -2), 3);
				//var poly5 = new FortunePoly(new PT(-2, 0), 4);
				//var vtx1 = new FortuneVertex(new PT(1, 1));
				//var vtx2 = new FortuneVertex(new PT(1, -1));
				//var vtx3 = new FortuneVertex(new PT(-1, -1));
				//var vtx4 = new FortuneVertex(new PT(-1, 1));
				//FortuneVertex vtx5;
				//var edge1 = new FortuneEdge();
				//var edge2 = new FortuneEdge();
				//var edge3 = new FortuneEdge();
				//var edge4 = new FortuneEdge();
				//edge2.SetPolys(poly1, poly2);
				//edge3.SetPolys(poly1, poly3);
				//edge4.SetPolys(poly1, poly4);
				//edge1.SetPolys(poly1, poly5);
				//edge1.VtxStart = vtx4;
				//edge1.VtxEnd = vtx1;
				//edge2.VtxStart = vtx1;
				//edge2.VtxEnd = vtx2;
				//edge3.VtxStart = vtx2;
				//edge3.VtxEnd = vtx3;
				//edge4.VtxStart = vtx3;
				//edge4.VtxEnd = vtx4;

				//var polyTest = new FortunePoly(new PT(0, 0), 0);
				//polyTest.AddEdge(edge1);
				//polyTest.AddEdge(edge3);
				//polyTest.AddEdge(edge2);
				//polyTest.AddEdge(edge4);

				//polyTest.SortEdges();

				//vtx1.Pt = new PT(3, 4);
				//vtx2.Pt = new PT(4, 3);
				//vtx3.Pt = new PT(-1, -2);
				//vtx4.Pt = new PT(-2, -1);
				//polyTest.EdgesCW.Clear();

				//polyTest.AddEdge(edge4);
				//polyTest.AddEdge(edge3);
				//polyTest.AddEdge(edge1);
				//polyTest.AddEdge(edge2);

				//polyTest.SortEdges();
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));

				//poly1.VoronoiPoint = new PT(10, 10);
				//vtx1.Pt = new PT(13, 14);
				//vtx2.Pt = new PT(14, 13);
				//vtx3.Pt = new PT(9, 8);
				//vtx4.Pt = new PT(8, 9);
				//polyTest.EdgesCW.Clear();

				//polyTest.AddEdge(edge1);
				//polyTest.AddEdge(edge3);
				//polyTest.AddEdge(edge2);
				//polyTest.AddEdge(edge4);

				//polyTest.SortEdges();
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));

				//poly1.VoronoiPoint = new PT(0, 0);
				//vtx1 = FortuneVertex.InfiniteVertex(new PT(1, 2), true);
				//vtx2.Pt = new PT(8, -1);
				//vtx3.Pt = new PT(0, -3);
				//vtx4.Pt = new PT(-8, -1);
				//vtx5 = FortuneVertex.InfiniteVertex(new PT(-1, 2), true);
				//edge1.VtxStart = vtx2;
				//edge1.VtxEnd = vtx1;
				//edge2.VtxStart = vtx2;
				//edge2.VtxEnd = vtx3;
				//edge3.VtxStart = vtx3;
				//edge3.VtxEnd = vtx4;
				//edge4.VtxStart = vtx4;
				//edge4.VtxEnd = vtx5;
				//polyTest.EdgesCW.Clear();

				//polyTest.AddEdge(edge3);
				//polyTest.AddEdge(edge1);
				//polyTest.AddEdge(edge2);
				//polyTest.AddEdge(edge4);

				//polyTest.SortEdges();
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				//Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));
			}
		}
#endif
		#endregion
	}
}
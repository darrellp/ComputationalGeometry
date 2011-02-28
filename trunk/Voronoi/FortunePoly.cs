﻿using System;
using System.Collections.Generic;
using System.Linq;
using NetTrace;
#if NUNIT || DEBUG
using NUnit.Framework;
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
		/// <remarks>	
		/// Sadly, it was after creating and using this variable in numerous places that I found out that
		/// the "standard" order for keeping edges is in CCW order.  The enumerator at WePolygon was made
		/// with this convention in mind and thus returns the edges in CCW order so we have the confusion
		/// that one edge enumerator returns edges in CW order and one in CCW order.  Given that they're
		/// both used so heavily it would be tough to remove or modify either one.  I've made this one
		/// internal to avoid confusion externally. 
		/// </remarks>
		///
		/// <value>	The edges in Clockwise order. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal List<FortuneEdge> FortuneEdges { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Adds an edge to the Fortune polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/22/2011. </remarks>
		///
		/// <param name="edge">	The edge to be added. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void AddEdge(FortuneEdge edge)
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

		public PointD VoronoiPoint { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	A generic index to identify this polygon for debugging purposes.. </summary>
		///
		/// <value>	The index. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		internal int Index { get; set; }

		#endregion

		#region Queries

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Return real vertices where this voronoi cell intersects with a passed in bounding box. 
		/// </summary>
		///
		/// <remarks>	
		/// <para>I'm trying to handle all the exceptional cases.  There is one incredibly exceptional
		/// case which I'm ignoring.  That is the case where there are two vertices at infinity which are
		/// at such nearly opposite directions without being completely collinear that we can't push
		/// their points at infinity out far enough to encompass the rest of the box within the range of
		/// a double.  If this is important to you, then see the comments below, but it's hard to imagine
		/// it ever arising.</para>
		/// 
		/// <para>Editorial comment - The annoying thing about all of this is that, like in so much of
		/// computational geometry, the rarer and less significant the exceptional cases are, the more
		/// difficult they are to handle.  It's both a blessing and a curse - it means that the normal
		/// cases are generally faster, but it also makes it difficult to get excited about slogging
		/// through the tedious details of situations that will probably never arise in practice.  Still,
		/// in order to keep our noses clean, we press on regardless.</para>
		/// 
		/// Darrellp, 2/26/2011. 
		/// </remarks>
		///
		/// <param name="ptUL">	The upper left point of the box. </param>
		/// <param name="ptLR">	The lower right point of the box. </param>
		///
		/// <returns>	An enumerable of real points representing the voronoi cell clipped to the box. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<PointD> RealVertices(PointD ptUL, PointD ptLR)
		{
			// If no edges, then it's just the entire box
			if (!Edges.Any())
			{
				foreach (var pt in BoxPoints(ptUL, ptLR))
				{
					yield return pt;
				}
				yield break;
			}

			IEnumerable<PointD> ptsToBeClipped;
			var ptsBox = BoxPoints(ptUL, ptLR);

			var fFound = FCheckEasy(out ptsToBeClipped);
			if (!fFound)
			{
				fFound = FCheckParallelLines(ptsBox, out ptsToBeClipped);
			}
			if (!fFound)
			{
				fFound = FCheckDoublyInfinite(ptsBox, out ptsToBeClipped);
			}
			if (!fFound)
			{
				ptsToBeClipped = RealVertices(CalcRayLength(ptsBox));
			}

			foreach (var pt in ConvexPolyIntersection.FindIntersection(ptsToBeClipped, ptsBox))
			{
				yield return pt;
			}
			yield break;
		}

		private bool FCheckParallelLines(IEnumerable<PointD> ptsBox, out IEnumerable<PointD> ptsToBeClipped)
		{
			// Do the required initialization of our out parameter
			ptsToBeClipped = null;

			// See if our cell is made up of two parallel lines.
			// This is the only case where we will have exactly two lines at
			// infinity - one connecting each end of the parallel lines.
			// We will have exactly six edges - it will look like the following:
			//
			//      Inf vtx-------------finite vtx 0 -----------------Inf vtx
			//      |                                                       |
			//      |<-Edge at infinity                   Edge at infinity->|
			//      |                                                       |
			//      Inf vtx-------------finite vtx 1 ------------------Inf vtx
			//
			// So that's a total of six edges and two of them at infinity.
			if (Edges.Where(e => e.FAtInfinity).Count() == 2)
			{
				// Retrieve the two finite points
				var ptsFinite = Vertices.Where(v => !v.FAtInfinity).Select(v => v.Pt).ToArray();

				// Find out the max dist from any finite point to any finite point on the box and double it for good measure
				var maxDist0 = Math.Sqrt(ptsBox.Select(pt => Geometry.DistanceSq(pt, ptsFinite[0])).Max());
				var maxDist1 = Math.Sqrt(ptsBox.Select(pt => Geometry.DistanceSq(pt, ptsFinite[1])).Max());
				var maxDist = 2.0 * Math.Max(maxDist0, maxDist1);

				// Use that as a ray length to get real vertices which will later be clipped to the box
				ptsToBeClipped = RealVertices(maxDist);
				return true;
			}
			return false;
		}

		private bool FCheckDoublyInfinite(IEnumerable<PointD> ptsBox, out IEnumerable<PointD> ptsToBeClipped)
		{
			ptsToBeClipped = null;

			// This case will always have exactly three vertices - one finite and two infinite
			if (Vertices.Count() != 3)
			{
				return false;
			}

			// Get the finite vertex
			var vtx = Vertices.Where(v => !v.FAtInfinity).First();

			// If it's only got two edges emanating from it (a doubly infinite line)
			if (vtx.Edges.Count() == 2)
			{
				// Figure out a satisfactory distance for our ray length
				var maxDist = 2.0 * Math.Sqrt(ptsBox.Select(pt => Geometry.DistanceSq(pt, vtx.Pt)).Max());
				var arRealPts = RealVertices(maxDist).ToArray();

				// For every point in the box
				//
				// We have to include the points to the left of the line in our rectangle we want clipped
				// So find the max distance and extend a rect that length to the side of our line
				// segment.  We double the offset just to be safe.
				double maxLineDist = 2 * ptsBox.
					// Get the distance to our line
					Select(p => Geometry.PtToLineDistance(p, arRealPts[0], arRealPts[2])).
					// Keep only the ones to the left of the line
					Where(d => d > 0).
					// Find the max distance
					Max();

				// If there were no points to the left of our line
				if (maxLineDist == 0)
				{
					// return an empty array
					ptsToBeClipped = new List<PointD>();
				}
				else
				{
					// Return the rectangle formed from our line segment extended out by maxLineDist
					var vcOffset = (arRealPts[2] - arRealPts[0]).Normalize().Flip90Ccw()*maxLineDist;
					ptsToBeClipped = new List<PointD>
				                 		{
				                 			arRealPts[0],
				                 			arRealPts[2],
				                 			arRealPts[2] + vcOffset,
				                 			arRealPts[0] + vcOffset
				                 		};
				}
				return true;
			}
			return false;
		}

		private bool FCheckEasy(out IEnumerable<PointD> ptsToBeClipped)
		{
			ptsToBeClipped = null;
			if (!Edges.Where(e => e.VtxStart.FAtInfinity || e.VtxEnd.FAtInfinity).Any())
			{
				ptsToBeClipped = Vertices.Select(v => v.Pt);
				return true;
			}
			return false;
		}


		private double CalcRayLength(IEnumerable<PointD> ptsBox)
		{
			// Initialize
			var oes = OrientedEdges.ToArray();
			var ioeOutgoing = 0;

			// For each oriented edge
			for (int i = 0; i < oes.Length; i++)
			{
				// If it's the outgoing infinite edge
				if (oes[i].EndVtx.FAtInfinity)
				{
					// Set ioeOutgoing to it's index
					ioeOutgoing = i;
					break;
				}
			}

			// Get the outgoing and incoming infinite edges.
			//
			// The incoming edge is always two further away from the outgoing,
			// separated by the edge at infinity.
			var oeOut = oes[ioeOutgoing];
			var oeIn = oes[(ioeOutgoing + 2)%oes.Count()];

			// Make an initial guess for a good ray length
			double length = CalcInitialGuess(oeIn, oeOut, ptsBox);

			// While the length is still not satisfactory
			while (!Satisfactory(length, oeIn, oeOut, ptsBox) && length < double.MaxValue / 2.0)
			{
				// Double it
				length *= 2;
			}

			// Return the final length
			return (double)length;
		}

		private static bool Satisfactory(double length, OrientedEdge oeIn, OrientedEdge oeOut, IEnumerable<PointD> ptsBox)
		{
			// The length is satisfactory if all the points in the box are on the same side of it
			var ptRealOut = oeOut.EndVtx.ConvertToReal(oeOut.StartPt, length);
			var ptRealIn = oeIn.StartVtx.ConvertToReal(oeIn.EndPt, length);
			return !ptsBox.Where(pt => !Geometry.FLeft(ptRealOut, ptRealIn, pt)).Any();
		}

		private static double CalcInitialGuess(OrientedEdge oeIn, OrientedEdge oeOut, IEnumerable<PointD> ptsBox)
		{
			// Find out the max dist from any finite point to any point on the box and double it for good measure
			var maxDist0 = Math.Sqrt(ptsBox.Select(pt => Geometry.DistanceSq(pt, oeIn.StartPt)).Max());
			var maxDist1 = Math.Sqrt(ptsBox.Select(pt => Geometry.DistanceSq(pt, oeOut.StartPt)).Max());
			return 2.0 * Math.Max(maxDist0, maxDist1);

		}



		private static IEnumerable<PointD> BoxPoints(PointD ptUL, PointD ptLR)
		{
			return new List<PointD>
			       	{
			       		new PointD(ptUL.X, ptLR.Y),
			       		new PointD(ptLR.X, ptLR.Y),
			       		new PointD(ptLR.X, ptUL.Y),
			       		new PointD(ptUL.X, ptUL.Y)
			       	};
		}

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
				var vtxStart = oe.StartVtx;
				var vtxEnd = oe.EndVtx;

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

		internal FortunePoly(PointD pt, int index)
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
		}
#endif
		#endregion
	}
}
using System.Drawing;
using NetTrace;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Represents a winged edge vertex in the fortune algorithm. </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class FortuneVertex : WeVertex
	{
		#region Private variables
		bool _fAlreadyOrdered;			// True after this vertex has had its edges ordered in post processing
		bool _fAddedToWingedEdge;		// True if this vertex has already been added to the winged edge data structure
		#endregion

		#region Constructors
		internal FortuneVertex(PointF pt) : base(pt) { }
		internal FortuneVertex() { }
		#endregion

		#region Polygons

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Finds the third polygon which created this vertex besides the two on each side of the passed
		/// in edge. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="edge">	Edge in question. </param>
		///
		/// <returns>	The polygon "opposite" this edge. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal FortunePoly PolyThird(FortuneEdge edge)
		{
			// Get the indices for the two polys on each side of our passed in edge
			var i1 = edge.Poly1.Index;
			var i2 = edge.Poly2.Index;

			// For each edge incident with this vertex
			foreach (FortuneEdge edgeDifferent in Edges)
			{
				// If it's not our own edge
				if (edgeDifferent != edge)
				{
					// The polygon we want is on one side or the other of this edge
					var i1Diff = edgeDifferent.Poly1.Index;

					// If that edge's poly1 is one of our polygons
					if (i1Diff == i1 || i1Diff == i2)
					{
						// Then we're looking for his poly2
						return edgeDifferent.Poly2;
					}
					// Otherwise, we're looking for his poly1
					return edgeDifferent.Poly1;
				}
			}
			// Diagnostics
			Tracer.Assert(t.Assertion, false, "Couldn't find third generator");
			return null;
		}
		#endregion

		#region Winged Edge
		internal void AddToWingedEdge(WingedEdge we)
		{
			if (!_fAddedToWingedEdge)
			{
				we.AddVertex(this);
				_fAddedToWingedEdge = true;
			}
		}
		#endregion

		#region Edges

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Return the point at the other end of the given edge.  If the opposite point is a point at
		/// infinity, a "real" point on that edge is created and returned. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="edge">	Edge to traverse. </param>
		///
		/// <returns>	The point at the opposite end of the edge. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private PointF PtAtOtherEnd(FortuneEdge edge)
		{
			// Get the vertex at the other end
			var ptRet = VtxOtherEnd(edge).Pt;

			// If it's at infinity
			if (edge.VtxEnd.FAtInfinity)
			{
				// Produce a "real" point
				ptRet = new PointF(Pt.X + ptRet.X, Pt.Y + ptRet.Y);
			}

			// Return the result
			return ptRet;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Reset the ordered flag so we get ordered in the next call to Order() </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void ResetOrderedFlag()
		{
			_fAlreadyOrdered = false;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Compare edges clockwise around this vertex. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="e1">	The first WeEdge. </param>
		/// <param name="e2">	The second WeEdge. </param>
		///
		/// <returns>	+1 if they are CW around the generator, -1 if they're CCW, 0 if neither. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		int CompareEdges(WeEdge e1, WeEdge e2)
		{
			// Compare edges for clockwise order
			var fe1 = (FortuneEdge)e1;
			var fe2 = (FortuneEdge)e2;
			return Geometry.ICompareCw(Pt, fe1.PolyOrderingTestPoint, fe2.PolyOrderingTestPoint);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Order the three edges around this vertex. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void Order()
		{
			// If this vertex has already been ordered or is a vertex at infinity
			if (_fAlreadyOrdered || FAtInfinity)
			{
				return;
			}

			// If we have the usual case of 3 edges
			if (CtEdges == 3)
			{
				// Get the points at the other end of each of the 3 edges
				var pt0 = PtAtOtherEnd(Edges[0] as FortuneEdge);
				var pt1 = PtAtOtherEnd(Edges[1] as FortuneEdge);
				var pt2 = PtAtOtherEnd(Edges[2] as FortuneEdge);

				// If ordered incorrectly
				if (Geometry.ICcw(pt0, pt1, pt2) > 0)
				{
					// Swap the first two
					var edge0 = Edges[0];
					Edges[0] = Edges[1];
					Edges[1] = edge0;
				}

				// Mark ourselves as ordered
				_fAlreadyOrdered = true;
			}
			else
			{
				// Do the more complicated sort
				Edges.Sort(CompareEdges); 
			}
		}
		#endregion

		#region Infinite Vertices

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Produce a vertex at infinity. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="ptDirection">	Direction for the vertex. </param>
		/// <param name="fNormalize">	If true we normalize, else not. </param>
		///
		/// <returns>	The vertex at infinity. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal static FortuneVertex InfiniteVertex(PointF ptDirection, bool fNormalize)
		{
			var vtx = new FortuneVertex(ptDirection);
			vtx.SetInfinite(ptDirection, fNormalize);
			return vtx;
		}
		#endregion
	}
}
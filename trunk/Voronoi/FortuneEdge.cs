using System.Collections.Generic;
using System.Drawing;
using NetTrace;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Fortune edge. </summary>
	///
	/// <remarks>
	/// An edge for a winged edge structure but specifically designed for the voronoi algorithm.
	/// Darrellp, 2/18/2011.
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class FortuneEdge : WeEdge
	{
		#region Private Variables
		bool _fStartVertexSet;										// True if the first vertex has already been set on this edge
		bool _fAddedToWingedEdge;									// True if we've already been added to a winged edge data structure
		readonly FortunePoly[] _arPoly = new FortunePoly[2];		// The polygons on each side of us
		#endregion

		#region Properties
		internal FortunePoly Poly1 { get { return _arPoly[0]; } }
		internal FortunePoly Poly2 { get { return _arPoly[1]; } }

		public bool FSplit { get; set; }

		public bool FAtInfinity
		{
			get
			{
				return VtxStart.FAtInfinity;
			}
		}

		/// <summary>
		/// Return a point suitable for testing angle around the generator so that we can order
		/// the edges of polygons in postprocessing.  This is used in the CompareToVirtual() to
		/// effect that ordering.
		/// </summary>
		internal PointF PolyOrderingTestPoint
		{
			get
			{
				if (!VtxEnd.FAtInfinity)
				{
					return Geometry.MidPoint(VtxStart.Pt, VtxEnd.Pt);
				}
				if (!VtxStart.FAtInfinity)
				{
					return new PointF(
						VtxStart.Pt.X + VtxEnd.Pt.X,
						VtxStart.Pt.Y + VtxEnd.Pt.Y);
				}
				return Geometry.MidPoint(
					EdgeCCWSuccessor.VtxStart.Pt,
					EdgeCWPredecessor.VtxStart.Pt);
			}
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return string.Format("{0} : Gens {1} - {2}:", base.ToString(), _arPoly[0].Index, _arPoly[1].Index);
		}
		#endregion

		#region Queries
		/// <summary>
		/// Is this edge zero length?  Infinite rays are never zero length.
		/// </summary>
		/// <returns></returns>
		public bool FZeroLength()
		{
			return !VtxEnd.FAtInfinity && Geometry.FCloseEnough(VtxStart.Pt, VtxEnd.Pt);
		}

		/// <summary>
		/// Find the index of this edge in an adjacent vertex
		/// </summary>
		/// <param name="fStartVertex">If true, search start vertex, else search end vertex</param>
		/// <returns>Index in the vertice's edge list of this edge</returns>
		internal int EdgeIndex(bool fStartVertex)
		{
			var vtx = (FortuneVertex)(fStartVertex ? VtxStart : VtxEnd);

			for (int iEdge = 0; iEdge < vtx.Edges.Count; iEdge++)
			{
				if (vtx.Edges[iEdge] == this)
				{
					return iEdge;
				}
			}
			Tracer.Assert(t.Assertion, false, "Edge isn't in it's adjoining edge list");
			return -1;
		}

		internal FortunePoly OtherPoly(FortunePoly polyThis)
		{
			return Poly1 == polyThis ? Poly2 : Poly1;
		}

		/// <summary>
		/// Determine whether two edges connect at a common vertex and if so, how they connect
		/// </summary>
		/// <param name="edge1">First edge</param>
		/// <param name="edge2">Second edge</param>
		/// <param name="fEdge1ConnectsAtStartVtx">True if edge1 connects to edge2 at its start vertex, else false</param>
		/// <param name="fEdge2ConnectsAtStartVtx">True if edge2 connects to edge1 at its start vertex, else false</param>
		/// <returns>true if the edges connect</returns>
		static internal bool FConnectsTo(
			FortuneEdge edge1,
			FortuneEdge edge2,
			out bool fEdge1ConnectsAtStartVtx,
			out bool fEdge2ConnectsAtStartVtx)
		{
			bool fRet = false;

			fEdge1ConnectsAtStartVtx = false;
			fEdge2ConnectsAtStartVtx = false;

			if (ReferenceEquals(edge1.VtxStart, edge2.VtxStart))
			{
				fEdge1ConnectsAtStartVtx = true;
				fEdge2ConnectsAtStartVtx = true;
				fRet = true;
			}
			else if(ReferenceEquals(edge1.VtxStart, edge2.VtxEnd))
			{
				fEdge1ConnectsAtStartVtx = true;
				fRet = true;
			}
			else if (ReferenceEquals(edge1.VtxEnd, edge2.VtxStart))
			{
				fEdge2ConnectsAtStartVtx = true;
				fRet = true;
			}
			else if (ReferenceEquals(edge1.VtxEnd, edge2.VtxEnd))
			{
				fRet = true;
			}
			return fRet;
		}
		#endregion

		#region Modification
		/// <summary>
		/// Relabel all the end vertices of this edge to point to it's start vertex
		/// </summary>
		private void RelabelEndVerticesToStart()
		{
			foreach (FortuneEdge edgeCur in VtxEnd.Edges)
			{
				if (edgeCur != this)
				{
					if (edgeCur.VtxStart == VtxEnd)
					{
						edgeCur.VtxStart = VtxStart;
					}
					else
					{
						edgeCur.VtxEnd = VtxStart;
					}
				}
			}
			((FortuneVertex)VtxStart).ResetOrderedFlag();
		}

		/// <summary>
		/// Splice all the end vertices of this edge into the edge list of it's start vertex
		/// </summary>
		private void SpliceEndEdgesIntoStart()
		{
			int iEnd = EdgeIndex(false);
			int iStart = EdgeIndex(true);
			IList<WeEdge> lstSpliceInto = VtxStart.Edges;
			IList<WeEdge> lstSpliceFrom = VtxEnd.Edges;
			// Now add all our end vertices to the start vertex's edge list.  We add them in reverse
			// order starting from the edge before this one so that they end up in proper order in
			// the target list
			for (int i = (iEnd + lstSpliceFrom.Count - 1) % lstSpliceFrom.Count; i != iEnd; i = (i + lstSpliceFrom.Count - 1) % lstSpliceFrom.Count)
			{
				lstSpliceInto.Insert(iStart, lstSpliceFrom[i]);
			}

			Tracer.Indent();
			// Take care of CW Predecessor
			if (EdgeCWPredecessor.VtxStart == VtxEnd)
			{
				EdgeCWPredecessor.EdgeCCWSuccessor = EdgeCCWSuccessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCCWSuccessor = {1}", EdgeCWPredecessor, EdgeCCWSuccessor);
			}
			else
			{
				EdgeCWPredecessor.EdgeCCWPredecessor = EdgeCCWSuccessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCCWPredecessor = {1}", EdgeCWPredecessor, EdgeCCWSuccessor);
			}

			// and CCW Predecessor
			if (EdgeCCWPredecessor.VtxStart == VtxEnd)
			{
				EdgeCCWPredecessor.EdgeCWSuccessor = EdgeCWSuccessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCWSuccessor = {1}", EdgeCCWPredecessor, EdgeCWSuccessor);
			}
			else
			{
				EdgeCCWPredecessor.EdgeCWPredecessor = EdgeCWSuccessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCWPredecessor = {1}", EdgeCCWPredecessor, EdgeCWSuccessor);
			}

			// and CW Successor
			if (EdgeCWSuccessor.VtxStart == VtxStart)
			{
				EdgeCWSuccessor.EdgeCCWSuccessor = EdgeCCWPredecessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCCWSuccessor = {1}", EdgeCWSuccessor, EdgeCCWPredecessor);
			}
			else
			{
				EdgeCWSuccessor.EdgeCCWPredecessor = EdgeCCWPredecessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCCWPredecessor = {1}", EdgeCWSuccessor, EdgeCCWPredecessor);
			}

			// and CCW Successor
			if (EdgeCCWSuccessor.VtxStart == VtxStart)
			{
				EdgeCCWSuccessor.EdgeCWSuccessor = EdgeCWPredecessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCWSuccessor = {1}", EdgeCCWSuccessor, EdgeCWPredecessor);
			}
			else
			{
				EdgeCCWSuccessor.EdgeCWPredecessor = EdgeCWPredecessor;
				Tracer.Trace(tv.ZeroLengthEdges, "{0} EdgeCWPredecessor = {1}", EdgeCCWSuccessor, EdgeCWPredecessor);
			}

			Tracer.Unindent();
			// Remove us from the start vertex's list - our index has been bumped by all the edges
			// we inserted which is just all the ones from the end vertex minus 1 for ourself.
			lstSpliceInto.RemoveAt(iStart + lstSpliceFrom.Count - 1);
		}

		/// <summary>
		/// Move all the edges on the end vertex to the start
		/// </summary>
		/// <remarks>
		/// This is done in preparation for removing a zero length edge.  Since the edge is zero length
		/// we can assume that all the end edges fit in the "wedge" occupied by this edge in the
		/// start vertices list of edges.  That is, we can assume that we can just splice the end
		/// edges into the start vertex's list of edges without having to resort based on angle
		/// </remarks>
		internal void ReassignVertexEdges()
		{
			// Make all the end edges join to our start vertex rather than our end vertex
			RelabelEndVerticesToStart();
			// Put the end edges into our start vertex's list of edges
			SpliceEndEdgesIntoStart();
		}

		/// <summary>
		/// Add a vertex in the proper place according to _fStartVertexSet
		/// </summary>
		/// <param name="vtx">Vertex to add</param>
		internal void AddVertex(FortuneVertex vtx)
		{
			if (_fStartVertexSet)
			{
				VtxEnd = vtx;
			}
			else
			{
				_fStartVertexSet = true;
				VtxStart = vtx;
			}
		}

		/// <summary>
		/// Get the next edge in both the cw and ccw directions from this edge at the given vertex
		/// </summary>
		/// <param name="vtx">vertex to use</param>
		/// <param name="edgeCW">returned cw edge</param>
		/// <param name="edgeCCW">returned ccw edge</param>
		void GetSuccessorEdgesFromVertex(
			WeVertex vtx,
			out WeEdge edgeCW,
			out WeEdge edgeCCW)
		{
			// This is the case if there are no zero length edges
			if (vtx.Edges.Count == 3)
			{
				GetSuccessorEdgesFrom3ValentVertex(vtx, out edgeCW, out edgeCCW);
			}
			else
			{
				int iEdge;
				int cEdges;

				if (vtx == VtxStart)
				{
					iEdge = EdgeIndex(true);
					cEdges = VtxStart.CtEdges;
				}
				else
				{
					iEdge = EdgeIndex(false);
					cEdges = VtxEnd.CtEdges;
				}
				edgeCW = vtx.Edges[(iEdge + 1) % cEdges];
				edgeCCW = vtx.Edges[(iEdge + cEdges - 1) % cEdges];
			}
		}

		/// <summary>
		/// Get the next edge in both the cw and ccw directions from this edge at the given 3 valent vertex
		/// </summary>
		/// <param name="vtx">vertex to use</param>
		/// <param name="edgeCW">returned cw edge</param>
		/// <param name="edgeCCW">returned ccw edge</param>
		void GetSuccessorEdgesFrom3ValentVertex(
			WeVertex vtx,
			out WeEdge edgeCW,
			out WeEdge edgeCCW)
		{
			int iEdge;

			Tracer.Assert(t.Assertion, vtx.Edges.Count == 3, "Vertex without valency of 3");
			for (iEdge = 0; iEdge < 3; iEdge++)
			{
				if (ReferenceEquals(vtx.Edges[iEdge], this))
				{
					break;
				}
			}
			edgeCW = vtx.Edges[(iEdge + 1) % 3] as FortuneEdge;
			edgeCCW = vtx.Edges[(iEdge + 2) % 3] as FortuneEdge;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Place the poly on the proper side of this edge.  We use the generator of the poly to locate
		/// it properly WRT this edge. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="poly">	Polygon to locate. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void SetOrderedPoly(FortunePoly poly)
		{
			if (FLeftOf(poly.VoronoiPoint))
			{
				PolyLeft = poly;
			}
			else
			{
				PolyRight = poly;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Set up the "wings" for this edge - i.e., it's successor edges in both cw and ccw directions
		/// at both start and end vertices. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void SetSuccessorEdges()
		{
			WeEdge edgeCW, edgeCCW;

			if (VtxStart.Edges.Count == 2)
			{
				// Handle degenerate case of a fully infinite line
				//
				// With exactly two points in the voronoi diagram we would
				// normally have a fully infinite line.  This is the only situation
				// where this arises and we handle it by turning it into two
				// rays pointing in opposite directions.  If we're one of those
				// edges, then both our predecessor and our successor are the
				// other edge.

				// If we're Edges[0]
				if (ReferenceEquals(this, VtxStart.Edges[0]))
				{
					// Set our pred and succ to Edges[1]
					EdgeCCWPredecessor = EdgeCWPredecessor = VtxStart.Edges[1];
				}
				else
				{
					// Set our pred ans succ to Edges[0]
					EdgeCCWPredecessor = EdgeCWPredecessor = VtxStart.Edges[0];
				}
				return;
			}

			// Sort the edges around the vertex
			//
			// We can't keep edges sorted during the sweepline processing so we do it here in
			// postprocessing
			((FortuneVertex)VtxStart).Order();

			GetSuccessorEdgesFromVertex(
				VtxStart,
				out edgeCW,
				out edgeCCW);

			EdgeCWPredecessor = edgeCW;
			EdgeCCWPredecessor = edgeCCW;

			// If one end is an infinite ray then it's successors will be set up when we add the polygon
			// at infinity on.
			if (!VtxEnd.FAtInfinity)
			{
				((FortuneVertex)VtxEnd).Order();
				GetSuccessorEdgesFromVertex(
					VtxEnd,
					out edgeCW,
					out edgeCCW);
				EdgeCWSuccessor = edgeCW;
				EdgeCCWSuccessor = edgeCCW;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Add a poly to the edge and the edge to the winged edge data structure. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="poly">	Polygon to add. </param>
		/// <param name="we">	Winged edge structure to add edge to. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void Process(FortunePoly poly, WingedEdge we)
		{
			// Put the poly properly to the left or right of this edge
			SetOrderedPoly(poly);

			// Avoid adding the edge twice
			//
			// We'll attempt to add this edge twice, once from each polygon on each side of
			// it.  We only want to add it one of those times.
			if (!_fAddedToWingedEdge)
			{
				// Set up the successor edges
				SetSuccessorEdges();

				we.AddEdge(this);
				_fAddedToWingedEdge = true;
				((FortuneVertex)VtxStart).AddToWingedEdge(we);
				((FortuneVertex)VtxEnd).AddToWingedEdge(we);
			}
		}

		/// <summary>
		/// Set up the polygons which surround this edge.  During the sweepline processing we don't necessarily
		/// know where the final vertex for an edge will be before we know the polygons on each side of the
		/// edge so we can't actually determine which side of the edge the polygons will lie on.  Consequently, we
		/// have to just keep them handy until we finally get our second point.
		/// </summary>
		/// <param name="poly1">First poly</param>
		/// <param name="poly2">Second poly</param>
		internal void SetPolys(FortunePoly poly1, FortunePoly poly2)
		{
			_arPoly[0] = poly1;
			_arPoly[1] = poly2;
		}
		#endregion

		#region IComparable Members
		/// <summary>
		/// Find the common polygon between two edges.  An assertion will be raised if there is no
		/// common polygon.
		/// </summary>
		/// <param name="edge">Edge to find a common poly with</param>
		/// <returns>The common polygon</returns>
		private FortunePoly PolyCommon(FortuneEdge edge)
		{
			if (ReferenceEquals(Poly1, edge.Poly1) || ReferenceEquals(Poly1, edge.Poly2))
			{
				return Poly1;
			}
			Tracer.Assert(t.Assertion,
			              ReferenceEquals(Poly2, edge.Poly1) || ReferenceEquals(Poly2, edge.Poly2),
			              "Calling GenCommon on two edges with no common generator");
			return Poly2;
		}

		/// <summary>
		/// Compare two edges based on their cw order around a common generator.  It is an error to compare edges
		/// which do not have a common generator so this is only a "partial" comparer which is probably strictly
		/// verboten according to C# rules, but we have to do it in order to use the framework Sort routine to sort
		/// edges around a generator.
		/// </summary>
		/// <param name="edgeIn">Edge to compare</param>
		/// <returns>Comparison output</returns>
		public override int CompareToVirtual(WeEdge edgeIn)
		{
			var edge = edgeIn as FortuneEdge;

			Tracer.Assert(t.Assertion, edge != null, "Non-fortuneEdge passed to fortuneEdge compare");

			if (ReferenceEquals(edgeIn, this))
			{
				return 0;
			}

			return Geometry.ICompareCw(PolyCommon(edge).VoronoiPoint, PolyOrderingTestPoint, edge.PolyOrderingTestPoint);
		}
		#endregion
	}
}
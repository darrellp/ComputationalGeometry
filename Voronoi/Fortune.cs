#define NEW
#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System.Collections.Generic;
using System.Collections;
using System;
using NUnit.Framework;
using NetTrace;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Fortune implements the Fortune algorithm for Voronoi diagrams. </summary>
	///
	/// <remarks>	
	/// When run initially, Fortune.Voronoi() returns a list of polytons that make up the diagram.
	/// These can be transformed into a fully blown winged edge data structure if desired.  It's
	/// input is simply a list of points to calculate the diagram for.  It is based on the
	/// description of the algorithm given in "Computational Geometry - Algorithms and Applications"
	/// by M. de Berg et al. with a LOT of details filled in.  Some differences between the book's
	/// solution and mine:
	/// 
	/// The book suggests a doubly connected edge list.  I prefer a winged edge data structure.
	/// Internally I use a structure which is a primitive winged edge structure which contains
	/// polygons, edges and vertices but not much of the redundancy available in a fully fleshed out
	/// winged edge, but optionally allow for a conversion to a fully fleshed out winged edge data
	/// structure.  This conversion is a bit expensive and may not be necessary (for instance, it
	/// suffices to merely draw the diagram) so is made optional.
	/// 
	/// Another big difference between this algorithm and the one there is the handling of polygons
	/// which extend to infinity.  These don't necessarily fit into winged edge which requires a
	/// cycle of edges on each polygon and another polygon on the opposite site of each edge.  The
	/// book suggests surrounding the diagram with a rectangle to solve this problem.  Outside of the
	/// fact that I still don't see how that truly solves the problem (what's on the other side of
	/// the edges of the rectangle?), I hate the solution which introduces a bunch of spurious
	/// elements which have nothing to do with the diagram. I solve it by using a "polygon at
	/// infinity" and a bunch of "sides at infinity".  The inspiration for all this is projective
	/// geometry which has a "point at infinity".  This maintains the winged edge structure with the
	/// introduction of these "at infinity" elements which are natural extensions of the diagram
	/// rather than the ugly introduced rectangle suggested in the book.  They also allow easy
	/// reference to these extended polygons.  For instance, to enumerate them, just step off all the
	/// polygons which "surround" the polygon at infinity.
	/// 
	/// I take care of a lot of bookkeeping details and border cases not mentioned in the book - zero
	/// length edges, collinear points, degenerate solutions with wholly infinite lines and many other
	/// minor to major points not specifically covered in the book. 
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class Fortune
	{
		#region Private Variables
		Beachline _bchl = new Beachline();						// The beachline which is the primary data structure
																// kept as we sweep downward throught the points
		EventQueue _qevEvents = new EventQueue();				// Priority queue for events which occur during the sweep
		readonly List<FortunePoly> _lstPolys = new List<FortunePoly>();	// The list of Polygons which are the output of the algorithm
		#endregion

		#region Constructor

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Add all points to the event queue as site events. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="points">	Points whose Voronoi diagram will be calculated. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public Fortune(IEnumerable points)
		{
			// For every point
			foreach (PT pt in points)
			{
				// Add it to our list
				QevEvents.Add(new SiteEvent(InsertPoly(pt)));
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Each polygon in the final solution is associated with a point in the input.  We initialize
		/// that polygon for this point here. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="pt">	The point. </param>
		///
		/// <returns>	The polygon produced. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		FortunePoly InsertPoly(PT pt)
		{
			Tracer.Trace(tv.GeneratorList, "Generator {0}: ({1}, {2})",
				_lstPolys.Count, pt.X, pt.Y);

			// The count is being passed in only as a unique identifier for this point.
			var poly = new FortunePoly(pt, _lstPolys.Count);
			_lstPolys.Add(poly);
			return poly;
		}
		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the list of polygons which make up the voronoi diagram. </summary>
		///
		/// <value>	The polygons. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public List<FortunePoly> Polygons
		{
			get
			{
				return _lstPolys;
			}
		}

		internal EventQueue QevEvents
		{
			get
			{
				return _qevEvents;
			}
			set
			{
				_qevEvents = value;
			}
		}

		internal Beachline Bchl
		{
			get
			{
				return _bchl;
			}
			set
			{
				_bchl = value;
			}
		}
		#endregion

		#region Public methods

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Calculates the voronoi diagram. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void Voronoi()
		{
			// Process all the events
			//
			// The algorithm works by a sweepline technique.  As the sweekpline moves down
			// through the points, events are generated and placed in a priority queue.  These
			// events are removed from the queue and are in turn used to advance the sweep line.
			ProcessEvents();

			// Tie up loose ends after all the processing
			Finish();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Return the Winged edge structure for the voronoi diagram.  This is a complicated procedure
		/// with a lot of details to be taken care of as follows.
		/// 
		/// Every polygon has to have its edges sorted in clockwise order.
		/// 
		/// Set all the successor edges to each edge and the list of edges to each vertex.
		/// 
		/// Zero length edges are eliminated and their "endpoints" are consolidated.
		/// 
		/// The polygon at infinity and all the edges at infinity must be added
		/// 
		/// NOTE ON THE POLYGON AT INFINITY In the WingedEdge structure each polygon has a list of edges
		/// with another polygon on the other side of each edge.  This poses a bit of a difficulty for
		/// the polygons in a Voronoi diagram whose edges are rays to infinity.  We'll call these
		/// "infinite polygons" for convenience.  The solution we use in our WingedEdge data structure is
		/// to produce a single "polygon at infinity" (not to be confused with the infinite polygons
		/// previously mentioned) and a series of edges at infinity which "separate" the infinite
		/// polygons from the polygon at infinity.  These edges have to be ordered in the same order as
		/// the infinite polygons around the border.  All of this is done in AddEdgeAtInfinity().
		/// 
		/// In order to set up the polygon at infinity we have to start with an infinite polygon and
		/// then work our way around the exterior of the diagram, moving from one infinite polygon to
		/// another and adding them to the list of polygons "adjacent" to the polygon at infinity.  We
		/// keep track of the first infinite polygon we find to use it as the starting polygon in that
		/// process.
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <returns>	The winged edge structure for the diagram. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WingedEdge BuildWingedEdge()
		{
			// Initialize
			// The first infinite polygon we locate and the index for the infinite
			// edge ccw to that polygon
			FortunePoly polyInfinityStart = null;
			var iLeadingInfiniteEdgeCw = -1;
			var we = new WingedEdge();

			// for all polygons in the voronoi diagram
			foreach (var poly in _lstPolys)
			{
				// Process the polygon's edges
				ProcessPolygonEdges(poly, we, ref polyInfinityStart, ref iLeadingInfiniteEdgeCw);
			}

			// Take care of a corner case with zero length edges in the starting infinite polygon
			//
			// In odd cases, if the starting infinite polygon has zero length edges, then when they were dropped
			// in HandleZeroLengthEdges, our indices changed so that iLeadingInfiniteEdgeCw might be wrong
			// now.  In that corner case, we have to just do a linear search once again to recalculate
			// iLeadingInfiniteEdgeCw correctly.
			if (polyInfinityStart != null && polyInfinityStart.FZeroLengthEdge)
			{
				// Recalc the index
				iLeadingInfiniteEdgeCw = RecalcLeadingInfiniteEdge(polyInfinityStart);
			}

			// Create the polygon at infinity
			AddPolygonAtInfinity(we, polyInfinityStart, iLeadingInfiniteEdgeCw);

#if NETTRACE || DEBUG
			// If we're doing validation
			if (Tracer.FTracing(t.WeValidate))
			{
				// Check the validation of our final winged edge structure
				Tracer.Assert(t.WeValidate, we.Validate(), "Invalid Winged edge");
			}
#endif
			// Return the final winged edge
			return we;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Recalc leading infinite edge. </summary>
		///
		/// <remarks>	
		/// This is a lot of code for a very corner case.  If an initial infinite polygon has some of
		/// it's edges removed because they were zero length then a previously calculated index for the
		/// leading infinite edge will be wrong and we have to recalculate it which is the purpose of
		/// this little routine. Darrellp, 2/18/2011. 
		/// </remarks>
		///
		/// <param name="polyInfinityStart">	The starting infinite polygon. </param>
		///
		/// <returns>	The newly calculated index for the leading edge. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static int RecalcLeadingInfiniteEdge(FortunePoly polyInfinityStart)
		{
			// Initialize our return index to an illegal value
			var iLeadingInfiniteEdgeCw = -1;

			// If this is a fully infinite line
			//
			// If there are only two edges, they'll both be at infinity and we can't just use their
			// indices to distinguish - we have to actually check the angles to see which one is
			// "leading".
			if (polyInfinityStart.VertexCount == 2)
			{
				// Diagnostics
				Tracer.Assert(t.Assertion,
				              polyInfinityStart.EdgesCW[0].VtxEnd.FAtInfinity && polyInfinityStart.EdgesCW[1].VtxEnd.FAtInfinity,
				              "Two edged polygon without both edges at infinity");
				// If we run clockwise from the origin through edge 0 through edge 1
				if (Geometry.ICcw(new PT(0, 0), polyInfinityStart.EdgesCW[0].VtxEnd.Pt, polyInfinityStart.EdgesCW[1].VtxEnd.Pt) > 0)
				{
					// Edge 1 is our leading edge
					iLeadingInfiniteEdgeCw = 1;
				}
				else
				{
					// Edge 0 is our leading edge
					iLeadingInfiniteEdgeCw = 0;
				}
			}
			else
			{
				// Find the leading edge which goes to infinity
				for (var iEdge = 0; iEdge < polyInfinityStart.VertexCount; iEdge++)
				{
					// Retrieve the edge and the next edge in CW order
					var edge = polyInfinityStart.EdgesCW[iEdge] as FortuneEdge;
					var iEdgeNext = (iEdge + 1) % polyInfinityStart.VertexCount;
					var edgeNextCW = polyInfinityStart.EdgesCW[iEdgeNext] as FortuneEdge;

					// If this edge and the next both end at infinity
					if (edge.VtxEnd.FAtInfinity && edgeNextCW.VtxEnd.FAtInfinity)
					{
						// Then this is our leading CW edge to infinity
						//
						// This means that this edge is to the left as we look "out" from the
						// interior of the voronoi diagram.
						iLeadingInfiniteEdgeCw = iEdgeNext;
						break;
					}
				}
			}
			return iLeadingInfiniteEdgeCw;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Process a polygon's edges. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="poly">						Polygon to be processed. </param>
		/// <param name="we">						WingedEdge structure we'll add the polygon to. </param>
		/// <param name="polyInfinityStart">		[in,out] The current infinite polygon or null if none
		/// 										yet found. </param>
		/// <param name="iLeadingInfiniteEdgeCw">	[in,out] The CW leading infinite edge if a new
		/// 										infinite polygon has been found. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void ProcessPolygonEdges(
			FortunePoly poly,
			WingedEdge we,
			ref FortunePoly polyInfinityStart,
			ref int iLeadingInfiniteEdgeCw)
		{
			// Diagnostics
			Tracer.Trace(tv.FinalEdges, "Edges for generator {0}", poly.Index);
			Tracer.Indent();

			// Add the poly to the winged edge struct and sort it's edges
			we.AddPoly(poly);
			poly.SortEdges();

			// For each edge in the polygon
			for (var iEdge = 0; iEdge < poly.VertexCount; iEdge++)
			{
				// Get the edge following the current edge in clockwise order
				var edge = poly.EdgesCW[iEdge] as FortuneEdge;
				var iEdgeNext = (iEdge + 1) % poly.VertexCount;
				var edgeNextCW = poly.EdgesCW[iEdgeNext] as FortuneEdge;
				Tracer.Trace(tv.FinalEdges, edge.ToString());

				// Incorporate the edge into the winged edge
				edge.Process(poly, we);

				// If this is an infinite polygon and we've not located any infinite polygons before
				if (polyInfinityStart == null && edge.VtxEnd.FAtInfinity && edgeNextCW.VtxEnd.FAtInfinity)
				{
					// Keep track of the first infinite polygon we've seen
					//
					// We define the "leading edge" as the one to the left when looking outward.  We have to
					// explicitly locate this first one - after that, we can locate the leading edge
					// for the next polygon to the right as the edge which follows the current poly's
					// leading edge.  As we work our way around the outside in AddPolygonAtInfinity we can
					// therefore easily set leading edges on successive polygons.
					iLeadingInfiniteEdgeCw = iEdge;
					polyInfinityStart = poly;
				}
			}

			// Remove any zero length edges in the polygon
			//
			// We have to put this after we've processed all the edges since that's where we determine any
			// zero length edges.  This is not as good as I'd like it because it means that this may, on rare
			// occasions, jigger the indices for the poly which in turn means that the iLeadingInfiniteEdgeCw
			// value may be off and we have to check for that later.  It's ugly code, but rarely occurs in
			// practice so isn't a performance hit.
			poly.HandleZeroLengthEdges();
			Tracer.Unindent();
			return;
		}

		#endregion

		#region Polygon and edges at infinity

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Set up the polygon at infinity.  The main difficulty here consists in traversing around the
		/// infinite polygons at the edge of the diagram in order. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="we">				WingedEdge structure we'll add the polygon at infinity to. </param>
		/// <param name="polyStart">		Infinite polygon to start the polygon at infinity's polygon list with. </param>
		/// <param name="iLeadingEdgeCw">	Starting infinite edge. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void AddPolygonAtInfinity(WingedEdge we, FortunePoly polyStart, int iLeadingEdgeCw)
		{
			// See if we've got a degenerate case
			//
			// Such as a single point
			if (polyStart == null)
			{
				return;
			}

			// Initialize
			var polyCur = polyStart;
			int iLeadingEdgeNext;

			// Create the infamous polygon at infinity...
			var polyAtInfinity = new FortunePoly(new PT(0, 0), -1);
			FortuneEdge edgePreviousAtInfinity = null;

			// Declare this the official polygon at infinity
			polyAtInfinity.FAtInfinity = true;

			// Add it to the winged edge
			we.AddPoly(polyAtInfinity);
  
			do
			{
				// Add the edge at infinity between our current poly and the poly at infinity
				FortunePoly polyNext;
				edgePreviousAtInfinity = AddEdgeAtInfinity(
					polyAtInfinity,
					polyCur,
					iLeadingEdgeCw,
					edgePreviousAtInfinity,
					out polyNext,
					out iLeadingEdgeNext);
				we.AddEdge(edgePreviousAtInfinity);

				// Move to the neighboring polygon at infinity
				polyCur = polyNext;
				iLeadingEdgeCw = iLeadingEdgeNext;
			}
			// we reach the end of the "outer" infinite polygons of the diagram
			//
			// Set each of the outer infinite polygons up with an edge at infinity to separate
			// them from the polygon at infinity
			while (polyCur != polyStart);

			// Thread the last poly back to the first
			var edgeFirstAtInfinity = polyCur.EdgesCW[iLeadingEdgeNext];
			edgePreviousAtInfinity.EdgeCCWPredecessor = edgeFirstAtInfinity;
			edgeFirstAtInfinity.EdgeCWSuccessor = edgePreviousAtInfinity;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Add an edge at infinity and step the polygon and edge along to the next infinite polygon and
		/// rayed edge. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="polyAtInfinity">			Polygon at infinity. </param>
		/// <param name="poly">						Infinite polygon we're adding the edge to. </param>
		/// <param name="iLeadingEdgeCw">			index to rayed edge  we start with. </param>
		/// <param name="edgePreviousAtInfinity">	Edge at infinity we added in the previous infinite
		/// 										polygon. </param>
		/// <param name="polyNextCcw">				[out] Returns the next infinite polygon to be
		/// 										processed. </param>
		/// <param name="iLeadingEdgeNext">			[out] Returns the next infinite edge. </param>
		///
		/// <returns>	. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static FortuneEdge AddEdgeAtInfinity(
			FortunePoly polyAtInfinity,
			FortunePoly poly,
			int iLeadingEdgeCw,
			FortuneEdge edgePreviousAtInfinity, 
			out FortunePoly polyNextCcw,
			out int iLeadingEdgeNext)
		{
			// Get the other infinite edge
			//
			// This is the edge to the right as we look outward
			var iTrailingEdgeCw = (iLeadingEdgeCw + 1) % poly.VertexCount;
			var edgeLeadingCw = poly.EdgesCW[iLeadingEdgeCw] as FortuneEdge;
			var edgeTrailingCw = poly.EdgesCW[iTrailingEdgeCw] as FortuneEdge;

			// Next polygon in order is to the left of our leading edge
			polyNextCcw = edgeLeadingCw.PolyLeft as FortunePoly;

			// Diagnostics
			Tracer.Assert(t.Assertion, polyNextCcw.Index != poly.Index,
				"Next polygon in AddEdgeAtInfinity is the same as the current poly");
			
			// Create the edge at infinity
			//
			// Create the edge at infinity separating the current infinite polygon from
			// the polygon at infinity.  The vertices for this edge will both be vertices
			// at infinity.  This, of course, doesn't really have any real impact on the
			// "position" of the edge at infinity, but allows us to maintain a properly
			// set up winged edge structure.
			var edgeAtInfinity = new FortuneEdge
									{
										PolyRight = poly,
										PolyLeft = polyAtInfinity,
										VtxStart = edgeLeadingCw.VtxEnd,
										VtxEnd = edgeTrailingCw.VtxEnd
									};

			// The poly at infinity is to the left of the edge, the infinite poly is to its right
			//
			// Start and end vertices are the trailing and leading infinite edges
			// Add the edge at infinity to the poly at infinity and the current infinite poly
			polyAtInfinity.AddEdge(edgeAtInfinity);
			poly.EdgesCW.Insert(iTrailingEdgeCw, edgeAtInfinity);

			// Set up the wings of the wingedEdge
			edgeAtInfinity.EdgeCWPredecessor = edgeLeadingCw;
			edgeAtInfinity.EdgeCCWSuccessor = edgeTrailingCw;
			edgeLeadingCw.EdgeCCWSuccessor = edgeAtInfinity;
			edgeTrailingCw.EdgeCWSuccessor = edgeAtInfinity;

			// If we've got a previous edge at infinity
			if (edgePreviousAtInfinity != null)
			{
				// Incorporate it properly
				edgePreviousAtInfinity.EdgeCCWPredecessor = edgeAtInfinity;
				edgeAtInfinity.EdgeCWSuccessor = edgePreviousAtInfinity;
			}
			
			// Hook up our edge at infinity to our vertices at infinity
			AddEdgeAtInfinityToVerticesAtInfinity(
				edgeAtInfinity,
				edgeAtInfinity.VtxStart as FortuneVertex,
				edgeAtInfinity.VtxEnd as FortuneVertex);

			// Locate the leading edge index in the next polygon

			// For each Edge of the infinite polygon to our left
			for (iLeadingEdgeNext = 0; iLeadingEdgeNext < polyNextCcw.VertexCount; iLeadingEdgeNext++)
			{
				// If it's the same as our leading CW infinite edge
				if (polyNextCcw.EdgesCW[iLeadingEdgeNext] == edgeLeadingCw)
				{
					// then their leading edge is the one immediately preceding it in CW order
					iLeadingEdgeNext = (polyNextCcw.VertexCount + iLeadingEdgeNext - 1) % polyNextCcw.VertexCount;
					break;
				}
			}

			// Return the edge at infinity we've created
			return edgeAtInfinity;
		}

		/// <summary>
		/// Insert the edge at infinity into the edge list for the vertices at infinty
		/// </summary>
		/// <param name="edge">Edge at infinity being added</param>
		/// <param name="leadingVtxCw">Vertex on the left of infinite poly as we look out</param>
		/// <param name="trailingVtxCw">Vertex on the right</param>
		private static void AddEdgeAtInfinityToVerticesAtInfinity(FortuneEdge edge, FortuneVertex leadingVtxCw, FortuneVertex trailingVtxCw)
		{
			if (leadingVtxCw.Edges.Count == 1)
			{
				// This will be overwritten later but we have to insert here so that we can insert ourselves at
				// index 2...
				leadingVtxCw.Edges.Add(edge);
			}
			leadingVtxCw.Edges.Add(edge);
			if (trailingVtxCw.Edges.Count == 3)
			{
				// Here is where the overwriting referred to above occurs
				trailingVtxCw.Edges[1] = edge;
			}
			else
			{
				trailingVtxCw.Edges.Add(edge);
			}
		}
		#endregion

		#region Event handler
		/// <summary>
		/// Remove events from the queue and handle them one at a time.
		/// </summary>
		private void ProcessEvents()
		{
			// Create an impossible fictional "previous event"
			FortuneEvent evtPrev = new CircleEvent(new PT(TPT.MaxValue, TPT.MaxValue), 0);

			while (QevEvents.Count > 0)
			{
				// Events are pulled off in top to bottom, left to right, site event before
				// circle event order.  This ensures that events/sites at identical locations
				// will be pulled off one after the other which allows us to cull them.
				FortuneEvent evt = QevEvents.Pop();

				// Check for identically placed events
				if (Geometry.FCloseEnough(evt.Pt, evtPrev.Pt))
				{
					Type tpPrev = evtPrev.GetType();
					Type tpCur = evt.GetType();

					if (tpPrev == typeof(SiteEvent))
					{
						if (tpCur == typeof(SiteEvent))
						{
							// Skip identical site events.
							continue;
						}
					}
					else if (tpCur == typeof(CircleEvent))
					{
						var cevt = evt as CircleEvent;
						var cevtPrev = evtPrev as CircleEvent;

						// Identically placed circle events still have to be processed but we handle the
						// case specially.  The implication of identically placed circle events is that
						// we had four or more cocircular points which implies that the polygons
						// for those points come together to a point.  Since we only allow for vertices
						// of order three during voronoi processing, we create "zero length" edges for the
						// polygons which meet at that common point. These will be removed later in postprocessing.
						if (Geometry.FCloseEnough(cevt.VoronoiVertex, cevtPrev.VoronoiVertex))
						{
							cevt.FZeroLength = true;
						}
					}
				}

				// Handle the event
				evt.Handle(this);
				evtPrev = evt;
			}
		}
		#endregion

		#region Finishing
		/// <summary>
		/// Finish up loose ends
		/// </summary>
		private void Finish()
		{
			FixInfiniteEdges();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// In the course of processing, edges are created initially with null endpoints.  The endpoints
		/// are filled in during the course of the Fortune algorithm.  When we finish processing, any null
		/// endpoints represent "points at infinity" where the edge is a ray or (rarely) an infinitely
		/// extended line.  We have to go through and fix up any of these loose ends to turn them into
		/// points at infinity.
		/// 
		/// This is a lot of really tedious, detail oriented, nitpicky code.  I'd avoid it if I were you.
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private void FixInfiniteEdges()
		{
			// For each of our polygons
			foreach(FortunePoly poly in _lstPolys)
			{
				// Loop through the edges of the poly searching for infinite ones
				//
				// Ensure that singly infinite edges have the infinite vertex in the
				// VtxEnd position and split doubly infinite edges into two singly infinite edges.  Replace
				// the null vertices with the proper infinite vertices.
				foreach (var wedge in poly.EdgesCW)
				{
					// Obtain our fortune edge
					var edge = wedge as FortuneEdge;
					Tracer.Assert(t.Assertion, edge != null, "Non-FortuneEdge in FortunePoly list");

					// Is this an infinite edge?
					if (edge.VtxStart == null || edge.VtxEnd == null)
					{
						if (edge.VtxEnd == null && edge.VtxStart == null)
						{
							// If they're both null, we've got a double infinite edge
							//
							// Split doubly infinite edges into two singly infinite edges.  This only occurs in rare
							// cases where there are only two generators or all the generators are collinear so that
							// we end up with parallel infinite lines rather than infinite rays.  We can't handle
							// true infinite lines in our winged edge data structure, so we turn the infinite lines into
							// two rays pointing in opposite directions and originating at the midpoint of the two generators.

							// Initialize
							var pt1 = edge.Poly1.VoronoiPoint;
							var pt2 = edge.Poly2.VoronoiPoint;
							var dx = pt2.X - pt1.X;
							var dy = pt2.Y - pt1.Y;
							var ptMid = new PT((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);

							// Infinite vertices have directions in them rather than locations
							var vtx1 = FortuneVertex.InfiniteVertex(new PT(-dy, dx), true);
							var vtx2 = FortuneVertex.InfiniteVertex(new PT(dy, -dx), true);

							// Create the new edge an link it in 
							edge.VtxStart = new FortuneVertex(ptMid);
							edge.VtxEnd = vtx1;
							var edgeNew = new FortuneEdge {VtxStart = edge.VtxStart, VtxEnd = vtx2};
							edgeNew.SetPolys(edge.Poly1, edge.Poly2);
							edge.Poly1.AddEdge(edgeNew);
							edge.Poly2.AddEdge(edgeNew);
							edge.VtxStart.Add(edge);
							edge.VtxStart.Add(edgeNew);
							vtx1.Add(edge);
							vtx2.Add(edgeNew);
							edge.FSplit = edgeNew.FSplit = true;

							// If the edge "leans right"
							//
							// We have to be very picky about how we set up the left and right
							// polygons for our new rays.
							if (dx == 0 || dx * dy > 0) // dy == 0 case needs to fall through...
							{
								// Set up left and right polygons one way
								edge.PolyRight = edgeNew.PolyLeft = edge.Poly1;
								edge.PolyLeft = edgeNew.PolyRight = edge.Poly2;
							}
							else
							{
								// Set left and right polygons the other way
								edge.PolyLeft = edgeNew.PolyRight = edge.Poly1;
								edge.PolyRight = edgeNew.PolyLeft = edge.Poly2;
							}

							// Diagnostics
							Tracer.Trace(tv.FinalEdges, "Edge split into {0} and {1}", edge, edgeNew);

							// Can't continue or C# will complain
							//
							// If we continue then we'll be messing with the collection in the foreach
							// statement which C# will complain about.  It's okay because to break here
							// because there can be at most two doubly infinite edges per generator and
							// if there's a second it will be handled by the generator on the other side
							// of that edge.
							break;
						}
						else
						{
							// Singly infinite edges get turned into rays with one point at infinity
							if (edge.VtxStart == null)
							{
								// Swap if necessary to ensure that the infinite vertex is in the VtxEnd position
								//
								// This is what justifies the assertion in WeEdge.FLeftOf() (see the code).
								edge.VtxStart = edge.VtxEnd;
								edge.VtxEnd = null;
							}

							// Replace the null vertex with an infinite vertex
							var pt1 = edge.Poly1.VoronoiPoint;
							var pt2 = edge.Poly2.VoronoiPoint;
							var ptMid = new PT((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);

							// Point the ray in the proper direction
							//
							// We have to be careful to get this ray pointed in the proper orientation.  At
							// this point we just have a vertex and points which are the original voronoi
							// points which created these infinite polys.  That's enough to figure out the
							// absolute direction the voronoi points but not enough to determine which "orientation"
							// it points along.  To do this, we find the third polygon at the "base" of this ray
							// and point the ray "away" from it.
							var polyThird = ((FortuneVertex)edge.VtxStart).PolyThird(edge);
							var fThirdOnLeft = Geometry.FLeft(pt1, pt2, polyThird.VoronoiPoint);
							var dx = pt2.X - pt1.X;
							var dy = pt2.Y - pt1.Y;
							var ptProposedDirection = new PT(dy, -dx);
							var ptInProposedDirection = new PT(ptMid.X + dy, ptMid.Y - dx);
							var fProposedOnLeft = Geometry.FLeft(pt1, pt2, ptInProposedDirection);

							if (fProposedOnLeft == fThirdOnLeft)
							{
								ptProposedDirection.X = -ptProposedDirection.X;
								ptProposedDirection.Y = -ptProposedDirection.Y;
							}
							edge.VtxEnd = FortuneVertex.InfiniteVertex(ptProposedDirection, true);
							edge.VtxEnd.Edges.Add(edge);
						}
					}
				}
			}
		}
		#endregion

		#region NUNIT
#if NUNIT || DEBUG
		[TestFixture]
		public class TestVoronoi
		{
			static Fortune Example()
			{
				var pts = new[] {
					new PT(0, 0),
					new PT(1, 1),
					new PT(1, -1)
				};
				return new Fortune(pts);
			}

			[Test]
			public void TestGeneratorAdds()
			{
				Assert.AreEqual(3, Example()._lstPolys.Count);
			}

			[Test]
			public void TestProcessEvents()
			{
				Example().ProcessEvents();
			}
		}
#endif
		#endregion
	}

	internal class EventQueue : PriorityQueueWithDeletions<FortuneEvent>
	{
		#region Private Variables

		readonly List<CircleEvent> _lstcevt = new List<CircleEvent>();		// List of circle events so we can keep track of them
		#endregion

		#region Circle event special handling
		internal void AddCircleEvent(CircleEvent cevt)
		{
			Add(cevt);
			_lstcevt.Add(cevt);
		}

		internal IList<CircleEvent> CircleEvents
		{
			get
			{
				return _lstcevt;
			}
		}
		#endregion
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Fortune polygon. </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class FortunePoly : WePolygon
	{
		#region Private Variables

		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Indicates that this is the singleton polygon at infinity </summary>
		///
		/// <value>	true if at it's the polygon at infinity. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FAtInfinity { get; set; }

		public bool FZeroLengthEdge { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	The original point which caused this voronoi cell to exist. </summary>
		///
		/// <value>	The point in the original set of data points. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PT VoronoiPoint { get; set; }

		public int Index { get; set; }

		#endregion

		#region Constructor
		internal FortunePoly(PT pt, int iGen)
		{
			FZeroLengthEdge = false;
			FAtInfinity = false;
			Index = iGen;
			VoronoiPoint = pt;
		}
		#endregion

		#region Edge operations
		/// <summary>
		/// Sort the edges in Clockwise order.  We do this partially by knowing that all the polygons
		/// in a Voronoi diagram are convex.  That means we can sort edges by measuring their angle around
		/// the generator for the polygon.  We have to pick the point to measure this angle carefully which
		/// is what WEEdge.PolyOrderingTestPoint() does.  We also have to make a special case for the rare
		/// doubly infinite lines (such as that created with only two generators).
		/// </summary>
		internal void SortEdges()
		{
			if (EdgesCW.Count == 2)
			{
				if (((FortuneEdge)EdgesCW[0]).FSplit)
				{
					if (Geometry.ICcw(
						((FortuneEdge)EdgesCW[0]).PolyOrderingTestPoint,
						((FortuneEdge)EdgesCW[1]).PolyOrderingTestPoint,
						VoronoiPoint) < 0)
					{
						WeEdge edgeT = EdgesCW[0];
						EdgesCW[0] = EdgesCW[1];
						EdgesCW[1] = edgeT;
					}
				}
				else
				{
					// We want the edges ordered around the single base point properly
					if (Geometry.ICcw(EdgesCW[0].VtxStart.Pt,
						((FortuneEdge)EdgesCW[0]).PolyOrderingTestPoint,
						((FortuneEdge)EdgesCW[1]).PolyOrderingTestPoint) > 0)
					{
						WeEdge edgeT = EdgesCW[0];
						EdgesCW[0] = EdgesCW[1];
						EdgesCW[1] = edgeT;
					}
				}
			}
			else
			{
				EdgesCW.Sort();
			}
		}

		/// <summary>
		/// Remove an edge
		/// </summary>
		/// <remarks>
		/// This really only makes much sense for zero length edges
		/// </remarks>
		/// <param name="edge">Edge to remove</param>
		internal void DetachEdge(FortuneEdge edge)
		{
			// Let's remove the zero length edge.  We do this by removing it's end vertex, reassigning the proper
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
				var edgeCheck = (FortuneEdge)EdgesCW[i];

				// If it's zero length
				if (edgeCheck.FZeroLength())
				{
					// Diagnostics
					Tracer.Trace(tv.ZeroLengthEdges, "Fixing zero length edge {0} for poly {1}", edgeCheck, this);

					// Remove the edge from both this polygon and the polygon "across" the zero length edge
					DetachEdge(edgeCheck);
					EdgesCW.Remove(edgeCheck);
					edgeCheck.OtherPoly(this).EdgesCW.Remove(edgeCheck);

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
				var poly1 = new FortunePoly(new PT(0, 0), 0);
				var poly2 = new FortunePoly(new PT(0, 2), 1);
				var poly3 = new FortunePoly(new PT(2, 0), 2);
				var poly4 = new FortunePoly(new PT(0, -2), 3);
				var poly5 = new FortunePoly(new PT(-2, 0), 4);
				var vtx1 = new FortuneVertex(new PT(1, 1));
				var vtx2 = new FortuneVertex(new PT(1, -1));
				var vtx3 = new FortuneVertex(new PT(-1, -1));
				var vtx4 = new FortuneVertex(new PT(-1, 1));
				FortuneVertex vtx5;
				var edge1 = new FortuneEdge();
				var edge2 = new FortuneEdge();
				var edge3 = new FortuneEdge();
				var edge4 = new FortuneEdge();
				edge2.SetPolys(poly1, poly2);
				edge3.SetPolys(poly1, poly3);
				edge4.SetPolys(poly1, poly4);
				edge1.SetPolys(poly1, poly5);
				edge1.VtxStart = vtx4;
				edge1.VtxEnd = vtx1;
				edge2.VtxStart = vtx1;
				edge2.VtxEnd = vtx2;
				edge3.VtxStart = vtx2;
				edge3.VtxEnd = vtx3;
				edge4.VtxStart = vtx3;
				edge4.VtxEnd = vtx4;

				var polyTest = new FortunePoly(new PT(0, 0), 0);
				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge2);
				polyTest.AddEdge(edge4);

				polyTest.SortEdges();
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));

				vtx1.Pt = new PT(3, 4);
				vtx2.Pt = new PT(4, 3);
				vtx3.Pt = new PT(-1, -2);
				vtx4.Pt = new PT(-2, -1);
				polyTest.EdgesCW.Clear();

				polyTest.AddEdge(edge4);
				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge2);

				polyTest.SortEdges();
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));

				poly1.VoronoiPoint = new PT(10, 10);
				vtx1.Pt = new PT(13, 14);
				vtx2.Pt = new PT(14, 13);
				vtx3.Pt = new PT(9, 8);
				vtx4.Pt = new PT(8, 9);
				polyTest.EdgesCW.Clear();

				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge2);
				polyTest.AddEdge(edge4);

				polyTest.SortEdges();
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));

				poly1.VoronoiPoint = new PT(0, 0);
				vtx1 = FortuneVertex.InfiniteVertex(new PT(1, 2), true);
				vtx2.Pt = new PT(8, -1);
				vtx3.Pt = new PT(0, -3);
				vtx4.Pt = new PT(-8, -1);
				vtx5 = FortuneVertex.InfiniteVertex(new PT(-1, 2), true);
				edge1.VtxStart = vtx2;
				edge1.VtxEnd = vtx1;
				edge2.VtxStart = vtx2;
				edge2.VtxEnd = vtx3;
				edge3.VtxStart = vtx3;
				edge3.VtxEnd = vtx4;
				edge4.VtxStart = vtx4;
				edge4.VtxEnd = vtx5;
				polyTest.EdgesCW.Clear();

				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge2);
				polyTest.AddEdge(edge4);

				polyTest.SortEdges();
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[0], edge1));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[1], edge2));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[2], edge3));
				Assert.IsTrue(ReferenceEquals(polyTest.EdgesCW[3], edge4));
			}
		}
#endif
		#endregion
	}

	public class FortuneVertex : WeVertex
	{
		#region Private variables
		bool _fAlreadyOrdered;			// True after this vertex has had its edges ordered in post processing
		bool _fAddedToWingedEdge;		// True if this vertex has already been added to the winged edge data structure
		#endregion

		#region Constructors
		internal FortuneVertex(PT pt) : base(pt) { }
		internal FortuneVertex() { }
		#endregion

		#region Polygons
		/// <summary>
		/// Finds the third polygon which created this vertex besides the two on each side of
		/// the passed in edge
		/// </summary>
		/// <param name="edge">Edge in question</param>
		/// <returns>The polygon "opposite" this edge</returns>
		internal FortunePoly PolyThird(FortuneEdge edge)
		{
			int i1 = edge.Poly1.Index;
			int i2 = edge.Poly2.Index;

			foreach (FortuneEdge edgeDifferent in Edges)
			{
				if (edgeDifferent != edge)
				{
					var i1Diff = edgeDifferent.Poly1.Index;

					if (i1Diff == i1 || i1Diff == i2)
					{
						return edgeDifferent.Poly2;
					}
					return edgeDifferent.Poly1;
				}
			}
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
		/// <summary>
		/// Return the point at the other end of the given edge.  If the opposite point is
		/// a point at infinity, a "real" point on that edge is created and returned.
		/// </summary>
		/// <param name="edge">Edge to traverse</param>
		/// <returns>The point at the opposite end of the edge</returns>
		private PT PtAtOtherEnd(FortuneEdge edge)
		{
			PT ptRet = VtxOtherEnd(edge).Pt;
			if (edge.VtxEnd.FAtInfinity)
			{
				ptRet = new PT(Pt.X + ptRet.X, Pt.Y + ptRet.Y);
			}
			return ptRet;
		}

		/// <summary>
		/// Reset the ordered flag so we get ordered in the next call to Order()
		/// </summary>
		internal void ResetOrderedFlag()
		{
			_fAlreadyOrdered = false;
		}

		/// <summary>
		/// Delegate to compare edges around this vertex
		/// </summary>
		/// <param name="e1"></param>
		/// <param name="e2"></param>
		/// <returns></returns>
		int CompareEdges(WeEdge e1, WeEdge e2)
		{
			var fe1 = (FortuneEdge)e1;
			var fe2 = (FortuneEdge)e2;
			return Geometry.ICompareCw(Pt, fe1.PolyOrderingTestPoint, fe2.PolyOrderingTestPoint);
		}


		/// <summary>
		/// Order the three edges around this vertex
		/// </summary>
		internal void Order()
		{
			if (_fAlreadyOrdered || FAtInfinity)
			{
				return;
			}

			if (CtEdges == 3)
			{
				// Quick fix for the vastly more common case
				var pt0 = PtAtOtherEnd(Edges[0] as FortuneEdge);
				var pt1 = PtAtOtherEnd(Edges[1] as FortuneEdge);
				var pt2 = PtAtOtherEnd(Edges[2] as FortuneEdge);

				if (Geometry.ICcw(pt0, pt1, pt2) > 0)
				{
					var edge0 = Edges[0];
					Edges[0] = Edges[1];
					Edges[1] = edge0;
				}
				_fAlreadyOrdered = true;
			}
			else
			{
				Edges.Sort(CompareEdges); 
			}
		}
		#endregion

		#region Infinite Vertices
		/// <summary>
		/// Produce a vertex at infinity
		/// </summary>
		/// <param name="ptDirection">Direction for the vertex</param>
		/// <param name="fNormalize">If true we normalize, else not</param>
		/// <returns>The vertex at infinity</returns>
		internal static FortuneVertex InfiniteVertex(PT ptDirection, bool fNormalize)
		{
			var vtx = new FortuneVertex(ptDirection);
			vtx.SetInfinite(ptDirection, fNormalize);
			return vtx;
		}
		#endregion
	}
}

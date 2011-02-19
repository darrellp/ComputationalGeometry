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
			Bchl = new Beachline();
			QevEvents = new EventQueue();
			Polygons = new List<FortunePoly>();
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
				Polygons.Count, pt.X, pt.Y);

			// The count is being passed in only as a unique identifier for this point.
			var poly = new FortunePoly(pt, Polygons.Count);
			Polygons.Add(poly);
			return poly;
		}
		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the list of polygons which make up the voronoi diagram. </summary>
		///
		/// <value>	The polygons. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<FortunePoly> Polygons { get; private set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the event queue. </summary>
		///
		/// <value>	The event queue. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal EventQueue QevEvents { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	The beachline which is the primary data structure kept as we sweep 
		/// downward through the points </summary>
		///
		/// <value>	The bchl. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		internal Beachline Bchl { get; set; }

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
		/// If the callers only want a list of undifferentiated polygons, they can call Voronoi and
		/// get such a list.  This is fine for drawing and other things, but doesn't give any kind of
		/// relationship between elements in diagram.  If that is desired, this routine can be called
		/// to get a much richer Winged Edge data structure for the diagram.  It is a relatively expensive
		/// thing to compute which is why it's made separate from the raw voronoi diagram calculations.
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
			foreach (var poly in Polygons)
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

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Insert the edge at infinity into the edge list for the vertices at infinity. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="edge">				Edge at infinity being added. </param>
		/// <param name="leadingVtxCw">		Vertex on the left of infinite poly as we look out. </param>
		/// <param name="trailingVtxCw">	Vertex on the right. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void AddEdgeAtInfinityToVerticesAtInfinity(FortuneEdge edge, FortuneVertex leadingVtxCw, FortuneVertex trailingVtxCw)
		{
			// if we've only got one edge at infinity
			if (leadingVtxCw.Edges.Count == 1)
			{
				// Add this one as a placeholder
				//
				// This will be overwritten later but we have to insert here so that we can insert ourselves at
				// index 2.  This is just a placeholder.
				leadingVtxCw.Edges.Add(edge);
			}
			// Add this edge into it's proper position
			leadingVtxCw.Edges.Add(edge);

			// If we have three edges
			if (trailingVtxCw.Edges.Count == 3)
			{
				// Overwrite the placeholder we placed in another call
				//
				// Here is where the overwriting referred to above occurs
				// This will happen on a later call - not on the current one
				trailingVtxCw.Edges[1] = edge;
			}
			else
			{
				// Otherwise just add us in to the trailing vertex's list of edges
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

			// While there are events in the queue
			while (QevEvents.Count > 0)
			{
				// Get the next event
				//
				// Events are pulled off in top to bottom, left to right, site event before
				// circle event order.  This ensures that events/sites at identical locations
				// will be pulled off one after the other which allows us to cull them.
				var evt = QevEvents.Pop();

				// If we've got a pair of identically placed events
				if (Geometry.FCloseEnough(evt.Pt, evtPrev.Pt))
				{
					// Locals
					var tpPrev = evtPrev.GetType();
					var tpCur = evt.GetType();

					// If the previous event was a site event
					if (tpPrev == typeof(SiteEvent))
					{
						// And the current one is also
						if (tpCur == typeof(SiteEvent))
						{
							// Skip identical site events.
							//
							// This handles cases where the same point is in the input data two or more times.
							continue;
						}
					}
					// Else if it's a circle event
					else if (tpCur == typeof(CircleEvent))
					{
						// Locals
						var cevt = evt as CircleEvent;
						var cevtPrev = evtPrev as CircleEvent;

						// If we have idneically placed circle events
						//
						// Identically placed circle events still have to be processed but we handle the
						// case specially.  The implication of identically placed circle events is that
						// we had four or more cocircular points which implies that the polygons
						// for those points come together to a point.  Since we only allow for vertices
						// of order three during voronoi processing, we create "zero length" edges for the
						// polygons which meet at that common point. These will be removed later in postprocessing.
						if (Geometry.FCloseEnough(cevt.VoronoiVertex, cevtPrev.VoronoiVertex))
						{
							// We're going to create a zero length edge.
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
			foreach(FortunePoly poly in Polygons)
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
						// Are both vertices infinite/null?
						//
						// Split doubly infinite edges into two singly infinite edges.  This only occurs in rare
						// cases where there are only two generators or all the generators are collinear so that
						// we end up with parallel infinite lines rather than infinite rays.  We can't handle
						// true infinite lines in our winged edge data structure, so we turn the infinite lines into
						// two rays pointing in opposite directions and originating at the midpoint of the two generators.
						if (edge.VtxEnd == null && edge.VtxStart == null)
						{
							// Split the doubly infinite edge
							SplitDoublyInfiniteEdge(edge);

							// Can't continue or C# will complain
							//
							// If we continue then we'll be messing with the collection in the foreach
							// statement which C# will complain about.  It's okay because to break here
							// because there can be at most two doubly infinite edges per generator and
							// if there's a second it will be handled by the generator on the other side
							// of that edge.

							break;
						}
						// Singly infinite edges get turned into rays with one point at infinity
						ProcessRay(edge);
					}
				}
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Turn a null endpoint of the ray into an infinite vertex. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	Edge with the null vertex. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void ProcessRay(FortuneEdge edge)
		{
			// If it's the start vertex that's null
			if (edge.VtxStart == null)
			{
				// Swap the vertices
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

			// Do we need to reverse orientation?
			if (fProposedOnLeft == fThirdOnLeft)
			{
				// Do it
				ptProposedDirection.X = -ptProposedDirection.X;
				ptProposedDirection.Y = -ptProposedDirection.Y;
			}

			// Create the new infinite vertex and add it to our edge
			edge.VtxEnd = FortuneVertex.InfiniteVertex(ptProposedDirection, true);
			edge.VtxEnd.Edges.Add(edge);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Splits a doubly infinite edge. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	Edge we need to split. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void SplitDoublyInfiniteEdge(FortuneEdge edge)
		{
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
				Assert.AreEqual(3, Example().Polygons.Count);
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

	////////////////////////////////////////////////////////////////////////////////////////////////////
}

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
using System.Drawing;
using System.IO;
using System.Text;
using System;
using NUnit.Framework;
using NetTrace;

namespace DAP.CompGeom
{
	/// <summary>
	/// Fortune implements the Fortune algorithm for Voronoi diagrams.
	/// </summary>
	/// <remarks>
	/// When run initially, Fortune.Voronoi() returns
	/// a list of polytons that make up the diagram.  These can be transformed into a fully blown
	/// winged edge data structure if desired.  It's input is simply a list of points to calculate the
	/// diagram for.  It is based on the description of the algorithm given in "Computational Geometry -
	/// Algorithms and Applications" by M. de Berg et al. with a LOT of details filled in.  Some
	/// differences between the book's solution and mine:
	/// 
	/// The book suggests a doubly connected edge list.  I prefer a winged edge data structure.  Internally
	/// I use a structure which is a primitive winged edge structure which contains polygons, edges and
	/// vertices but not much of the redundancy available in a fully fleshed out winged edge, but
	/// optionally allow for a conversion to a fully fleshed out winged edge data structure.  This conversion
	/// is a bit expensive and may not be necessary (for instance, it suffices to merely draw the diagram) so
	/// is made optional.
	/// 
	/// Another big difference between this algorithm and the one there is the handling of polygons which extend to
	/// infinity.  These don't necessarily fit into winged edge which requires a cycle of edges on each
	/// polygon and another polygon on the opposite site of each edge.  The book suggests surrounding the
	/// diagram with a rectangle to solve this problem.  Outside of the fact that I still don't see how
	/// that truly solves the problem (what's on the other side of the edges of the rectangle?), I hate the
	/// solution which introduces a bunch of spurious elements which have nothing to do with the diagram.
	/// I solve it by using a "polygon at infinity" and a bunch of "sides at infinity".  The inspiration
	/// for all this is projective geometry which has a "point at infinity".  This maintains the winged edge
	/// structure with the introduction of these "at infinity" elements which are natural extensions of
	/// the diagram rather than the ugly introduced rectangle suggested in the book.  They also allow easy
	/// reference to these extended polygons.  For instance, to enumerate them, just step off all the polygons
	/// which "surround" the polygon at infinity.
	/// 
	/// I take care of a lot of bookkeeping details and border cases not mentioned in the book - zero length
	/// edges, collinear points, and many other minor to major points not specifically covered in the book.
	/// </remarks>
	public class Fortune
	{
		#region Private Variables
		Beachline _bchl = new Beachline();						// The beachline which is the primary data structure
																// kept as we sweep downward throught the points
		EventQueue _qevEvents = new EventQueue();				// Priority queue for events which occur during the sweep
		List<FortunePoly> _lstPolys = new List<FortunePoly>();	// The list of Polygons which are the output of the algorithm
		#endregion

		#region Constructor
		/// <summary>
		/// Add all points to the event queue as site events
		/// </summary>
		/// <param name="points">Points whose Voronoi diagram will be calculated</param>
		public Fortune(IEnumerable points)
		{
			foreach (PT pt in points)
			{
				QevEvents.Add(new SiteEvent(InsertPoly(pt)));
			}
		}
		
		/// <summary>
		/// Each polygon in the final solution is associated with a point in the input.  We initialize
		/// that polygon for this point here.
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		FortunePoly InsertPoly(PT pt)
		{
			Tracer.Trace(tv.GeneratorList, "Generator {0}: ({1}, {2})",
				_lstPolys.Count, pt.X, pt.Y);

			// The count is being passed in only as a unique identifier for this point.
			FortunePoly poly = new FortunePoly(pt, _lstPolys.Count);
			_lstPolys.Add(poly);
			return poly;
		}
		#endregion

		#region Properties
		public List<FortunePoly> PolyList
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
		public void Voronoi()
		{
			// The algorithm works by a sweepline technique.  As the sweekpline moves down
			// through the points, events are generated and placed in a priority queue.  These
			// events are removed from the queue and are in turn used to advance the sweep line.
			ProcessEvents();

			// Tie up loose ends after all the processing
			Finish();
		}

		/// <summary>
		/// Return the Winged edge structure for the voronoi diagram.  This is a complicated procedure
		/// with a lot of details to be taken care of as follows.
		/// 
		/// Every polygon has to have its edges sorted in clockwise order.
		/// 
		/// Set all the successor edges to each edge and the list of edges to each vertex.
		/// 
		/// Zero length edges are elminated and their "endpoints" are consolidated.
		/// 
		/// The polygon at infinity and all the edges at infinity must be added
		/// 
		/// NOTE ON THE POLYGON AT INFINITY
		/// In the WingedEdge structure each polygon has a list of edges with another
		/// polygon on the other side of each edge.  This poses a bit of a difficulty for
		/// the polygons in a Voronoi diagram whose edges are rays to infinity.  We'll call
		/// these "infinite polygons" for convenience.  The solution we use in our
		/// WingedEdge data structure is to produce a single "polygon at infinity" (not to
		/// be confused with the infinite polygons previously mentioned) and a series of
		/// edges at infinity which "separate" the infinite polygons from the polygon at
		/// infinity.  These edges have to be ordered in the same order as the infinite
		/// polygons around the border.  All of this is done in AddEdgeAtInfinity().
		/// </summary>
		/// <returns>The winged edge structure for the diagram</returns>
		public WingedEdge BuildWingedEdge()
		{
			/// The first infinite polygon we locate and the index for the infinite
			/// edge ccw to that polygon
			FortunePoly polyInfinityStart = null;
			int iLeadingInfiniteEdgeCw = -1;

			WingedEdge we = new WingedEdge();

			foreach (FortunePoly poly in _lstPolys)
			{
				Tracer.Trace(tv.FinalEdges, "Edges for generator {0}", poly.Index);
				Tracer.Indent();

				we.AddPoly(poly);
				poly.SortEdges();

				for (int iEdge = 0; iEdge < poly.VertexCount; iEdge++)
				{
					FortuneEdge edge = poly.EdgesCW[iEdge] as FortuneEdge;
					int iEdgeNext = (iEdge + 1) % poly.VertexCount;
					FortuneEdge edgeNextCW = poly.EdgesCW[iEdgeNext] as FortuneEdge;

					Tracer.Trace(tv.FinalEdges, edge.ToString());

					edge.Process(poly, we);

					if (polyInfinityStart == null && edge.VtxEnd.FAtInfinity && edgeNextCW.VtxEnd.FAtInfinity)
					{
						// If this is the first infinite polygon we've seen, make a note of it.
						// iLeadingInfiniteEdgeCw is the edge on the "left" as we look "out" from
						// the infinite polygon
						iLeadingInfiniteEdgeCw = iEdge;
						polyInfinityStart = poly;
					}
				}
				poly.HandleZeroLengthEdges();
				Tracer.Unindent();
			}

			if (polyInfinityStart != null && polyInfinityStart.FZeroLengthEdge)
			{
				// If there were zero length edges in polyInfinityStart then the edge indices may
				// have gotten shifted around due to the removal of the zero length edges so
				// relocate it...
				
				// If there are only two edges, they'll both be at infinity and we can't just use their
				// indices to distinguish - we have to actually check the angles
				if (polyInfinityStart.VertexCount == 2)
				{
					Tracer.Assert(t.Assertion,
						polyInfinityStart.EdgesCW[0].VtxEnd.FAtInfinity && polyInfinityStart.EdgesCW[1].VtxEnd.FAtInfinity,
						"Two edged polygon without both edges at infinity");
					if (Geometry.ICcw(new PT(0, 0), polyInfinityStart.EdgesCW[0].VtxEnd.Pt, polyInfinityStart.EdgesCW[1].VtxEnd.Pt) > 0)
					{
						iLeadingInfiniteEdgeCw = 1;
					}
					else
					{
						iLeadingInfiniteEdgeCw = 0;
					}
				}
				else
				{
					for (int iEdge = 0; iEdge < polyInfinityStart.VertexCount; iEdge++)
					{
						FortuneEdge edge = polyInfinityStart.EdgesCW[iEdge] as FortuneEdge;
						int iEdgeNext = (iEdge + 1) % polyInfinityStart.VertexCount;
						FortuneEdge edgeNextCW = polyInfinityStart.EdgesCW[iEdgeNext] as FortuneEdge;
						if (edge.VtxEnd.FAtInfinity && edgeNextCW.VtxEnd.FAtInfinity)
						{
							iLeadingInfiniteEdgeCw = iEdgeNext;
							break;
						}
					}
				}
			}

			AddEdgesAtInfinity(we, polyInfinityStart, iLeadingInfiniteEdgeCw);

#if NETTRACE || DEBUG
			if (Tracer.FTracing(t.WeValidate))
			{
				Tracer.Assert(t.WeValidate, we.Validate(), "Invalid Winged edge");
			}
#endif

			return we;
		}
		#endregion

		#region Polygon and edges at infinity

		/// <summary>
		/// Set up the polygon at infinity.  The main difficulty here consists in traversing
		/// around the infinite polygons at the edge of the diagram in order.
		/// </summary>
		/// <param name="we">WingedEdge structure we'll add the polygon at infinity to</param>
		/// <param name="polyStart">Infinite polygon to start it's polygon list with</param>
		/// <param name="iLeadingEdgeCw">Starting infinite edge</param>
		private void AddEdgesAtInfinity(WingedEdge we, FortunePoly polyStart, int iLeadingEdgeCw)
		{
			if (polyStart == null)
			{
				return;
			}

			FortunePoly polyCur = polyStart;
			FortunePoly polyNext;
			int iLeadingEdgeNext;
			// The infamous polygon at infinity...
			FortunePoly polyAtInfinity = new FortunePoly(new PT(0, 0), -1);
			FortuneEdge edgePreviousAtInfinity = null;

			polyAtInfinity.FAtInfinity = true;
			we.AddPoly(polyAtInfinity);
  
			/// Loop through the "outer" infinite polygons of the diagram, setting each of them up
			/// with an edge at infinity to separate them from the polygon at infinity
			do
			{
				edgePreviousAtInfinity = AddEdgeAtInfinity(
					polyAtInfinity,
					polyCur,
					iLeadingEdgeCw,
					edgePreviousAtInfinity,
					out polyNext,
					out iLeadingEdgeNext);
				we.AddEdge(edgePreviousAtInfinity);
				polyCur = polyNext;
				iLeadingEdgeCw = iLeadingEdgeNext;
			} while (polyCur != polyStart);

			/// Thread the last poly back to the first
			WeEdge EdgeFirstAtInfinity = polyCur.EdgesCW[iLeadingEdgeNext];
			edgePreviousAtInfinity.EdgeCCWPredecessor = EdgeFirstAtInfinity;
			EdgeFirstAtInfinity.EdgeCWSuccessor = edgePreviousAtInfinity;
		}

		/// <summary>
		/// Add an edge at infinity and step the polygon and edge along to the next
		/// infinite polygon and rayed edge.
		/// </summary>
		/// <param name="polyAtInfinity">Polygon at infinity</param>
		/// <param name="poly">Infinite polygon we're adding the edge to</param>
		/// <param name="iLeadingEdgeCw">index to rayed edge  we start with</param>
		/// <param name="edgePreviousAtInfinity">Edge at infinity we added in the previous infinite polygon</param>
		/// <param name="polyNextCcw">Returns the next infinite polygon to be processed</param>
		/// <param name="iLeadingEdgeNext">Returns the next infinite edge</param>
		/// <returns></returns>
		private FortuneEdge AddEdgeAtInfinity(
			FortunePoly polyAtInfinity,
			FortunePoly poly,
			int iLeadingEdgeCw,
			FortuneEdge edgePreviousAtInfinity, 
			out FortunePoly polyNextCcw,
			out int iLeadingEdgeNext)
		{
			int iTrailingEdgeCw = (iLeadingEdgeCw + 1) % poly.VertexCount;

			FortuneEdge edgeLeadingCw = poly.EdgesCW[iLeadingEdgeCw] as FortuneEdge;
			FortuneEdge edgeTrailingCw = poly.EdgesCW[iTrailingEdgeCw] as FortuneEdge;

			// Next polygon in order is to the left of our leading edge
			polyNextCcw = edgeLeadingCw.PolyLeft as FortunePoly;
			// polyNextCcw = edgeTrailingCw.PolyLeft as FortunePoly;
			Tracer.Assert(t.Assertion, polyNextCcw.Index != poly.Index,
				"Next polygon in AddEdgeAtInfinity is the same as the current poly");

			// Create the edge at infinity separating the current infinite polygon from
			// the polygon at infinity.  The vertices for this edge will both be vertices
			// at infinity.  This, of course, doesn't really have any real impact on the
			// "position" of the edge at infinity, but allows us to maintain a properly
			// set up winged edge structure.

			FortuneEdge edgeAtInfinity = new FortuneEdge();
			// The poly at infinity is to the left of the edge, the infinite poly is to its right
			edgeAtInfinity.PolyRight = poly;
			edgeAtInfinity.PolyLeft = polyAtInfinity;
			// Start and end vertices are the trailing and leading infinite edges
			edgeAtInfinity.VtxStart = edgeLeadingCw.VtxEnd;
			edgeAtInfinity.VtxEnd = edgeTrailingCw.VtxEnd;
			// Add the edge at infinity to the poly at infinity and the current infinite poly
			polyAtInfinity.AddEdge(edgeAtInfinity);
			poly.EdgesCW.Insert(iTrailingEdgeCw, edgeAtInfinity);
			// Set up the wings of the wingedEdge
			edgeAtInfinity.EdgeCWPredecessor = edgeLeadingCw;
			edgeAtInfinity.EdgeCCWSuccessor = edgeTrailingCw;
			edgeLeadingCw.EdgeCCWSuccessor = edgeAtInfinity;
			edgeTrailingCw.EdgeCWSuccessor = edgeAtInfinity;
			if (edgePreviousAtInfinity != null)
			{
				edgePreviousAtInfinity.EdgeCCWPredecessor = edgeAtInfinity;
				edgeAtInfinity.EdgeCWSuccessor = edgePreviousAtInfinity;
			}
			AddEdgeAtInfinityToVerticesAtInfinity(
				edgeAtInfinity,
				edgeAtInfinity.VtxStart as FortuneVertex,
				edgeAtInfinity.VtxEnd as FortuneVertex);

			// Locate the leading edge index in the next polygon
			for (iLeadingEdgeNext = 0; iLeadingEdgeNext < polyNextCcw.VertexCount; iLeadingEdgeNext++)
			{
				if (polyNextCcw.EdgesCW[iLeadingEdgeNext] == edgeLeadingCw)
				{
					iLeadingEdgeNext = (polyNextCcw.VertexCount + iLeadingEdgeNext - 1) % polyNextCcw.VertexCount;
					break;
				}
			}
			return edgeAtInfinity;
		}

		/// <summary>
		/// Insert the edge at infinity into the edge list for the vertices at infinty
		/// </summary>
		/// <param name="edge">Edge at infinity being added</param>
		/// <param name="LeadingVtxCw">Vertex on the left of infinite poly as we look out</param>
		/// <param name="TrailingVtxCw">Vertex on the right</param>
		private void AddEdgeAtInfinityToVerticesAtInfinity(FortuneEdge edge, FortuneVertex LeadingVtxCw, FortuneVertex TrailingVtxCw)
		{
			if (LeadingVtxCw.Edges.Count == 1)
			{
				// This will be overwritten later but we have to insert here so that we can insert ourselves at
				// index 2...
				LeadingVtxCw.Edges.Add(edge);
			}
			LeadingVtxCw.Edges.Add(edge);
			if (TrailingVtxCw.Edges.Count == 3)
			{
				// Here is where the overwriting referred to above occurs
				TrailingVtxCw.Edges[1] = edge;
			}
			else
			{
				TrailingVtxCw.Edges.Add(edge);
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
						CircleEvent cevt = evt as CircleEvent;
						CircleEvent cevtPrev = evtPrev as CircleEvent;

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

		/// <summary>
		/// In the course of processing, edges are created initially without endpoints.  The endpoints are filled in
		/// during the course of processing.  When we finish processing, any missing endpoints represent "points at
		/// infinity" where the edge is a ray or (rarely) an infinitely extended line.  We have to go through and fix
		/// up any of these loose ends to turn them into points at infinity.
		/// </summary>
		private void FixInfiniteEdges()
		{
			foreach(FortunePoly poly in _lstPolys)
			{
				// Handle infinite edges.  Ensure that singly infinite edges have the infinite vertex in the
				// VtxEnd position and split doubly infinite edges into two singly infinite edges.  Replace
				// the null vertices with the proper infinite vertices.
				foreach (WeEdge wedge in poly.EdgesCW)
				{
					FortuneEdge edge = wedge as FortuneEdge;

					Tracer.Assert(t.Assertion, edge != null, "Non-FortuneEdge in FortunePoly list");
					if (edge.VtxStart == null || edge.VtxEnd == null)
					{
						if (edge.VtxEnd == null && edge.VtxStart == null)
						{
							// Split a doubly infinite edge into two singly infinite edges.  This only occurs in rare
							// cases where there are only two generators or all the generators are collinear so that
							// we end up with parallel infinite lines rather than infinite rays.  We can't handle
							// true infinite lines in our winged edge data structure, so we turn the infinite lines into
							// two rays pointing in opposite directions and originating at the midpoint of the two generators.

							PT pt1 = edge.Poly1.Pt;
							PT pt2 = edge.Poly2.Pt;
							TPT dx = pt2.X - pt1.X;
							TPT dy = pt2.Y - pt1.Y;
							PT ptMid = new PT((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);
							FortuneVertex vtx1, vtx2;

							vtx1 = FortuneVertex.InfiniteVertex(new PT(-dy, dx), true);
							vtx2 = FortuneVertex.InfiniteVertex(new PT(dy, -dx), true);

							edge.VtxStart = new FortuneVertex(ptMid);
							edge.VtxEnd = vtx1;
							FortuneEdge edgeNew = new FortuneEdge();
							edgeNew.VtxStart = edge.VtxStart;
							edgeNew.VtxEnd = vtx2;
							edgeNew.SetPolys(edge.Poly1, edge.Poly2);
							edge.Poly1.AddEdge(edgeNew);
							edge.Poly2.AddEdge(edgeNew);
							edge.VtxStart.Add(edge);
							edge.VtxStart.Add(edgeNew);
							vtx1.Add(edge);
							vtx2.Add(edgeNew);
							edge.FSplit = edgeNew.FSplit = true;

							if (dx == 0 || dx * dy > 0) // dy == 0 case needs to fall through...
							{
								edge.PolyRight = edgeNew.PolyLeft = edge.Poly1;
								edge.PolyLeft = edgeNew.PolyRight = edge.Poly2;
							}
							else
							{
								edge.PolyLeft = edgeNew.PolyRight = edge.Poly1;
								edge.PolyRight = edgeNew.PolyLeft = edge.Poly2;
							}

							Tracer.Trace(tv.FinalEdges, "Edge split into {0} and {1}", edge, edgeNew);

							// Can't continue or C# will complain that we're messing with the collection
							// in the foreach statement.  It's okay because there can be at most two doubly
							// infinite edges per generator and if there's a second it will be handled by
							// the generator on the other side of that edge.
							break;
						}
						else
						{
							// Singly infinite edges get turned into rays with one point at infinity
							if (edge.VtxStart == null)
							{
								// Swap if necessary to ensure that the infinite vertex is in the VtxEnd position

								edge.VtxStart = edge.VtxEnd;
								edge.VtxEnd = null;
							}

							// Replace the null vertex with an infinite vertex

							PT pt1 = edge.Poly1.Pt;
							PT pt2 = edge.Poly2.Pt;
							PT ptMid = new PT((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);

							// We have to be careful to get this ray pointed in the proper orientation.  We find
							// the third polygon at the "base" of this ray and point the ray "away" from it.
							FortunePoly polyThird = ((FortuneVertex)edge.VtxStart).PolyThird(edge);
							bool fThirdOnLeft = Geometry.FLeft(pt1, pt2, polyThird.Pt);
							TPT dx = pt2.X - pt1.X;
							TPT dy = pt2.Y - pt1.Y;
							PT ptProposedDirection = new PT(dy, -dx);
							PT ptInProposedDirection = new PT(ptMid.X + dy, ptMid.Y - dx);
							bool fProposedOnLeft = Geometry.FLeft(pt1, pt2, ptInProposedDirection);

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
			Fortune Example()
			{
				PT[] pts = new PT[] {
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

	internal class EventQueue : PQWithDeletions<FortuneEvent>
	{
		#region Private Variables
		List<CircleEvent> _lstcevt = new List<CircleEvent>();		// List of circle events so we can keep track of them
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

	public class FortuneEdge : WeEdge
	{
		#region Private Variables
		bool _fSplit = false;							// True if this is really half of an infinite split edge
		bool _fStartVertexSet = false;					// True if the first vertex has already been set on this edge
		bool _fAddedToWingedEdge = false;				// True if we've already been added to a winged edge data structure
		FortunePoly[] _arPoly = new FortunePoly[2];		// The polygons on each side of us
		#endregion

		#region Properties
		internal FortunePoly Poly1 { get { return _arPoly[0]; } }
		internal FortunePoly Poly2 { get { return _arPoly[1]; } }

		public bool FSplit
		{
			get { return _fSplit; }
			set { _fSplit = value; }
		}

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
		internal PT PolyOrderingTestPoint
		{
			get
			{
				if (!VtxEnd.FAtInfinity)
				{
					return Geometry.MidPoint(VtxStart.Pt, VtxEnd.Pt);
				}
				else if (!VtxStart.FAtInfinity)
				{
					return new PT(
						VtxStart.Pt.X + VtxEnd.Pt.X,
						VtxStart.Pt.Y + VtxEnd.Pt.Y);
				}
				else
				{
					return Geometry.MidPoint(
						EdgeCCWSuccessor.VtxStart.Pt,
						EdgeCWPredecessor.VtxStart.Pt);
				}
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
			FortuneVertex vtx = (FortuneVertex)(fStartVertex ? VtxStart : VtxEnd);

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

			if (Object.ReferenceEquals(edge1.VtxStart, edge2.VtxStart))
			{
				fEdge1ConnectsAtStartVtx = true;
				fEdge2ConnectsAtStartVtx = true;
				fRet = true; ;
			}
			else if(Object.ReferenceEquals(edge1.VtxStart, edge2.VtxEnd))
			{
				fEdge1ConnectsAtStartVtx = true;
				fRet = true; ;
			}
			else if (Object.ReferenceEquals(edge1.VtxEnd, edge2.VtxStart))
			{
				fEdge2ConnectsAtStartVtx = true;
				fRet = true; ;
			}
			else if (Object.ReferenceEquals(edge1.VtxEnd, edge2.VtxEnd))
			{
				fRet = true; ;
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
				if (Object.ReferenceEquals(vtx.Edges[iEdge], this))
				{
					break;
				}
			}
			edgeCW = vtx.Edges[(iEdge + 1) % 3] as FortuneEdge;
			edgeCCW = vtx.Edges[(iEdge + 2) % 3] as FortuneEdge;
		}

		/// <summary>
		/// Place the poly on the proper side of this edge.  We use the generator of the poly to locate
		/// it properly WRT this edge.
		/// </summary>
		/// <param name="poly">Polygon to locate</param>
		internal void SetOrderedPoly(FortunePoly poly)
		{
			if (FLeftOf(poly.Pt))
			{
				PolyLeft = poly;
			}
			else
			{
				PolyRight = poly;
			}
		}

		/// <summary>
		/// Set up the "wings" for this edge - i.e., it's successor edges in both cw and ccw
		/// directions at both start and end vertices
		/// </summary>
		internal void SetSuccessorEdges()
		{
			WeEdge edgeCW, edgeCCW;

			if (VtxStart.Edges.Count == 2)
			{
				// Half Infinite Edge forming half of fully infinite edge
				WeEdge edgeTest = VtxStart.Edges[0];
				if (Object.ReferenceEquals(this, edgeTest))
				{
					EdgeCCWPredecessor = EdgeCWPredecessor = VtxStart.Edges[1];
				}
				else
				{
					EdgeCCWPredecessor = EdgeCWPredecessor = edgeTest;
				}
				return;
			}

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

		/// <summary>
		/// Add a poly to the edge and the edge to the winged edge data structure.
		/// </summary>
		/// <param name="poly">Polygon to add</param>
		/// <param name="we">Winged edge structure to add edge to</param>
		internal void Process(FortunePoly poly, WingedEdge we)
		{
			// Put the poly properly to the left or right of this edge
			SetOrderedPoly(poly);

			// We'll attempt to add this edge twice, once from the polygon on each side of
			// it.  Only do this the first time.
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
			if (Object.ReferenceEquals(Poly1, edge.Poly1) || Object.ReferenceEquals(Poly1, edge.Poly2))
			{
				return Poly1;
			}
			Tracer.Assert(t.Assertion,
				Object.ReferenceEquals(Poly2, edge.Poly1) || Object.ReferenceEquals(Poly2, edge.Poly2),
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
			FortuneEdge edge = edgeIn as FortuneEdge;

			Tracer.Assert(t.Assertion, edge != null, "Non-fortuneEdge passed to fortuneEdge compare");

			if (Object.ReferenceEquals(edgeIn, this))
			{
				return 0;
			}

			return Geometry.ICompareCw(PolyCommon(edge).Pt, PolyOrderingTestPoint, edge.PolyOrderingTestPoint);
		}
		#endregion
	}

	public class FortunePoly : WePolygon
	{
		#region Private Variables
		int _iGen = -1;						// Index for this polygon in the polygon list of the Fortune class 
		PT _pt;								// The generator for this polygon
		bool _fAtInfinity = false;			// True if this is the polygon at infinity
		bool _fZeroLengthEdge = false;		// True if there is a zero length edge in this polygon
		#endregion

		#region Properties
		public bool FAtInfinity
		{
			get { return _fAtInfinity; }
			set { _fAtInfinity = value; }
		}

		public bool FZeroLengthEdge
		{
			get { return _fZeroLengthEdge; }
			set { _fZeroLengthEdge = value; }
		}

		public PT Pt
		{
			get { return _pt; }
			set { _pt = value; }
		}

		public int Index
		{
			get { return _iGen; }
			set { _iGen = value; }
		}
		#endregion

		#region Constructor
		internal FortunePoly(PT pt, int iGen)
		{
			_iGen = iGen;
			_pt = pt;
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
			if (LstCWEdges.Count == 2)
			{
				if (((FortuneEdge)LstCWEdges[0]).FSplit)
				{
					if (Geometry.ICcw(
						((FortuneEdge)LstCWEdges[0]).PolyOrderingTestPoint,
						((FortuneEdge)LstCWEdges[1]).PolyOrderingTestPoint,
						Pt) < 0)
					{
						WeEdge edgeT = LstCWEdges[0];
						LstCWEdges[0] = LstCWEdges[1];
						LstCWEdges[1] = edgeT;
					}
				}
				else
				{
					// We want the edges ordered around the single base point properly
					if (Geometry.ICcw(LstCWEdges[0].VtxStart.Pt,
						((FortuneEdge)LstCWEdges[0]).PolyOrderingTestPoint,
						((FortuneEdge)LstCWEdges[1]).PolyOrderingTestPoint) > 0)
					{
						WeEdge edgeT = LstCWEdges[0];
						LstCWEdges[0] = LstCWEdges[1];
						LstCWEdges[1] = edgeT;
					}
				}
			}
			else
			{
				LstCWEdges.Sort();
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

		/// <summary>
		/// Sort out zero length edge issues
		/// </summary>
		/// <remarks>
		/// Zero length edges are a pain and have to be dealt with specially since they don't sort
		/// properly using normal geometrical position nor do can "sidedness" be determined solely from
		/// their geometry (a zero length line has no "sides").  Instead, we have to look at the non-zero
		/// length edges around them and determine this information by extrapolating from those edges
		/// topological connection to this edge.
		/// </remarks>
		internal void HandleZeroLengthEdges()
		{
			if (!FZeroLengthEdge)
			{
				return;
			}

			for (int i = 0; i < VertexCount; i++)
			{
				FortuneEdge edgeCheck = (FortuneEdge)EdgesCW[i];
				if (edgeCheck.FZeroLength())
				{
					Tracer.Trace(tv.ZeroLengthEdges, "Fixing zero length edge {0} for poly {1}", edgeCheck, this);
#if NEW
					DetachEdge(edgeCheck);
					EdgesCW.Remove(edgeCheck);
					edgeCheck.OtherPoly(this).EdgesCW.Remove(edgeCheck);
					// We have to back up one because we deleted edge i
					i--;
#else
					FortuneEdge edgePrev = EdgesCW[(i + VertexCount - 1) % VertexCount] as FortuneEdge;
					bool fPrevStart,					// If true then the start vertex of the prev edge connects, else end vertex connects
						fCurStart;						// If true then the start vertex of this edge connects, else end vertex connects

					// See which vertices connect the two edges
					bool fConnects = FortuneEdge.FConnectsTo(edgePrev, (FortuneEdge)EdgesCW[i], out fPrevStart, out fCurStart);
					// True if we are to the left side of the previous edge
					bool fLeftToPrev = edgePrev.PolyLeft == this;

					Tracer.Assert(t.Assertion, fConnects, "Zero length edge doesn't connect to its neighbors");

					if (fPrevStart == fCurStart)
					{
						if (fLeftToPrev)
						{
							EdgesCW[i].PolyRight = this;
						}
						else
						{
							EdgesCW[i].PolyLeft = this;
						}
					}
					else
					{
						if (fLeftToPrev)
						{
							EdgesCW[i].PolyLeft = this;
						}
						else
						{
							EdgesCW[i].PolyRight = this;
						}
					}
#endif
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
				FortunePoly poly1 = new FortunePoly(new PT(0, 0), 0);
				FortunePoly poly2 = new FortunePoly(new PT(0, 2), 1);
				FortunePoly poly3 = new FortunePoly(new PT(2, 0), 2);
				FortunePoly poly4 = new FortunePoly(new PT(0, -2), 3);
				FortunePoly poly5 = new FortunePoly(new PT(-2, 0), 4);
				FortuneVertex vtx1 = new FortuneVertex(new PT(1, 1));
				FortuneVertex vtx2 = new FortuneVertex(new PT(1, -1));
				FortuneVertex vtx3 = new FortuneVertex(new PT(-1, -1));
				FortuneVertex vtx4 = new FortuneVertex(new PT(-1, 1));
				FortuneVertex vtx5;
				FortuneEdge edge1 = new FortuneEdge();
				FortuneEdge edge2 = new FortuneEdge();
				FortuneEdge edge3 = new FortuneEdge();
				FortuneEdge edge4 = new FortuneEdge();
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

				FortunePoly polyTest = new FortunePoly(new PT(0, 0), 0);
				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge2);
				polyTest.AddEdge(edge4);

				polyTest.SortEdges();
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[0], edge1));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[1], edge2));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[2], edge3));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[3], edge4));

				vtx1.Pt = new PT(3, 4);
				vtx2.Pt = new PT(4, 3);
				vtx3.Pt = new PT(-1, -2);
				vtx4.Pt = new PT(-2, -1);
				polyTest.LstCWEdges.Clear();

				polyTest.AddEdge(edge4);
				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge2);

				polyTest.SortEdges();
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[0], edge1));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[1], edge2));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[2], edge3));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[3], edge4));

				poly1.Pt = new PT(10, 10);
				vtx1.Pt = new PT(13, 14);
				vtx2.Pt = new PT(14, 13);
				vtx3.Pt = new PT(9, 8);
				vtx4.Pt = new PT(8, 9);
				polyTest.LstCWEdges.Clear();

				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge2);
				polyTest.AddEdge(edge4);

				polyTest.SortEdges();
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[0], edge1));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[1], edge2));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[2], edge3));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[3], edge4));

				poly1.Pt = new PT(0, 0);
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
				polyTest.LstCWEdges.Clear();

				polyTest.AddEdge(edge3);
				polyTest.AddEdge(edge1);
				polyTest.AddEdge(edge2);
				polyTest.AddEdge(edge4);

				polyTest.SortEdges();
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[0], edge1));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[1], edge2));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[2], edge3));
				Assert.IsTrue(Object.ReferenceEquals(polyTest.LstCWEdges[3], edge4));
			}
		}
#endif
		#endregion
	}

	public class FortuneVertex : WeVertex
	{
		#region Private variables
		bool _fAlreadyOrdered = false;			// True after this vertex has had its edges ordered in post processing
		bool _fAddedToWingedEdge = false;		// True if this vertex has already been added to the winged edge data structure
		#endregion

		#region Constructors
		internal FortuneVertex(PT pt) : base(pt) { }
		internal FortuneVertex() : base() { }
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
					int i1Diff = edgeDifferent.Poly1.Index;
					int i2Diff = edgeDifferent.Poly2.Index;

					if (i1Diff == i1 || i1Diff == i2)
					{
						return edgeDifferent.Poly2;
					}
					else
					{
						return edgeDifferent.Poly1;
					}
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
			FortuneEdge fe1 = (FortuneEdge)e1;
			FortuneEdge fe2 = (FortuneEdge)e2;
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
				PT pt0 = PtAtOtherEnd(Edges[0] as FortuneEdge);
				PT pt1 = PtAtOtherEnd(Edges[1] as FortuneEdge);
				PT pt2 = PtAtOtherEnd(Edges[2] as FortuneEdge);

				if (Geometry.ICcw(pt0, pt1, pt2) > 0)
				{
					WeEdge edge0 = Edges[0];
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
			FortuneVertex vtx = new FortuneVertex(ptDirection);
			vtx.SetInfinite(ptDirection, fNormalize);
			return vtx;
		}
		#endregion
	}
}

#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System;
using System.Diagnostics;
using NetTrace;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	
	/// The beachline is the union of all the parabolas formed from all sites.  This is actually
	/// maintained as a height balanced tree as suggested in the book by de Berg, et al. 
	/// </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	internal class Beachline
	{
		#region Constructor

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Default constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public Beachline()
		{
			NdRoot = null;
		}

		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or the root node of the beachline tree. </summary>
		///
		/// <value>	The root node of the beachline tree. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal Node NdRoot { get; private set; }

		#endregion

		#region Search

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Find the leaf node representing the parabola on the beachline which lays at a specified X
		/// coordinate.  Does a binary search on the parabolas to locate the parabola in question. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <exception cref="InvalidOperationException">	Thrown when there is no root node. </exception>
		///
		/// <param name="xSite">		The X coordinate to search for. </param>
		/// <param name="yScanLine">	Height of the scan line. </param>
		///
		/// <returns>	The leaf node for the parabola over xSite. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		LeafNode LfnSearch(TPT xSite, TPT yScanLine)
		{
			if (NdRoot == null)
			{
				throw new InvalidOperationException("Searching a null beachline");
			}

			return LfnSearchNode(NdRoot, xSite, yScanLine);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Do a binary search down the tree looking for the leaf node which covers the passed in X
		/// coordinate. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="nd">			Node to start search at. </param>
		/// <param name="xSite">		X coordinate. </param>
		/// <param name="yScanLine">	Where the scan line is at now. </param>
		///
		/// <returns>	Leaf node for parabola covering xSite. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static LeafNode LfnSearchNode(Node nd, TPT xSite, TPT yScanLine)
		{
			// Initialize
			LeafNode ndRet;

			while (true)
			{
				// Diagnostics
				Tracer.Trace(tv.Search, "Searching for x={0} with yScan={1} at {2}", xSite, yScanLine, nd.ToString());

				// If it's a leaf node, we've arrived
				if (nd.IsLeaf)
				{
					// The leaf node is our return
					ndRet = nd as LeafNode;
					break;
				}
				// It's an internal node
				// Internal nodes represent developing edges as the sweep line sweeps downward.  They've got pointers to the
				// polygons on each side of that line.  The place those two polygons meet is the place where two parabolas with
				// foci at the voronoi input points and directrix at the current sweep line meet.  This is pure geometry and is
				// determined in CurrentEdgeXPos below.

				// Determine the break point on the beach line 
				var edgeXPos = ((InternalNode)nd).CurrentEdgeXPos(yScanLine);

				// Search the side of the break point that xSite is on
				Tracer.Trace(tv.Search, "Current edge X pos = {0}", edgeXPos);
				nd = edgeXPos < xSite ? nd.RightChild : nd.LeftChild;
			}

			// Return the node we located
			Tracer.Trace(tv.Search, "Located node at {0}", ndRet.ToString());
			return ndRet;
		}
		#endregion

		#region Deletion

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Remove a parabola node from the beachline since it's being squeezed out and insert a vertex
		/// into the voronoi diagram. 
		/// </summary>
		///
		/// <remarks>	
		/// This happens when a circle event occurs.  It's a rather delicate operation. From the point of
		/// view of the voronoi diagram, we have two edges from above coming together into the newly
		/// created vertex and a new edge created which descends below it.  This is really where the meat
		/// of actually creating the voronoi diagram occurs.  One of the important details which seems to
		/// be left totally out of the book is the importance of keeping accurate left and right sibling
		/// pointers on the leaf nodes.  Since each leaf node represents a parabola in the beachline,
		/// these pointers represent the set of parabolas from the left to the right of the beachline.
		/// In a case like this where a parabola is being squeezed out, it's left and right siblings will
		/// not butt up against each other forming a new edge and we need to be able to locate both these
		/// nodes in order to make everything come out right.
		/// 
		/// This is very persnickety code.
		/// </remarks>
		///
		/// <param name="cevt">				Circle event which caused this. </param>
		/// <param name="lfnEliminated">	Leaf node for the parabola being eliminated. </param>
		/// <param name="voronoiVertex">	The new vertex to be inserted into the voronoi diagram. </param>
		/// <param name="evq">				Event queue. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void RemoveNodeAndInsertVertex(CircleEvent cevt, LeafNode lfnEliminated, PT voronoiVertex, EventQueue evq)
		{
			// Initialize
			Tracer.Assert(t.Assertion, NdRoot != null, "Trying to delete from a null tree");
			var yScanLine = cevt.Pt.Y;
			Tracer.Trace(tv.NDelete, "Deleting node {0}", lfnEliminated.Poly.Index);

			// Determine whether we're the left or right child of our parent
			var fLeftChildEliminated = lfnEliminated.IsLeftChild;
			var innParent = lfnEliminated.NdParent;

			// Retrieve sibling nodes
			var lfnLeft = lfnEliminated.LeftAdjacentLeaf;
			var lfnRight = lfnEliminated.RightAdjacentLeaf;
			var lfnNearSibling = fLeftChildEliminated ? lfnRight : lfnLeft;

			// Remove from the queue any circle events which involve the eliminated node
			RemoveAssociatedCircleEvents(lfnEliminated, evq);

			// remove the leaf from the tree and rearrange the nodes around it
			RemoveLeaf(lfnEliminated);

			// Locate the internal node which represents the breakpoint opposite our near sibling
			var innFarSiblingEdge = lfnNearSibling.InnFindSiblingEdge(fLeftChildEliminated);

			// Get the edges being developed on each side of us
			//
			// The meeting of these edges is what causes the creation of our vertex in the voronoi diagram
			var edgeNearSibling = innParent.Edge;
			var edgeFarSibling = innFarSiblingEdge.Edge;

			// Create a new fortune vertex to insert into the diagram
			var vertex = new FortuneVertex {Pt = voronoiVertex};

			// Give both edges from above their brand new vertex - hooray!
			edgeFarSibling.AddVertex(vertex);
			edgeNearSibling.AddVertex(vertex);

			// Is this a zero length edge?
			//
			// Some of our incoming edges are zero length due to cocircular points,
			// so keep track of it in the polys which border them. This will be used
			// later in Fortune.BuildWingedEdge() to determine when to try and remove
			// zero length edges.
			if (cevt.FZeroLength)
			{
				// Mark the poly as having a zero length edge.
				//
				// We can't eliminate it here because most of our winged edge machinery
				// needs to assume three edges entering every vertex.  We'll take care of
				// it later in post-processing.  This flag is the signal to do that.
				SetZeroLengthFlagOnPolys(edgeNearSibling, edgeFarSibling);
			}

			// RQS- Add edges to the vertex in proper clockwise direction 
			if (fLeftChildEliminated)
			{
				vertex.Add(edgeFarSibling);
				vertex.Add(edgeNearSibling);
			}
			else
			{
				vertex.Add(edgeNearSibling);
				vertex.Add(edgeFarSibling);
			}
			// -RQS

			// Create the new edge which emerges below this vertex
			var edge = new FortuneEdge();
			edge.AddVertex(vertex);
			vertex.Add(edge);

			// Add the edge to our siblings
			//
			// Since lfnEliminated is being removed, it's siblings now butt against each other forming
			// the new edge so save that edge on the internal node and add it to the poly for the
			// generator represented by the near sibling.  This means that polygon edges get added in
			// a fairly random order.  They'll be sorted in postprocessing.

			// Add it to our winged edge polygon
			lfnNearSibling.Poly.AddEdge(edge);

			// Also add it to our beachline sibling
			innFarSiblingEdge.Edge = edge;

			// Fix up the siblings and the edges/polygons they border
			//
			// The inner node which used to represent one of the incoming edges now takes on responsibility
			// for the newly created edge so it no longer borders the polygon represented by the eliminated
			// leaf node, but rather borders the polygon represented by its sibling on the other side.
			// Also, that polygon receives a new edge.

			// If we eliminated the left child from a parent
			if (fLeftChildEliminated)
			{
				// Reset the data on the inner node representing our far sibling
				innFarSiblingEdge.PolyRight = lfnNearSibling.Poly;
				innFarSiblingEdge.PolyLeft.AddEdge(edge);

				// If this event represented a zero length edge
				if (cevt.FZeroLength)
				{
					// Keep track of it in the fortune polygon
					innFarSiblingEdge.PolyLeft.FZeroLengthEdge = true;
				}
			}
			else
			{
				// Reset the data on the inner node representing our far sibling
				innFarSiblingEdge.PolyLeft = lfnNearSibling.Poly;
				innFarSiblingEdge.PolyRight.AddEdge(edge);

				// If this event represented a zero length edge
				if (cevt.FZeroLength)
				{
					// Keep track of it in the fortune polygon
					innFarSiblingEdge.PolyRight.FZeroLengthEdge = true;
				}
			}
			// Set the polygons which border the new edge
			edge.SetPolys(innFarSiblingEdge.PolyRight, innFarSiblingEdge.PolyLeft);

			// Create new circle events for our siblings
			//
			// Now that we're squeezed out, our former left and right siblings become immediate siblings
			// so we need to set new circle events to represent when they get squeezed out by their
			// newly acquired siblings
			CreateCircleEventFromTriple(lfnLeft.LeftAdjacentLeaf, lfnLeft, lfnRight, yScanLine, evq);
			CreateCircleEventFromTriple(lfnLeft, lfnRight, lfnRight.RightAdjacentLeaf, yScanLine, evq);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Remove a leaf node. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="lfn">	node to remove. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private void RemoveLeaf(LeafNode lfn)
		{
			// If we're the root, then tree go bye bye...
			if (lfn == NdRoot)
			{
				NdRoot = null;

				return;
			}

			// If we're a child of the root then all we do is change the root to our immediate
			// sibling...
			if (lfn.NdParent == NdRoot)
			{
				NdRoot = lfn.ImmediateSibling;

				return;
			}

			// Remove the leaf node and its parent
			//
			// We remove both the leafnode and it's parent (it's parent represents the edge
			// that just terminated in our new fortune vertex, hence it's need to be removed
			// also).  It's immediate sibling
			// is moved up to be a child of the grandparent.  This changes the height
			// balance on the grandparent since it loses a level.
			var innParent = lfn.NdParent;
			var innGrandparent = innParent.NdParent;
			var fIsParentLeftChild = innParent.IsLeftChild;
			innParent.SnipFromParent();

			// Insert our sibling in place of our parent

			// Was our parent on the left side of their parent?
			if (fIsParentLeftChild)
			{
				// Move us in on the left side
				innGrandparent.LeftChild = lfn.ImmediateSibling;
				innGrandparent.DecDht();
			}
			else
			{
				// Move us in on the right side
				innGrandparent.RightChild = lfn.ImmediateSibling;
				innGrandparent.IncDht();
			}

			// Link our former siblings together
			//
			// Now that we're being removed, our former siblings become direct siblings so link them together
			// in the adjacent leaf chain
			lfn.LinkSiblingsTogether();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	One of our incoming edges is zero length so note it properly in the polygons. </summary>
		///
		/// <remarks>	
		/// This happens when cocircular generators cause more than one circle event at the same
		/// location. 
		/// </remarks>
		///
		/// <param name="edgeNearSibling">	Immediate sibling. </param>
		/// <param name="edgeFarSibling">	Far sibling. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void SetZeroLengthFlagOnPolys(FortuneEdge edgeNearSibling, FortuneEdge edgeFarSibling)
		{
			// If it's the edge between us and our near sibling that's zero length
			if (edgeNearSibling.VtxEnd != null && edgeNearSibling.FZeroLength())
			{
				// Both polys on each side of the far edge need to be marked as having a zero length edge
				edgeNearSibling.Poly1.FZeroLengthEdge =
					edgeNearSibling.Poly2.FZeroLengthEdge = true;
			}
			else
			{
				// Both polys on each side of the near edge need to be marked as having a zero length edge
				edgeFarSibling.Poly1.FZeroLengthEdge =
					edgeFarSibling.Poly2.FZeroLengthEdge = true;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Delete any circle events associated with the leaf node. </summary>
		///
		/// <remarks>	
		/// Circle events are composed of three adjacent leaf nodes so the ones	associated with us
		/// include the one directly on us and the ones on our left and right siblings. 
		/// </remarks>
		///
		/// <param name="lfnEliminated">	Leaf node being eliminated. </param>
		/// <param name="evq">				Event queue. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void RemoveAssociatedCircleEvents(LeafNode lfnEliminated, EventQueue evq)
		{
			// Delete circle events which involve us and our siblings
			Tracer.Trace(tv.CircleDeletions, "Deleting Circle events associated with the leaf node...");
			Tracer.Indent();
			lfnEliminated.DeleteAssociatedCircleEvent(evq);
			lfnEliminated.LeftAdjacentLeaf.DeleteAssociatedCircleEvent(evq);
			lfnEliminated.RightAdjacentLeaf.DeleteAssociatedCircleEvent(evq);
			Tracer.Unindent();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Catch special circle events and disallow them. </summary>
		///
		/// <remarks>	
		/// In the book it says to add a circle event if it isn't already in the queue.  That seems a bit
		/// wasteful to me - search the whole queue every time you add a circle event?  There has to be a
		/// better way.  This routine is the alternative.  Just a few checks on the circle parameters
		/// ensures that they'll only enter the queue once.  Much better than a searh of the queue. It's essentially
		/// an extension of the counter clockwise generic routine which deals with collinear points as though
		/// they were points on an infinitely large circle.
		/// </remarks>
		///
		/// <param name="pt1">	First point for proposed circle event. </param>
		/// <param name="pt2">	Second point for proposed circle event. </param>
		/// <param name="pt3">	Third point for proposed circle event. </param>
		///
		/// <returns>	Acceptable if less than or equal to zero, else rejected. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal static int ICcwVoronoi(PT pt1, PT pt2, PT pt3)
		{
			// Do the geometry to see if they're clockwise
			var iSign = Geometry.ICcw(pt1, pt2, pt3);

			// If they're not collilnear
			if (iSign != 0)
			{
				// Return their orientation
				return iSign;
			}

			// RQS- Treat the Collinear points as though they are on an infinite circle
			var dx1 = pt2.X - pt1.X;
			var dx2 = pt3.X - pt1.X;
			var dy1 = pt2.Y - pt1.Y;
			var dy2 = pt3.Y - pt1.Y;
			if ((dx1 * dx2 < 0) || (dy1 * dy2 < 0))
				return -1;
			if ((dx1 * dx1 + dy1 * dy1) < (dx2 * dx2 + dy2 * dy2))
				return +1;
			// -RQS

			return 0;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Create a circle event from a triple of leaf nodes. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="lfnLeft">		Leaf node representing the leftmost parabola. </param>
		/// <param name="lfnCenter">	Leaf node representing the center parabola. </param>
		/// <param name="lfnRight">		Leaf node representing the rightmost parabola. </param>
		/// <param name="yScanLine">	Where the scan line is located. </param>
		/// <param name="evq">			Event queue. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		static void CreateCircleEventFromTriple(
			LeafNode lfnLeft,
			LeafNode lfnCenter,
			LeafNode lfnRight,
			TPT yScanLine,
			EventQueue evq)
		{
			// This happens if we're the farthest right or left parabola...
			if (lfnLeft == null || lfnRight == null || lfnCenter == null)
			{
				// No circle events associated with non-existent parabolas

				return;
			}

			// Diagnostics
			Tracer.Trace(tv.CCreate, "Considering creation of cevt: {0}-{1}-{2}...",
				lfnLeft.Poly.Index, lfnCenter.Poly.Index, lfnRight.Poly.Index);

			// We need at least three points
			if (lfnRight == lfnCenter || lfnRight == lfnLeft || lfnCenter == lfnLeft)
			{
				// If two of the points are identical then we don't have three points
				Tracer.Trace(tv.CCreate, "Rejected circle event because it involves fewer than three generators");

				return;
			}

			// Make sure we don't insert the same circle eventin twice
			if (ICcwVoronoi(lfnLeft.Poly.VoronoiPoint, lfnCenter.Poly.VoronoiPoint, lfnRight.Poly.VoronoiPoint) > 0)
			{
				// Don't create an event if we've already put it in before
				Tracer.Trace(tv.CCreate, "Rejected circle event because it is not properly clockwise");

				return;
			}

			// Create the circle event
			var cevt = FortuneEvent.CreateCircleEvent(lfnLeft.Poly, lfnCenter.Poly, lfnRight.Poly, yScanLine);

			// If we got a valid circle event
			if (cevt != null)
			{
				// Diagnostics
				Tracer.Trace(tv.CCreate, "Creating circle for gens {0}, {1} and {2} to fire at ({3}, {4})",
					lfnLeft.Poly.Index,
					lfnCenter.Poly.Index,
					lfnRight.Poly.Index,
					cevt.Pt.X,
					cevt.Pt.Y);

				// Indicate which leaf node gets snuffed when this event is handled
				cevt.LfnEliminated = lfnCenter;

				// Add it to the event queue
				evq.AddCircleEvent(cevt);

				// Let the center node know this event will bring about its ominous demise
				lfnCenter.SetCircleEvent(cevt);
			}
		}
		#endregion

		#region Insertion

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Handle the top N nodes located on a single horizontal line. </summary>
		///
		/// <remarks>	
		/// This only handles the corner case where the top N nodes are on the same horizontal
		/// line.  In that case the parabolas from previous points are vertically straight up and only
		/// project to a single point on the x axis so that the beachline is a series of points rather
		/// than a series of parabolas.  When that is the case we can't "intersect" new points with
		/// parabolas that span the x axis.  After the scanline passes that initial set of topmost points,
		/// there will always be a parabola which projects to the entire x axis so no need for this
		/// special handling. Normally, we produce two new parabolas at a site event like this - the new
		/// parabola for the site itself and the new parabola produced when we split the parabola above
		/// us.  In this case there is no parabola above us so we only produce one new parabola - the one
		/// inserted by the site. 
		/// </remarks>
		///
		/// <param name="lfn">				LeafNode of the (degenerate) parabola nearest us. </param>
		/// <param name="lfnNewParabola">	LeafNode we're inserting. </param>
		/// <param name="innParent">		Parent of lfnOld. </param>
		/// <param name="innSubRoot">		Root of the tree. </param>
		/// <param name="fLeftChild">		Left child of innParent. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void NdInsertAtSameY(
			LeafNode lfn,
			LeafNode lfnNewParabola,
			InternalNode innParent,
			InternalNode innSubRoot,
			bool fLeftChild)
		{
			// Locals
			LeafNode lfnLeft, lfnRight;
			var lfnAdjacentParabolaLeft = lfn.LeftAdjacentLeaf;
			var lfnAdjacentParabolaRight = lfn.RightAdjacentLeaf;

			if (lfnNewParabola.Poly.VoronoiPoint.X < lfn.Poly.VoronoiPoint.X)
			{
				lfnLeft = lfnNewParabola;
				lfnRight = lfn;
			}
			else
			{
				//! Note: I don't think this case ever occurs in practice since we pull events off with higher
				//! x coordinates before events with lower x coordinates
				lfnLeft = lfn;
				lfnRight = lfnNewParabola;
			}
			innSubRoot.PolyLeft = lfnLeft.Poly;
			innSubRoot.PolyRight = lfnRight.Poly;

			innSubRoot.LeftChild = lfnLeft;
			innSubRoot.RightChild = lfnRight;

			FortuneEdge edge = new FortuneEdge();
			innSubRoot.SetEdge(edge);
			innSubRoot.AddEdgeToPolygons(edge);
			lfnLeft.LeftAdjacentLeaf = lfnAdjacentParabolaLeft;
			lfnLeft.RightAdjacentLeaf = lfnRight;
			lfnRight.LeftAdjacentLeaf = lfnLeft;
			lfnRight.RightAdjacentLeaf = lfnAdjacentParabolaRight;

			if (innParent != null)
			{
				if (fLeftChild)
				{
					innParent.PolyLeft = lfnRight.Poly;
				}
				else
				{
					innParent.PolyRight = lfnLeft.Poly;
				}
			}
			edge.SetPolys(innSubRoot.PolyRight, innSubRoot.PolyLeft);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Insert a new parabola into the beachline when the beachline spans the X axis. </summary>
		///
		/// <remarks>	
		/// This is the normal case.  We insert our new parabola and split the parabola above our site in
		/// two. This means one new leaf node is created for leftmost of the two nodes in the split (the
		/// old lfn is recycled to become the right node of the split).  Also a new internal node to
		/// parent all this. 
		/// </remarks>
		///
		/// <param name="lfnOld">			Parabola above the new site. </param>
		/// <param name="lfnNewParabola">	parabola for the new site. </param>
		/// <param name="innSubRoot">		Parent node of both lfnOld and lfnNewParabola represneting
		/// 								the breakpoint between them. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void InsertAtDifferentY(LeafNode lfnOld, LeafNode lfnNewParabola, InternalNode innSubRoot)
		{
			// The old lfn will become the new right half of the split but we need a new leaf node
			// for the left half of the split...
			var lfnLeftHalf = new LeafNode(lfnOld.Poly);
			var innSubRootLeftChild = new InternalNode(lfnOld.Poly, lfnNewParabola.Poly);
			var edge = new FortuneEdge();

			// This is all fairly straightforward (albeit dense) insertion of a node into a binary tree.
			innSubRoot.RightChild = lfnOld;
			innSubRoot.LeftChild = innSubRootLeftChild;
			innSubRoot.SetEdge(edge);
			innSubRoot.AddEdgeToPolygons(edge);
			innSubRootLeftChild.LeftChild = lfnLeftHalf;
			innSubRootLeftChild.RightChild = lfnNewParabola;
			innSubRootLeftChild.SetEdge(edge);
			lfnNewParabola.LeftAdjacentLeaf = lfnLeftHalf;
			lfnNewParabola.RightAdjacentLeaf = lfnOld;
			lfnLeftHalf.LeftAdjacentLeaf = lfnOld.LeftAdjacentLeaf;
			lfnLeftHalf.RightAdjacentLeaf = lfnNewParabola;

			if (lfnOld.LeftAdjacentLeaf != null)
			{
				lfnOld.LeftAdjacentLeaf.RightAdjacentLeaf = lfnLeftHalf;
			}
			lfnOld.LeftAdjacentLeaf = lfnNewParabola;
			edge.SetPolys(innSubRoot.PolyRight, innSubRoot.PolyLeft);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Insert a new LeafNode into the tree. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="lfn">	Place to put the new leaf node. </param>
		/// <param name="evt">	The event to insert. </param>
		///
		/// <returns>	. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		static InternalNode NdCreateInsertionSubtree(LeafNode lfn, SiteEvent evt)
		{
			// Initialize locals
			var innParent = lfn.NdParent;
			var lfnNewParabola = new LeafNode(evt.Poly);
			var innSubRoot = new InternalNode(evt.Poly, lfn.Poly);
			var fLeftChild = true;

			// If this isn't on the root node, shuffle things around a bit
			if (innParent != null)
			{
				fLeftChild = lfn.IsLeftChild;

				lfn.SnipFromParent();
				if (fLeftChild)
				{
					innParent.LeftChild = innSubRoot;
				}
				else
				{
					innParent.RightChild = innSubRoot;
				}
			}

			// Watch for the odd corner case of the top n generators having the same y coordinate.  See comments
			// on NdInsertAtSameY().
			if (Geometry.FCloseEnough(evt.Pt.Y, lfn.Poly.VoronoiPoint.Y))
			{
				NdInsertAtSameY(lfn, lfnNewParabola, innParent, innSubRoot, fLeftChild);
			}
			else
			{
				InsertAtDifferentY(lfn, lfnNewParabola, innSubRoot);
			}

			return innSubRoot;
		}

		/// <summary>
		/// Create the new circle events that arise from a site event
		/// </summary>
		/// <param name="lfnLeft">Node to the left</param>
		/// <param name="lfnRight">Node to the right</param>
		/// <param name="yScanLine">Scan line position</param>
		/// <param name="evq">Event queue</param>
		void CreateCircleEventsFromSiteEvent(
			LeafNode lfnLeft,
			LeafNode lfnRight,
			TPT yScanLine,
			EventQueue evq)
		{
			if (lfnLeft != null && lfnLeft.RightAdjacentLeaf != null)
			{
				CreateCircleEventFromTriple(
					lfnLeft, lfnLeft.RightAdjacentLeaf, lfnLeft.RightAdjacentLeaf.RightAdjacentLeaf, yScanLine, evq);
			}

			if (lfnRight != null && lfnRight.LeftAdjacentLeaf != null)
			{
				CreateCircleEventFromTriple(
					lfnRight.LeftAdjacentLeaf.LeftAdjacentLeaf, lfnRight.LeftAdjacentLeaf, lfnRight, yScanLine, evq);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Insert a new polygon arising from a site event. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="evt">	Site event causing the new polygon. </param>
		/// <param name="evq">	Event queue. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void PolyInsertNode(SiteEvent evt, EventQueue evq)
		{
			// If there's no tree yet
			if (NdRoot == null)
			{
				// Create a leaf node and make it the tree root
				NdRoot = new LeafNode(evt.Poly);

				return;
			}

			// Get the parabola above this site and the parabolas to its left and right
			var lfn = LfnSearch(evt.Pt.X, evt.Pt.Y);
			var lfnLeft = lfn.LeftAdjacentLeaf;
			var lfnRight = lfn.RightAdjacentLeaf;

			// Remove the circle event associated with the parabola we intersect
			//
			// We are inserting ourselves into this parabola which means it's old circle event is defunct
			// so toss it.
			Tracer.Trace(tv.CircleDeletions, "Deleting circle for intersected parabolic arc...");
			Tracer.Indent();
			lfn.DeleteAssociatedCircleEvent(evq);
			Tracer.Unindent();

			// Create a new subtree to hold the new leaf node
			var innSubRoot = NdCreateInsertionSubtree(lfn, evt);

			// If the root node is a leaf
			if (NdRoot.IsLeaf)
			{
				// Replace it with the new inner node we just created
				NdRoot = innSubRoot;
			}

			// TODO: Rebalancing the tree
			//Rebalance(innSubRoot);

			// For every circle event
			//
			// Remove any circle events that this generator is inside since it will be closer to the center
			// of the circle than any of the three points which lie on the circle
			// TODO: Is there a good way to optimize this?
			int cevt = evq.CircleEvents.Count;
			for (var icevt = 0; icevt < cevt; icevt++)
			{
				// If the circle event contains the site event
				if (evq.CircleEvents[icevt].Contains(evt.Pt))
				{
					// Delete the circle event
					Tracer.Trace(tv.CircleDeletions, "Removing {0} (contains ({1}, {2}))",
					             evq.CircleEvents[icevt].ToString(), evt.Pt.X, evt.Pt.Y);
					cevt--;
					evq.CircleEvents.RemoveAt(icevt);
				}
			}

			// Create any circle events which this site causes
			CreateCircleEventsFromSiteEvent(lfnLeft, lfnRight, evt.Pt.Y, evq);
		}

		// TODO: Implement rebalancing
		private void Rebalance(InternalNode innSubRoot)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		#endregion

		#region Debugging
		LeafNode LfnLeftmost()
		{
			Node nd = NdRoot;

			while (!nd.IsLeaf)
			{
				nd = nd.LeftChild;
			}
			return nd as LeafNode;
		}

		[Conditional("DEBUG")]
		internal void TraceBeachline(TPT yScanLine)
		{
			Tracer.Trace(tv.Beachline, "Current beachline (scanline = {0}:", yScanLine);
			if (NdRoot == null)
			{
				Tracer.Trace(tv.Beachline, "No Beachline yet...");
				return;
			}
			Tracer.Indent();
			LeafNode lfn = LfnLeftmost();
			while (lfn.RightAdjacentLeaf != null)
			{
				TPT tptBreakpoint = Geometry.ParabolicCut(
					lfn.RightAdjacentLeaf.Poly.VoronoiPoint,
					lfn.Poly.VoronoiPoint,
					yScanLine);

				Tracer.Trace(tv.Beachline, "bpt between gens {0} and {1}: {2}", 
					lfn.Poly.Index, lfn.RightAdjacentLeaf.Poly.Index, tptBreakpoint);
				lfn = lfn.RightAdjacentLeaf;
			}
			Tracer.Unindent();
			Tracer.Trace(tv.Beachline, "End of BeachLine");
		}
		#endregion
	}
}

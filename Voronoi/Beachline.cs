#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
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

		public Beachline()
		{
			NdRoot = null;
		}

		#endregion

		#region Properties

		internal Node NdRoot { get; set; }

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
			Tracer.Trace(tv.Search, "Searching for x={0} with yScan={1} at {2}", xSite, yScanLine, nd.ToString());

			// If it's a leaf node, we've arrived
			// TODO: This really doesn't need to be recursive...
			if (nd.IsLeaf)
			{
				// The leaf node is our return
				ndRet = nd as LeafNode;
			}
			else
			{
				// It's an internal node
				// Internal nodes represent developing edges as the sweep line sweeps downward.  They've got pointers to the
				// polygons on each side of that line.  The place those two polygons meet is the place where two parabolas with
				// foci at the voronoi input points and directrix at the current sweep line meet.  This is pure geometry and is
				// determined in CurrentEdgeXPos below.

				// Determine the break point on the beach line 
				var ndInt = nd as InternalNode;
				var edgeXPos = ndInt.CurrentEdgeXPos(yScanLine);

				// Search the tree recursively on the side of the break point that xSite is on
				Tracer.Trace(tv.Search, "Current edge X pos = {0}", edgeXPos);
				ndRet = LfnSearchNode(edgeXPos < xSite ? nd.NdRight : nd.NdLeft, xSite, yScanLine);
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

			// The inner node which used to represent one of the incoming edges now takes on responsibility
			// for the newly created edge so it no longer borders the polygon represented by the eliminated
			// leaf node, but rather borders the polygon represented by its sibling on the other side.
			// Also, that polygon receives a new edge.
			if (fLeftChildEliminated)
			{
				innFarSiblingEdge.PolyRight = lfnNearSibling.Poly;
				innFarSiblingEdge.PolyLeft.AddEdge(edge);
				if (cevt.FZeroLength)
				{
					innFarSiblingEdge.PolyLeft.FZeroLengthEdge = true;
				}
			}
			else
			{
				innFarSiblingEdge.PolyLeft = lfnNearSibling.Poly;
				innFarSiblingEdge.PolyRight.AddEdge(edge);
				if (cevt.FZeroLength)
				{
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

			// We remove both the leafnode and it's parent.  It's immediate sibling
			// is moved up to the grandparent.  This changes the height balance on the
			// grandparent since it loses a level.
			var innParent = lfn.NdParent;
			var innGrandparent = innParent.NdParent;
			var fIsParentLeftChild = innParent.IsLeftChild;
			innParent.SnipFromParent();

			// Insert us on the proper side of our grandparent
			if (fIsParentLeftChild)
			{
				innGrandparent.NdLeft = lfn.ImmediateSibling;
				innGrandparent.DecDht();
			}
			else
			{
				innGrandparent.NdRight = lfn.ImmediateSibling;
				innGrandparent.IncDht();
			}

			// Now that we're being removed, our former siblings become direct siblings so link them together
			// in the adjacent leaf chain
			lfn.LinkSiblingsTogether();
		}

		/// <summary>
		/// One of our incoming edges is zero length so note it properly in the polygons
		/// </summary>
		/// <remarks>
		/// This happens when cocircular generators cause more than one circle event at the same location.
		/// </remarks>
		/// <param name="edgeNearSibling">Immediate sibling</param>
		/// <param name="edgeFarSibling">Far sibling</param>
		private static void SetZeroLengthFlagOnPolys(FortuneEdge edgeNearSibling, FortuneEdge edgeFarSibling)
		{
			if (edgeNearSibling.VtxEnd != null && edgeNearSibling.FZeroLength())
			{
				edgeNearSibling.Poly1.FZeroLengthEdge =
					edgeNearSibling.Poly2.FZeroLengthEdge = true;
			}
			else
			{
				edgeFarSibling.Poly1.FZeroLengthEdge =
					edgeFarSibling.Poly2.FZeroLengthEdge = true;
			}
		}

		/// <summary>
		/// Delete any circle events associated with the leaf node.
		/// </summary>
		/// <remarks>
		/// Circle events are composed of three adjacent leaf nodes so the ones	associated with us include
		/// the one directly on us and the ones on our left and right siblings.
		/// </remarks>
		/// <param name="lfnEliminated">Leaf node being eliminated</param>
		/// <param name="evq">Event queue</param>
		private static void RemoveAssociatedCircleEvents(LeafNode lfnEliminated, EventQueue evq)
		{
			Tracer.Trace(tv.CircleDeletions, "Deleting Circle events associated with the leaf node...");
			Tracer.Indent();
			lfnEliminated.DeleteAssociatedCircleEvent(evq);
			lfnEliminated.LeftAdjacentLeaf.DeleteAssociatedCircleEvent(evq);
			lfnEliminated.RightAdjacentLeaf.DeleteAssociatedCircleEvent(evq);
			Tracer.Unindent();
		}

		/// <summary>
		/// Catch special circle events and disallow them
		/// </summary>
		/// <remarks>
		/// In the book it says to add a circle event if it isn't already in the queue.  That seems
		/// a bit wasteful to me - search the whole queue every time you add a circle event?  There
		/// has to be a better way.  This routine is the alternative.  Just a few checks on the
		/// circle parameters ensures that they'll only enter the queue once.  Much better than a searh
		/// of the queue.
		/// </remarks>
		/// <param name="pt1">First point for proposed circle event</param>
		/// <param name="pt2">Second point for proposed circle event</param>
		/// <param name="pt3">Third point for proposed circle event</param>
		/// <returns>Acceptable if less than or equal to zero, else rejected</returns>
		internal static int ICcwVoronoi(PT pt1, PT pt2, PT pt3)
		{
			int iSign = Geometry.ICcw(pt1, pt2, pt3);
			if (iSign != 0)
			{
				return iSign;
			}
			TPT dx1 = pt2.X - pt1.X;
			TPT dx2 = pt3.X - pt1.X;
			TPT dy1 = pt2.Y - pt1.Y;
			TPT dy2 = pt3.Y - pt1.Y;
			if ((dx1 * dx2 < 0) || (dy1 * dy2 < 0))
				return -1;
			if ((dx1 * dx1 + dy1 * dy1) < (dx2 * dx2 + dy2 * dy2))
				return +1;
			return 0;
		}

		/// <summary>
		/// Create a circle event from a triple of leaf nodes
		/// </summary>
		/// <param name="lfnLeft">Leaf node representing the leftmost parabola</param>
		/// <param name="lfnCenter">Leaf node representing the center parabola</param>
		/// <param name="lfnRight">Leaf node representing the rightmost parabola</param>
		/// <param name="yScanLine">Where the scan line is located</param>
		/// <param name="evq">Event queue</param>
		void CreateCircleEventFromTriple(
			LeafNode lfnLeft,
			LeafNode lfnCenter,
			LeafNode lfnRight,
			TPT yScanLine,
			EventQueue evq)
		{
			// This happens if we're the farthest right or left parabola...
			if (lfnLeft == null || lfnRight == null || lfnCenter == null)
			{
				return;
			}

			Tracer.Trace(tv.CCreate, "Considering creation of cevt: {0}-{1}-{2}...",
				lfnLeft.Poly.Index, lfnCenter.Poly.Index, lfnRight.Poly.Index);

			// We need at least three points
			if (lfnRight == lfnCenter || lfnRight == lfnLeft || lfnCenter == lfnLeft)
			{
				Tracer.Trace(tv.CCreate, "Rejected circle event because it involves fewer than three generators");
				return;
			}

			// Make sure we don't insert the same circle eventin twice
			if (ICcwVoronoi(lfnLeft.Poly.VoronoiPoint, lfnCenter.Poly.VoronoiPoint, lfnRight.Poly.VoronoiPoint) > 0)
			{
				Tracer.Trace(tv.CCreate, "Rejected circle event because it is not properly clockwise");
				return;
			}

			// Create the circle event
			CircleEvent cevt = FortuneEvent.CreateCircleEvent(lfnLeft.Poly, lfnCenter.Poly, lfnRight.Poly, yScanLine);
			if (cevt != null)
			{
				Tracer.Trace(tv.CCreate, "Creating circle for gens {0}, {1} and {2} to fire at ({3}, {4})",
					lfnLeft.Poly.Index,
					lfnCenter.Poly.Index,
					lfnRight.Poly.Index,
					cevt.Pt.X,
					cevt.Pt.Y);
				// Indicate which leaf node gets snuffed when this event is handled
				cevt.LfnEliminated = lfnCenter;
				evq.AddCircleEvent(cevt);
				lfnCenter.SetCircleEvent(cevt);
			}
		}
		#endregion

		#region Insertion
		/// <summary>
		/// Handle the top N nodes located on a single horizontal line
		/// </summary>
		/// <remarks>
		/// Note that this only handles the corner case where the top N nodes are on the same horizontal
		/// line.  In that case the parabolas from previous points are vertically straight up and only
		/// project to a single point on the x axis so that the beachline is a series of points rather than
		/// a series of parabolas.  When that is the case we can't "intersect" new points with parabolas
		/// that span the x axis.  After the scanline passes that initial set of topmost points, there
		/// will always be a parabola which projects to the entire x axis so no need for this special
		/// handling.
		/// Normally, we produce two new parabolas at a site event like this - the new parabola for the
		/// site itself and the new parabola produced when we split the parabola above us.  In this case
		/// there is no parabola above us so we only produce one new parabola - the one inserted by the site.
		/// </remarks>
		/// <param name="lfn">LeafNode of the (degenerate) parabola nearest us</param>
		/// <param name="lfnNewParabola">LeafNode we're inserting</param>
		/// <param name="innParent">Parent of lfn</param>
		/// <param name="innSubRoot">Root of the tree</param>
		/// <param name="fLeftChild">Left child of innParent</param>
		private void NdInsertAtSameY(
			LeafNode lfn,
			LeafNode lfnNewParabola,
			InternalNode innParent,
			InternalNode innSubRoot,
			bool fLeftChild)
		{
			LeafNode lfnLeft, lfnRight;
			LeafNode lfnAdjacentParabolaLeft = lfn.LeftAdjacentLeaf;
			LeafNode lfnAdjacentParabolaRight = lfn.RightAdjacentLeaf;

			if (lfnNewParabola.Poly.VoronoiPoint.X < lfn.Poly.VoronoiPoint.X)
			{
				lfnLeft = lfnNewParabola;
				lfnRight = lfn;
			}
			else
			{
				// I don't think this case ever occurs in practice since we pull events off with higher
				// x coordinates before events with lower x coordinates
				lfnLeft = lfn;
				lfnRight = lfnNewParabola;
			}
			innSubRoot.PolyLeft = lfnLeft.Poly;
			innSubRoot.PolyRight = lfnRight.Poly;

			innSubRoot.NdLeft = lfnLeft;
			innSubRoot.NdRight = lfnRight;
			FortuneEdge edge = new FortuneEdge();
			innSubRoot.AddEdge(edge);
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

		/// <summary>
		/// Insert a new parabola into the beachline when the beachline spans the X axis
		/// </summary>
		/// <remarks>
		/// This is the normal case.  We insert our new parabola and split the parabola above
		/// our site in two.
		/// </remarks>
		/// <param name="lfn">Parabola above the new site</param>
		/// <param name="lfnNewParabola">parabola for the new site</param>
		/// <param name="innSubRoot">Parent node of both lfn and lfnNewParabola represneting the
		/// breakpoint between them</param>
		private static void InsertAtDifferentY(LeafNode lfn, LeafNode lfnNewParabola, InternalNode innSubRoot)
		{
			// The old lfn will become the new right half of the split but we need a new leaf node
			// for the left half of the split...
			LeafNode lfnLeftHalf = new LeafNode(lfn.Poly);
			InternalNode innSubRootLeftChild = new InternalNode(lfn.Poly, lfnNewParabola.Poly);
			FortuneEdge edge = new FortuneEdge();

			// This is all fairly straightforward insertion of a node into a binary tree.
			innSubRoot.NdRight = lfn;
			innSubRoot.NdLeft = innSubRootLeftChild;
			innSubRoot.AddEdge(edge);
			innSubRoot.AddEdgeToPolygons(edge);
			innSubRootLeftChild.NdLeft = lfnLeftHalf;
			innSubRootLeftChild.NdRight = lfnNewParabola;
			innSubRootLeftChild.AddEdge(edge);

			lfnNewParabola.LeftAdjacentLeaf = lfnLeftHalf;
			lfnNewParabola.RightAdjacentLeaf = lfn;
			lfnLeftHalf.LeftAdjacentLeaf = lfn.LeftAdjacentLeaf;
			lfnLeftHalf.RightAdjacentLeaf = lfnNewParabola;
			if (lfn.LeftAdjacentLeaf != null)
			{
				lfn.LeftAdjacentLeaf.RightAdjacentLeaf = lfnLeftHalf;
			}
			lfn.LeftAdjacentLeaf = lfnNewParabola;
			edge.SetPolys(innSubRoot.PolyRight, innSubRoot.PolyLeft);
		}

		/// <summary>
		/// Insert a new LeafNode into the tree
		/// </summary>
		/// <param name="lfn">Place to put the new leaf node</param>
		/// <param name="evt">The event to insert</param>
		/// <returns></returns>
		InternalNode NdCreateInsertionSubtree(LeafNode lfn, SiteEvent evt)
		{
			InternalNode innParent = lfn.NdParent;
			LeafNode lfnNewParabola = new LeafNode(evt.Poly);
			InternalNode innSubRoot = new InternalNode(evt.Poly, lfn.Poly);
			bool fLeftChild = true;

			// If this isn't on the root node, shuffle things around a bit
			if (innParent != null)
			{
				fLeftChild = lfn.IsLeftChild;

				lfn.SnipFromParent();
				if (fLeftChild)
				{
					innParent.NdLeft = innSubRoot;
				}
				else
				{
					innParent.NdRight = innSubRoot;
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

		/// <summary>
		/// Insert a new polygon arising from a site event
		/// </summary>
		/// <param name="evt">Site event causing the new polygon</param>
		/// <param name="evq">Event queue</param>
		/// <returns>The polygon inserted</returns>
		internal FortunePoly PolyInsertNode(SiteEvent evt, EventQueue evq)
		{
			FortunePoly polyRet = null;

			if (NdRoot == null)
			{
				NdRoot = new LeafNode(evt.Poly);
			}
			else
			{
				// Get the parabola above this site and the parabolas to its left and right
				LeafNode lfn = LfnSearch(evt.Pt.X, evt.Pt.Y);
				LeafNode lfnLeft = lfn.LeftAdjacentLeaf;
				LeafNode lfnRight = lfn.RightAdjacentLeaf;

				// We are inserting ourselves into this parabola which means it's old circle event is defunct
				// so toss it.
				polyRet = lfn.Poly;
				Tracer.Trace(tv.CircleDeletions, "Deleting circle for intersected parabolic arc...");
				Tracer.Indent();
				lfn.DeleteAssociatedCircleEvent(evq);
				Tracer.Unindent();

				// Create a new subtree to hold the new leaf node
				InternalNode innSubRoot = NdCreateInsertionSubtree(lfn, evt);
				if (NdRoot.IsLeaf)
				{
					NdRoot = innSubRoot;
				}

				//Rebalance(innSubRoot);

				// Remove any circle events that this generator is inside since it will be closer to the center
				// of the circle than any of the three points which lie on the circle
				for (int icevt = 0; icevt < evq.CircleEvents.Count; icevt++)
				{
					if (evq.CircleEvents[icevt].Contains(evt.Pt))
					{
						Tracer.Trace(tv.CircleDeletions, "Removing {0} (contains ({1}, {2}))",
							evq.CircleEvents[icevt].ToString(), evt.Pt.X, evt.Pt.Y);
						evq.CircleEvents.RemoveAt(icevt);
					}
				}

				// Create any circle events which this site causes
				CreateCircleEventsFromSiteEvent(lfnLeft, lfnRight, evt.Pt.Y, evq);
			}
			return polyRet;
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
				nd = nd.NdLeft;
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

	#region Node classes
	abstract internal class Node
	{
		#region Private Variables
		Node _ndLeft = null;					// Left child
		Node _ndRight = null;					// Right child
		InternalNode _ndParent = null;			// Parent
		#endregion

		#region Trace Tree
#if DEBUG || NETTRACE
		internal void TraceTree(string str, tv TraceEnumElement)
		{
			Tracer.Trace(tv.Trees, str);
			TraceTreeWithIndent(TraceEnumElement, 0);
		}

		abstract internal void TraceTreeWithIndent(tv TraceEnumElement, int cIndent);
#endif
		#endregion

		#region Properties
		internal Node ImmediateSibling
		{
			get
			{
				return IsLeftChild ? NdParent.NdRight : NdParent.NdLeft;
			}
		}

		internal bool IsLeftChild
		{
			get
			{
				return this == NdParent.NdLeft;
			}
		}

		internal bool IsLeaf
		{
			get
			{
				return NdLeft == null;
			}
		}

		internal InternalNode NdParent
		{
			get
			{
				return _ndParent;
			}
			set
			{
				_ndParent = value;
			}
		}

		internal Node NdLeft
		{
			get
			{
				return _ndLeft;
			}
			set
			{
				_ndLeft = value;
				_ndLeft.NdParent = (InternalNode)this;
			}
		}

		internal Node NdRight
		{
			get
			{
				return _ndRight;
			}
			set
			{
				_ndRight = value;
				_ndRight.NdParent = (InternalNode)this;
			}
		}
		#endregion

		#region Modification

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Remove pointers to parent and mark the node as an orphan. </summary>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal void SnipFromParent()
		{
			// If it's the left child of its parent
			if (IsLeftChild)
			{
				// Null the parent's left child pointer
				// Can't use NdLeft property here since it will try to set the parent of the incoming null node.
				NdParent._ndLeft = null;
			}
			else
			{
				// Null the parent's right child pointer
				NdParent._ndRight = null;
			}
			// Null our own parent pointer
			NdParent = null;
		}
		#endregion

		#region NUnit
#if NUNIT || DEBUG
		[TestFixture]
		public class TestNode
		{
			[Test]
			public void TestInsertDelete()
			{
				FortunePoly poly = new FortunePoly(new PT(0, 0), 0);

				Node ndRoot = new InternalNode(poly, poly);
				Node ndLeft = new InternalNode(poly, poly);
				Node ndRight = new InternalNode(poly, poly);
				Node ndLL = new LeafNode(poly);
				Node ndLR = new LeafNode(poly);
				Node ndRL = new LeafNode(poly);
				Node ndRR = new LeafNode(poly);

				ndRoot.NdLeft = ndLeft;
				ndRoot.NdRight = ndRight;
				ndRight.NdLeft = ndRL;
				ndRight.NdRight = ndRR;
				ndLeft.NdLeft = ndLL;
				ndLeft.NdRight = ndLR;

				Assert.IsTrue(ndLeft.NdParent == ndRoot);
				Assert.IsTrue(ndRL.NdParent == ndRight);
				Assert.IsTrue(ndLL.IsLeaf);
				Assert.IsFalse(ndRoot.IsLeaf);
				Assert.IsTrue(ndLeft.IsLeftChild);
				Assert.IsFalse(ndRight.IsLeftChild);

				ndLeft.SnipFromParent();
				Assert.IsNull(ndLeft.NdParent);
				Assert.IsNull(ndRoot.NdLeft);
			}
		}
#endif
		#endregion
	}

	internal class InternalNode : Node
	{
		#region Properties

		// Winged edge that this internal node represents
		internal FortuneEdge Edge { get; set; }

		// Height of left subtree minus height of right for balancing
		internal int DHtLeftMinusRight { get; set; }

		// Winged edge polygon on one side of us
		internal FortunePoly PolyRight { get; set; }

		// Winged edge polgon on the other
		internal FortunePoly PolyLeft { get; set; }

		#endregion

		#region overrides
#if DEBUG || NETTRACE
		override internal void TraceTreeWithIndent(tv traceEnumElement, int cIndent)
		{
			var sbIndent = new StringBuilder();

			// *SURELY* there is a better way to repeat a single character into a string, but I can't
			// seem to locate it for the life of me.

			for (int iIndent = 0; iIndent < cIndent; iIndent++)
			{
				sbIndent.Append("|  ");
			}

			Tracer.Trace(traceEnumElement, sbIndent.ToString() + ToString());

			NdLeft.TraceTreeWithIndent(traceEnumElement, cIndent + 1);
			NdRight.TraceTreeWithIndent(traceEnumElement, cIndent + 1);
		}
#endif
		#endregion

		#region Manipulation
		//!+ TODO: Implement height balanced tree
		// Right now we don't bother trying to keep the beachline tree balanced in any way.
		internal void IncDht()
		{
			DHtLeftMinusRight++;
		}

		internal void DecDht()
		{
			DHtLeftMinusRight--;
		}
		#endregion

		#region Information
		internal TPT CurrentEdgeXPos(TPT yScanLine)
		{
			return Geometry.ParabolicCut(PolyRight.VoronoiPoint, PolyLeft.VoronoiPoint, yScanLine);
		}
		#endregion

		#region Edge handling
		internal void AddEdge(FortuneEdge edge)
		{
			Edge = edge;
		}

		internal void AddEdgeToPolygons(FortuneEdge edge)
		{
			PolyLeft.AddEdge(edge);
			PolyRight.AddEdge(edge);
		}
		#endregion

		#region Constructor
		internal InternalNode(FortunePoly polyLeft, FortunePoly polyRight)
		{
			DHtLeftMinusRight = 0;
			PolyLeft = polyLeft;
			PolyRight = polyRight;
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return string.Format("InternNode: Gens = {0}, {1}", PolyLeft.Index, PolyRight.Index);
		}
		#endregion

		#region NUnit
#if DEBUG || NUNIT
		[TestFixture]
		public class TestInternalNode
		{
			[Test]
			public void TestParabolicCut()
			{
				FortunePoly poly1 = new FortunePoly(new PT(0, 0), 0);
				FortunePoly poly2 = new FortunePoly(new PT(8, 4), 1);

				InternalNode inn = new InternalNode(poly1, poly2);
				InternalNode innReverse = new InternalNode(poly2, poly1);

				Assert.AreEqual(-7, inn.CurrentEdgeXPos(-1));
				Assert.AreEqual(3, innReverse.CurrentEdgeXPos(-1));
			}
		}
#endif
		#endregion
	}

	internal class LeafNode : Node
	{
		#region Private Variables
		FortunePoly _poly;						// Polygon being created by this node
		CircleEvent _cevt;						// Circle event which will snuff this parabola out
		LeafNode _ndLeftLeaf = null;			// Node for the parabola to our left
		LeafNode _ndRightLeaf = null;			// Node for the parabola to our right
		#endregion

		#region Properties
		internal FortunePoly Poly
		{
			get { return _poly; }
			set { _poly = value; }
		}

		internal LeafNode RightAdjacentLeaf
		{
			get { return _ndRightLeaf; }
			set { _ndRightLeaf = value; }
		}

		internal LeafNode LeftAdjacentLeaf
		{
			get { return _ndLeftLeaf; }
			set { _ndLeftLeaf = value; }
		}
		#endregion

		#region Constructor
		internal LeafNode(FortunePoly poly)
		{
			_poly = poly;
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return string.Format("LeafNode: Gen = {0}", Poly.Index);
		}
		#endregion

		#region overrides
#if DEBUG || NETTRACE
		override internal void TraceTreeWithIndent(tv TraceEnumElement, int cIndent)
		{
			StringBuilder sbIndent = new StringBuilder();

			// *SURELY* there is a better way to repeat a single character into a string, but I can't
			// seem to locate it for the life of me.

			for (int iIndent = 0; iIndent < cIndent; iIndent++)
			{
				sbIndent.Append("|  ");
			}

			Tracer.Trace(TraceEnumElement, sbIndent.ToString() + ToString());
		}
#endif
		#endregion

		#region Queries
		/// <summary>
		/// Figure out where the breakpoint to our right is
		/// </summary>
		/// <param name="yScanLine">Where the scan line resides currently</param>
		/// <returns>The X coordinate of the right breakpoint</returns>
		internal TPT RightBreak(TPT yScanLine)
		{
			// If we're the leaf furthest to the right...
			if (RightAdjacentLeaf == null)
			{
				return TPT.MaxValue;
			}
			else
			{
				// Calculate where we intersect the parabola to our right
				return Geometry.ParabolicCut(RightAdjacentLeaf.Poly.VoronoiPoint, Poly.VoronoiPoint, yScanLine);
			}
		}

		/// <summary>
		/// Figure out where the breakpoint to our left is
		/// </summary>
		/// <param name="yScanLine">Where the scan line resides currently</param>
		/// <returns>The X coordinate of the left breakpoint</returns>
		internal TPT LeftBreak(TPT yScanLine)
		{
			// If we're the leaf furthest to the left...
			if (LeftAdjacentLeaf == null)
			{
				return TPT.MinValue;
			}
			else
			{
				// Calculate where we intersect the parabola to our left
				return Geometry.ParabolicCut(Poly.VoronoiPoint, LeftAdjacentLeaf.Poly.VoronoiPoint, yScanLine);
			}
		}
		#endregion

		#region Modification
		/// <summary>
		/// Remove the circle event which snuffs this node's parabola
		/// </summary>
		/// <param name="evq">Event queue</param>
		internal void DeleteAssociatedCircleEvent(EventQueue evq)
		{
			if (_cevt != null)
			{
				Tracer.Trace(tv.CircleDeletions, "Deleting {0}", _cevt.ToString());
				evq.Delete(_cevt);
				_cevt = null;
			}
		}

		/// <summary>
		/// Set the circle event which snuff's this node's parabola
		/// </summary>
		/// <param name="cevt"></param>
		internal void SetCircleEvent(CircleEvent cevt)
		{
			_cevt = cevt;
		}

		/// <summary>
		/// Locate the inner node which represents the edge on a selected side
		/// </summary>
		/// <param name="fLeftSibling">Which side to locate</param>
		/// <returns>The inner node for the breakpoint</returns>
		internal InternalNode InnFindSiblingEdge(bool fLeftSibling)
		{
			InternalNode innCur = NdParent;
			FortunePoly polyOfSibling = fLeftSibling ? LeftAdjacentLeaf.Poly : RightAdjacentLeaf.Poly;

			while ((fLeftSibling ? innCur.PolyLeft : innCur.PolyRight) != polyOfSibling)
			{
				innCur = innCur.NdParent;
			}

			return innCur;
		}

		/// <summary>
		/// Link my sibling nodes together as siblings
		/// </summary>
		internal void LinkSiblingsTogether()
		{
			if (LeftAdjacentLeaf != null)
			{
				LeftAdjacentLeaf.RightAdjacentLeaf = RightAdjacentLeaf;
			}
			if (RightAdjacentLeaf != null)
			{
				RightAdjacentLeaf.LeftAdjacentLeaf = LeftAdjacentLeaf;
			}
		}
		#endregion
	}
	#endregion
}

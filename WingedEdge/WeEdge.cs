﻿using System;
using System.Drawing;
using NetTrace;
// ReSharper disable MemberCanBeProtected.Global
// 
namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	The Edge in a WingedEdge data structure. </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class WeEdge : IComparable<WeEdge>
	{
		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	A place for users to store information. </summary>
		///
		/// <value>	User specific info. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public object Cookie { get; set; }
		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the starting vertex for this edge. </summary>
		///
		/// <value>	The starting vertex for this edge. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeVertex VtxStart { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the ending vertex for this edge. </summary>
		///
		/// <value>	The ending vertex for this edge. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeVertex VtxEnd { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the successor edge in a CW direction. </summary>
		///
		/// <value>	The successor edge in a CW direction. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeEdge EdgeCWSuccessor { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the successor edge in a CCW direction. </summary>
		///
		/// <value>	The successor edge in a CCW direction. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeEdge EdgeCCWSuccessor { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the polygon to the right of this edge. </summary>
		///
		/// <value>	The polygon to the right of this edge. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WePolygon PolyRight { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the polygon to the left of this edge. </summary>
		///
		/// <value>	The polygon to the left of this edge. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WePolygon PolyLeft { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the predecessor edge in a CW direction. </summary>
		///
		/// <value>	The predecessor edge in a CW direction. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeEdge EdgeCWPredecessor { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the predecessor edge in a CCW direction. </summary>
		///
		/// <value>	The predecessor edge in a CCW direction. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeEdge EdgeCCWPredecessor { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets a value indicating whether this is an edge at infinity. </summary>
		///
		/// <value>	true if the edge is at infinity, false if not. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FAtInfinity => VtxEnd.FAtInfinity && VtxStart.FAtInfinity;

	    ////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets a value indicating whether this edge is a ray. </summary>
		///
		/// <value>	true if it's a ray, false if not. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FRay => !VtxStart.FAtInfinity && VtxEnd.FAtInfinity;

	    #endregion

		#region Validation
		/// <summary>
		/// Place to set breakpoint to observe failure in validation routines
		/// </summary>
		/// <returns></returns>
		static bool Failure()
		{
			return false;
		}

		/// <summary>
		/// Validate the edge information
		/// </summary>
		/// <returns></returns>
		internal bool Validate()
		{
			// RQS- All variables should be set
			if (VtxEnd == null)
			{
				return Failure();
			}
			if (VtxStart == null)
			{
				return Failure();
			}
			if (EdgeCCWPredecessor == null)
			{
				return Failure();
			}
			if (EdgeCCWSuccessor == null)
			{
				return Failure();
			}
			if (EdgeCWPredecessor == null)
			{
				return Failure();
			}
			if (EdgeCWSuccessor == null)
			{
				return Failure();
			}
			if (PolyLeft == null)
			{
				return Failure();
			}
			if (PolyRight == null)
			{
				return Failure();
			}
			// -RQS

			// RQS- Check adjacencies
			//
			// Make sure that we and all our CW/CCW successor/predecessor edges
			// are marked as adjacent in our start/end vertices
			if (!VtxEnd.FValidateEdgeIsAdjacent(this))
			{
				return Failure();
			}
			if (!VtxStart.FValidateEdgeIsAdjacent(EdgeCCWPredecessor))
			{
				return Failure();
			}
			if (!VtxEnd.FValidateEdgeIsAdjacent(EdgeCCWSuccessor))
			{
				return Failure();
			}
			if (!VtxStart.FValidateEdgeIsAdjacent(EdgeCWPredecessor))
			{
				return Failure();
			}
			if (!VtxEnd.FValidateEdgeIsAdjacent(EdgeCWSuccessor))
			{
				return Failure();
			}
			if (!VtxStart.FValidateEdgeIsAdjacent(this))
			{
				return Failure();
			}
			// -RQS

			// RQS- Check adjacency of all listed edges to the proper polygons
			if (!PolyLeft.FValidateEdgeIsAdjacent(this))
			{
				return Failure();
			}
			if (!PolyRight.FValidateEdgeIsAdjacent(this))
			{
				return Failure();
			}
			if (!PolyLeft.FValidateEdgeIsAdjacent(EdgeCWSuccessor))
			{
				return Failure();
			}
			if (!PolyLeft.FValidateEdgeIsAdjacent(EdgeCCWPredecessor))
			{
				return Failure();
			}
			if (!PolyRight.FValidateEdgeIsAdjacent(EdgeCCWSuccessor))
			{
				return Failure();
			}
			if (!PolyRight.FValidateEdgeIsAdjacent(EdgeCWPredecessor))
			{
				return Failure();
			}
			// -RQS

			return true;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Ensure that this edge connects to the passed in edge. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	Edge to check. </param>
		///
		/// <returns>	True if this edge connects to the passed in edge, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool FConnectsToEdge(WeEdge edge)
		{
			// Has to be adjacent to either our start or our end vertex
			return VtxEnd.FValidateEdgeIsAdjacent(edge) || VtxStart.FValidateEdgeIsAdjacent(edge);
		}
		#endregion

		#region Geometry

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Determine if a point is to the left of this edge when facing from the start vertex to the end
		/// vertex. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="pt">	Point to check out. </param>
		///
		/// <returns>	true if it succeeds, false if it fails. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FLeftOf(PointD pt)
		{
			// We should never have a start vertex at infinity
			//
			// Fortune.ProcessRay(), which creates the vertices at infinity, specifically makes sure that
			// this never happens (except with edges at infinity which are really placeholders for the
			// winged edge structure and have no position at all.  They aren't processed here (and if they
			// were, it would be a problem).  While this is a touch Fortune specific, there's no reason not to
			// insist on it in the general case.
			Tracer.Assert(t.Assertion, !VtxStart.FAtInfinity, "Found non-infinite edge with start vertex at infinity");
			
			// If the end vtx is at infinity, convert it to a real point
			var pt1 = VtxStart.Pt;
			var pt2 = VtxEnd.FAtInfinity ? VtxEnd.ConvertToReal(pt1, 10) : VtxEnd.Pt;

			// Do the geometry on pt1 and pt2
			return Geometry.FLeft(pt1, pt2, pt);
		}
		#endregion

		#region ToString

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <returns>	
		/// A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />. 
		/// </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public override string ToString()
		{
			var strStart = VtxStart?.ToString() ?? "Inf";
			var strEnd = VtxEnd?.ToString() ?? "Inf";
			return strStart + " - " + strEnd;
		}
		#endregion

		#region IComparable Members

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Virtual version of compare. </summary>
		///
		/// <remarks>
		/// There's probably a better way to handle this with abstract classes or something but I'm not
		/// sure right off the bat what it is and this works fine so I'm leaving well enough alone.
		/// Darrellp, 2/18/2011.
		/// </remarks>
		///
		/// <exception cref="Exception">	Thrown always. </exception>
		///
		/// <param name="edge">	The edge to compare. </param>
		///
		/// <returns>	Never returns. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		internal virtual int CompareToVirtual(WeEdge edge)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		int IComparable<WeEdge>.CompareTo(WeEdge other)
		{
			return CompareToVirtual(other);
		}
		#endregion

		#region Drawing

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Draw the edge onto a graphics object. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="g">					Graphics object to draw with. </param>
		/// <param name="pen">					Pen to draw the edge with. </param>
		/// <param name="infiniteLineLength">	Length long enough to guarantee we draw to the edge of
		/// 									the Graphics area. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void Draw(Graphics g, Pen pen, Single infiniteLineLength)
		{
			// Declare the two points we're going to draw a segment on
			PointD pt1, pt2;

			// If both vertices are at infinity, there's nothing to draw
			if (VtxStart.FAtInfinity && VtxEnd.FAtInfinity)
			{
				return;
			}

			// If one vertex is at infinity
			//
			// If one vertex is at infinity and the other isn't, replace the one at infinity with a "real point.
			// That point needs to be off the edge of the graphics area so that the resulting line is clipped at
			// the edge.  We can't use float.MaxValue because we're going to add to it and overflow so we really
			// have to rely on the caller to pass us some reasonable length in the InfiniteLineLength parameter
			// which will guarantee us to be off the edge.  The width plus the height of the graphics area would
			// normally be a good value to use for that parameter.
			if (VtxEnd.FAtInfinity || VtxStart.FAtInfinity)
			{
				// Locals
				WeVertex vtxFinite, vtxInfinite;

				// If it's the end vertex
				if (VtxEnd.FAtInfinity)
				{
					// Set finite to start and infinite to end
					vtxFinite = VtxStart;
					vtxInfinite = VtxEnd;
				}
				else
				{
					// Set finite to end and infinite to start
					vtxFinite = VtxEnd;
					vtxInfinite = VtxStart;
				}
				// pt1 is the finite vertex and pt2 is the infinite converted to a real point
				pt1 = vtxFinite.Pt;
				pt2 = vtxInfinite.ConvertToReal(vtxFinite.Pt, infiniteLineLength);
			}
			else
			{
				// Set pt1 and pt2 to start and end respectively
				pt1 = VtxStart.Pt;
				pt2 = VtxEnd.Pt;
			}

			// Draw a line from pt1 to pt2
			g.DrawLine(pen, pt1, pt2);

		}
		#endregion
	}
}
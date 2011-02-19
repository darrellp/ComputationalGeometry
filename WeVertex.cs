using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
	/// <summary>	
	/// The vertex in a WingedEdge data structure.  In order to accommodate edges that extend off the
	/// edge of the picture, we allow for two types of vertices - normal vertices and vertices at
	/// infinity.  Vertices at infinity really represent "directions" rather than true points and are
	/// normally associated with another (non-infinite) vertex in an edge. For a point at infinity,
	/// the coordinates don't represent the true coordinates of the "point" but rather represent a
	/// normal vertex in the proper direction.  The ability to represent these points at infinity as
	/// psuedo-points is crucial to making the WingedEdge structure work out since many of the edges
	/// in that structure will be between a point in the B-rep and a ray to infinity from that point.
	/// The standard WingedEdge structure makes no accommodations for such things, but we have to in
	/// the Voronoi diagram. 
	/// </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class WeVertex
	{
		#region Private Variables
		readonly List<WeEdge> _lstCWEdges = new List<WeEdge>();	// List of edges in clockwise order (if 
		#endregion

		#region Constructor

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Default constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeVertex() {}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Construct a vertex from a point. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="pt">	The point the vertex is located at. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeVertex(PT pt)
		{
			Pt = pt;
		}
		#endregion

		#region Infinite Vertices

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Make this a vertex at infinity. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="ptDirection">	The point direction. </param>
		/// <param name="fNormalize">	true to normalize. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected void SetInfinite(PT ptDirection, bool fNormalize)
		{
			FAtInfinity = true;
			if (fNormalize)
			{
				var norm = Geometry.Distance(new PT(0, 0), ptDirection);
				Pt = new PT(Pt.X / norm, Pt.Y / norm);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Convert to real physical point</summary>
		///
		/// <remarks>
		/// Points at infinity are just directions from real points.  Occasionally, though, we need a "real"
		/// point on the ray represented by the point at infinity.  This routine takes the starting point
		/// of the ray and the point at infinity and produces a point which lies on the ray.
		/// Darrellp, 2/18/2011. 
		/// </remarks>
		///
		/// <param name="ptStart">				The starting point of the ray. </param>
		/// <param name="rayLength">			Length along the ray from the starting point to our produced point. </param>
		///
		/// <returns>	A point on the ray different than the starting point. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PT ConvertToReal(PT ptStart, Single rayLength)
		{
			return new PT(
				Pt.X * rayLength + ptStart.X,
				Pt.Y * rayLength + ptStart.Y);
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return String.Format("{0}({1}, {2})", FAtInfinity ? "I" : "", Pt.X, Pt.Y);
		}
		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the count of edges abutting this vertex. </summary>
		///
		/// <value>	The count of edges. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public int CtEdges
		{
			get
			{
				return _lstCWEdges.Count;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the list of edges butting this vertex. </summary>
		///
		/// <value>	The list of edges. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public List<WeEdge> Edges
		{
			get
			{
				return _lstCWEdges;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the polygons which contain this vertex. </summary>
		///
		/// <value>	The polygons. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<WePolygon> Polygons
		{
			get
			{
				return new PolyEnumerable(this);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets a value indicating whether this is a point at infinity. </summary>
		///
		/// <value>	true if a point at infinity, false if not. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FAtInfinity { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the location of the ver. </summary>
		///
		/// <value>	The point. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PT Pt { get; set; }
		#endregion

		#region Modifiers

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Edges are assumed to be added in a Clockwise direction.  The first edge is random and has no
		/// particular significance. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <param name="edge">	Next clockwise edge to add. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public virtual void Add(WeEdge edge)
		{
			_lstCWEdges.Add(edge);
		}
		#endregion

		#region Validation

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Ensure that the passed in edge is adjacent to this vertex. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	Edge to check. </param>
		///
		/// <returns>	True if the edge is adjacent, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool FValidateEdgeIsAdjacent(WeEdge edge)
		{
			return _lstCWEdges.Any(edgeCur => ReferenceEquals(edgeCur, edge));
		}
		#endregion

		#region Queries

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Find the vertex on the other end of an edge. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	Edge to check. </param>
		///
		/// <returns>	Vertex at the other end of the edge. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WeVertex VtxOtherEnd(WeEdge edge)
		{
			// If we're the start vertex, then return the end and vice verse.
			return ReferenceEquals(edge.VtxStart, this) ? edge.VtxEnd : edge.VtxStart;
		}
		#endregion

		#region Internal Classes

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// WingedEdge doesn't directly give the polygons which contain this vertex so we give an
		/// enumerator for that. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class PolyEnumerator : IEnumerator<WePolygon>
		{
			#region Private Variables
			int _iEdge = -1;		// The index to the edge to the right of the current polygon
			readonly WeVertex _vtx;			// The vertex we're enumerating around
			#endregion

			#region Constructor
			internal PolyEnumerator(WeVertex vtx)
			{
				_vtx = vtx;
			}
			#endregion

			#region IEnumerator<WEPolygon> Members
			public WePolygon Current
			{
				get
				{
					WeEdge edge = _vtx._lstCWEdges[_iEdge];

					// The edge is to the right of the polygon so the polygon is to the left of
					// the edge as we look from start to end vertex.
					return (_vtx == edge.VtxStart) ?
					                               	edge.PolyLeft :
					                               	              	edge.PolyRight;
				}
			}
			#endregion

			#region IDisposable Members
			public void Dispose()
			{
			}
			#endregion

			#region IEnumerator Members

			object System.Collections.IEnumerator.Current
			{
				get
				{
					WeEdge edge = _vtx._lstCWEdges[_iEdge];

					// The edge is to the right of the polygon so the polygon is to the left of
					// the edge as we look from start to end vertex.
					return (_vtx == edge.VtxStart) ? 
					                               	edge.PolyLeft : 
					                               	              	edge.PolyRight;
				}
			}

			public bool MoveNext()
			{
				return ++_iEdge < _vtx._lstCWEdges.Count;
			}

			public void Reset()
			{
				_iEdge = 0;
			}

			#endregion
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	The enumerable for PolyEnumerator. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class PolyEnumerable : IEnumerable<WePolygon>
		{
			#region Private Variables
			readonly WeVertex _vtx;
			#endregion

			#region Constructor
			internal PolyEnumerable(WeVertex vtx)
			{
				_vtx = vtx;
			}
			#endregion

			#region IEnumerable<WEPolygon> Members
			IEnumerator<WePolygon> IEnumerable<WePolygon>.GetEnumerator()
			{
				return new PolyEnumerator(_vtx);
			}
			#endregion

			#region IEnumerable Members
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return new PolyEnumerator(_vtx);
			}
			#endregion
		}
		#endregion
	}
}
using System.Collections.Generic;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	
	/// Polygons in a WingedEdge structure.
	/// </summary>
	///
	/// <remarks>	
	/// Essentially a list of the edges comprising this polygon
	/// in clockwise order.  Which edge is first has no particular significance. 	/// 
	/// 
	/// Darrellp, 2/18/2011. 
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class WePolygon
	{
		#region Constructor

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Default constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WePolygon()
		{
			EdgesCW = new List<WeEdge>();
		}

		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	A place for users to store information. </summary>
		///
		/// <value>	User specific info. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public object Cookie { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the list of our edges in clockwise order. </summary>
		///
		/// <value>	The edges cw. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public List<WeEdge> EdgesCW { get; protected set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the number of vertices. </summary>
		///
		/// <value>	The number of vertexes. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public int VertexCount
		{
			get
			{
				return EdgesCW.Count;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets our vertices. </summary>
		///
		/// <value>	The vertices. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<WeVertex> Vertices
		{
			get
			{
				return new VertexEnumerable(this);
			}
		}
		#endregion

		#region Modifiers

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Add edges to this polygon	</summary>
		///
		/// <remarks>	
		/// Edges are expected to be added in a Clockwise direction. 
		/// Darrellp, 2/18/2011.
		/// </remarks>
		///
		/// <param name="edge">	Next edge to add in clockwise order. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void AddEdge(WeEdge edge)
		{
			EdgesCW.Add(edge);
		}
		#endregion

		#region Validation
		/// <summary>
		/// Place to set a breakpoint to detect failure in the validation routines
		/// </summary>
		/// <returns></returns>
		static bool Failure()
		{
			return false;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Ensure that a given edge is adjacent to this polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	Edge to be checked. </param>
		///
		/// <returns>	True if the edge is adjacent, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool FValidateEdgeIsAdjacent(WeEdge edge)
		{
			return EdgesCW.Contains(edge);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Ensure that all edges connect to each other. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <returns>	True if all edges connect in order, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal bool FValidateEdgesInOrder()
		{
			// Declarations
			int iNextEdge;
			int iEdge;

			// For each edge in Clockwise order
			for (iEdge = 0, iNextEdge = 1; iEdge < EdgesCW.Count; iEdge++)
			{
				// Retrieve the edge
				var edge = EdgesCW[iEdge];

				// If we need to wrap the index for the next edge
				if (iNextEdge == EdgesCW.Count)
				{
					// Wrap to 0
					iNextEdge = 0;
				}

				// If this edge doesn't hook to the next one
				if (!edge.FConnectsToEdge(EdgesCW[iNextEdge]))
				{
					return Failure();
				}

				// Continue around the polygon
				iNextEdge++;
			}
			return true;
		}
		#endregion

		#region Internal Classes

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// WingedEdge doesn't directly give the edges around a polygon so this is an enumerator for that. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class VertexEnumerator : IEnumerator<WeVertex>
		{
			#region Private Variables
			int _iEdge = -1;		// Index of the edge preceding this vertex in CW order
			readonly WePolygon _poly;		// Polygon whose vertices we're enumerating
			#endregion

			#region Constructor
			internal VertexEnumerator(WePolygon poly)
			{
				_poly = poly;
			}
			#endregion

			#region IEnumerator<WEPolygon> Members
			public WeVertex Current
			{
				get
				{
					var edge = _poly.EdgesCW[_iEdge];
					return (_poly == edge.PolyLeft) ? edge.VtxEnd : edge.VtxStart;
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
					var edge = _poly.EdgesCW[_iEdge];
					return (_poly == edge.PolyLeft) ? edge.VtxEnd : edge.VtxStart;
				}
			}

			public bool MoveNext()
			{
				return ++_iEdge < _poly.EdgesCW.Count;
			}

			public void Reset()
			{
				_iEdge = 0;
			}

			#endregion
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	An enumerator for the vertices of a polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class VertexEnumerable : IEnumerable<WeVertex>
		{
			#region Private Variables
			readonly WePolygon _poly;
			#endregion

			#region Constructor
			internal VertexEnumerable(WePolygon poly)
			{
				_poly = poly;
			}
			#endregion

			#region IEnumerable<WEPolygon> Members
			IEnumerator<WeVertex> IEnumerable<WeVertex>.GetEnumerator()
			{
				return new VertexEnumerator(_poly);
			}
			#endregion

			#region IEnumerable Members
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return new VertexEnumerator(_poly);
			}
			#endregion
		}
		#endregion
	}
}
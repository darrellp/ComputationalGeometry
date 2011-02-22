using System.Collections.Generic;
using System.Linq;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Polygons in a WingedEdge structure. </summary>
	///
	/// <remarks>	
	/// <para>Essentially a list of the edges comprising this polygon in clockwise order.  Which edge
	/// is first has no particular significance.</para>
	/// 
	/// Darrellp, 2/18/2011. 
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class WePolygon
	{
		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	A place for users to store information. </summary>
		///
		/// <value>	User specific info. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public object Cookie { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the first edge of an enumeration. </summary>
		///
		/// <value>	The first edge in an enumeration. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		internal WeEdge FirstEdge { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the list of our edges in clockwise order. </summary>
		///
		/// <value>	The edges cw. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public List<WeEdge> EdgesCWwe { get; protected set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the edges in clockwise order. </summary>
		///
		/// <value>	The edges. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<OrientedEdge> Edges
		{
			get
			{
				return new EdgeEnumerable(this);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the vertices in clockwise order. </summary>
		///
		/// <value>	The vertices. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<WeVertex> Vertices
		{
			get
			{
				return Edges.Select(oe => oe.Forward ? oe.Edge.VtxStart : oe.Edge.VtxEnd);
			}
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
			return edge.PolyLeft == this || edge.PolyRight == this;
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
			var fFirstTimeThroughLoop = true;
			var edgePrev = new OrientedEdge();
			var edgeFirst = new OrientedEdge();

			foreach (var orientedEdge in Edges)
			{
				if (fFirstTimeThroughLoop)
				{
					fFirstTimeThroughLoop = false;
					edgePrev = edgeFirst = orientedEdge;
				}
				else
				{
					if (!orientedEdge.Edge.FConnectsToEdge(edgePrev.Edge))
					{
						return Failure();
					}
					edgePrev = orientedEdge;
				}
			}
			if (!edgePrev.Edge.FConnectsToEdge(edgeFirst.Edge))
			{
				return Failure();
			}
			return true;
		}
		#endregion

		#region Internal Classes

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Gives an edge along with an orientation showing whether it starts at the actual StartVertex
		/// or is reversed and starts at the EndVertex.  I wouldn't have to do this if I'd have used the
		/// half edge structure rather than winged edge.  Live and learn. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/22/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public struct OrientedEdge
		{
			internal OrientedEdge(WeEdge edge, bool fForward)
			{
				Forward = fForward;
				Edge = edge;
			}

			/// <summary> True if the edge travels from the StartVertex to the EndVertex.  If false
			/// then we should traverse the edge from the EndVertex to the StartVertex. </summary>
			public bool Forward;

			/// <summary> The edge in question </summary>
			public WeEdge Edge;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// The standard WingedEdge doesn't directly give the edges around a polygon so this is an enumerator for that. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class EdgeEnumerator : IEnumerator<OrientedEdge>
		{
			#region Private Variables
			private WeEdge _edgeCur;
			readonly WePolygon _poly;		// Polygon whose vertices we're enumerating
			#endregion

			#region Properties
			bool FForward
			{
				get
				{
					return _poly == _edgeCur.PolyLeft;
				}
			}
			#endregion

			#region Constructor
			internal EdgeEnumerator(WePolygon poly)
			{
				_poly = poly;
				_edgeCur = poly.FirstEdge;
			}
			#endregion

			#region IEnumerator<WEPolygon> Members
			public OrientedEdge Current
			{
				get
				{
					return new OrientedEdge(_edgeCur, FForward);
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
					return Current;
				}
			}

			public bool MoveNext()
			{
				_edgeCur = FForward ? _edgeCur.EdgeCWSuccessor : _edgeCur.EdgeCWPredecessor;
				return _edgeCur == _poly.FirstEdge;
			}

			public void Reset()
			{
				_edgeCur = _poly.FirstEdge;
			}

			#endregion
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	An enumerator for the vertices of a polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class EdgeEnumerable : IEnumerable<OrientedEdge>
		{
			#region Private Variables
			readonly WePolygon _poly;
			#endregion

			#region Constructor
			internal EdgeEnumerable(WePolygon poly)
			{
				_poly = poly;
			}
			#endregion

			#region IEnumerable<OrientedEdge> Members
			IEnumerator<OrientedEdge> IEnumerable<OrientedEdge>.GetEnumerator()
			{
				return new EdgeEnumerator(_poly);
			}
			#endregion

			#region IEnumerable Members
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return new EdgeEnumerator(_poly);
			}
			#endregion
		}
		#endregion
	}
}
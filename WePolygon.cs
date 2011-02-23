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
		/// <summary>	Gets the edges in clockwise order. </summary>
		///
		/// <value>	The edges. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public IEnumerable<WeEdge> Edges
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
				return Edges.Select(e => ReferenceEquals(e.PolyLeft, this) ? e.VtxStart : e.VtxEnd);
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
			var edgePrev = new WeEdge();
			var edgeFirst = new WeEdge();

			foreach (var edgeCur in Edges)
			{
				if (fFirstTimeThroughLoop)
				{
					fFirstTimeThroughLoop = false;
					edgePrev = edgeFirst = edgeCur;
				}
				else
				{
					if (!edgeCur.FConnectsToEdge(edgePrev))
					{
						return Failure();
					}
					edgePrev = edgeCur;
				}
			}
			return edgePrev.FConnectsToEdge(edgeFirst) || Failure();
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

		class EdgeEnumerator : IEnumerator<WeEdge>
		{
			#region Private Variables

			readonly WePolygon _poly;		// Polygon whose vertices we're enumerating
			#endregion

			#region Constructor
			internal EdgeEnumerator(WePolygon poly)
			{
				_poly = poly;
			}
			#endregion

			#region IEnumerator<WEPolygon> Members

			public WeEdge Current { get; private set; }

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
				// If this is the first call
				if (Current == null)
				{
					// Use the first edge
					Current = _poly.FirstEdge;

					// Return true only if there's an edge in the polygon at all
					return Current != null;
				}

				// If not our first call, just get the next edge until we've looped back
				Current = ReferenceEquals(Current.PolyLeft, _poly) ? Current.EdgeCWSuccessor : Current.EdgeCWPredecessor;
				return !ReferenceEquals(Current, _poly.FirstEdge);
			}

			public void Reset()
			{
				Current = null;
			}

			#endregion
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	An enumerator for the vertices of a polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		class EdgeEnumerable : IEnumerable<WeEdge>
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
			IEnumerator<WeEdge> IEnumerable<WeEdge>.GetEnumerator()
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
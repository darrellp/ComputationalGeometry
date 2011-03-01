using System.Linq;
using System.Collections.Generic;
#if NUNIT || DEBUG
using NUnit.Framework;
#endif

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	
	/// Winged Edge data structure for representing a B-rep or "boundary representation".
	/// </summary>
	///
	/// <remarks>
	/// Intuitively,
	/// a B-rep can be though of as the representation for a map of polygonal countries.  This is
	/// precisely what the voronoi diagram returns.  Winged Edge is a bit redundant in it's
	/// representation but provides a very flexible structure for working with the underlying B-rep.
	/// WingedEdge centers on the edges and represents each edge by the polygons on each side of it
	/// and the next edge in each of those polygons.  Geometrically, the information retained with
	/// each edge looks like:
	/// 				\      /
	/// Other Edges		 \____/    Other Edges
	/// 				 /    \
	/// 				/      \
	/// The "WingedEdge" name comes from the resemblance of this diagram to a butterfly.
	/// 
	/// The version here is not much more than a shell.  It has some validation routines and some
	/// navigation routines, but it's pretty much up to the user to set up the structure via adding
	/// Polygons, vertices and edges to the structure and ensuring that their fields are set up
	/// correctly.
	/// 
	/// Darrellp, 2/18/2011.
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class WingedEdge<P, E, V>
		where P:WePolygon
		where E:WeEdge
		where V:WeVertex
	{
		#region Constructor

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Default constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public WingedEdge()
		{
			LstVertices = new List<V>();
			LstEdges = new List<E>();
			LstPolygons = new List<P>();
		}

		#endregion

		#region Properties

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the list of contained polygons. </summary>
		///
		/// <value>	The list of contained polygons. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<P> LstPolygons { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the list of contained edges. </summary>
		///
		/// <value>	The list of contained edges. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<E> LstEdges { get; set; }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets or sets the list of contained vertices. </summary>
		///
		/// <value>	The list of contained vertices. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public List<V> LstVertices { get; set; }

		#endregion

		#region Validation
		/// <summary>
		/// Set a breakpoint in this routine to find out when Validate() fails.
		/// </summary>
		/// <returns>Always false</returns>
		private static bool Failure()
		{
			return false;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Validates the WingedEdge structure in several ways. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <returns>	true if the winged edge structure is valid. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool Validate()
		{
			// Each edge has two vertices associated with it so we can have at most twice as many
			// vertices as edges...
			if (LstVertices.Count > 2 * LstEdges.Count)
			{
				return Failure();
			}

			// Validate the edges one by one
			if (LstEdges.Any(edge => !edge.Validate()))
			{
				return Failure();
			}

			// Validate the polygons one by one
			if (LstPolygons.Any(poly => !poly.FValidateEdgesInOrder()))
			{
				return Failure();
			}
			return true;
		}
		#endregion

		#region Adding elements

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Adds an edge. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="edge">	The edge to be added. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void AddEdge(E edge)
		{
			LstEdges.Add(edge);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Adds a polygon. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="polygon">	The polygon to be added. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void AddPoly(P polygon)
		{
			LstPolygons.Add(polygon);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Adds a vertex. </summary>
		///
		/// <remarks>	Darrellp, 2/18/2011. </remarks>
		///
		/// <param name="vertex">	The vertex to be added. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void AddVertex(V vertex)
		{
			LstVertices.Add(vertex);
		}
		#endregion

	}

	#region NUnit
#if DEBUG || NUNIT
	[TestFixture]
	public class TestWingedEdge
	{
		#region Private Variables
		WingedEdge<WePolygon, WeEdge,WeVertex> _we;
		#endregion

		#region Example
		// Produce a WE which looks like this:
		//			         0---0---1
		//			  0      |       |
		//                   1   1   2
		//			         |       |
		//			2----3---3---4---4----5---5
		//			|        |       |        |
		//          6    2   7   3   8    4   9
		//			|        |       |        |
		//			6---10---7--11---8---12---9
		//			         |       |
		//                  13   5  14
		//			         |       |
		//			        10--15--11
		// with the outer region representing the "polygon at infinity", polygon 0.
		// All elements are indexed starting at the upper left and working our way
		// left to right then top to bottom.
		static WingedEdge<WePolygon, WeEdge, WeVertex> WeExample()
		{
			var edges = new List<WeEdge>(16);
			var vertices = new List<WeVertex>(12);
			var polys = new List<WePolygon>(6);
			var we = new WingedEdge<WePolygon, WeEdge, WeVertex>();

			for (var i = 0; i < polys.Capacity; i++)
			{
				polys.Add(new WePolygon());
			}
			for (var i = 0; i < edges.Capacity; i++)
			{
				edges.Add(new WeEdge());
			}
			for (var i = 0; i < vertices.Capacity; i++)
			{
				vertices.Add(new WeVertex());
			}

			we.LstEdges = edges;
			we.LstPolygons = polys;
			we.LstVertices = vertices;

			#region Polygons
			// Polygon at infinity
			polys[0].FirstEdge = edges[0];

			// Top Square
			polys[1].FirstEdge = edges[0];

			// Left Square
			polys[2].FirstEdge = edges[3];

			// Center Square
			polys[3].FirstEdge = edges[4];

			// Right Square
			polys[4].FirstEdge = edges[5];

			// Bottom Square
			polys[5].FirstEdge = edges[11];
			#endregion

			#region Vertices
			vertices[0].Pt = new PointD(1, 3);
			vertices[1].Pt = new PointD(2, 3);
			vertices[2].Pt = new PointD(0, 2);
			vertices[3].Pt = new PointD(1, 2);
			vertices[4].Pt = new PointD(2, 2);
			vertices[5].Pt = new PointD(3, 2);
			vertices[6].Pt = new PointD(0, 1);
			vertices[7].Pt = new PointD(1, 1);
			vertices[8].Pt = new PointD(2, 1);
			vertices[9].Pt = new PointD(3, 1);
			vertices[10].Pt = new PointD(1, 0);
			vertices[11].Pt = new PointD(2, 0);

			vertices[0].FirstEdge = edges[0];
			vertices[1].FirstEdge = edges[0];
			vertices[2].FirstEdge = edges[3];
			vertices[3].FirstEdge = edges[3];
			vertices[4].FirstEdge = edges[2];
			vertices[5].FirstEdge = edges[5];
			vertices[6].FirstEdge = edges[6];
			vertices[7].FirstEdge = edges[7];
			vertices[8].FirstEdge = edges[8];
			vertices[9].FirstEdge = edges[9];
			vertices[10].FirstEdge = edges[13];
			vertices[11].FirstEdge = edges[14];
			#endregion

			#region Edges
			#region Start/stop vertices
			edges[0].VtxStart = vertices[0];
			edges[0].VtxEnd = vertices[1];

			edges[1].VtxStart = vertices[0];
			edges[1].VtxEnd = vertices[3];

			edges[2].VtxStart = vertices[1];
			edges[2].VtxEnd = vertices[4];

			edges[3].VtxStart = vertices[2];
			edges[3].VtxEnd = vertices[3];

			edges[4].VtxStart = vertices[3];
			edges[4].VtxEnd = vertices[4];

			edges[5].VtxStart = vertices[4];
			edges[5].VtxEnd = vertices[5];

			edges[6].VtxStart = vertices[2];
			edges[6].VtxEnd = vertices[6];

			edges[7].VtxStart = vertices[3];
			edges[7].VtxEnd = vertices[7];

			edges[8].VtxStart = vertices[4];
			edges[8].VtxEnd = vertices[8];

			edges[9].VtxStart = vertices[5];
			edges[9].VtxEnd = vertices[9];

			edges[10].VtxStart = vertices[6];
			edges[10].VtxEnd = vertices[7];

			edges[11].VtxStart = vertices[7];
			edges[11].VtxEnd = vertices[8];

			edges[12].VtxStart = vertices[8];
			edges[12].VtxEnd = vertices[9];

			edges[13].VtxStart = vertices[7];
			edges[13].VtxEnd = vertices[10];

			edges[14].VtxStart = vertices[8];
			edges[14].VtxEnd = vertices[11];

			edges[15].VtxStart = vertices[10];
			edges[15].VtxEnd = vertices[11];
			#endregion

			#region Polygons
			edges[0].PolyLeft = polys[0];
			edges[0].PolyRight = polys[1];

			edges[1].PolyLeft = polys[1];
			edges[1].PolyRight = polys[0];

			edges[2].PolyLeft = polys[0];
			edges[2].PolyRight = polys[1];

			edges[3].PolyLeft = polys[0];
			edges[3].PolyRight = polys[2];

			edges[4].PolyLeft = polys[1];
			edges[4].PolyRight = polys[3];

			edges[5].PolyLeft = polys[0];
			edges[5].PolyRight = polys[4];

			edges[6].PolyLeft = polys[2];
			edges[6].PolyRight = polys[0];

			edges[7].PolyLeft = polys[3];
			edges[7].PolyRight = polys[2];

			edges[8].PolyLeft = polys[4];
			edges[8].PolyRight = polys[3];

			edges[9].PolyLeft = polys[0];
			edges[9].PolyRight = polys[4];

			edges[10].PolyLeft = polys[2];
			edges[10].PolyRight = polys[0];

			edges[11].PolyLeft = polys[3];
			edges[11].PolyRight = polys[5];

			edges[12].PolyLeft = polys[4];
			edges[12].PolyRight = polys[0];

			edges[13].PolyLeft = polys[5];
			edges[13].PolyRight = polys[0];

			edges[14].PolyLeft = polys[0];
			edges[14].PolyRight = polys[5];

			edges[15].PolyLeft = polys[5];
			edges[15].PolyRight = polys[0];

			#endregion

			#region Predecessor/successor edges
			edges[0].EdgeCCWPredecessor = edges[1];
			edges[0].EdgeCWPredecessor = edges[1];
			edges[0].EdgeCCWSuccessor = edges[2];
			edges[0].EdgeCWSuccessor = edges[2];

			edges[1].EdgeCCWPredecessor = edges[0];
			edges[1].EdgeCWPredecessor = edges[0];
			edges[1].EdgeCCWSuccessor = edges[3];
			edges[1].EdgeCWSuccessor = edges[4];

			edges[2].EdgeCCWPredecessor = edges[0];
			edges[2].EdgeCWPredecessor = edges[0];
			edges[2].EdgeCCWSuccessor = edges[4];
			edges[2].EdgeCWSuccessor = edges[5];

			edges[3].EdgeCCWPredecessor = edges[6];
			edges[3].EdgeCWPredecessor = edges[6];
			edges[3].EdgeCCWSuccessor = edges[7];
			edges[3].EdgeCWSuccessor = edges[1];

			edges[4].EdgeCCWPredecessor = edges[1];
			edges[4].EdgeCWPredecessor = edges[7];
			edges[4].EdgeCCWSuccessor = edges[8];
			edges[4].EdgeCWSuccessor = edges[2];

			edges[5].EdgeCCWPredecessor = edges[2];
			edges[5].EdgeCWPredecessor = edges[8];
			edges[5].EdgeCCWSuccessor = edges[9];
			edges[5].EdgeCWSuccessor = edges[9];

			edges[6].EdgeCCWPredecessor = edges[3];
			edges[6].EdgeCWPredecessor = edges[3];
			edges[6].EdgeCCWSuccessor = edges[10];
			edges[6].EdgeCWSuccessor = edges[10];

			edges[7].EdgeCCWPredecessor = edges[4];
			edges[7].EdgeCWPredecessor = edges[3];
			edges[7].EdgeCCWSuccessor = edges[10];
			edges[7].EdgeCWSuccessor = edges[11];

			edges[8].EdgeCCWPredecessor = edges[5];
			edges[8].EdgeCWPredecessor = edges[4];
			edges[8].EdgeCCWSuccessor = edges[11];
			edges[8].EdgeCWSuccessor = edges[12];

			edges[9].EdgeCCWPredecessor = edges[5];
			edges[9].EdgeCWPredecessor = edges[5];
			edges[9].EdgeCCWSuccessor = edges[12];
			edges[9].EdgeCWSuccessor = edges[12];

			edges[10].EdgeCCWPredecessor = edges[6];
			edges[10].EdgeCWPredecessor = edges[6];
			edges[10].EdgeCCWSuccessor = edges[13];
			edges[10].EdgeCWSuccessor = edges[7];

			edges[11].EdgeCCWPredecessor = edges[7];
			edges[11].EdgeCWPredecessor = edges[13];
			edges[11].EdgeCCWSuccessor = edges[14];
			edges[11].EdgeCWSuccessor = edges[8];

			edges[12].EdgeCCWPredecessor = edges[8];
			edges[12].EdgeCWPredecessor = edges[14];
			edges[12].EdgeCCWSuccessor = edges[9];
			edges[12].EdgeCWSuccessor = edges[9];

			edges[13].EdgeCCWPredecessor = edges[11];
			edges[13].EdgeCWPredecessor = edges[10];
			edges[13].EdgeCCWSuccessor = edges[15];
			edges[13].EdgeCWSuccessor = edges[15];

			edges[14].EdgeCCWPredecessor = edges[12];
			edges[14].EdgeCWPredecessor = edges[11];
			edges[14].EdgeCCWSuccessor = edges[15];
			edges[14].EdgeCWSuccessor = edges[15];

			edges[15].EdgeCCWPredecessor = edges[13];
			edges[15].EdgeCWPredecessor = edges[13];
			edges[15].EdgeCCWSuccessor = edges[14];
			edges[15].EdgeCWSuccessor = edges[14];

			#endregion
			#endregion

			return we;
		}
		#endregion

		[SetUp]
		public void Setup()
		{
			_we = WeExample();
		}

		[Test]
		public void TestExampleCreation()
		{
			Assert.IsNotNull(_we);
			Assert.IsTrue(_we.Validate());
		}

		[Test]
		public void TestVertexEnumerationAtPolygon()
		{
			List<WeVertex> lstvtxAroundPoly1 = _we.LstPolygons[1].Vertices.ToList();

			Assert.AreEqual(lstvtxAroundPoly1.Count, 4);
			Assert.IsTrue(lstvtxAroundPoly1.Contains(_we.LstVertices[0]));
			Assert.IsTrue(lstvtxAroundPoly1.Contains(_we.LstVertices[1]));
			Assert.IsTrue(lstvtxAroundPoly1.Contains(_we.LstVertices[3]));
			Assert.IsTrue(lstvtxAroundPoly1.Contains(_we.LstVertices[4]));
		}

		[Test]
		public void TestPolygonEnumerationAtVertex()
		{
			List<WePolygon> lstplyAroundVertex8 = _we.LstVertices[8].Polygons.ToList();

			Assert.AreEqual(lstplyAroundVertex8.Count, 4);
			Assert.IsTrue(lstplyAroundVertex8.Contains(_we.LstPolygons[0]));
			Assert.IsTrue(lstplyAroundVertex8.Contains(_we.LstPolygons[5]));
			Assert.IsTrue(lstplyAroundVertex8.Contains(_we.LstPolygons[3]));
			Assert.IsTrue(lstplyAroundVertex8.Contains(_we.LstPolygons[4]));
		}
	}
#endif
		#endregion
}

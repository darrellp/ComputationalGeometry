#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using System.Linq;
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;

namespace DAP.CompGeom
{
	/// <summary>
	/// Winged Edge data structure for representing a B-rep or "boundary representation".
	/// Intuitively, a B-rep can be though of as the represnetation for a map of polygonal
	/// countries.  This is precisely what the voronoi diagram returns.  Winged Edge is a
	/// bit redundant in it's representation but provides a very flexible structure for
	/// working with the underlying B-rep.  WingedEdge centers on the edges and represents
	/// each edge by the polygons on each side of it and the next edge in each of those
	/// polygons.  Geometrically, the information retained with each edge looks like:
	///					\      /
	///	Other Edges		 \____/    Other Edges
	/// 				 /    \
	/// 				/      \
	/// The "WingedEdge" name comes from the resemblance of this diagram to a butterfly.
	/// </summary>
	
	public class WingedEdge
	{
		#region Private variables
		/// <summary>
		/// WingedEdge represents its B-rep as a list of edges, polygons and vertices...
		/// </summary>
		List<WeEdge> _lstEdges = new List<WeEdge>();
		List<WePolygon> _lstPolygons = new List<WePolygon>();
		List<WeVertex> _lstVertices = new List<WeVertex>();
		#endregion

		#region Properties
		public List<WePolygon> LstPolygons
		{
			get { return _lstPolygons; }
			set { _lstPolygons = value; }
		}

		public List<WeEdge> LstEdges
		{
			get { return _lstEdges; }
			set { _lstEdges = value; }
		}

		public List<WeVertex> LstVertices
		{
			get { return _lstVertices; }
			set { _lstVertices = value; }
		}
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

		/// <summary>
		/// Validates the WingedEdge structure in several ways.
		/// </summary>
		/// <returns></returns>
		public bool Validate()
		{
			// Each edge has two vertices associated with it so we can have at most twice as many
			// vertices as edges...
			if (_lstVertices.Count > 2 * _lstEdges.Count)
			{
				return Failure();
			}

			// Validate the edges one by one
			if (_lstEdges.Any(edge => !edge.Validate()))
			{
				return Failure();
			}

			// Validate the polygons one by one
			if (_lstPolygons.Any(poly => !poly.FValidateEdgesInOrder()))
			{
				return Failure();
			}
			return true;
		}
		#endregion

		#region Adding elements
		public void AddEdge(WeEdge edge)
		{
			_lstEdges.Add(edge);
		}

		public void AddPoly(WePolygon polygon)
		{
			_lstPolygons.Add(polygon);
		}

		public void AddVertex(WeVertex vertex)
		{
			_lstVertices.Add(vertex);
		}
		#endregion

		#region NUnit
#if DEBUG || NUNIT
		[TestFixture]
		public class TestWingedEdge
		{
			#region Private Variables
			WingedEdge _we;
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
			static WingedEdge WeExample()
			{
				List<WeEdge> edges = new List<WeEdge>(16);
				List<WeVertex> vertices = new List<WeVertex>(12);
				List<WePolygon> polys = new List<WePolygon>(6);
				WingedEdge we = new WingedEdge();

				for (int i = 0; i < polys.Capacity; i++)
				{
					polys.Add(new WePolygon());
				}
				for (int i = 0; i < edges.Capacity; i++)
				{
					edges.Add(new WeEdge());
				}
				for (int i = 0; i < vertices.Capacity; i++)
				{
					vertices.Add(new WeVertex());
				}

				we.LstEdges = edges;
				we.LstPolygons = polys;
				we.LstVertices = vertices;

				#region Polygons
				for (int iPoly = 0; iPoly < polys.Count; iPoly++)
				{
					polys[iPoly].Cookie = iPoly;
				}

				// Polygon at infinity
				polys[0].AddEdge(edges[0]);
				polys[0].AddEdge(edges[1]);
				polys[0].AddEdge(edges[3]);
				polys[0].AddEdge(edges[6]);
				polys[0].AddEdge(edges[10]);
				polys[0].AddEdge(edges[13]);
				polys[0].AddEdge(edges[15]);
				polys[0].AddEdge(edges[14]);
				polys[0].AddEdge(edges[12]);
				polys[0].AddEdge(edges[9]);
				polys[0].AddEdge(edges[5]);
				polys[0].AddEdge(edges[2]);

				// Top Square
				polys[1].AddEdge(edges[0]);
				polys[1].AddEdge(edges[2]);
				polys[1].AddEdge(edges[4]);
				polys[1].AddEdge(edges[1]);

				// Left Square
				polys[2].AddEdge(edges[3]);
				polys[2].AddEdge(edges[7]);
				polys[2].AddEdge(edges[10]);
				polys[2].AddEdge(edges[6]);

				// Center Square
				polys[3].AddEdge(edges[4]);
				polys[3].AddEdge(edges[8]);
				polys[3].AddEdge(edges[11]);
				polys[3].AddEdge(edges[7]);

				// Right Square
				polys[4].AddEdge(edges[5]);
				polys[4].AddEdge(edges[9]);
				polys[4].AddEdge(edges[12]);
				polys[4].AddEdge(edges[8]);

				// Bottom Square
				polys[5].AddEdge(edges[11]);
				polys[5].AddEdge(edges[14]);
				polys[5].AddEdge(edges[15]);
				polys[5].AddEdge(edges[13]);
				#endregion

				#region Vertices
				for (int iVertex = 0; iVertex < vertices.Count; iVertex++)
				{
					vertices[iVertex].Cookie = iVertex;
				}

				vertices[0].Pt = new PT(1, 3);
				vertices[1].Pt = new PT(2, 3);
				vertices[2].Pt = new PT(0, 2);
				vertices[3].Pt = new PT(1, 2);
				vertices[4].Pt = new PT(2, 2);
				vertices[5].Pt = new PT(3, 2);
				vertices[6].Pt = new PT(0, 1);
				vertices[7].Pt = new PT(1, 1);
				vertices[8].Pt = new PT(2, 1);
				vertices[9].Pt = new PT(3, 1);
				vertices[10].Pt = new PT(1, 0);
				vertices[11].Pt = new PT(2, 0);

				vertices[0].Add(edges[0]);
				vertices[0].Add(edges[1]);

				vertices[1].Add(edges[0]);
				vertices[1].Add(edges[2]);

				vertices[2].Add(edges[3]);
				vertices[2].Add(edges[6]);

				vertices[3].Add(edges[3]);
				vertices[3].Add(edges[1]);
				vertices[3].Add(edges[4]);
				vertices[3].Add(edges[7]);

				vertices[4].Add(edges[2]);
				vertices[4].Add(edges[5]);
				vertices[4].Add(edges[4]);
				vertices[4].Add(edges[8]);

				vertices[5].Add(edges[5]);
				vertices[5].Add(edges[9]);

				vertices[6].Add(edges[6]);
				vertices[6].Add(edges[10]);

				vertices[7].Add(edges[7]);
				vertices[7].Add(edges[11]);
				vertices[7].Add(edges[13]);
				vertices[7].Add(edges[10]);

				vertices[8].Add(edges[8]);
				vertices[8].Add(edges[12]);
				vertices[8].Add(edges[14]);
				vertices[8].Add(edges[11]);

				vertices[9].Add(edges[9]);
				vertices[9].Add(edges[12]);

				vertices[10].Add(edges[13]);
				vertices[10].Add(edges[15]);

				vertices[11].Add(edges[14]);
				vertices[11].Add(edges[15]);
				#endregion

				#region Edges
				for (int iEdge = 0; iEdge < edges.Count; iEdge++)
				{
					edges[iEdge].Cookie = iEdge;
				}

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

	/// <summary>
	/// The Edge in a WingedEdge data structure
	/// </summary>
	public class WeEdge : IComparable<WeEdge>
	{
		#region Private Variables
		#endregion

		#region Properties
		public object Cookie { get; set; }
		public WeVertex VtxStart { get; set; }
		public WeVertex VtxEnd { get; set; }
		public WeEdge EdgeCWSuccessor { get; set; }
		public WeEdge EdgeCCWSuccessor { get; set; }
		public WePolygon PolyRight { get; set; }
		public WePolygon PolyLeft { get; set; }
		public WeEdge EdgeCWPredecessor { get; set; }
		public WeEdge EdgeCCWPredecessor { get; set; }
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
			// All variables should be set
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

			// Check adjacency of all listed edges to the proper polygons
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

			return true;
		}

		/// <summary>
		/// Ensure that this edge connects to the passed in edge
		/// </summary>
		/// <param name="edge">Edge to check</param>
		/// <returns>True if this edge connects to the passed in edge, else false</returns>
		internal bool FConnectsToEdge(WeEdge edge)
		{
			// Has to be adjacent to either our start or our end vertex
			return VtxEnd.FValidateEdgeIsAdjacent(edge) || VtxStart.FValidateEdgeIsAdjacent(edge);
		}
		#endregion

		#region Geometry
		/// <summary>
		/// Determine if a point is to the left of this edge when facing from the start vertex
		/// to the end vertex.
		/// </summary>
		/// <param name="pt">Point to check out</param>
		/// <returns></returns>
		public bool FLeftOf(PT pt)
		{
			PT pt1, pt2;

			// If one of the points is at infinity, replace it with a real point
			if (VtxEnd.FAtInfinity || VtxStart.FAtInfinity)
			{

				if (VtxEnd.FAtInfinity)
				{
					pt1 = VtxStart.Pt;
					pt2 = VtxEnd.ConvertToReal(pt1, 10);
				}
				else
				{
					pt1 = VtxEnd.Pt;
					pt2 = VtxStart.ConvertToReal(pt1, 10);
				}
			}
			else
			{
				pt1 = VtxStart.Pt;
				pt2 = VtxEnd.Pt;
			}

			return Geometry.FLeft(pt1, pt2, pt);
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			string strStart = VtxStart == null ? "Inf" : VtxStart.ToString();
			string strEnd = VtxEnd == null ? "Inf" : VtxEnd.ToString();
			return strStart + " - " + strEnd;
		}
		#endregion

		#region IComparable Members
		virtual public int CompareToVirtual(WeEdge edge)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		int IComparable<WeEdge>.CompareTo(WeEdge other)
		{
			return CompareToVirtual(other);
		}
		#endregion

		#region Drawing
		/// <summary>
		/// Draw the edge onto a graphics object
		/// </summary>
		/// <param name="g">Graphis object to draw with</param>
		/// <param name="pen">Pen to draw the edge with</param>
		/// <param name="infiniteLineLength">Length long enough to guarantee we draw to the edge of the Graphics area</param>
		public void Draw(Graphics g, Pen pen, TPT infiniteLineLength)
		{
			PT pt1, pt2;

			// If both vertices are at infinity, there's nothing to draw
			if (VtxStart.FAtInfinity && VtxEnd.FAtInfinity)
			{
				return;
			}

			// If one vertex is at infinity and the other isn't, replace the one at infinity with a "real point.
			// That point needs to be off the edge of the graphics area so that the resulting line is clipped at
			// the edge.  We can't use float.MaxValue because we're going to add to it and overflow so we really
			// have to rely on the caller to pass us some reasonable length in the InfiniteLineLength parameter
			// which will guarantee us to be off the edge.  The width plus the height of the graphics area would
			// normally be a good value to use for that parameter.
			if (VtxEnd.FAtInfinity || VtxStart.FAtInfinity)
			{
				WeVertex vtxFinite, vtxInfinite;

				if (VtxEnd.FAtInfinity)
				{
					vtxFinite = VtxStart;
					vtxInfinite = VtxEnd;
				}
				else
				{
					vtxFinite = VtxEnd;
					vtxInfinite = VtxStart;
				}
				pt1 = vtxFinite.Pt;
				pt2 = vtxInfinite.ConvertToReal(vtxFinite.Pt, infiniteLineLength);
			}
			else
			{
				pt1 = VtxStart.Pt;
				pt2 = VtxEnd.Pt;
			}

			g.DrawLine(pen, pt1, pt2);
		}
		#endregion
	}

	/// <summary>
	/// The vertex in a WingedEdge data structure.  In order to accommodate edges that extend
	/// off the edge of the picture, we allow for two types of vertices - normal vertices and
	/// vertices at infinity.  Vertices at infinity really represent "directions" rather than
	/// true points and are normally associated with another (non-infinite) vertex in an edge.
	/// For a point at infinity, the coordinates don't represent the true coordinates of the
	/// "point" but rather represent a normal vertex in the proper direction.  The ability to
	/// represent these points at infinity as psuedo-points is crucial to making the WingedEdge
	/// structure work out since many of the edges in that structure will be between a point in
	/// the B-rep and a ray to infinity from that point.  The standard WingedEdge structure
	/// makes no accommodations for such things, but we have to in the Voronoi diagram.
	/// </summary>
	public class WeVertex
	{
		#region Private Variables
		readonly List<WeEdge> _lstCWEdges = new List<WeEdge>();	// List of edges in clockwise order (if 
		#endregion

		#region Constructor
		public WeVertex()
		{
		}

		public WeVertex(PT pt)
		{
			Pt = pt;
		}

		public WeVertex(PT pt, Object cookie) : this(pt)
		{
			Cookie = cookie;
		}
		#endregion

		#region Infinite Vertices
		protected void SetInfinite(PT ptDirection, bool fNormalize)
		{
			FAtInfinity = true;
			if (fNormalize)
			{
				TPT norm = Geometry.Distance(new PT(0, 0), ptDirection);
				Pt = new PT(Pt.X / norm, Pt.Y / norm);
			}
		}

		public PT ConvertToReal(PT ptStart, TPT infiniteLineLength)
		{
			return new PT(
				Pt.X * infiniteLineLength + ptStart.X,
				Pt.Y * infiniteLineLength + ptStart.Y);
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return String.Format("{0}({1}, {2})", FAtInfinity ? "I" : "", Pt.X, Pt.Y);
		}
		#endregion

		#region Properties
		public object Cookie { get; set; }

		public int CtEdges
		{
			get
			{
				return _lstCWEdges.Count;
			}
		}

		public List<WeEdge> Edges
		{
			get
			{
				return _lstCWEdges;
			}
		}

		public IEnumerable<WePolygon> Polygons
		{
			get
			{
				return new PolyEnumerable(this);
			}
		}

		public bool FAtInfinity { get; set; }

		public PointF Pt { get; set; }
		#endregion

		#region Modifiers
		/// <summary>
		/// Edges are assumed to be added in a Clockwise direction.  The first edge is random
		/// and has no particular significance.
		/// </summary>
		/// <param name="edge">Next clockwise edge to add</param>
		public virtual void Add(WeEdge edge)
		{
			_lstCWEdges.Add(edge);
		}
		#endregion

		#region Validation
		/// <summary>
		/// Ensure that the passed in edge is adjacent to this vertex
		/// </summary>
		/// <param name="edge">Edge to check</param>
		/// <returns>True if the edge is adjacent, else false</returns>
		internal bool FValidateEdgeIsAdjacent(WeEdge edge)
		{
			return _lstCWEdges.Any(edgeCur => ReferenceEquals(edgeCur, edge));
		}
		#endregion

		#region Queries
		/// <summary>
		/// Find the vertex on the other end of an edge.
		/// </summary>
		/// <param name="edge">Edge to check</param>
		/// <returns>Vertex at the other end of the edge</returns>
		public WeVertex VtxOtherEnd(WeEdge edge)
		{
			// If we're the start vertex, then return the end and vice verse.
			if (ReferenceEquals(edge.VtxStart, this))
			{
				return edge.VtxEnd;
			}
			return edge.VtxStart;
		}
		#endregion

		#region Internal Classes
		/// <summary>
		/// WingedEdge doesn't directly give the polygons which contain this vertex so
		/// we give an enumerator for that.
		/// </summary>
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

		/// <summary>
		/// The enumerable for PolyEnumerator
		/// </summary>
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

	/// <summary>
	/// Polygons in a WingedEdge structure.  Essentially a list of the edges comprising this
	/// polygon in clockwise order.  The first edge is random and has no particular significance.
	/// </summary>
	public class WePolygon
	{
		#region Private Variables
		protected List<WeEdge> LstCWEdges = new List<WeEdge>();	// List of edges in a Clockwise direction
		#endregion

		#region Properties
		public List<WeEdge> EdgesCW
		{
			get
			{
				return LstCWEdges;
			}
		}

		public int VertexCount
		{
			get
			{
				return LstCWEdges.Count;
			}
		}

		public object Cookie { get; set; }

		public PT CenterOfMass
		{
			get
			{
				TPT xTotal = 0, yTotal = 0;

				foreach (WeVertex vtx in Vertices)
				{
					xTotal += vtx.Pt.X;
					yTotal += vtx.Pt.Y;
				}
				return new PT(xTotal / VertexCount, yTotal / VertexCount);
			}		
		}

		public IEnumerable<WeVertex> Vertices
		{
			get
			{
				return new VertexEnumerable(this);
			}
		}
		#endregion

		#region Modifiers
		/// <summary>
		/// Edges are expected to be added in a Clockwise direction
		/// </summary>
		/// <param name="edge">Next edge to add in clockwise order</param>
		public void AddEdge(WeEdge edge)
		{
			LstCWEdges.Add(edge);
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

		/// <summary>
		/// Ensure that a given edge is adjacent to this polygon
		/// </summary>
		/// <param name="edge">Edge to be checked</param>
		/// <returns>True if the edge is adjacent, else false</returns>
		internal bool FValidateEdgeIsAdjacent(WeEdge edge)
		{
			return LstCWEdges.Contains(edge);
		}

		/// <summary>
		/// Ensure that all edges connect to each other
		/// </summary>
		/// <returns>True if all edges connect in order, else false</returns>
		internal bool FValidateEdgesInOrder()
		{
			int iNextEdge;
			int iEdge;
			for (iEdge = 0, iNextEdge = 1; iEdge < LstCWEdges.Count; iEdge++)
			{
				WeEdge edge = LstCWEdges[iEdge];
				if (iNextEdge == LstCWEdges.Count)
				{
					iNextEdge = 0;
				}
				if (!edge.FConnectsToEdge(LstCWEdges[iNextEdge]))
				{
					return Failure();
				}
				iNextEdge++;
			}
			return true;
		}
		#endregion

		#region Internal Classes
		/// <summary>
		/// WingedEdge doesn't directly give the edges around a polygon so we
		/// supply an enumerator for that
		/// </summary>
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
					WeEdge edge = _poly.LstCWEdges[_iEdge];

					return (_poly == edge.PolyLeft) ?
						edge.VtxEnd :
						edge.VtxStart;
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
					WeEdge edge = _poly.LstCWEdges[_iEdge];

					return (_poly == edge.PolyLeft) ?
						edge.VtxEnd :
						edge.VtxStart;
				}
			}

			public bool MoveNext()
			{
				return ++_iEdge < _poly.LstCWEdges.Count;
			}

			public void Reset()
			{
				_iEdge = 0;
			}

			#endregion
		}

		/// <summary>
		/// The enumerable for the VertexEnumerator
		/// </summary>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using DAP.CompGeom;
using Geometry = DAP.CompGeom.Geometry;

namespace WpfTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		Random _rnd = new Random();

		private IEnumerable<PointD> PolyCanvasExp()
		{
			return new List<PointD>
			       	{
			                         		new PointD(0, 0),
			                         		new PointD(cvsMain.ActualWidth, 0),
			                         		new PointD(cvsMain.ActualWidth, cvsMain.ActualHeight),
			                         		new PointD(0, cvsMain.ActualHeight)
			                         	};
		}

		private struct LevelInfo
		{
			public readonly Gradient GrdFill;
			public readonly double StrokeThickness;
			public readonly double Density;
			public readonly byte Alpha;
			public readonly int Relaxations;

			public LevelInfo(
				Gradient grdFill,
				double strokeThickness,
				double density,
				byte alpha,
				int relaxations)
			{
				GrdFill = grdFill;
				StrokeThickness = strokeThickness;
				Density = density;
				Alpha = alpha;
				Relaxations = relaxations;
			}
		}

		private void SubVoronoi(IEnumerable<PointD> poly, IEnumerable<LevelInfo> ienInfo)
		{
			if (!ienInfo.Any() || !poly.Any())
			{
				return;
			}

			var info = ienInfo.First();
			var pxLeft = double.MaxValue;
			var pxRight = double.MinValue;
			var pxBottom = double.MaxValue;
			var pxTop = double.MinValue;

			foreach (var pt in poly)
			{
				if (pt.X < pxLeft)
				{
					pxLeft = pt.X;
				}
				if (pt.X > pxRight)
				{
					pxRight = pt.X;
				}
				if (pt.Y < pxBottom)
				{
					pxBottom = pt.Y;
				}
				if (pt.Y > pxTop)
				{
					pxTop = pt.Y;
				}
			}

			var bbWidth = pxRight - pxLeft;
			var bbHeight = pxTop - pxBottom;
			var ptUL = new PointD(pxLeft, pxTop);
			var ptLR = new PointD(pxRight, pxBottom);
			var cPts = (int)(bbWidth * bbHeight * info.Density * 100 / (cvsMain.ActualWidth * cvsMain.ActualHeight));
			var lstPoints = new List<PointD>();

			for (var i = 0; i < cPts ; i++)
			{
				var ptCand = new PointD(
					_rnd.NextDouble() * bbWidth + pxLeft,
					_rnd.NextDouble() * bbHeight + pxBottom);
				if (Geometry.PointInConvexPoly(ptCand, poly))
				{
					lstPoints.Add(ptCand);
				}
			}
			if (lstPoints.Count == 0)
			{
				lstPoints.Add(poly.First());
			}
			ptUL += new PointD(-1, 1);
			ptLR += new PointD(1, -1);
			var we = Fortune.ComputeVoronoi(lstPoints);
			for (var iRelax = 0; iRelax < info.Relaxations; iRelax++)
			{
				we = Fortune.LloydRelax(we, 10000, poly, 1.0);
			}

			var lstPoly = new List<Polygon>();

			foreach (var subPoly in we.LstPolygons.Where(p => !p.FAtInfinity))
			{
				var ptsClipped = ConvexPolyIntersection.FindIntersection(subPoly.RealVertices(10000, ptUL, ptLR), poly);
				var clrFill = info.GrdFill.GetRandomColor();
				clrFill.A = info.Alpha;
				var subPolyWpf = new Polygon
					{
						Points = new PointCollection(ptsClipped.Select(p => new Point(p.X, p.Y))),
						Fill = new SolidColorBrush(clrFill),
						Stroke = new SolidColorBrush(Colors.Black),
						StrokeThickness = info.StrokeThickness
					};
				SubVoronoi(ptsClipped, ienInfo.Skip(1));
				cvsMain.Children.Add(subPolyWpf);
			}
		}

		private void NewDesign()
		{
			cvsMain.Children.Clear();
			var grd = new Gradient();
			grd.SetStart(Colors.Red);
			grd.AddStop(0.45, Colors.Purple);
			grd.AddStop(0.95, Colors.Green);
			grd.SetEnd(Colors.Green);
			var li = new List<LevelInfo>
			         	{
										new LevelInfo(grd, 4, 0.2, 70, 2),
										new LevelInfo(grd, 2, 3, 90, 1),
										new LevelInfo(grd, 1, 15, 150, 0),
										new LevelInfo(grd, .5, 60, 255, 0)
			                     	};
			SubVoronoi(PolyCanvasExp(), li);
			return;
		}

		private bool _fSizedOnce;

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (!_fSizedOnce)
			{
				NewDesign();
				_fSizedOnce = true;
			}
		}

		private void cvsMain_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			NewDesign();
		}
	}
}

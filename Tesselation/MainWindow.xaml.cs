using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DAP.CompGeom;
using WpfTest;

namespace Tesselation
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		readonly Random _rnd = new Random();
		private const int cRndPoints = 4;
		public MainWindow()
		{
			InitializeComponent();
			rcMain.Fill = CreateTile(100, 100);
		}

		internal class SiteMarker
		{
			public int SiteIndex;
			public Color Clr;
			public SiteMarker(int iSiteIndex, Color clr)
			{
				SiteIndex = iSiteIndex;
				Clr = clr;
			}
		}

		Color RandomColor()
		{
			var rgb = new byte[3];
			_rnd.NextBytes(rgb);
			return Color.FromRgb(rgb[0], rgb[1], rgb[2]);
		}

		Brush CreateTile(int width, int height)
		{
			var grd = new Gradient();
			grd.SetStart(RandomColor());
			grd.AddStop(0.5, RandomColor());
			grd.SetEnd(RandomColor());
			var grdBuffer = new Canvas {Width = width, Height = height};

			var lstPts = new List<PointD>();
			for (var iPt = 0; iPt < cRndPoints; iPt++)
			{
				var pt = new PointD(
					_rnd.NextDouble(), 
					_rnd.NextDouble(), 
					new SiteMarker(iPt, grd.GetRandomColor()));
				SetSymmetricPoints(lstPts, pt);
				pt.X = 1 - pt.X;
				//SetSymmetricPoints(lstPts, pt);
			}
			var ptUL = new PointD(-0.5, 1.5);
			var ptLR = new PointD(1.5, -0.5);
			var we = Fortune.ComputeVoronoi(lstPts);

			foreach (var polygon in we.LstPolygons.Where(p => !p.FAtInfinity))
			{
				var ptSite = polygon.VoronoiPoint;
				var sm = (SiteMarker) (ptSite.Cookie);

				var polyWpf = new Polygon
				              	{
				              		Points = new PointCollection(polygon.BoxVertices(ptUL, ptLR).Select(p => new Point(p.X * width, p.Y * height))),
				              		Fill = new SolidColorBrush(sm.Clr),
				              		Stroke = new SolidColorBrush(Colors.Black),
				              		StrokeThickness = 2
				              	};
				grdBuffer.Children.Add(polyWpf);
			}
			return new VisualBrush
			         	{
			         		Visual = grdBuffer,
			         		Stretch = Stretch.None,
			         		TileMode = TileMode.Tile,
			         		Viewbox = new Rect(0, 0, width, height),
			         		ViewboxUnits = BrushMappingMode.Absolute,
			         		Viewport = new Rect(0, 0, width, height),
			         		ViewportUnits = BrushMappingMode.Absolute
			         	};
		}

		private static void SetSymmetricPoints(List<PointD> lstPts, PointD pt)
		{
			lstPts.Add(pt);
			pt.X += 1;
			lstPts.Add(pt);
			pt.Y += 1;
			lstPts.Add(pt);
			pt.X -= 1;
			lstPts.Add(pt);
			pt.X -= 1;
			lstPts.Add(pt);
			pt.Y -= 1;
			lstPts.Add(pt);
			pt.Y -= 1;
			lstPts.Add(pt);
			pt.X += 1;
			lstPts.Add(pt);
			pt.X += 1;
			lstPts.Add(pt);
		}

		private void rcMain_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			rcMain.Fill = CreateTile(100, 100);
		}
	}
}

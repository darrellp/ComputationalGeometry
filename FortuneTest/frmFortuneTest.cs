#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using NetTrace;
using DAP.CompGeom;

namespace FortuneTest
{
	[TraceTags]
	enum t
	{
		[TagDesc("Auto Test/Save after each point is entered")]
		autosave
	}

	public partial class frmFortuneTest : Form
	{
		#region Constants
		const int radPt = 2;
		const int radSelectPt = 5;
		const int radSelectDraw = 5;
		#endregion

		#region Private Variables
		List<PT> _lstPt = new List<PT>();
		List<FortunePoly> _lstPoly = null;
		Matrix _mtxFromWorld;
		Matrix _mtxToWorld;
		int _iptSelected = -1;
		#endregion

		#region Constructor
		public frmFortuneTest()
		{
#if NETTRACE
			XmlDocument xd = new XmlDocument();
			xd.Load(@"..\..\App.config");
			//Tracer.SetDefaultXmlDocument(xd);
#endif
			InitializeComponent();
			_mtxFromWorld = new Matrix();
			_mtxFromWorld.Translate(pnlDraw.Width / 2, pnlDraw.Height / 2);
			_mtxFromWorld.Scale(1, -1);
			_mtxToWorld = _mtxFromWorld.Clone();
			_mtxToWorld.Invert();
		}
		#endregion

		#region Graphic space conversions
		Point PtFromPtf(PT ptf)
		{
			return new Point((int)ptf.X + pnlDraw.Width / 2, pnlDraw.Height / 2 - (int)ptf.Y);
		}

		PT PtfFromPt(Point pt)
		{
			return new PT(pt.X - pnlDraw.Width / 2, pnlDraw.Height / 2 - pt.Y);
		}
		#endregion

		#region Drawing
		void DrawPoint(Graphics g, PT ptf, bool fSelected)
		{
			Point pt = PtFromPtf(ptf);

			if (fSelected)
			{
				Pen pen = new Pen(Color.Red);

				g.DrawEllipse(pen, pt.X - radSelectDraw, pt.Y - radSelectDraw, 2 * radSelectDraw, 2 * radSelectDraw);
			}
			else
			{
				SolidBrush br = new SolidBrush(Color.Black);

				g.FillEllipse(br, pt.X - radPt, pt.Y - radPt, 2 * radPt, 2 * radPt);
			}
		}
		#endregion

		#region Point I/O
		private void ReadPts()
		{
			if (ofdPoints.ShowDialog() == DialogResult.OK)
			{
				using (FileStream fs = new FileStream(ofdPoints.FileName, FileMode.Open))
				{
					using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
					{
						_lstPt.Clear();
						_lstPoly = null;
						while (!sr.EndOfStream)
						{
							string str = sr.ReadLine();
							if (str.StartsWith("//") || str == String.Empty)
							{
								continue;
							}
							string[] arstr = str.Split(new char[] { ',' });
							PT ptNew = new PT(TPT.Parse(arstr[0]), TPT.Parse(arstr[1]));
							_lstPt.Add(ptNew);
						}
						pnlDraw.Invalidate();
					}
				}
			}
		}

		private void WritePts()
		{
			if (sfdPoints.ShowDialog() == DialogResult.OK)
			{
				using (FileStream fs = new FileStream(sfdPoints.FileName, FileMode.Create))
				{
					using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
					{
						foreach (PT pt in _lstPt)
						{
							sw.WriteLine(string.Format("{0},{1}", pt.X, pt.Y));
						}
						sw.Flush();
					}
				}
			}
		}
		#endregion

		#region Event handlers
		private void panel1_Paint(object sender, PaintEventArgs e)
		{
			foreach (PT ptf in _lstPt)
			{
				DrawPoint(e.Graphics, ptf, false);
			}

			if (_iptSelected >= 0)
			{
				DrawPoint(e.Graphics, _lstPt[_iptSelected], true);
			}

			if (_lstPoly != null)
			{
				Pen pen = new Pen(Color.Black);

				e.Graphics.Transform = _mtxFromWorld;

				// We can't just use the width/height of the drawing surface - that would work if
				// one of the vertices was guaranteed to be on the drawing surface but that's
				// not necessarily the case.
				TPT infiniteLength = (TPT)Math.Max(pnlDraw.Height, pnlDraw.Width) * 1000;
				foreach (FortunePoly poly in _lstPoly)
				{
					foreach (FortuneEdge edge in poly.Edges)
					{
						edge.Draw(e.Graphics, pen, (float)infiniteLength);
					}
				}
			}
		}

		private void pnlDraw_MouseDown(object sender, MouseEventArgs e)
		{
			if (_iptSelected < 0)
			{
				_lstPt.Add(PtfFromPt(new Point(e.X, e.Y)));
			}
			else
			{
				_lstPt.RemoveAt(_iptSelected);
				_iptSelected = -1;
			}

			_lstPoly = null;
			pnlDraw.Invalidate();

#if NETTRACE || DEBUG
			if (Tracer.FTracing(t.autosave))
			{
				WingedEdge<FortunePoly, FortuneEdge, FortuneVertex> we = Fortune.ComputeVoronoi(_lstPt);
				_lstPoly = we.LstPolygons;
				pnlDraw.Invalidate();
			}
#endif
		}

		private void btnSinglePoint_Click(object sender, EventArgs e)
		{
			_lstPt.Add(new PT(0, 0));
			pnlDraw.Invalidate();
		}

		private void btnVTriangle_Click(object sender, EventArgs e)
		{
			_lstPt.Add(new PT(0, 0));
			_lstPt.Add(new PT(100, 100));
			_lstPt.Add(new PT(100, -100));
			pnlDraw.Invalidate();
		}

		private void btnHTriangle_Click(object sender, EventArgs e)
		{
			//_lstPt.Add(new PT(0, 0));
			_lstPt.Add(new PT(50, 0));
			_lstPt.Add(new PT(30, 40));
			_lstPt.Add(new PT(40, 30));
			_lstPt.Add(new PT(0, 50));
			_lstPt.Add(new PT(-30, 40));
			_lstPt.Add(new PT(-40, 30));
			_lstPt.Add(new PT(-50, 0));
			_lstPt.Add(new PT(-40, -30));
			_lstPt.Add(new PT(-30, -40));
			_lstPt.Add(new PT(0, -50));
			_lstPt.Add(new PT(30, -40));
			_lstPt.Add(new PT(40, -30));
			pnlDraw.Invalidate();
		}

		private void btnCHTriangle_Click(object sender, EventArgs e)
		{
			_lstPt.Add(new PT(0, 0));
			_lstPt.Add(new PT(0, 100));
			_lstPt.Add(new PT(-100, -100));
			_lstPt.Add(new PT(100, -100));
			pnlDraw.Invalidate();
		}


		private void btnRectangle_Click(object sender, EventArgs e)
		{
			_lstPt.Add(new PT(-50, -100));
			_lstPt.Add(new PT(50, -100));
			_lstPt.Add(new PT(-50, 100));
			_lstPt.Add(new PT(50, 100));
			pnlDraw.Invalidate();
		}

		private void btnReadLastPts_Click(object sender, EventArgs e)
		{
			ReadPts();
		}

		private void btnTraceTags_Click(object sender, EventArgs e)
		{
			Tracer.ShowTraceTagDialog(this);
		}

		private void btnCompute_Click(object sender, EventArgs e)
		{
			_lstPoly = (List<FortunePoly>) (Fortune.ComputeVoronoi(_lstPt).LstPolygons);
			pnlDraw.Invalidate();
		}

		private void btnClear_Click(object sender, EventArgs e)
		{
			_lstPt.Clear();
			_lstPoly = null;
			pnlDraw.Invalidate();
		}

		private void btnWriteTags_Click(object sender, EventArgs e)
		{
			//Tracer.SaveTagsToXmlFile(@"..\..\App.config");
		}

		private void pnlDraw_MouseMove(object sender, MouseEventArgs e)
		{
			TPT distMin = TPT.MaxValue;
			TPT distCur;
			int iptSelected = -1;

			if ((Control.ModifierKeys & Keys.Alt) != 0)
			{
				PT ptMouse = PtfFromPt(new Point(e.X, e.Y));
				for (int ipt = 0; ipt < _lstPt.Count; ipt++)
				{
					if ((distCur = Geometry.Distance(ptMouse, _lstPt[ipt])) < distMin && distCur <= radSelectPt)
					{
						iptSelected = ipt;
						distMin = distCur;
					}
				}
				sstlblGenIndex.Text = iptSelected < 0 ? "None" : iptSelected.ToString();
			}

			if (iptSelected != _iptSelected)
			{
				_iptSelected = iptSelected;
				pnlDraw.Invalidate();
			}
		}

		private void btnWritePts_Click(object sender, EventArgs e)
		{
			WritePts();
		}
		#endregion
	}
}
#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace DAP.CompGeom
{
	// Various comparers for points
	public class PointSorterXDownYDown : IComparer<PT>
	{
		#region IComparer<PT> Members

		int IComparer<PT>.Compare(PT pt1, PT pt2)
		{
			if (pt1.X == pt2.X)
			{
				return -pt1.Y.CompareTo(pt2.Y);
			}
			return -pt1.X.CompareTo(pt2.X);
		}
		#endregion
	}

	public class PointSorterXDownYUp : IComparer<PT>
	{
		#region IComparer<PT> Members

		int IComparer<PT>.Compare(PT pt1, PT pt2)
		{
			if (pt1.X == pt2.X)
			{
				return pt1.Y.CompareTo(pt2.Y);
			}
			return -pt1.X.CompareTo(pt2.X);
		}
		#endregion
	}

	public class PointSorterXUpYDown : IComparer<PT>
	{
		#region IComparer<PT> Members

		int IComparer<PT>.Compare(PT pt1, PT pt2)
		{
			if (pt1.X == pt2.X)
			{
				return -pt1.Y.CompareTo(pt2.Y);
			}
			return pt1.X.CompareTo(pt2.X);
		}
		#endregion
	}

	public class PointSorterXUpYUp : IComparer<PT>
	{
		#region IComparer<PT> Members

		int IComparer<PT>.Compare(PT pt1, PT pt2)
		{
			if (pt1.X == pt2.X)
			{
				return pt1.Y.CompareTo(pt2.Y);
			}
			return pt1.X.CompareTo(pt2.X);
		}
		#endregion
	}

	#region NUnit
	[TestFixture]
	class TestSorts
	{
		[Test]
		public void TestXDownYDown()
		{
			PT[] arpt = new PT[]
				{
					new PT(0, 0),
					new PT(10, 0),
					new PT(5, 0),
					new PT(0, 10),
					new PT(0,5),
					new PT(5,5),
				};
			TPT[] arXExpected = new TPT[]
				{
					(TPT)10, (TPT)5, (TPT)5, (TPT)0, (TPT)0, (TPT)0
				};

			TPT[] arYExpected = new TPT[]
				{
					(TPT)0, (TPT)5, (TPT)0, (TPT)10, (TPT)5, (TPT)0
				};

			Array.Sort(arpt, new PointSorterXDownYDown());
			for (int iElement = 0; iElement < arpt.Length; iElement++)
			{
				Assert.AreEqual(arXExpected[iElement], arpt[iElement].X);
				Assert.AreEqual(arYExpected[iElement], arpt[iElement].Y);
			}
		}
	}
	#endregion
}

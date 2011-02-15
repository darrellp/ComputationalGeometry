using System;
using System.Collections.Generic;

namespace DAP.CompGeom
{
	/// <summary>
	/// Double precision points
	/// </summary>
	public struct PointD
	{
		public double X;
		public double Y;

		public PointD(double XParm, double YParm)
		{
			X = XParm;
			Y = YParm;
		}
	}
}

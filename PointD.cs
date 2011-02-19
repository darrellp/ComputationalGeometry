using System;
using System.Collections.Generic;
using System.Drawing;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Double precision points. </summary>
	///
	/// <remarks>	Darrellp, 2/17/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public struct PointD
	{
		/// <summary> The x coordinate </summary>
		public double X;
		/// <summary> The y coordinate </summary>
		public double Y;

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="x">	The x coordinate. </param>
		/// <param name="y">	The y coordinate. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PointD(double x, double y)
		{
			X = x;
			Y = y;
		}

		public PointF ToPointf()
		{
			return new PointF((Single) X, (Single) Y);
		}
	}
}

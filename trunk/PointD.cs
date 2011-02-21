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

		/// <summary> Cookie to hold arbitrary information for the user </summary>
		public object Cookie;

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="x">	The x coordinate. </param>
		/// <param name="y">	The y coordinate. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PointD(double x, double y) : this(x, y, null) {}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/20/2011. </remarks>
		///
		/// <param name="x">		The x coordinate. </param>
		/// <param name="y">		The y coordinate. </param>
		/// <param name="cookie">	A cookie to hold user specified info in. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PointD(double x, double y, object cookie)
		{
			Cookie = cookie;
			X = x;
			Y = y;
		}

		public static implicit operator PointF(PointD pt)
		{
			return new PointF((float)pt.X, (float)pt.Y);
		}
	}
}

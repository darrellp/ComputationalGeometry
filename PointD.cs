using System;
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
		/// <param name="cookie">	pt1 cookie to hold user specified info in. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PointD(double x, double y, object cookie)
		{
			Cookie = cookie;
			X = x;
			Y = y;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Convert this object into a string representation. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <returns>	A string representation of this object. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public override string ToString()
		{
			return string.Format("({0},{1})", X, Y);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns a vector 90 degrees in a CCW direction from the original. </summary>
		///
		/// <remarks>	Darrellp, 2/27/2011. </remarks>
		///
		/// <returns>	Vector 90 degrees from original. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PointD Flip90Ccw()
		{
			return new PointD(-Y, X);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	returns the distance from the origin to the point. </summary>
		///
		/// <remarks>	Darrellp, 2/27/2011. </remarks>
		///
		/// <returns>	Length of the point considered as a vector. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public double Length()
		{
			return Math.Sqrt(X * X + Y * Y);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns a normalized version of the point. </summary>
		///
		/// <remarks>	Darrellp, 2/27/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public PointD Normalize()
		{
			var ln = Length();
			return new PointD(X /= ln, Y /= ln);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Subtract two PointDs. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <param name="pt1">	First PointD. </param>
		/// <param name="pt2">	Second PointD. </param>
		///
		/// <returns>	The result of the operation. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static PointD operator -(PointD pt1, PointD pt2)
		{
			return new PointD(pt1.X - pt2.X, pt1.Y - pt2.Y);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Multiply by a scalar. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <param name="pt">	Point to be multiplied. </param>
		/// <param name="v">	Scalar to multiply by. </param>
		///
		/// <returns>	The result of the operation. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static PointD operator *(PointD pt, double v)
		{
			return new PointD(pt.X * v, pt.Y * v);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Subtract two PointDs. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <param name="pt1">	First PointD. </param>
		/// <param name="pt2">	Second PointD. </param>
		///
		/// <returns>	The result of the operation. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static PointD operator +(PointD pt1, PointD pt2)
		{
			return new PointD(pt1.X + pt2.X, pt1.Y + pt2.Y);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	PointF casting operator. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="pt">	The PointD. </param>
		///
		/// <returns>	The PointF value of this PointD. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static implicit operator PointF(PointD pt)
		{
			return new PointF((float)pt.X, (float)pt.Y);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Compares this vector with another one. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <param name="obj">	The object to compare to this object. </param>
		///
		/// <returns>	true if the objects are considered equal, false if they are not. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public override bool Equals(object obj)
		{
			if (!(obj is PointD))
			{
				return false;
			}
			var ptCompare = (PointD)obj;
			return X == ptCompare.X && Y == ptCompare.Y;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Retrieves a hashcode that is dependent on the elements. </summary>
		///
		/// <remarks>	Darrellp, 2/23/2011. </remarks>
		///
		/// <returns>	The hashcode. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode();
		}
	}
}

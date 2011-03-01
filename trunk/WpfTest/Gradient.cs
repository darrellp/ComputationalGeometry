using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace WpfTest
{
	internal class Gradient
	{
		private static readonly Random Rnd = new Random();
		private SortedList<Double, Color> Stops { get; set; }

		public Gradient()
		{
			Stops = new SortedList<double, Color> {{0, Colors.White}, {1, Colors.Black}};
		}

		private static byte Lerp(byte v1, byte v2, double t)
		{
			return (byte)(v1 + (v2 - v1)*t);
		}

		public bool AddStop(double pos, Color c)
		{
			if (pos <= 0 || pos >= 1 || Stops.ContainsKey(pos))
			{
				return false;
			}
			Stops[pos] = c;
			return true;
		}

		public bool RemoveStop(double pos)
		{
			if (pos == 0 || pos == 1)
			{
				return false;
			}

			if (Stops.ContainsKey(pos))
			{
				Stops.Remove(pos);
				return true;
			}
			return false;
		}

		public bool RemoveStop(int index)
		{
			if (index <= 0 || index >= Stops.Count - 1)
			{
				return false;
			}
			Stops.RemoveAt(index);
			return true;
		}

		public bool ModifyStop(int index, Color c)
		{
			if (index < 0 || index >= Stops.Count)
			{
				return false;
			}
			// This is really odd - why can't I do this directly - i.e.,
			// Stops.Values[index] = hsv?  It's just essentially a syntax change
			// but I get an error on the above saying that I'm trying to "change
			// the underlying SortedList".  Yes, I'm changing the value at a key
			// which doesn't change the sort at all so why the big fuss?
			Stops[Stops.Keys[index]] = c;
			return true;
		}

		public bool ModifyStop(double pos, Color c)
		{
			if (pos < 0 || pos > 1 || !Stops.ContainsKey(pos))
			{
				return false;
			}
			Stops[pos] = c;
			return true;
		}

		public void SetStart(Color c)
		{
			Stops[0] = c;
		}

		public void SetEnd(Color c)
		{
			Stops[1] = c;
		}

		public Color GetRandomColor()
		{
			var rnd = Rnd.NextDouble();
			var clrRet = Colors.Black;

			for (var i = 0; i < Stops.Count; i++)
			{
				if (Stops.Keys[i] > rnd)
				{
					var frac = (rnd - Stops.Keys[i-1])/(Stops.Keys[i] - Stops.Keys[i - 1]);
					var clr1 = Stops.Values[i - 1];
					var clr2 = Stops.Values[i];
					clrRet = Color.FromRgb(
						Lerp(clr1.R, clr2.R, frac),
						Lerp(clr1.G, clr2.G, frac),
						Lerp(clr1.B, clr2.B, frac)
						);
					break;
				}
			}
			return clrRet;
		}
	}
}
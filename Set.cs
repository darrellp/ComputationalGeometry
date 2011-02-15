using System;
using System.Collections.Generic;
using System.Text;

namespace DAP.CompGeom
{
	/// <summary>
	/// Simple implementation of a set based on a dictionary with no values, only keys
	/// (well, really, the values exist but they are all 0).
	/// </summary>
	/// <typeparam name="T">Type of elements in the set</typeparam>
	class Set<T> : ICollection<T>
	{
		#region Private Variables
		Dictionary<T, int> _dict = new Dictionary<T, int>();
		#endregion

		#region ICollection<T> Members

		void ICollection<T>.Add(T item)
		{
			_dict.Add(item, 0);
		}

		void ICollection<T>.Clear()
		{
			_dict.Clear();
		}

		bool ICollection<T>.Contains(T item)
		{
			return _dict.ContainsKey(item);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			_dict.Keys.CopyTo(array, arrayIndex);
		}

		int ICollection<T>.Count
		{
			get { return _dict.Count; }
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		bool ICollection<T>.Remove(T item)
		{
			return _dict.Remove(item);
		}

		#endregion

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _dict.Keys.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _dict.Keys.GetEnumerator();
		}

		#endregion
	}
}

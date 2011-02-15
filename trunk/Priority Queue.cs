using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using NetTrace;

namespace DAP.CompGeom
{
	// A priority queue implemented as an array.  This is a pretty standard implementation.
	public class PriorityQueue<T> : IEnumerable<T> where T : IComparable
	{
		#region Private Variables
		/// <summary>
		/// Array to keep elements in.  C# lists are actually implemented as arrays.  This is contrary
		/// to everything I learned about the terms "array" and "list", but that's essentially the way
		/// they're implemented.
		/// </summary>
		protected List<T> _lstHeap = new List<T>();
		#endregion

		#region Properties
		public virtual int Count
		{
			get { return _lstHeap.Count; }
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Insert a value into the priority queue
		/// </summary>
		/// <param name="val">Value to insert</param>
		public virtual void Add(T val)
		{
			Tracer.Trace(t.PqInserts, "Adding {0}...", val.ToString());
			Tracer.Indent();
			_lstHeap.Add(val);
			SetAt(_lstHeap.Count - 1, val);
			UpHeap(_lstHeap.Count - 1);
			Tracer.Unindent();
			Tracer.Assert(t.PqValidate, FValidate(), "Invalid heap");
			PrintTree();
		}

		/// <summary>
		/// Return the maximal element in the queue
		/// </summary>
		/// <returns>Maximal element</returns>
		public virtual T Peek()
		{
			if (_lstHeap.Count == 0)
			{
				throw new IndexOutOfRangeException("Peeking at an empty priority queue");
			}
			return _lstHeap[0];
		}

		/// <summary>
		/// Remove and return the maximal element in the queue
		/// </summary>
		/// <returns>Maximal element</returns>
		public virtual T Pop()
		{
			if (_lstHeap.Count == 0)
			{
				throw new IndexOutOfRangeException("Popping an empty priority queue");
			}
			T valRet = _lstHeap[0];

			Tracer.Trace(t.PqDeletes, "Popping {0}", valRet.ToString());
			Tracer.Indent();
			Tracer.Trace(
				t.PqDeletes,
				"Removing {0} from the array end and placing at the first position",
				_lstHeap[_lstHeap.Count - 1]);
			SetAt(0, _lstHeap[_lstHeap.Count - 1]);
			_lstHeap.RemoveAt(_lstHeap.Count - 1);
			DownHeap(0);
			Tracer.Trace(t.PqInserts, "Array Count = {0}", _lstHeap.Count);
			Tracer.Unindent();
			PrintTree();
			Tracer.Assert(t.PqValidate, FValidate(), "Invalid heap");

			return valRet;
		}
		#endregion

		#region Virtual methods
		protected virtual void SetAt(int i, T val)
		{
			_lstHeap[i] = val;
		}
		#endregion

		#region Heapifying
		protected bool FRSonExists(int i)
		{
			return Rndx(i) < _lstHeap.Count;
		}

		protected bool FLSonExists(int i)
		{
			return Lndx(i) < _lstHeap.Count;
		}

		protected int Pndx(int i)
		{
			return (i - 1) / 2;
		}

		protected int Lndx(int i)
		{
			return 2 * i + 1;
		}

		protected int Rndx(int i)
		{
			return 2 * (i + 1);
		}

		protected T Val(int i)
		{
			return _lstHeap[i];
		}

		protected T Parent(int i)
		{
			return _lstHeap[Pndx(i)];
		}

		protected T Left(int i)
		{
			return _lstHeap[Lndx(i)];
		}

		protected T Right(int i)
		{
			return _lstHeap[Rndx(i)];
		}

		protected void Swap(int i, int j)
		{
			T valHold = Val(i);
			SetAt(i, _lstHeap[j]);
			SetAt(j, valHold);
		}

		protected void UpHeap(int i)
		{
			while (i > 0 && Val(i).CompareTo(Parent(i)) > 0)
			{
				Tracer.Trace(t.PqInserts, "Moving {0} to {1}", Parent(i).ToString(), i);
				Tracer.Trace(t.PqPercolates, "UpHeap: Swapping {0} (indx={1}) and {2} (indx={3})",
					_lstHeap[i].ToString(),
					i,
					_lstHeap[Pndx(i)],
					Pndx(i));
				Swap(i, Pndx(i));
				i = Pndx(i);
			}
		}

		protected void DownHeap(int i)
		{
			while (i >= 0)
			{
				int iContinue = -1;

				if (FRSonExists(i) && Right(i).CompareTo(Val(i)) > 0)
				{
					if (Left(i).CompareTo(Right(i)) < 0)
					{
						iContinue = Rndx(i);
					}
					else
					{
						iContinue = Lndx(i);
					}
				}
				else if (FLSonExists(i) && Left(i).CompareTo(Val(i)) > 0)
				{
					iContinue = Lndx(i);
				}

				if (iContinue >= 0 && iContinue < _lstHeap.Count)
				{
					Tracer.Trace(t.PqPercolates, "DownHeap: Swapping {0} (indx={1}) and {2} (indx={3})",
						_lstHeap[i].ToString(),
						i,
						_lstHeap[iContinue],
						iContinue);
					Swap(i, iContinue);
				}
				i = iContinue;
			}
		}
		#endregion

		#region Debugging
		protected string _strIndent = "";
		[Conditional("DEBUG")]
		virtual protected void TraceElement(int iPos, T val)
		{
			Tracer.Trace(t.PqTrees, "Pos " + iPos + ":" + _strIndent + val.ToString());
		}

		[Conditional("DEBUG")]
		void Indent()
		{
			_strIndent = _strIndent + "\t";
		}

		[Conditional("DEBUG")]
		void Unindent()
		{
			if (_strIndent != String.Empty)
			{
				_strIndent = _strIndent.Remove(_strIndent.Length - 1, 1);
			}
		}

		[Conditional("DEBUG")]
		protected void PrintTree()
		{
			if (!Tracer.FTracing(t.PqTrees))
			{
				return;
			}
			Tracer.Trace(t.PqTrees, "<<< PQ TREE >>>");
			if (_lstHeap.Count == 0)
			{
				return;
			}
			PrintTree(0);
			Tracer.Trace(t.PqTrees, "<<< PQ TREE END >>>");
		}

		[Conditional("DEBUG")]
		protected void PrintTree(int i)
		{
			TraceElement(i, _lstHeap[i]);
			Indent();

			if (FLSonExists(i))
			{
				PrintTree(Lndx(i));
			}
			if (FRSonExists(i))
			{
				PrintTree(Rndx(i));
			}
			Unindent();
		}
		#endregion

		#region Validation
		internal virtual bool FValidate()
		{
			if (_lstHeap.Count == 0)
			{
				return true;
			}
			return FValidate(0);
		}

		bool FValidate(int iRoot)
		{
			T valRoot = _lstHeap[iRoot];

			if (FLSonExists(iRoot))
			{
				if (valRoot.CompareTo(Left(iRoot)) < 0)
				{
					Tracer.Assert(t.PqValidate, false, "Child is > than parent");
					return false;
				}
				if (!FValidate(Lndx(iRoot)))
				{
					return false;
				}
			}
			if (FRSonExists(iRoot))
			{
				if (valRoot.CompareTo(Right(iRoot)) < 0)
				{
					Tracer.Assert(t.PqValidate, false, "Child is > than parent");
					return false;
				}
				if (!FValidate(Lndx(iRoot)))
				{
					return false;
				}
			}
			return true;
		}
		#endregion

		#region IEnumerable<T> Members
		protected virtual IEnumerator<T> GetEnumerator()
		{
			return _lstHeap.GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	/// <summary>
	/// In a priority queue which supports deletion, elements must keep track of their position
	/// within the queue's heap list.  This interface supports that.
	/// </summary>
	public interface IPriorityQueueElement : IComparable
	{
		void SetIndex(int i);
		int Index
		{
			get;
			set;
		}
	}

	public class PQWithDeletions<T> : PriorityQueue<T> where T : IPriorityQueueElement
	{
		#region Public overrides
		public override T Pop()
		{
			T valRet;

			valRet = base.Pop();

			// When an element is removed from the heap, it's index must be reset.
			valRet.Index = -1;
			return valRet;
		}

		/// <summary>
		/// Delete a value from the heap
		/// </summary>
		/// <param name="val">Value to remove</param>
		public void Delete(T val)
		{
			int i = val.Index;

			if (i >= 0)
			{
				val.Index = -1;
				Tracer.Assert(t.Assertion, i < _lstHeap.Count, "Trying to remove an element beyond the end of the heap");
				Tracer.Trace(t.PqDeletes, "Deleting {0} (pos {1})", val.ToString(), i);
				Swap(i, _lstHeap.Count - 1);
				_lstHeap.RemoveAt(_lstHeap.Count - 1);
				if (i < _lstHeap.Count)
				{
					if (i != 0 && _lstHeap[i].CompareTo(Parent(i)) > 0)
					{
						UpHeap(i);
					}
					else
					{
						DownHeap(i);
					}
				}
			}
			PrintTree();
			Tracer.Assert(t.PqValidate, FValidate(), "Invalid heap");
		}
		#endregion

		#region Private overrides
		internal override bool FValidate()
		{
			for (int iVal = 0; iVal < _lstHeap.Count; iVal++)
			{
				if (_lstHeap[iVal].Index != iVal)
				{
					Tracer.Assert(t.Assertion, false, "Indices not set correctly");
					return false;
				}
			}

			return base.FValidate();
		}
		protected override void TraceElement(int iPos, T val)
		{
			Tracer.Trace(t.PqTrees,
				"Pos " + iPos + (val.Index < 0 ? "<DEL>" : "" + ":") + _strIndent + val.ToString());
		}

		/// <summary>
		/// This override is the magic that makes the deletions work by keeping track of the index
		/// a particular element is moved to.
		/// </summary>
		/// <param name="i"></param>
		/// <param name="val"></param>
		protected override void SetAt(int i, T val)
		{
			base.SetAt(i, val);
			val.SetIndex(i);
		}
		#endregion
	}

	#region NUnit
#if DEBUG || NUNIT
	[TestFixture]
	public class TestPriorityQueue
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void TestCreation()
		{
			Assert.IsNotNull(new PriorityQueue<int>());
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void TestPeekException()
		{
			(new PriorityQueue<int>()).Peek();
		}

		[Test]
		[ExpectedException(typeof(IndexOutOfRangeException))]
		public void TestPopException()
		{
			(new PriorityQueue<int>()).Pop();
		}

		[Test]
		public void TestPQ()
		{
			PriorityQueue<int> pq = new PriorityQueue<int>();

			pq.Add(80);
			Assert.AreEqual(80, pq.Peek());
			pq.Add(90);
			Assert.AreEqual(2, pq.Count);
			Assert.AreEqual(90, pq.Peek());
			Assert.AreEqual(90, pq.Pop());
			Assert.AreEqual(80, pq.Peek());
			pq.Add(30);
			pq.Add(90);
			pq.Add(85);
			pq.Add(20);
			Assert.AreEqual(5, pq.Count);
			Assert.AreEqual(90, pq.Pop());
			Assert.AreEqual(85, pq.Pop());
			Assert.AreEqual(3, pq.Count);
			pq.Add(50);
			pq.Add(35);
			Assert.AreEqual(5, pq.Count);
			Assert.AreEqual(80, pq.Pop());
			Assert.AreEqual(50, pq.Pop());
			Assert.AreEqual(35, pq.Pop());
			Assert.AreEqual(30, pq.Pop());
			Assert.AreEqual(20, pq.Pop());
			Assert.AreEqual(0, pq.Count);
		}

		[Test]
		public void TestPQWithDeletions()
		{
			PQWithDeletions<PQWDElement> pq = new PQWithDeletions<PQWDElement>();

			PQWDElement pq80 = new PQWDElement(80);
			PQWDElement pq90 = new PQWDElement(90);
			PQWDElement pq30 = new PQWDElement(30);
			PQWDElement pq85 = new PQWDElement(85);
			PQWDElement pq20 = new PQWDElement(20);
			PQWDElement pq40 = new PQWDElement(40);
			PQWDElement pq50 = new PQWDElement(50);
			PQWDElement pq35 = new PQWDElement(35);

			pq.Add(pq40);
			pq.Add(pq90);
			pq.Add(pq20);
			pq.Add(pq30);
			pq.Add(pq35);
			pq.Add(pq50);
			pq.Add(pq85);
			pq.Delete(pq30);
			Assert.IsTrue(pq.FValidate());
			Assert.AreEqual(6, pq.Count);
			int cEnums = 0;
			foreach (IPriorityQueueElement pqe in pq)
			{
				cEnums++;
			}
			Assert.AreEqual(6, cEnums);
			pq.Pop();
			pq.Pop();
			pq.Pop();
			pq.Pop();
			pq.Pop();
			pq.Pop();

			pq.Add(pq80);
			Assert.AreEqual(0, ((IPriorityQueueElement)pq80).Index);
			pq.Delete(pq80);
			pq80 = new PQWDElement(80);
			Assert.AreEqual(-1, ((IPriorityQueueElement)pq80).Index);

			pq.Add(pq80);
			Assert.AreEqual(pq80, pq.Peek());
			pq.Add(pq90);
			Assert.AreEqual(2, pq.Count);
			Assert.AreEqual(pq90, pq.Peek());
			Assert.AreEqual(pq90, pq.Pop());
			Assert.AreEqual(pq80, pq.Peek());
			pq.Add(pq30);
			pq.Add(pq90);
			pq.Add(pq85);
			pq.Add(pq20);
			Assert.AreEqual(5, pq.Count);
			Assert.AreEqual(pq90, pq.Pop());
			Assert.AreEqual(pq85, pq.Pop());
			Assert.AreEqual(3, pq.Count);
			pq.Add(pq50);
			pq.Add(pq35);
			Assert.AreEqual(5, pq.Count);
			Assert.AreEqual(pq80, pq.Pop());
			Assert.AreEqual(pq50, pq.Pop());
			Assert.AreEqual(pq35, pq.Pop());
			Assert.AreEqual(pq30, pq.Pop());
			Assert.AreEqual(pq20, pq.Pop());
			Assert.AreEqual(0, pq.Count);

			pq.Add(pq35);
			pq.Add(pq50);
			pq.Add(pq20);
			pq.Add(pq85);
			pq.Add(pq30);
			pq.Add(pq90);
			pq.Add(pq80);

			pq.Delete(pq50);
			pq.Delete(pq30);
			Assert.AreEqual(5, pq.Count);
			Assert.AreEqual(pq90, pq.Pop());
			Assert.AreEqual(pq85, pq.Pop());
			Assert.AreEqual(pq80, pq.Pop());
			Assert.AreEqual(pq35, pq.Pop());
			Assert.AreEqual(pq20, pq.Pop());
			Assert.AreEqual(0, pq.Count);
		}

		class PQWDElement : IPriorityQueueElement
		{
			#region Private Variables
			int _i = -1;
			int _val;
			#endregion

			public int Val
			{
				get
				{
					return _val;
				}
				set
				{
					_val = value;
				}
			}

			#region Constructor
			public PQWDElement(int val)
			{
				_val = val;
			}
			#endregion

			#region PriorityQueueElement Members
			void IPriorityQueueElement.SetIndex(int i)
			{
				_i = i;
			}

			int IPriorityQueueElement.Index
			{
				get { return _i; }
				set { _i = value; }
			}

			#endregion

			#region IComparable Members

			int IComparable.CompareTo(object obj)
			{
				if (Val > ((PQWDElement)obj).Val)
				{
					return 1;
				}
				else if (Val < ((PQWDElement)obj).Val)
				{
					return -1;
				}
				return 0;
			}

			#endregion
		}
}
#endif
	#endregion
}

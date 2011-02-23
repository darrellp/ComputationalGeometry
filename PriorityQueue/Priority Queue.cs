using System;
using System.Collections.Generic;
using System.Diagnostics;
using NetTrace;
#if DEBUG || NUNIT
using NUnit.Framework;
#endif

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	A priority queue implemented as an array.  This is a pretty standard implementation. </summary>
	///
	/// <remarks>	Darrellp, 2/17/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class PriorityQueue<T> : IEnumerable<T> where T : IComparable
	{
		#region Private Variables
		/// <summary>
		/// Array to keep elements in.  C# lists are actually implemented as arrays.  This is contrary
		/// to everything I learned about the terms "array" and "list", but that's nonetheless the way
		/// they're implemented in the CLR Framework.
		/// </summary>
		protected List<T> LstHeap = new List<T>();
		#endregion

		#region Properties
		///<summary>
		/// Count of items in the priority queue
		///</summary>
		public virtual int Count
		{
			get { return LstHeap.Count; }
		}
		#endregion

		#region Public methods

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Insert a value into the priority queue. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="val">	Value to insert. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public virtual void Add(T val)
		{
			// Tracing
			Tracer.Trace(t.PqInserts, "Adding {0}...", val.ToString());
			Tracer.Indent();

			// Add the new element to the end of the list
			LstHeap.Add(val);
			SetAt(LstHeap.Count - 1, val);

			// Move it up the tree to it's correct position
			UpHeap(LstHeap.Count - 1);
			Tracer.Unindent();
			Tracer.Assert(t.PqValidate, FValidate(), "Invalid heap");
			PrintTree();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Return the maximal element in the queue. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <exception cref="IndexOutOfRangeException">	Trying to peek at an empty priority queue. </exception>
		///
		/// <returns>	Maximal element in the queue. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public virtual T Peek()
		{
			// If there are no elements to peek
			if (LstHeap.Count == 0)
			{
				// Throw and exception
				throw new IndexOutOfRangeException("Peeking at an empty priority queue");
			}

			// Otherwise, the root is our largest element
			// The 0'th element in the list is always the root
			return LstHeap[0];
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Remove and return the maximal element in the queue. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <exception cref="IndexOutOfRangeException">	Thrown when the priority queue is empty. </exception>
		///
		/// <returns>	Maximal element. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public virtual T Pop()
		{
			// If There's nothing to pop
			if (LstHeap.Count == 0)
			{
				// Throw an exception
				throw new IndexOutOfRangeException("Popping an empty priority queue");
			}

			// Save away the max value in the heap
			var valRet = LstHeap[0];

			// Diagnostics
			Tracer.Trace(t.PqDeletes, "Popping {0}", valRet.ToString());
			Tracer.Indent();
			Tracer.Trace(
				t.PqDeletes,
				"Removing {0} from the array end and placing at the first position",
				LstHeap[LstHeap.Count - 1]);

			// Move the last element in the list to the now vacated first
			// Yea, and I sayeth unto you, the last shall be first...
			SetAt(0, LstHeap[LstHeap.Count - 1]);

			// Drop the now redundant last item
			LstHeap.RemoveAt(LstHeap.Count - 1);

			// Move the top item down the tree to its proper position
			DownHeap(0);
			Tracer.Trace(t.PqInserts, "Array Count = {0}", LstHeap.Count);
			Tracer.Unindent();
			PrintTree();
			Tracer.Assert(t.PqValidate, FValidate(), "Invalid heap");

			// Return the element we removed
			return valRet;
		}
		#endregion

		#region Virtual methods

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Sets an element in the list we keep our heap elements in </summary>
		///
		/// <remarks>
		/// This is the only way elements should be inserted into LstHeap.  This ensures, among other things,
		/// that the elements in a queue with deletions always have their held indices up to date.
		/// Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="i">	The index into LstHeap. </param>
		/// <param name="val">	The value to be set. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected virtual void SetAt(int i, T val)
		{
			LstHeap[i] = val;
		}
		#endregion

		#region Heapifying

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Queries if a given right son exists. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index of the parent. </param>
		///
		/// <returns>	true if the right son exists. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected bool RightSonExists(int i)
		{
			return RightChildIndex(i) < LstHeap.Count;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Queries if a given left son exists. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index of the parent. </param>
		///
		/// <returns>	true if the left son exists. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected bool LeftSonExists(int i)
		{
			return LeftChildIndex(i) < LstHeap.Count;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Index of parent node. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	Child's index. </param>
		///
		/// <returns>	Index to the parent. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected int ParentIndex(int i)
		{
			return (i - 1) / 2;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Index of left child's node. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	Parent's index. </param>
		///
		/// <returns>	Index to the left child. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected int LeftChildIndex(int i)
		{
			return 2 * i + 1;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Index of right child's node. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	Parent's index. </param>
		///
		/// <returns>	Index to the right child. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected int RightChildIndex(int i)
		{
			return 2 * (i + 1);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Array value at index i. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index. </param>
		///
		/// <returns>	Array value at i. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected T ArrayVal(int i)
		{
			return LstHeap[i];
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns the parent. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index of the child. </param>
		///
		/// <returns>	The parent. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected T Parent(int i)
		{
			return LstHeap[ParentIndex(i)];
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns the left child. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index of the parent. </param>
		///
		/// <returns>	The left child. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected T Left(int i)
		{
			return LstHeap[LeftChildIndex(i)];
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns the right child. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index of the parent. </param>
		///
		/// <returns>	The right child. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected T Right(int i)
		{
			return LstHeap[RightChildIndex(i)];
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Swaps two elements of the priority queue. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index of the first element. </param>
		/// <param name="j">	The index of the second element. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected void Swap(int i, int j)
		{
			var valHold = ArrayVal(i);
			SetAt(i, LstHeap[j]);
			SetAt(j, valHold);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Move an element up the heap to it's proper position </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="i">	The index of the element to move. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected void UpHeap(int i)
		{
			// While we're not the root and our parent is smaller than we are
			while (i > 0 && ArrayVal(i).CompareTo(Parent(i)) > 0)
			{
				//Diagnostics
				Tracer.Trace(t.PqInserts, "Moving {0} to {1}", Parent(i).ToString(), i);
				Tracer.Trace(t.PqPercolates, "UpHeap: Swapping {0} (indx={1}) and {2} (indx={3})",
					LstHeap[i].ToString(),
					i,
					LstHeap[ParentIndex(i)],
					ParentIndex(i));
				// Swap us with our parents
				Swap(i, ParentIndex(i));
				i = ParentIndex(i);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Move an element down the heap to it's proper position. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="i">	The index of the element to move. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected void DownHeap(int i)
		{
			// Until we've reached our final position
			while (i >= 0)
			{
				// Initialize
				var iContinue = -1;

				// If we have a right son and he is larger than us
				if (RightSonExists(i) && Right(i).CompareTo(ArrayVal(i)) > 0)
				{
					// Arrange to swap us with the larger of our two children
					iContinue = Left(i).CompareTo(Right(i)) < 0 ? RightChildIndex(i) : LeftChildIndex(i);
				}
				// Else if we have a left son and he is larger than us
				else if (LeftSonExists(i) && Left(i).CompareTo(ArrayVal(i)) > 0)
				{
					// Arrange to swap with him
					iContinue = LeftChildIndex(i);
				}

				// If we found a node to swap with
				if (iContinue >= 0 && iContinue < LstHeap.Count)
				{
					// Make the swap
					Tracer.Trace(t.PqPercolates, "DownHeap: Swapping {0} (indx={1}) and {2} (indx={3})",
						LstHeap[i].ToString(),
						i,
						LstHeap[iContinue],
						iContinue);
					Swap(i, iContinue);
				}

				// Continue on down the tree if we made a swap
				i = iContinue;
			}
		}
		#endregion

		#region Debugging
		/// <summary> The string indent </summary>
		protected string StrIndent = "";

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Trace element. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="iPos">	The position. </param>
		/// <param name="val">	The value. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		[Conditional("DEBUG")]
		virtual protected void TraceElement(int iPos, T val)
		{
			Tracer.Trace(t.PqTrees, "Pos " + iPos + ":" + StrIndent + val);
		}

		[Conditional("DEBUG")]
		void Indent()
		{
			StrIndent = StrIndent + "\t";
		}

		[Conditional("DEBUG")]
		void Unindent()
		{
			if (StrIndent != String.Empty)
			{
				StrIndent = StrIndent.Remove(StrIndent.Length - 1, 1);
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Print tree. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		[Conditional("DEBUG")]
		protected void PrintTree()
		{
			if (!Tracer.FTracing(t.PqTrees))
			{
				return;
			}
			Tracer.Trace(t.PqTrees, "<<< PQ TREE >>>");
			if (LstHeap.Count == 0)
			{
				return;
			}
			PrintTree(0);
			Tracer.Trace(t.PqTrees, "<<< PQ TREE END >>>");
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Print tree. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <param name="i">	The index into LstHeap. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		[Conditional("DEBUG")]
		protected void PrintTree(int i)
		{
			TraceElement(i, LstHeap[i]);
			Indent();

			if (LeftSonExists(i))
			{
				PrintTree(LeftChildIndex(i));
			}
			if (RightSonExists(i))
			{
				PrintTree(RightChildIndex(i));
			}
			Unindent();
		}
		#endregion

		#region Validation
		internal virtual bool FValidate()
		{
			return LstHeap.Count == 0 || FValidate(0);
		}

		bool FValidate(int iRoot)
		{
			var valRoot = LstHeap[iRoot];

			if (LeftSonExists(iRoot))
			{
				if (valRoot.CompareTo(Left(iRoot)) < 0)
				{
					Tracer.Assert(t.PqValidate, false, "Child is > than parent");
					return false;
				}
				if (!FValidate(LeftChildIndex(iRoot)))
				{
					return false;
				}
			}
			if (RightSonExists(iRoot))
			{
				if (valRoot.CompareTo(Right(iRoot)) < 0)
				{
					Tracer.Assert(t.PqValidate, false, "Child is > than parent");
					return false;
				}
				if (!FValidate(LeftChildIndex(iRoot)))
				{
					return false;
				}
			}
			return true;
		}
		#endregion

		#region IEnumerable<T> Members

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets an enumerator for the items in the queue. </summary>
		///
		/// <remarks>	Darrellp, 2/21/2011. </remarks>
		///
		/// <returns>	The enumerator. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected virtual IEnumerator<T> GetEnumerator()
		{
			return LstHeap.GetEnumerator();
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
		public void TestPq()
		{
			var pq = new PriorityQueue<int>();

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
			PriorityQueueWithDeletions<PQWDElement> priorityQueue = new PriorityQueueWithDeletions<PQWDElement>();

			PQWDElement pq80 = new PQWDElement(80);
			PQWDElement pq90 = new PQWDElement(90);
			PQWDElement pq30 = new PQWDElement(30);
			PQWDElement pq85 = new PQWDElement(85);
			PQWDElement pq20 = new PQWDElement(20);
			PQWDElement pq40 = new PQWDElement(40);
			PQWDElement pq50 = new PQWDElement(50);
			PQWDElement pq35 = new PQWDElement(35);

			priorityQueue.Add(pq40);
			priorityQueue.Add(pq90);
			priorityQueue.Add(pq20);
			priorityQueue.Add(pq30);
			priorityQueue.Add(pq35);
			priorityQueue.Add(pq50);
			priorityQueue.Add(pq85);
			priorityQueue.Delete(pq30);
			Assert.IsTrue(priorityQueue.FValidate());
			Assert.AreEqual(6, priorityQueue.Count);
			int cEnums = 0;
			foreach (IPriorityQueueElement pqe in priorityQueue)
			{
				cEnums++;
			}
			Assert.AreEqual(6, cEnums);
			priorityQueue.Pop();
			priorityQueue.Pop();
			priorityQueue.Pop();
			priorityQueue.Pop();
			priorityQueue.Pop();
			priorityQueue.Pop();

			priorityQueue.Add(pq80);
			Assert.AreEqual(0, ((IPriorityQueueElement)pq80).Index);
			priorityQueue.Delete(pq80);
			pq80 = new PQWDElement(80);
			Assert.AreEqual(-1, ((IPriorityQueueElement)pq80).Index);

			priorityQueue.Add(pq80);
			Assert.AreEqual(pq80, priorityQueue.Peek());
			priorityQueue.Add(pq90);
			Assert.AreEqual(2, priorityQueue.Count);
			Assert.AreEqual(pq90, priorityQueue.Peek());
			Assert.AreEqual(pq90, priorityQueue.Pop());
			Assert.AreEqual(pq80, priorityQueue.Peek());
			priorityQueue.Add(pq30);
			priorityQueue.Add(pq90);
			priorityQueue.Add(pq85);
			priorityQueue.Add(pq20);
			Assert.AreEqual(5, priorityQueue.Count);
			Assert.AreEqual(pq90, priorityQueue.Pop());
			Assert.AreEqual(pq85, priorityQueue.Pop());
			Assert.AreEqual(3, priorityQueue.Count);
			priorityQueue.Add(pq50);
			priorityQueue.Add(pq35);
			Assert.AreEqual(5, priorityQueue.Count);
			Assert.AreEqual(pq80, priorityQueue.Pop());
			Assert.AreEqual(pq50, priorityQueue.Pop());
			Assert.AreEqual(pq35, priorityQueue.Pop());
			Assert.AreEqual(pq30, priorityQueue.Pop());
			Assert.AreEqual(pq20, priorityQueue.Pop());
			Assert.AreEqual(0, priorityQueue.Count);

			priorityQueue.Add(pq35);
			priorityQueue.Add(pq50);
			priorityQueue.Add(pq20);
			priorityQueue.Add(pq85);
			priorityQueue.Add(pq30);
			priorityQueue.Add(pq90);
			priorityQueue.Add(pq80);

			priorityQueue.Delete(pq50);
			priorityQueue.Delete(pq30);
			Assert.AreEqual(5, priorityQueue.Count);
			Assert.AreEqual(pq90, priorityQueue.Pop());
			Assert.AreEqual(pq85, priorityQueue.Pop());
			Assert.AreEqual(pq80, priorityQueue.Pop());
			Assert.AreEqual(pq35, priorityQueue.Pop());
			Assert.AreEqual(pq20, priorityQueue.Pop());
			Assert.AreEqual(0, priorityQueue.Count);
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

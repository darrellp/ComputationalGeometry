using NetTrace;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Priority queue with deletions. </summary>
	///
	/// <remarks>
	/// This is the same as the priority queue but the elements stored in it must satisfy the
	/// IPriorityQueueElement interface.  This allows us to store and retrieve our local indices in
	/// the objects so when we go to delete them we can query them for their index and then remove
	/// the element at that index.
	/// 	
	/// Darrellp, 2/17/2011. 
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class PriorityQueueWithDeletions<T> : PriorityQueue<T> where T : IPriorityQueueElement
	{
		#region Public overrides

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Removes and returns the largest object. </summary>
		///
		/// <remarks>	Darrellp, 2/19/2011. </remarks>
		///
		/// <returns>	The previous largest object. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public override T Pop()
		{
			var valRet = base.Pop();

			// When an element is removed from the heap, it's index must be reset.
			valRet.Index = -1;
			return valRet;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Delete a value from the heap. </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="val">	Value to remove. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public void Delete(T val)
		{
			// Retrieve the item's index
			var i = val.Index;

			// Does it have a valid index?
			if (i >= 0)
			{
				// Reset the index on the item
				val.Index = -1;
				Tracer.Assert(t.Assertion, i < LstHeap.Count, "Trying to remove an element beyond the end of the heap");
				Tracer.Trace(t.PqDeletes, "Deleting {0} (pos {1})", val.ToString(), i);

				// Swap the item to be deleted for the last element in the array
				Swap(i, LstHeap.Count - 1);

				// Drop the last element of the array
				// This is the element we just placed there - i.e., the one to be deleted
				LstHeap.RemoveAt(LstHeap.Count - 1);

				// Do we need to make further adjustments
				// If this was already the last element of the array, we're done.  If
				// not then we have to make further adjustments
				if (i < LstHeap.Count)
				{
					// Are we larger than our parent?
					if (i != 0 && LstHeap[i].CompareTo(Parent(i)) > 0)
					{
						// Move us up the tree
						UpHeap(i);
					}
					else
					{
						// Move us down the tree
						DownHeap(i);
					}
				}
			}
			
			// Trace output
			PrintTree();
			Tracer.Assert(t.PqValidate, FValidate(), "Invalid heap");
		}
		#endregion

		#region Private overrides
		internal override bool FValidate()
		{
			for (int iVal = 0; iVal < LstHeap.Count; iVal++)
			{
				if (LstHeap[iVal].Index != iVal)
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
			             "Pos " + iPos + (val.Index < 0 ? "<DEL>" : "" + ":") + StrIndent + val);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// This override is the magic that makes the deletions work by keeping track of the index a
		/// particular element is moved to. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/17/2011. </remarks>
		///
		/// <param name="i">	The index. </param>
		/// <param name="val">	The value. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		protected override void SetAt(int i, T val)
		{
			base.SetAt(i, val);
			val.SetIndex(i);
		}
		#endregion
	}
}
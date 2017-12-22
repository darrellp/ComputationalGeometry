using System;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	
	/// In a priority queue which supports deletion, elements must keep track of their position
	/// within the queue's heap list.  This interface supports that. 
	/// </summary>
	///
	/// <remarks>	Darrellp, 2/17/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public interface IPriorityQueueElement : IComparable
	{
		///<summary>
		/// This is called when the priority queue needs to store it's index
		///</summary>
		///<param name="i">The index to be stored</param>
		void SetIndex(int i);

		///<summary>
		/// Returns the index stored in SetIndex
		///</summary>
		int Index { get; set; }
	}
}
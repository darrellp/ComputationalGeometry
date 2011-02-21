using System.Collections.Generic;

namespace DAP.CompGeom
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Queue of fortune events. </summary>
	///
	/// <remarks>	Darrellp, 2/18/2011. </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	internal class EventQueue : PriorityQueueWithDeletions<FortuneEvent>
	{
		#region Constructor

		public EventQueue()
		{
			CircleEvents = new LinkedList<CircleEvent>();
		}

		#endregion

		#region Circle event special handling
		internal void AddCircleEvent(CircleEvent cevt)
		{
			// We have to keep special track of circle events
			Add(cevt);
			CircleEvents.AddFirst(cevt);
			cevt.LinkedListNode = CircleEvents.First;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the circle events. </summary>
		///
		/// <value>	The circle events. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		internal LinkedList<CircleEvent> CircleEvents { get; private set; }

		#endregion
	}
}
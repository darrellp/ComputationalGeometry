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
			CircleEvents = new List<CircleEvent>();
		}

		#endregion

		#region Circle event special handling
		internal void AddCircleEvent(CircleEvent cevt)
		{
			// We have to keep special track of circle events
			Add(cevt);
			CircleEvents.Add(cevt);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets the circle events. </summary>
		///
		/// <value>	The circle events. </value>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		internal List<CircleEvent> CircleEvents { get; private set; }

		#endregion
	}
}
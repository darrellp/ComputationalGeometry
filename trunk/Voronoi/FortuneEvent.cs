#if DOUBLEPRECISION
using PT = DAP.CompGeom.PointD;
using TPT = System.Double;
#else
using PT = System.Drawing.PointF;
using TPT = System.Single;
#endif
using NetTrace;
using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace DAP.CompGeom
{
	/// <summary>
	/// The fortune algorithm works by moving a sweepline down through the sites of the 
	/// diagram.  During that movement, events are added, removed and popped from a priority
	/// queue.  Those events each have a y coordinate and the event with the largest y
	/// coordinate is popped out of the priority queue.  These events are of two types: circle
	/// events (CircleEvent) and site events (SiteEvent).  FortuneEvent is an abstract class
	/// which serves as the base for both these types of event.
	/// </summary>
	abstract internal class FortuneEvent : IPriorityQueueElement
	{
		#region Private Variables
		int _i = -1;				// Index we maintain for the IPriorityQueueElement operations
		PT _pt;						// Point this event is based on - the center of the circle for circle events
									// and the location of the site for site events.
		#endregion

		#region Properties
		internal PT Pt
		{
			get
			{
				return _pt;
			}
			set
			{
				_pt = value;
			}
		}
		#endregion

		#region Constructor
		internal FortuneEvent(PT pt)
		{
			_pt = pt;
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
		/// <summary>
		/// Compare two events.  We order them using y coordinate first and then x coordinate.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		int IComparable.CompareTo(object obj)
		{
			Tracer.Assert(t.Assertion, obj.GetType().IsSubclassOf(typeof(FortuneEvent)),
				"Passing in non-Event to CompareTo");

			PT ptCompare = ((FortuneEvent)obj).Pt;

			// If two events have essentially the same Y coordinate, we defer to the X coordinate
			if (Geometry.FCloseEnough(Pt.Y, ptCompare.Y))
			{
				if (Pt.X > ptCompare.X)
				{
					return 1;
				}
				else if (Pt.X < ptCompare.X)
				{
					return -1;
				}
				else if (GetType() == typeof(SiteEvent) && obj.GetType() == typeof(CircleEvent))
				{
					// Site events are bigger than circle events at the same point
					return 1;
				}
				return 0;
			}
			if (Pt.Y > ptCompare.Y)
			{
				return 1;
			}
			return -1;
		}
		#endregion

		#region Abstract handler
		/// <summary>
		/// Handle the event
		/// </summary>
		/// <param name="fortune">Fortune data structure being built</param>
		abstract internal void Handle(Fortune fortune);
		#endregion

		#region Circle creation
		internal static CircleEvent CreateCircleEvent(FortunePoly poly1, FortunePoly poly2, FortunePoly poly3, TPT yScanLine)
		{
			PT ptCenter;
			CircleEvent cevtRet = null;

			/// Circle events are created at the circumcenters of three sites - the sites for poly1/2/3.
			if (Geometry.FFindCircumcenter(poly1.VoronoiPoint, poly2.VoronoiPoint, poly3.VoronoiPoint, out ptCenter))
			{
				TPT radius = Geometry.Distance(poly1.VoronoiPoint, ptCenter);

				ptCenter.Y -= radius;

				// If the circumcenter is above the scan line we've already passed it by, so don't put it in the queue
				if (ptCenter.Y <= yScanLine)
				{
					cevtRet = new CircleEvent(ptCenter, radius);
				}
				else
				{
					Tracer.Trace(tv.CCreate, "Rejected circle event because it is above scan line");
				}
			}
			else
			{
				Tracer.Trace(tv.CCreate, "Rejected circle event generators are collinear");
			}

			return cevtRet;
		}
		#endregion

		#region NUnit
		[TestFixture]
		public class TestEvent
		{
			[Test]
			public void TestICompare()
			{
				SiteEvent evtSmaller = new SiteEvent(new FortunePoly(new PT(0, 0), 0));
				SiteEvent evtLarger = new SiteEvent(new FortunePoly(new PT(-1, 1), 0));
				SiteEvent evtEqual = new SiteEvent(new FortunePoly(new PT(1, 1), 0));

				Assert.IsTrue(((IComparable)evtSmaller).CompareTo(evtLarger) < 0);
			}
		}
		#endregion
	}

	/// <summary>
	/// Site events are inserted into the priority queue when the fortune object is created.
	/// See its constructor for details.
	/// </summary>
	internal class SiteEvent : FortuneEvent
	{
		#region Private variables
		FortunePoly _poly;				// The polygon in the Fortune object this site corresponds to
		#endregion

		#region Properties
		internal FortunePoly Poly
		{
			get { return _poly; }
			set { _poly = value; }
		}
		#endregion

		#region Constructor
		internal SiteEvent(FortunePoly poly) : base(poly.VoronoiPoint)
		{
			_poly = poly;
			Tracer.Trace(tv.InsertSite, "Inserting site: {0}", this.ToString());
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return string.Format("SiteEvent: Gen = {0}, Pt = ({1}, {2})", Poly.Index, Pt.X, Pt.Y);
		}
		#endregion

		#region Event handling
		/// <summary>
		/// Handle a site event.  This is done by adding a polgyon/parabola to the beachline.
		/// </summary>
		/// <param name="fortune">The fortune object to update</param>
		internal override void Handle(Fortune fortune)
		{
			Tracer.Trace(tv.SiteEvents, "Site event at ({0}, {1}) for gen {2}", Pt.X, Pt.Y, Poly.Index);
#if NETTRACE || DEBUG
			if (Tracer.FTracing(tv.Beachline))
			{
				fortune.Bchl.TraceBeachline(Pt.Y);
			}
#endif
			Tracer.Indent();

			// Insert the new parabola into the beachline
			fortune.Bchl.PolyInsertNode(this, fortune.QevEvents);
			Tracer.Unindent();
#if NETTRACE || DEBUG
			if (Tracer.FTracing(tv.Trees))
			{
				fortune.Bchl.NdRoot.TraceTree("After Site event:", tv.Trees);
			}
#endif
		}
		#endregion
	}

	/// <summary>
	/// Circle events snuff out a parabola on the beachline
	/// </summary>
	internal class CircleEvent : FortuneEvent
	{
		#region Private Variables
		TPT _radius = 0;						// Radius of the circle
		private TPT _radiusSq = 0;
		LeafNode _lfnEliminated = null;			// The leafnode which will be eliminated by this circle event
		bool _fZeroLength = false;				// True if this represents a zero length edge
		#endregion

		#region Properties
		internal bool FZeroLength
		{
			get { return _fZeroLength; }
			set { _fZeroLength = value; }
		}
		internal LeafNode LfnEliminated
		{
			get { return _lfnEliminated; }
			set
			{
				_lfnEliminated = value;
			}
		}

		internal PT VoronoiVertex
		{
			get
			{
				return new PT(Pt.X, Pt.Y + _radius);
			}
		}
		#endregion

		#region Constructor
		internal CircleEvent(PT pt, TPT radius) : base(pt)
		{
			_radius = radius;
			_radiusSq = radius*radius;
		}
		#endregion

		#region ToString
		public override string ToString()
		{
			return string.Format("CircleEvent: Gen = {0}, vtx = ({1}, {2}), yscl = {3}",
				LfnEliminated.Poly.Index, VoronoiVertex.X, VoronoiVertex.Y, Pt.Y);
		}
		#endregion

		#region Event handling
		/// <summary>
		/// Handle a circle event.  Snuff out the a parabola, form a vertex of the diagram.
		/// </summary>
		/// <param name="fortune"></param>
		internal override void Handle(Fortune fortune)
		{
			Tracer.Trace(tv.CircleEvents,
				"Handling Circle event {5}at y = {5} with vtx ({0}, {1}) for edge between gen {2} and gen {3} - eliminating gen {4}",
				VoronoiVertex.X, VoronoiVertex.Y,
				LfnEliminated.LeftAdjacentLeaf.Poly.Index,
				LfnEliminated.RightAdjacentLeaf.Poly.Index,
				LfnEliminated.Poly.Index,
				Pt.Y,
				FZeroLength ? "(fzero) " : "");
#if NETTRACE || DEBUG
			if (Tracer.FTracing(tv.Beachline))
			{
				fortune.Bchl.TraceBeachline(Pt.Y);
			}
#endif
			Tracer.Indent();
			// Remove a parabola node from the beachline since it's being squeezed out.  Insert a vertex into
			// the voronoi diagram
			fortune.Bchl.RemoveNodeAndInsertVertex(this, LfnEliminated, VoronoiVertex, fortune.QevEvents);
			Tracer.Unindent();

#if NETTRACE || DEBUG
			if (Tracer.FTracing(tv.Trees))
			{
				fortune.Bchl.NdRoot.TraceTree("After Circle event:", tv.Trees);
			}
#endif
		}
		#endregion

		#region Queries
		/// <summary>
		/// Returns true if the circle event contains the passed in point
		/// </summary>
		/// <param name="pt">Point to check</param>
		/// <returns>True if its contained in the circle, else false</returns>
		internal bool Contains(PT pt)
		{
			return Geometry.DistanceSq(pt, Pt) <= _radiusSq;
		}
		#endregion
	}
}

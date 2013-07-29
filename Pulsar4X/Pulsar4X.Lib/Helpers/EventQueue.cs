using System;
using System.Collections.Generic;
namespace Pulsar4X
{
	/// <summary>
	/// Event queue class. Used to find the next necessary time increment.
	/// Gives three functions. Currently stub.
	/// </summary>
	public class EventQueue
	{
		private List<DateTime> eventQueue;

		/// <summary>
		/// Initializes a new instance of the <see cref="Pulsar4X.EventQueue"/> class.
		/// </summary>
		public EventQueue () {
			 eventQueue = new List<DateTime>();
		}

		/// <summary>
		/// Inserts a new event that should happen at the specified time.
		/// Currently stub.
		/// </summary>
		/// <param name='dtToAdd'>
		/// The date/time of the event to add.
		/// </param>
		public void insertEvent(DateTime dtToAdd) {
			eventQueue.Add(dtToAdd);
			eventQueue.Sort();
		}

		/// <summary>
		/// Gets the next date/time the next event happens. Currently stub.
		/// </summary>
		/// <returns>
		/// The date/time of the next event. May be NULL if no events are currently stored.
		/// </returns>
		public DateTime getNextEventTime() {
			return eventQueue[0];
		}

		/// <summary>
		/// Extracts the next event and returns the date/time for it. Currently stub.
		/// </summary>
		/// <returns>
		/// The date/time of the next event. May be NULL if no events are currently stored.
		/// </returns>
		public DateTime extractNextEvent() {
			DateTime tmp = eventQueue[0];
			eventQueue.RemoveAt(0);
			return tmp;
		}

		/// <summary>
		/// Returns the number of elements in the queue.
		/// </summary>
		/// <returns>
		/// The number of events in queue. Always positive.
		/// </returns>
		public int getEventNumberInQueue() {
			return eventQueue.Count;
		}
	}
}


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
		private int elementsContained;
		/// <summary>
		/// Initializes a new instance of the <see cref="Pulsar4X.EventQueue"/> class.
		/// </summary>
		public EventQueue () {
			eventQueue = new List<DateTime>();
			elementsContained = 0;
		}

		/// <summary>
		/// Inserts a new event that should happen at the specified time.
		/// Currently stub.
		/// </summary>
		/// <param name='dtToAdd'>
		/// The date/time of the event to add.
		/// </param>
		public void insertEvent(DateTime dtToAdd) {
			if (elementsContained >= eventQueue.Count) {
				eventQueue.Add(dtToAdd);
			} else {
				eventQueue[elementsContained] = dtToAdd;
			}
			elementsContained++;
			siftUpIndex(elementsContained-1);
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
			if (elementsContained <= 0) {
				throw new InvalidOperationException("Tried extracting the root element of an empty heap");
			}
			DateTime tmp = eventQueue[0];
			swap(0, elementsContained-1);
			elementsContained--;
			siftDownIndex(0);
			return tmp;
		}

		/// <summary>
		/// Returns the number of elements in the queue.
		/// </summary>
		/// <returns>
		/// The number of events in queue. Always positive.
		/// </returns>
		public int getEventNumberInQueue() {
			return elementsContained;
		}

		private void siftUpIndex (int index) {
			int i = index;
			while (getParentIndex(i) != -1 && eventQueue[i] < eventQueue[getParentIndex(i)]) {
				swap(i, getParentIndex(i));
				i = getParentIndex (i);
			}
		}		

		private void siftDownIndex (int index) {
			int i = index;
			int childIndex = getLeftChildIndex(i);
			if (getRightChildIndex(i) != -1 && eventQueue[getRightChildIndex(i)] < eventQueue[getLeftChildIndex(i)]) {
				childIndex = getRightChildIndex(i);
			}
			while (childIndex != -1 && eventQueue[childIndex] < eventQueue[i]) {
				swap(i, childIndex);
				i = childIndex;
				childIndex = getLeftChildIndex(i);
				if (getRightChildIndex(i) != -1 && eventQueue[getRightChildIndex(i)] < eventQueue[getLeftChildIndex(i)]) {
					childIndex = getRightChildIndex(i);
				}
			}
		}

		/// <summary>
		/// Swap the cells with the indices i and j.
		/// </summary>
		/// <param name='i'>
		/// One of the cells to be swapped.
		/// Take care that it's smaller than elementscontained.
		/// </param>
		/// <param name='j'>
		/// One of the cells to be swapped.
		/// Take care that it's smaller than elementscontained.
		/// </param>
		private void swap(int i, int j) {
			DateTime tmp = eventQueue[i];
			eventQueue[i] = eventQueue[j];
			eventQueue[j] = tmp;
		}

		private int getParentIndex(int i) {
			if (i > 0) {
				return getSafeIndex((i-1)/2);
			}
			return -1;
		}

		private int getSafeIndex(int i) {
			if (i < elementsContained) {
				return i;
			}
			return -1;
		}

		private int getRightChildIndex(int i) {
			return getSafeIndex(2*i+2);
		}

		private int getLeftChildIndex(int i) {
			return getSafeIndex(2*i+1);
		}
	}
}


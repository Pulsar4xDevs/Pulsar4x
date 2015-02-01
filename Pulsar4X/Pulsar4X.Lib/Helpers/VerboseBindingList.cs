using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Pulsar4X.Helpers
{
    public class ListChangingEventArgs 
    { 
        public ListChangedType ListChangedType;
        public object ChangingObject;
        public int ChangingIndex;

        public ListChangingEventArgs(ListChangedType listChangedType, object changingObject, int changingIndex)
        {
            ListChangedType = listChangedType;
            ChangingObject = changingObject;
            ChangingIndex = changingIndex;
        }
    }

    /// <summary>
    /// While BindingList is great, it removes/adds the object before firing the event,
    /// and doesn't give any kind of access to the object that was added/removed.
    /// 
    /// This class fires ListChanging event BEFORE changing the list, we also provide
    /// easy access to the object that was changed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VerboseBindingList<T> : BindingList<T>
    {
        public delegate void ListChangingEventHandler(object sender, ListChangingEventArgs e);
        public event ListChangingEventHandler ListChanging;

        protected override void RemoveItem(int itemIndex)
        {
            T item = this.Items[itemIndex];

            if (ListChanging != null)
            {
                // Fire ListChanging before removal.
                ListChanging(this, new ListChangingEventArgs(ListChangedType.ItemDeleted, item, itemIndex));
            }

            // Remove item from list
            // This fires ListChanged
            base.RemoveItem(itemIndex);
        }

        protected override void InsertItem(int itemIndex, T item)
        {
            if (ListChanging != null)
            {
                // Fire ListChanging before adding.
                ListChanging(this, new ListChangingEventArgs(ListChangedType.ItemAdded, item, itemIndex));
            }

            // Add item to the list.
            // This fires ListChanged
            base.InsertItem(itemIndex, item);
        }
    }
}

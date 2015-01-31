using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Entities.Components;

/// <summary>
/// I am concerned that the distances here are going to get too massive without some bounds checking, that will have to be added in at some point.
/// </summary>

namespace Pulsar4X.Entities
{
    public class DistanceTable
    {
        private SystemContact m_parent;

        private Dictionary<SystemContact, float> m_distances;
        private Dictionary<SystemContact, int> m_lastUpdateSecond;
        private Dictionary<SystemContact, int> m_lastUpdateYear;

        public bool GetDistance(SystemContact contact, out float distance)
        {
            if (m_distances.TryGetValue(contact, out distance))
            {
                if (m_lastUpdateSecond[contact] == GameState.Instance.CurrentSecond && m_lastUpdateYear[contact] == GameState.Instance.CurrentYear)
                {
                    return true;
                }
            }
            distance = m_parent.Position.GetDistanceTo(contact.Position);
            UpdateDistance(contact, distance);
            contact.DistTable.UpdateDistance(m_parent, distance);
            return false;
        }

        public DistanceTable(SystemContact parent)
        {
            m_parent = parent;
        }

        private void UpdateDistance(SystemContact contact, float distance)
        {
            m_distances[contact] = distance;
            m_lastUpdateSecond[contact] = GameState.Instance.CurrentSecond;
            m_lastUpdateYear[contact] = GameState.Instance.CurrentYear;
        }

        public void Clear()
        {
            m_distances.Clear();
            m_lastUpdateSecond.Clear();
            m_lastUpdateYear.Clear();
        }

        internal void Remove(SystemContact contact)
        {
            m_distances.Remove(contact);
            m_lastUpdateSecond.Remove(contact);
            m_lastUpdateYear.Remove(contact);
        }
    }

    public class SystemContact : StarSystemEntity
    {
        /// <summary>
        /// To which faction does this contact belong?
        /// </summary>
        public Faction faction { get; set; }

        /// <summary>
        /// where the contact was on the last tick.
        /// </summary>
        public SystemPosition LastPosition;

        /// <summary>
        /// Bascking entity of this contact.
        /// </summary>
        public StarSystemEntity Entity;

        // TODO: Make distance table it's own class/struct, get it out of here.

        /// <summary>
        /// distance between this contact and the other contacts in the system in AU.
        /// </summary>
        public DistanceTable DistTable { get; set; }

        /// <summary>
        /// Creates a new system contact.
        /// </summary>
        /// <param name="Fact">Faction of contact.</param>
        /// <param name="entity">Backing entity of the contact.</param>
        public SystemContact(Faction Fact, StarSystemEntity entity)
        {
            Id = Guid.NewGuid();
            faction = Fact;
            Position = entity.Position;
            LastPosition = Position;

            Entity = entity;

            DistTable = new DistanceTable(this);

            SSEntity = entity.SSEntity;
        }

        /// <summary>
        /// Updates the location of the contact.
        /// </summary>
        /// <param name="X">X position in AU.</param>
        /// <param name="Y">Y Position in AU.</param>
        public void UpdateLocationInSystem(double X, double Y)
        {
            LastPosition.X = Position.X;
            LastPosition.Y = Position.Y;
            Position.X = X;
            Position.Y = Y;
        }

        /// <summary>
        /// Updates the contact after transiting a jump point, LastPosition.X needs to be set to current position for the travel line.
        /// </summary>
        /// <param name="X">X position in AU in the new system</param>
        /// <param name="Y">Y position in AU in the new system</param>
        public void UpdateLocationAfterTransit(double X, double Y)
        {
            LastPosition.X = X;
            LastPosition.Y = Y;
            Position.X = X;
            Position.Y = Y;
        }

        /// <summary>
        /// Updates the system location of this contact. The 4 blocks of updates to the lists will, I hope, facilitate efficient updating of the binding list.
        /// </summary>
        /// <param name="system">new System.</param>
        public void UpdateSystem(StarSystem system)
        {
            Position.System = system;

            DistTable.Clear();
        }
    }
}

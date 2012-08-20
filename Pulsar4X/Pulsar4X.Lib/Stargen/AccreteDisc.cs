using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;

namespace Pulsar4X.Stargen
{
    public abstract class AccreteDisc
    {
        public List<AccreteBand> Bands { get; set; }
        public List<ProtoPlanet> Planets { get; set; }

        public double PlanetInnerBound { get; set; }
        public double PlanetOuterBound { get; set; }
        public double OuterDustLimit { get; set; }
        public double InnerDustLimit { get; set; }

        public abstract double Mass { get; }

        public abstract Star Star { get; set; }

        public void initDisc(double innerdust, double outerdust)
        {
            InnerDustLimit = innerdust;
            OuterDustLimit = outerdust;

            PlanetInnerBound = NearestPlanet;
            PlanetOuterBound = FarthestPlanet;

            Bands = new List<AccreteBand>
                {
                    new AccreteBand { DustPresent = true, GasPresent = true, InnerEdge = InnerDustLimit, OuterEdge = OuterDustLimit }
                };

            Planets = new List<ProtoPlanet>();
        }

        public double FarthestPlanet
        {
            get
            {
                return (50.0 * Math.Pow(Mass, (1.0 / 3.0)));
            }
        }

        public double NearestPlanet
        {
            get
            {
                return (0.3 * Math.Pow(Mass, (1.0 / 3.0)));
            }
        }

        public bool DustAvailable
        {
            get
            {
                return IsDustLeftRange(PlanetInnerBound, PlanetOuterBound);
            }
        }

        public bool IsDustAvailable(ProtoPlanet p)
        {
            return IsDustLeftRange(p.InnerEffectLimit, p.OuterEffectLimit);
        }

        public bool GasAvailable
        {
            get
            {
                return IsGasLeftRange(PlanetInnerBound, PlanetOuterBound);
            }
        }

        public bool IsGasAvailable(ProtoPlanet p)
        {
            return IsGasLeftRange(p.InnerEffectLimit, p.OuterEffectLimit);
        }

        public bool IsDustLeftRange(double inner, double outer)
        {
            foreach (AccreteBand band in Bands)
            {
                if (band.OuterEdge <= inner) continue;
                if (band.InnerEdge >= outer) continue;

                if (band.DustPresent) return true;
            }
            return false;
        }

        public bool IsGasLeftRange(double inner, double outer)
        {
            foreach (AccreteBand band in Bands)
            {
                if (band.OuterEdge <= inner) continue;
                if (band.InnerEdge >= outer) continue;

                if (band.GasPresent) return true;
            }
            return false;
        }

        public void UpdateDust(ProtoPlanet p)
        {
            UpdateDust(p.InnerEffectLimit, p.OuterEffectLimit, p.Mass > p.CriticalLimit);
        }

        public void UpdateDust(double inner, double outer, bool gas)
        {
            List<AccreteBand> dustCopy = new List<AccreteBand>(Bands);
            foreach (AccreteBand band in dustCopy)
            {
                if (band.OuterEdge <= inner) continue;
                if (band.InnerEdge >= outer) continue;

                // Situation where band is fully contained
                if (band.InnerEdge >= inner && band.OuterEdge <= outer) band.DustPresent = false;
                else if (band.OuterEdge <= outer && band.InnerEdge < inner)
                {
                    // Upper split
                    AccreteBand newband = new AccreteBand() { DustPresent = false, GasPresent = (band.GasPresent && gas), InnerEdge = inner, OuterEdge = band.OuterEdge };
                    band.OuterEdge = inner;
                    Bands.Insert(Bands.IndexOf(band) + 1, newband);
                }
                else if (band.InnerEdge >= inner && band.OuterEdge > outer)
                {
                    // Lower split
                    AccreteBand newband = new AccreteBand() { DustPresent = false, GasPresent = (band.GasPresent && gas), InnerEdge = band.InnerEdge, OuterEdge = outer };
                    band.InnerEdge = outer;
                    Bands.Insert(Bands.IndexOf(band), newband);
                }
                else
                {
                    // Internal split
                    AccreteBand low_band = new AccreteBand() { DustPresent = band.DustPresent, GasPresent = band.GasPresent, InnerEdge = band.InnerEdge, OuterEdge = inner };
                    AccreteBand high_band = new AccreteBand() { DustPresent = band.DustPresent, GasPresent = band.GasPresent, InnerEdge = outer, OuterEdge = band.OuterEdge };
                    band.DustPresent = false;
                    band.GasPresent = band.GasPresent && gas;
                    band.InnerEdge = inner;
                    band.OuterEdge = outer;
                    Bands.Insert(Bands.IndexOf(band), low_band);
                    Bands.Insert(Bands.IndexOf(band) + 1, high_band);
                }
            }

            LinkedList<AccreteBand> dustList = new LinkedList<AccreteBand>(Bands);
            LinkedListNode<AccreteBand> current = dustList.First;
            LinkedListNode<AccreteBand> next = null;
            while (current != null)
            {
                next = current.Next;
                if (next != null)
                    if ((current.Value.DustPresent == next.Value.DustPresent) && (current.Value.GasPresent == next.Value.GasPresent))
                    {
                        // Merge dustbands
                        current.Value.OuterEdge = next.Value.OuterEdge;
                        dustList.Remove(next);
                        Bands.Remove(next.Value);
                    }
                current = current.Next;
            }
        }
    }
}

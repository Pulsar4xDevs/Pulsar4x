using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;
using System.Drawing;
using System.Collections.ObjectModel;
using Pulsar4X.Helpers.GameMath;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Point defense is a static class for these two public methods that are fairly large, but otherwise are related to simEntity.
    /// </summary>
    public class PointDefense
    {
        /// <summary>
        /// Final defensive fire scans through all potential FCs that could fire defensively on the incoming missile to see if it is intercepeted.
        /// All PD enabled FCs will attempt to shoot down this missile except ones from the same faction, as this missile is practically right on top of said FC.
        /// In other words allied/neutral status isn't taken into account.
        /// </summary>
        /// <param name="P">Faction list</param>
        /// <param name="Missile">Missile to try to intercept</param>
        /// <param name="RNG">Random Number Generator</param>
        /// <returns>Whether the missile has been intercepted</returns>
        public static bool FinalDefensiveFire(BindingList<Faction> P, OrdnanceTN Missile, Random RNG)
        {
            bool Intercept = false;
            StarSystem CurrentSystem = Missile.missileGroup.contact.Position.System;
            float PointBlank = (float)Distance.ToAU(10000.0);

            /// <summary>
            /// loop through every faction.
            /// </summary>
            foreach (Faction faction in P)
            {
                /// <summary>
                /// Is the current faction different from the missile group faction, and does the faction have a detected contacts list for the current system?
                /// </summary>
                if (faction != Missile.missileGroup.ordnanceGroupFaction && faction.DetectedContactLists.ContainsKey(CurrentSystem) == true)
                {
                    /// <summary>
                    /// Is the Missile group in this detected contact list?
                    /// </summary>
                    if (faction.DetectedContactLists[CurrentSystem].DetectedMissileContacts.ContainsKey(Missile.missileGroup) == true)
                    {
                        /// <summary>
                        /// Is the detection an active detection?
                        /// </summary>
                        if (faction.DetectedContactLists[CurrentSystem].DetectedMissileContacts[Missile.missileGroup].active == true)
                        {
                            /// <summary>
                            /// Does this faction have any point defense enabled FCs in this system?
                            /// </summary>
                            if (faction.PointDefense.ContainsKey(CurrentSystem) == true)
                            {
                                /// <summary>
                                /// loop through all the possible PD enabled FC.
                                /// </summary>
                                foreach (KeyValuePair<ComponentTN, ShipTN> pair in faction.PointDefense[CurrentSystem].PointDefenseFC)
                                {
                                    ShipTN Ship = pair.Value;

                                    /// <summary>
                                    /// Ship jump sickness will prevent point defense from operating.
                                    /// </summary>
                                    if (Ship.IsJumpSick())
                                        continue;

                                    /// <summary>
                                    /// Only want BFCs in FDF mode for now.
                                    /// </summary>
                                    if (faction.PointDefense[CurrentSystem].PointDefenseType[pair.Key] == false && pair.Value.ShipBFC[pair.Key.componentIndex].pDState == PointDefenseState.FinalDefensiveFire)
                                    {
                                        BeamFireControlTN ShipBeamFC = pair.Value.ShipBFC[pair.Key.componentIndex];

                                        /// <summary>
                                        /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                                        /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                                        /// </summary>
                                        float dist = -1;

                                        /// <summary>
                                        /// dist is in AU.
                                        /// </summary>
                                        Missile.missileGroup.contact.DistTable.GetDistance(Ship.ShipsTaskGroup.Contact, out dist);

                                        /// <summary>
                                        /// if distance is less than the 10k km threshold attempt to intercept at Point blank range.
                                        /// </summary>
                                        if (dist < PointBlank)
                                        {
                                            /// <summary>
                                            /// Finally intercept the target.
                                            /// </summary>
                                            bool WF = false;
                                            Intercept = ShipBeamFC.InterceptTarget(RNG, 0, Missile, Ship.ShipsFaction,
                                                                                                                    Ship.ShipsTaskGroup.Contact, Ship, out WF);
                                            /// <summary>
                                            /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                            /// </summary>
                                            if (WF == true)
                                            {
                                                if (faction.RechargeList.ContainsKey(Ship) == true)
                                                {
                                                    /// <summary>
                                                    /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                                    /// </summary>
                                                    if ((faction.RechargeList[pair.Value] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                    {
                                                        faction.RechargeList[pair.Value] = (faction.RechargeList[pair.Value] + (int)Faction.RechargeStatus.Weapons);
                                                    }
                                                }
                                                else
                                                {
                                                    faction.RechargeList.Add(pair.Value, (int)Faction.RechargeStatus.Weapons);
                                                }
                                            }

                                            /// <summary>
                                            /// break out of the first foreach loop.
                                            /// </summary>
                                            if (Intercept == true)
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                /// <summary>
                /// now break out of the faction loop as this missile has been shot down.
                /// </summary>
                if (Intercept == true)
                    break;
            }
            return Intercept;
        }

        /// <summary>
        /// Area defensive fire will sweep through a faction's list of BFCs and MFCs to fire at detected ordnance in range.
        /// </summary>
        /// <param name="Fact">Faction to search for fire controls of</param>
        /// <param name="RNG">"global" rng from further up.</param>
        public static void AreaDefensiveFire(Faction Fact, Random RNG)
        {
            /// <summary>
            /// No point defense set FCs, just return.
            /// </summary>
            if (Fact.PointDefense.Count == 0)
            {
                return;
            }

            /// <summary>
            /// Look through each starsystem with a point defense list.
            /// </summary>
            foreach (KeyValuePair<StarSystem, PointDefenseList> pair in Fact.PointDefense)
            {
                StarSystem CurrentSystem = pair.Key;

                /// <summary>
                /// No detected contacts in this system.
                /// </summary>
                if (Fact.DetectedContactLists.ContainsKey(CurrentSystem) == false)
                {
                    break;
                }

                /// <summary>
                /// No missile contacts in this system.
                /// </summary>
                if (Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts.Count == 0)
                {
                    break;
                }

                /// <summary>
                /// now loop through each FC in the current starsystem.
                /// </summary>
                foreach (KeyValuePair<ComponentTN, ShipTN> pair2 in pair.Value.PointDefenseFC)
                {
                    /// <summary>
                    /// The firing ship.
                    /// </summary>
                    ShipTN Ship = pair2.Value;

                    /// <summary>
                    /// Ship jump sickness will prevent point defense from operating.
                    /// </summary>
                    if (Ship.IsJumpSick())
                        continue;

                    /// <summary>
                    /// BFC set to Area defense mode
                    /// </summary>
                    if (pair.Value.PointDefenseType[pair2.Key] == false && pair2.Value.ShipBFC[pair2.Key.componentIndex].pDState == PointDefenseState.AreaDefense)
                    {
                        BeamFireControlTN ShipBeamFC = pair2.Value.ShipBFC[pair2.Key.componentIndex];
                        /// <summary>
                        /// loop through every missile contact. will have to do distance checks here.
                        /// </summary>
                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> MisPair in Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts)
                        {
                            OrdnanceGroupTN DetectedMissileGroup = MisPair.Key;
                            /// <summary>
                            /// This missile group is already destroyed and will be cleaned up by sim later.
                            /// </summary>
                            if (DetectedMissileGroup.missilesDestroyed == DetectedMissileGroup.missiles.Count)
                                break;

                            /// <summary>
                            /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                            /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                            /// </summary>
                            float dist;

                            DetectedMissileGroup.contact.DistTable.GetDistance(Ship.ShipsTaskGroup.Contact, out dist);

                            /// <summary>
                            /// Only bother with checks here that are within the maximum beam distance.
                            /// </summary>
                            if (dist <= Constants.Units.BEAM_AU_MAX)
                            {
                                /// <summary>
                                /// Value is in units of 10k km
                                /// </summary>
                                float rangeAreaDefenseKm;

                                /// <summary>
                                /// The user put in an absurdly large value. for convienence, just do this to get maximum range.
                                /// </summary>

                                if (ShipBeamFC.pDRange > (float)Constants.Units.TEN_KM_MAX)
                                {
                                    /// <summary>
                                    /// Max possible beam range in KM.
                                    /// </summary>
                                    rangeAreaDefenseKm = (float)Constants.Units.BEAM_KM_MAX;
                                }
                                else
                                {
#warning magic number related to 10k
                                    rangeAreaDefenseKm = ShipBeamFC.pDRange * 10000.0f;
                                }

                                float distKM = (float)Distance.ToKm(dist);

                                /// <summary>
                                /// Additional paranoia check of range, I need to fix Area defense PD range values to ship bfc range in any event, that hasn't been done yet.
                                /// </summary>
#warning magic number for total bfc range.
                                float totalRange = ShipBeamFC.beamFireControlDef.range * 2.0f;

                                float range = Math.Min(totalRange, rangeAreaDefenseKm);

                                /// <summary>
                                /// the BFC is set for range defense and is in range of this missile.
                                /// </summary>
                                if (distKM <= range)
                                {
#warning magic number related to 10k km increments.
                                    /// <summary>
                                    /// Increment is a 10k km unit, so distance must be divided by 10000 to yield the appropriate number.
                                    /// </summary>
                                    int increment = (int)Math.Floor((float)distKM / 10000.0f);

                                    bool Intercept = false;
                                    int MissilesToDestroy = 0;

                                    /// <summary>
                                    /// Can't foreach this one, as I do not want every missile, only the ones not destroyed.
                                    /// </summary>
                                    for (int MissileIterator = DetectedMissileGroup.missilesDestroyed; MissileIterator < DetectedMissileGroup.missiles.Count; MissileIterator++)
                                    {
                                        bool WF = false;
                                        Intercept = ShipBeamFC.InterceptTarget(RNG, increment, DetectedMissileGroup.missiles[MissileIterator], Ship.ShipsFaction,
                                                                                                                  Ship.ShipsTaskGroup.Contact, Ship, out WF);

                                        /// <summary>
                                        /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                        /// </summary>
                                        if (WF == true)
                                        {
                                            if (Fact.RechargeList.ContainsKey(Ship) == true)
                                            {
                                                /// <summary>
                                                /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                                /// </summary>
                                                if ((Fact.RechargeList[Ship] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                                {
                                                    Fact.RechargeList[Ship] = (Fact.RechargeList[Ship] + (int)Faction.RechargeStatus.Weapons);
                                                }
                                            }
                                            else
                                            {
                                                Fact.RechargeList.Add(Ship, (int)Faction.RechargeStatus.Weapons);
                                            }
                                        }

                                        if (Intercept == true)
                                        {
                                            /// <summary>
                                            /// Destroy the missile, check if the ordnance group should be removed, if its gone also remove it from the detected contacts list and break that loop.
                                            /// </summary>

                                            MissilesToDestroy++;
                                        }
                                        else if (Intercept == false)
                                        {
                                            /// <summary>
                                            /// This FC can't intercept any more missiles, advance to the next one.
                                            /// </summary>
                                            break;
                                        }
                                    }

                                    /// <summary>
                                    /// Set the missiles destroyed count as appropriate.
                                    /// </summary>
                                    DetectedMissileGroup.missilesDestroyed = DetectedMissileGroup.missilesDestroyed + MissilesToDestroy;

                                    if (DetectedMissileGroup.missilesDestroyed != 0 && Fact.MissileRemoveList.Contains(DetectedMissileGroup) == false)
                                    {
                                        /// <summary>
                                        /// Tell sim to remove missiles from this group, or remove it entirely.
                                        /// </summary>
                                        Fact.MissileRemoveList.Add(DetectedMissileGroup);
                                    }

                                    if (Intercept == false)
                                    {
                                        /// <summary>
                                        /// This condition means advance to the next FC.
                                        /// </summary>
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    /// <summary>
                    /// MFC set to any pd mode that can be applied: 1v2 up to 5v1.
                    /// </summary>
                    else if (pair.Value.PointDefenseType[pair2.Key] == true && pair2.Value.ShipMFC[pair2.Key.componentIndex].pDState >= PointDefenseState.AMM1v2 &&
                        pair2.Value.ShipMFC[pair2.Key.componentIndex].pDState <= PointDefenseState.AMM5v1)
                    {

                        MissileFireControlTN ShipMissileFC = pair2.Value.ShipMFC[pair2.Key.componentIndex];

                        int MissilesLaunched = 0;
                        int MissilesToLaunch = 0;
                        foreach (KeyValuePair<OrdnanceGroupTN, FactionContact> MisPair in Fact.DetectedContactLists[CurrentSystem].DetectedMissileContacts)
                        {
                            OrdnanceGroupTN DetectedMissileGroup = MisPair.Key;

                            /// <summary>
                            /// Advance to next missile group.
                            /// </summary>
                            if (DetectedMissileGroup.missilesDestroyed == DetectedMissileGroup.missiles.Count)
                            {
                                break;
                            }

                            /// <summary>
                            /// Do a distance check on pair.Value vs the missile itself. if that checks out to be less than 10k km(or equal to zero), then
                            /// check to see if the FC can shoot down said missile. This should never be run before a sensor sweep
                            /// </summary>
                            float dist;

                            DetectedMissileGroup.contact.DistTable.GetDistance(Ship.ShipsTaskGroup.Contact, out dist);


                            float MFCEngageDistKm = ShipMissileFC.mFCSensorDef.maxRange;
                            float rangeAreaDefenseKm = ShipMissileFC.pDRange;

                            /// <summary>
                            /// Range is in 10K units so it has to be adjusted to AU for later down.
                            /// </summary>
#warning magic 10k number here
                            float range = (float)Distance.ToAU((Math.Min(MFCEngageDistKm, rangeAreaDefenseKm)));
                            range = range * 10000.0f;

                            int MSize = 0;
                            int AltMSize = 0;

#warning the +6 is another magic number.
                            if ((int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) <= (Constants.OrdnanceTN.MissileResolutionMinimum + 6))
                            {
                                MSize = Constants.OrdnanceTN.MissileResolutionMinimum;
                                AltMSize = 0;
                            }
                            else if ((int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) <= (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                            {
                                MSize = (int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size);
                                AltMSize = 0;
                            }
                            else if ((int)Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) > (Constants.OrdnanceTN.MissileResolutionMaximum + 6))
                            {
                                MSize = -1;
                                AltMSize = (int)Math.Ceiling(Math.Ceiling(DetectedMissileGroup.missiles[0].missileDef.size) / (Constants.OrdnanceTN.MissileResolutionMaximum + 6));
                            }

                            int MFCRange = ShipMissileFC.mFCSensorDef.GetActiveDetectionRange(AltMSize, MSize);

                            bool CanDetect = Fact.LargeDetection(dist, MFCRange);

                            /// <summary>
                            /// Can this MFC fire on the targetted missile?
                            /// </summary>
                            if (CanDetect == true && dist <= range)
                            {
                                /// <summary>
                                /// Do AMM defense here. Check to see how many amms are targeted on this missile, if less than defense setting fire more.
                                /// How do I handle 1v2 mode? rounding obviously. if more than 1 missile in group send half, send atleast 1 for 1, and round for odd missile amounts.
                                /// missiles won't be destroyed here, as they were in beam fire mode, this will just launch amms at missile groups.
                                /// </summary>

                                /// <summary>
                                /// Get total missiles currently targetted on this group. Keeping track of a total missiles incoming variable would mean handling a lot of interactions where
                                /// missiles can be destroyed, run out of fuel, etc. so I'll just loop through this for now.
                                /// </summary>
#warning this missile count can be optimized, but it would be difficult to do so.
                                int TotalCount = 0;
                                foreach (OrdnanceGroupTN OrdGroupTargetting in DetectedMissileGroup.ordGroupsTargetting)
                                {
                                    TotalCount = TotalCount + OrdGroupTargetting.missiles.Count;
                                }

                                float Value = TotalCount / DetectedMissileGroup.missiles.Count;

                                switch (ShipMissileFC.pDState)
                                {
#warning more magic numbers in how point defense states are handled.
                                    case PointDefenseState.AMM1v2:
                                        if (Value < 0.5f)
                                        {
                                            int Max = (int)Math.Ceiling((float)DetectedMissileGroup.missiles.Count / 2.0f);
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                        break;
                                    case PointDefenseState.AMM1v1:
                                        if (Value < 1.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                        break;
                                    case PointDefenseState.AMM2v1:
                                        if (Value < 2.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 2;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                        break;
                                    case PointDefenseState.AMM3v1:
                                        if (Value < 3.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 3;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                        break;
                                    case PointDefenseState.AMM4v1:
                                        if (Value < 4.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 4;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                        break;
                                    case PointDefenseState.AMM5v1:
                                        if (Value < 5.0f)
                                        {
                                            int Max = DetectedMissileGroup.missiles.Count * 5;
                                            MissilesToLaunch = Max - TotalCount;
                                        }
                                        break;
                                }

                                if (MissilesToLaunch != 0)
                                {
                                    /// <summary>
                                    /// launch up to MissilesToLaunch amms in a new ord group at the target.
                                    /// <summary>
                                    MissilesLaunched = ShipMissileFC.FireWeaponsPD(Ship.ShipsTaskGroup, Ship, DetectedMissileGroup, MissilesToLaunch);


                                    /// <summary>
                                    /// Add this ship to the weapon recharge list since it has fired. This is done here in Sim, or for FDF_Self in Ship.cs
                                    /// </summary>
                                    if (MissilesLaunched != 0)
                                    {
                                        if (Fact.RechargeList.ContainsKey(Ship) == true)
                                        {
                                            /// <summary>
                                            /// If our recharge value does not have Recharge beams in it(bitflag 2 for now), then add it.
                                            /// </summary>
                                            if ((Fact.RechargeList[Ship] & (int)Faction.RechargeStatus.Weapons) != (int)Faction.RechargeStatus.Weapons)
                                            {
                                                Fact.RechargeList[Ship] = (Fact.RechargeList[Ship] + (int)Faction.RechargeStatus.Weapons);
                                            }
                                        }
                                        else
                                        {
                                            Fact.RechargeList.Add(Ship, (int)Faction.RechargeStatus.Weapons);
                                        }
                                    }

                                    /// <summary>
                                    /// This FC can no longer fire at ordnance groups in range.
                                    /// </summary>
                                    if (MissilesLaunched != MissilesToLaunch)
                                    {
                                        break;
                                    }
                                }
                            }

                            /// <summary>
                            /// advance to the next FC.
                            /// </summary>
                            if (MissilesLaunched != MissilesToLaunch && MissilesToLaunch != 0)
                            {
                                break;
                            }

                        }
                    }
                }
            }
        }
    }
}

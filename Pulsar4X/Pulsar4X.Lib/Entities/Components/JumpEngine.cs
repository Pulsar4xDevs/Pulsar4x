using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class JumpEngineDefTN : ComponentDefTN
    {
        public enum JDType
        {
            Military,
            Commercial
        }

        /// <summary>
        /// How space efficient is this jump drive?  4 points of efficiency means that 1 HS of jump drive can cover 4 HS of ship(1 for the JD and 3 for the rest of the ship).
        /// </summary>
        private float JumpEngineEfficiency;
        public float jumpEngineEfficiency
        {
            get { return JumpEngineEfficiency; }
        }

        /// <summary>
        /// What is the largest craft this engine can support?
        /// </summary>
        private int MaxJumpRating;
        public int maxJumpRating
        {
            get { return MaxJumpRating; }
        }

        /// <summary>
        /// How many ships may this jump drive permit to travel with it?
        /// </summary>
        private int SquadronSize;
        public int squadronSize
        {
            get { return SquadronSize; }
        }

        /// <summary>
        /// How many Km can this jump drive displace itself from a jump point during a squadron transit?
        /// </summary>
        private int JumpRadius;
        public int jumpRadius
        {
            get { return JumpRadius; }
        }

        /// <summary>
        /// Is this a military or commercial jump drive? both have differing characteristics for tonnage supported, squadron size, efficiency, and jump radius.
        /// </summary>
        private JDType JumpDriveType;
        public JDType jumpDriveType
        {
            get { return JumpDriveType; }
        }

        /// <summary>
        /// Constructor for the JumpEngine Definition
        /// </summary>
        /// <param name="title">Name of the engine.</param>
        /// <param name="EfficiencyTech">How many HS of hull can 1 HS of JE support?</param>
        /// <param name="SquadSizeTech">How many craft cane use this JE in a squadron transit</param>
        /// <param name="RadiusTech">How far can this JE support jumping away from the Jump Point</param>
        /// <param name="JType">Is this a military or commercial engine?</param>
        /// <param name="size">Size in HS of this jump engine. Max of 1000, will run into issues with htk above that in any event.</param>
        public JumpEngineDefTN(string title, int EfficiencyTech, int SquadSizeTech, int RadiusTech, JDType JType, int HS)
        {
            /// <summary>
            /// Everything needs a unique Id, or probably will if it doesn't already.
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.JumpEngine;

            Name = title;

            JumpDriveType = JType;

            /// <summary>
            /// Full efficiency: 1HS for Eff HS
            /// Full Squadron size
            /// Full Jump Radius
            /// </summary>
            if (JumpDriveType == JDType.Military)
            {
                /// <summary>
                /// Validation for all these is handled in the UI. There is only one place where jump engines should be created by the user. same for all components.
                /// </summary>
                JumpEngineEfficiency = Constants.JumpEngineTN.JumpEfficiency[EfficiencyTech];
                SquadronSize = Constants.JumpEngineTN.SquadSize[SquadSizeTech];
                JumpRadius = Constants.JumpEngineTN.JumpRadius[RadiusTech] * 10000;

                size = HS * Constants.JumpEngineTN.SquadSizeModifier[SquadSizeTech] * Constants.JumpEngineTN.JumpRadiusModifier[RadiusTech];

                MaxJumpRating = (int)(size * JumpEngineEfficiency) * (int)Constants.ShipTN.TonsPerHS;

                isMilitary = true;
            }
            /// <summary>
            /// 3/4ths Efficiency: (1HS * 10) for 3/4ths Eff HS 100 = 380 110 = 410
            /// Squadron size - 1
            /// Half Jump Radius(5000 instead of 10000)
            /// </summary>
            else if (JumpDriveType == JDType.Commercial)
            {
                JumpEngineEfficiency = Constants.JumpEngineTN.JumpEfficiency[EfficiencyTech] * 0.75f;
                SquadronSize = Constants.JumpEngineTN.SquadSize[SquadSizeTech] - 1;
                JumpRadius = Constants.JumpEngineTN.JumpRadius[RadiusTech] * 5000;

                size = HS * 10 * Constants.JumpEngineTN.SquadSizeModifier[SquadSizeTech] * Constants.JumpEngineTN.JumpRadiusModifier[RadiusTech];

                MaxJumpRating = (int)Math.Round(JumpEngineEfficiency * HS) * 10 * (int)Constants.ShipTN.TonsPerHS;

                isMilitary = false;
            }

            /// <summary>
            /// commercial jump drives shouldn't have a ridiculous HTK, especially because htk is a byte in length itself.
            /// </summary>
            htk = (byte)Math.Round( ((float)HS / 5.0f) );

            crew = (HS * 2);

            cost = (decimal)HS * 2.5m * (decimal)Constants.JumpEngineTN.SquadSizeModifier[SquadSizeTech] * (decimal)Constants.JumpEngineTN.JumpRadiusModifier[RadiusTech];
            if (cost < 10)
            {
                cost = 10;
            }

            //20% Duranium 80% Sorium
            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.2m;
            minerialsCost[(int)Constants.Minerals.MinerialNames.Sorium] = cost * 0.8m;

            isObsolete = false;
            isSalvaged = false;
            isElectronic = false;
            isDivisible = false;
        }
    }

    public class JumpEngineTN : ComponentTN
    {
        /// <summary>
        /// Definition for this component.
        /// </summary>
        private JumpEngineDefTN JumpEngineDef;
        public JumpEngineDefTN jumpEngineDef
        {
            get { return JumpEngineDef; }
        }

        /// <summary>
        /// How many seconds until this jump engine can jump again.
        /// </summary>
        private int JumpTimer;
        public int jumpTimer
        {
            get { return JumpTimer; }
        }

        /// <summary>
        /// Constructor for a jump engine component.
        /// </summary>
        /// <param name="definition">definition for this component to use.</param>
        public JumpEngineTN(JumpEngineDefTN definition)
        {
            JumpEngineDef = definition;

            JumpTimer = 0;

            isDestroyed = false;
        }

        /// <summary>
        /// Can This jump engine jump yet?
        /// </summary>
        /// <returns>true if yes, false if JumpTimer is > 0 or the jump engine is destroyed.</returns>
        public bool CanJump()
        {
            if (JumpTimer == 0 && isDestroyed == false)
                return true;
            else
                return false;
        }

        /// <summary>
        /// This engine has supported a transit so set its jump timer. Also the ship it is on should be put in the recharge list for Jump Recharge. And the taskgroup as a whole for transit sickness.
        /// </summary>
        public void Transit()
        {
            JumpTimer = Constants.JumpEngineTN.JumpRechargeTime;
        }
    }
}

using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;

namespace Pulsar4X.Movement
{
    public class NewtonSimpleMoveDB : BaseDataBlob
    {
        [JsonProperty]
        internal DateTime LastProcessDateTime = new DateTime();
        [JsonProperty]
        public DateTime ActionOnDateTime { get; internal set; }
        [JsonProperty]
        public KeplerElements CurrentTrajectory { get; internal set; }
        [JsonProperty]
        public KeplerElements TargetTrajectory { get; internal set; }
        [JsonProperty]

        public bool IsComplete = false;
        [JsonProperty]
        public Entity SOIParent { get; internal set; }
        [JsonProperty]
        public double ParentMass { get; internal set; }

        [JsonConstructor]
        private NewtonSimpleMoveDB() { }

        public NewtonSimpleMoveDB(Entity soiParent, KeplerElements start, KeplerElements end, DateTime onDateTime)
        {
            LastProcessDateTime = onDateTime;
            ActionOnDateTime = onDateTime;
            CurrentTrajectory = start;
            TargetTrajectory = end;
            SOIParent = soiParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().MassTotal;
            ThrowIfTrajectoryUnusable(soiParent, start, onDateTime, "CurrentTrajectory");
            ThrowIfTrajectoryUnusable(soiParent, end, onDateTime, "TargetTrajectory");
        }

        /// <summary>
        /// Trajectories are parent-relative and must use that parent's µ.
        /// A Phobos-frame Kepler hung on Mars (MoveTo circularise after a parent-switch
        /// drop-in) is the usual offender: at epoch |r| still looks like 13 km, then
        /// GetStateVectors a few seconds later is 1e8 AU.
        /// </summary>
        internal static void ThrowIfTrajectoryUnusable(
            Entity soiParent, KeplerElements ke, DateTime at, string which)
        {
            if (soiParent is null)
                throw new ArgumentNullException(nameof(soiParent));

            double parentSgp = GeneralMath.StandardGravitationalParameter(
                soiParent.GetDataBlob<MassVolumeDB>().MassTotal);
            if (ke.StandardGravParameter > 0 && parentSgp > 0)
            {
                double ratio = ke.StandardGravParameter / parentSgp;
                if (ratio < 0.1 || ratio > 10.0)
                {
                    throw new ArgumentException(
                        $"{which} µ={ke.StandardGravParameter:G6} does not match SOI parent " +
                        $"µ={parentSgp:G6} (ratio {ratio:G4}). Trajectory and parent are different gravity wells.");
                }
            }

            var r = OrbitalMath.GetStateVectors(ke, at).position;
            if (!double.IsFinite(r.X) || !double.IsFinite(r.Y) || !double.IsFinite(r.Z))
            {
                throw new ArgumentException($"{which} position at {at:o} is not finite: {r}");
            }

            const double maxParentRelative_m = 1e14; // ~670 AU — past any solar-system orbit
            if (r.Length() > maxParentRelative_m)
            {
                throw new ArgumentException(
                    $"{which} |r|={r.Length():G6} m from parent is not a solar-system trajectory.");
            }
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
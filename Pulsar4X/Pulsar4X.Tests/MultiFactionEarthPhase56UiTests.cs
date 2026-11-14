using System;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;

namespace Pulsar4X.Tests
{
    /// <summary>
    /// Client formatters and action reasons for the Workforce tab (multi-faction phases 5–6).
    /// No ImGui and no game.
    /// </summary>
    public class MultiFactionEarthPhase56UiTests
    {
        static WorkforceView View(long required = 6210, long employed = 6210, long commuters = 0, long target = 6210,
            long labourPool = 50_000, long contracted = 0, bool canContract = true)
            => new WorkforceView
            {
                Required = required,
                Employed = employed,
                Commuters = commuters,
                Civilians = 1790,
                Target = target,
                Contracts = new[]
                {
                    new WorkforceContractRow
                    {
                        FactionId = 2,
                        FactionName = "Americas Compact",
                        ColonyId = 20,
                        Contracted = contracted,
                        LabourPool = labourPool,
                        CanContract = canContract,
                    },
                },
            };

        static WorkforceContractRow Row(WorkforceView view) => view.Contracts[0];

        [Test]
        public void StaffedPercent_AndOpenPosts()
        {
            Assert.That(ColonyWorkforceDisplay.StaffedPercent(1), Is.EqualTo("100%"));
            Assert.That(ColonyWorkforceDisplay.StaffedPercent(0.5), Is.EqualTo("50%"));
            Assert.That(ColonyWorkforceDisplay.StaffedPercent(0), Is.EqualTo("0%"));
            var view = View(employed: 5210, commuters: 0, target: 6210);
            Assert.That(ColonyWorkforceDisplay.OpenPosts(view), Is.EqualTo(1000));
            Assert.That(ColonyWorkforceDisplay.OpenPosts(View()), Is.EqualTo(0));
        }

        [Test]
        public void Contract_End_AndRelocate_Reasons()
        {
            var open = View(employed: 5210, commuters: 0, target: 6210, labourPool: 50_000, contracted: 0);
            Assert.That(ColonyWorkforceDisplay.ContractReason(open, Row(open), 1000), Is.Null);
            Assert.That(ColonyWorkforceDisplay.ContractReason(open, Row(open), 0), Is.EqualTo("Enter a count of at least 1."));
            Assert.That(ColonyWorkforceDisplay.ContractReason(open, Row(open) with { CanContract = false }, 10),
                Is.EqualTo("Relations must be Friendly both ways."));
            Assert.That(ColonyWorkforceDisplay.ContractReason(open, Row(open) with { LabourPool = 0 }, 10),
                Is.EqualTo("No labour available in this bloc's pool."));
            Assert.That(ColonyWorkforceDisplay.ContractReason(View(), Row(View()), 10),
                Is.EqualTo("No open posts (raise Target to hire)."));

            var hired = View(employed: 5210, commuters: 1000, target: 6210, contracted: 1000);
            Assert.That(ColonyWorkforceDisplay.EndReason(Row(hired), 100), Is.Null);
            Assert.That(ColonyWorkforceDisplay.EndReason(Row(open), 10), Is.EqualTo("No commuters under contract."));
            Assert.That(ColonyWorkforceDisplay.RelocateReason(Row(hired), 10, 10_000m), Is.Null);
            Assert.That(ColonyWorkforceDisplay.RelocateReason(Row(open), 10, 10_000m), Is.EqualTo("No commuters to relocate."));
            Assert.That(ColonyWorkforceDisplay.RelocateReason(Row(hired), 10, 500m),
                Is.EqualTo("Need 1000 cr (100 cr each)."));
            Assert.That(ColonyWorkforceDisplay.RelocateButtonLabel(), Is.EqualTo("Relocate (100 cr each)"));
        }
    }
}

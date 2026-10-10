using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;

namespace Pulsar4X.Logistics;

/// <summary>One secret on a logistics office. Cargo stays in <see cref="MarketListing"/>.</summary>
public sealed class IntelListing
{
    [JsonProperty]
    public IntelKind Kind { get; set; }

    [JsonProperty]
    public string Subject { get; set; } = "";

    [JsonProperty]
    public decimal Ask { get; set; }

    [JsonProperty]
    public bool ForSale { get; set; }

    /// <summary>
    /// Faction that is paid and must still hold the secret.
    /// -1 means the office owner, including rows saved before this field existed.
    /// </summary>
    [JsonProperty]
    public int SellerFactionId { get; set; } = -1;

    public IntelListing Copy() => new()
    {
        Kind = Kind,
        Subject = Subject,
        Ask = Ask,
        ForSale = ForSale,
        SellerFactionId = SellerFactionId,
    };
}

/// <summary>
/// Read and write a logistics office's intel list, and sell a copy of a finished survey.
/// One switch per kind: the seller has it, the buyer has it, grant it, and the public label.
/// </summary>
public static class IntelBook
{
    public static string Key(IntelKind kind, string subject)
        => ((int)kind).ToString(CultureInfo.InvariantCulture) + "\u001f" + (subject ?? "");

    public static string SubjectOf(int entityId)
        => entityId.ToString(CultureInfo.InvariantCulture);

    public static string ResultTag(IntelKind kind) => kind switch
    {
        IntelKind.Geo => "partial minerals",
        IntelKind.Pin => "pin cleared",
        IntelKind.Jump => "jump",
        _ => "",
    };

    public static bool TryGet(Entity office, IntelKind kind, string subject, out IntelListing listing)
    {
        listing = null!;
        if (office == null || string.IsNullOrEmpty(subject))
            return false;
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book) || book.Intel == null)
            return false;
        return book.Intel.TryGetValue(Key(kind, subject), out listing!);
    }

    /// <summary>Replace the row. Rejects an empty subject, a negative ask, and a secret the owner has not finished.</summary>
    public static bool TrySet(Entity office, IntelKind kind, string subject, decimal ask, bool forSale, out string reason)
    {
        reason = "";
        if (string.IsNullOrWhiteSpace(subject))
        {
            reason = "Subject is required.";
            return false;
        }
        if (ask < 0)
        {
            reason = "Prices cannot be negative.";
            return false;
        }
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book))
        {
            reason = "The colony has no logistics office.";
            return false;
        }
        var game = office.Manager?.Game;
        if (game == null || !SellerHas(game, office.FactionOwnerID, kind, subject))
        {
            reason = "The seller has not finished that survey.";
            return false;
        }

        book.Intel ??= new Dictionary<string, IntelListing>(StringComparer.Ordinal);
        book.Intel[Key(kind, subject)] = new IntelListing
        {
            Kind = kind,
            Subject = subject,
            Ask = ask,
            ForSale = forSale,
            SellerFactionId = -1,
        };
        return true;
    }

    /// <summary>
    /// List a secret for the faction that finished it, on an office that faction does not own.
    /// The office owner does not gain the survey. <see cref="TrySet"/> stays owner-only.
    /// </summary>
    public static bool TryConsign(Entity office, Entity seller, IntelKind kind, string subject, decimal ask, out string reason)
    {
        reason = "";
        if (seller == null || office == null)
        {
            reason = "That intel is not listed.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(subject))
        {
            reason = "Subject is required.";
            return false;
        }
        if (ask < 0)
        {
            reason = "Prices cannot be negative.";
            return false;
        }
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book))
        {
            reason = "The colony has no logistics office.";
            return false;
        }
        var game = office.Manager?.Game;
        if (game == null || !SellerHas(game, seller.Id, kind, subject))
        {
            reason = "The seller has not finished that survey.";
            return false;
        }

        book.Intel ??= new Dictionary<string, IntelListing>(StringComparer.Ordinal);
        book.Intel[Key(kind, subject)] = new IntelListing
        {
            Kind = kind,
            Subject = subject,
            Ask = ask,
            ForSale = true,
            SellerFactionId = seller.Id == office.FactionOwnerID ? -1 : seller.Id,
        };
        return true;
    }

    /// <summary>Who is paid for this row. -1 on the row means the office owner.</summary>
    public static int SellerOf(Entity office, IntelListing row)
        => row.SellerFactionId >= 0 ? row.SellerFactionId : office.FactionOwnerID;

    /// <summary>Drop one row. A missing row is a no-op.</summary>
    public static void Remove(Entity office, IntelKind kind, string subject)
    {
        if (office == null || string.IsNullOrEmpty(subject))
            return;
        if (office.TryGetDataBlob<LogiBaseDB>(out var book) && book.Intel != null)
            book.Intel.Remove(Key(kind, subject));
    }

    public static bool TryBuy(Entity office, Entity buyer, IntelKind kind, string subject, out string reason)
    {
        reason = "";
        if (buyer == null || office == null)
        {
            reason = "That intel is not listed.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(subject))
        {
            reason = "Subject is required.";
            return false;
        }
        if (!office.TryGetDataBlob<LogiBaseDB>(out var book) || book.Intel == null
            || !book.Intel.TryGetValue(Key(kind, subject), out var row))
        {
            reason = "That intel is not listed.";
            return false;
        }
        if (!row.ForSale)
        {
            reason = "That intel is not for sale.";
            return false;
        }
        if (row.Ask < 0)
        {
            reason = "Prices cannot be negative.";
            return false;
        }

        var game = office.Manager?.Game;
        if (game == null)
        {
            reason = "That intel is not listed.";
            return false;
        }
        int sellerId = SellerOf(office, row);
        if (buyer.Id == sellerId || buyer.Id == office.FactionOwnerID)
        {
            reason = "A faction cannot buy its own intel.";
            return false;
        }
        if (!FactionStanceRules.CanTrade(game, buyer.Id, office.FactionOwnerID))
        {
            reason = "These factions cannot trade.";
            return false;
        }
        if (!SellerHas(game, sellerId, kind, subject))
        {
            reason = "The seller no longer has this intel.";
            return false;
        }
        if (BuyerHas(game, buyer.Id, kind, subject))
        {
            reason = "You already have this intel.";
            return false;
        }

        if (!buyer.TryGetDataBlob<FactionInfoDB>(out var buyerInfo)
            || !game.Factions.TryGetValue(sellerId, out var sellerEntity)
            || !sellerEntity.TryGetDataBlob<FactionInfoDB>(out var sellerInfo))
        {
            reason = "Faction has no ledger.";
            return false;
        }
        if (row.Ask > buyerInfo.Money.GetCurrentFunds())
        {
            reason = "Cannot afford that price.";
            return false;
        }

        if (!Grant(game, buyer.Id, kind, subject, office.StarSysDateTime))
        {
            reason = "The seller no longer has this intel.";
            return false;
        }

        if (row.Ask > 0)
        {
            string forBuyer = $"{kind} {subject} {sellerId}";
            string forSeller = $"{kind} {subject} {buyer.Id}";
            buyerInfo.Money.AddExpense(office.StarSysDateTime, TransactionCategory.Trade, forBuyer, row.Ask);
            sellerInfo.Money.AddIncome(office.StarSysDateTime, TransactionCategory.Trade, forSeller, row.Ask);
        }
        return true;
    }

    public static bool SellerHas(Game game, int factionId, IntelKind kind, string subject)
        => Has(game, factionId, kind, subject);

    public static bool BuyerHas(Game game, int factionId, IntelKind kind, string subject)
        => Has(game, factionId, kind, subject);

    public readonly struct IntelLabel
    {
        public string Name { get; init; }
        public string? SystemId { get; init; }
    }

    public readonly struct IntelCandidate
    {
        public IntelKind Kind { get; init; }
        public string Subject { get; init; }
        public string Name { get; init; }
        public string? SystemId { get; init; }
    }

    /// <summary>
    /// Public name for a row. <paramref name="unknownIndex"/> is 1-based among unknown jumps in that
    /// system. Zero means the viewer already has the jump, or the row is not a jump.
    /// </summary>
    public static IntelLabel Describe(Game game, int viewerId, IntelKind kind, string subject, int unknownIndex)
    {
        if (!TrySubject(game, subject, out var entity))
            return new IntelLabel { Name = subject ?? "", SystemId = null };

        string? systemId = SystemIdOf(entity);
        if (kind == IntelKind.Jump && unknownIndex > 0)
        {
            string systemName = SystemName(entity);
            string name = unknownIndex <= 1 ? $"Jump · {systemName}" : $"Jump · {systemName} {unknownIndex}";
            return new IntelLabel { Name = name, SystemId = systemId };
        }

        string known = entity.GetName(viewerId);
        if (kind == IntelKind.Jump
            && entity.TryGetDataBlob<JumpPointDB>(out var jump)
            && jump.DestinationId > 0
            && game.GlobalManager.TryGetGlobalEntityById(jump.DestinationId, out var destination)
            && destination.Manager is StarSystem far)
        {
            known = known + " to " + far.NameDB.DefaultName;
        }
        return new IntelLabel { Name = known, SystemId = systemId };
    }

    public static List<IntelCandidate> Candidates(Entity faction)
    {
        var found = new List<IntelCandidate>();
        var game = faction?.Manager?.Game;
        if (game == null || faction == null)
            return found;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var system in game.Systems)
        {
            if (system == null)
                continue;
            foreach (var geo in system.GetAllDataBlobsOfType<GeoSurveyableDB>())
            {
                if (!geo.IsSurveyComplete(faction.Id) || geo.OwningEntity == null)
                    continue;
                AddCandidate(seen, found, game, faction.Id, IntelKind.Geo, geo.OwningEntity);
            }
            foreach (var pin in system.GetAllDataBlobsOfType<JPSurveyableDB>())
            {
                if (!pin.IsSurveyComplete(faction.Id) || pin.OwningEntity == null)
                    continue;
                AddCandidate(seen, found, game, faction.Id, IntelKind.Pin, pin.OwningEntity);
            }
            foreach (var jump in system.GetAllDataBlobsOfType<JumpPointDB>())
            {
                if (!jump.IsDiscovered.Contains(faction.Id) || jump.OwningEntity == null)
                    continue;
                AddCandidate(seen, found, game, faction.Id, IntelKind.Jump, jump.OwningEntity);
            }
        }

        found.Sort(static (a, b) =>
        {
            int byName = string.CompareOrdinal(a.Name, b.Name);
            if (byName != 0)
                return byName;
            return string.CompareOrdinal(a.Subject, b.Subject);
        });
        return found;
    }

    static void AddCandidate(HashSet<string> seen, List<IntelCandidate> found, Game game, int factionId, IntelKind kind, Entity entity)
    {
        string subject = SubjectOf(entity.Id);
        if (!seen.Add(Key(kind, subject)))
            return;
        var label = Describe(game, factionId, kind, subject, 0);
        found.Add(new IntelCandidate
        {
            Kind = kind,
            Subject = subject,
            Name = label.Name,
            SystemId = label.SystemId,
        });
    }

    static bool Has(Game game, int factionId, IntelKind kind, string subject)
    {
        if (!TrySubject(game, subject, out var entity))
            return false;
        switch (kind)
        {
            case IntelKind.Geo:
                return entity.TryGetDataBlob<GeoSurveyableDB>(out var geo) && geo.IsSurveyComplete(factionId);
            case IntelKind.Pin:
                return entity.TryGetDataBlob<JPSurveyableDB>(out var pin) && pin.IsSurveyComplete(factionId);
            case IntelKind.Jump:
                return entity.TryGetDataBlob<JumpPointDB>(out var jump) && jump.IsDiscovered.Contains(factionId);
            default:
                return false;
        }
    }

    static bool Grant(Game game, int buyerId, IntelKind kind, string subject, DateTime at)
    {
        if (!TrySubject(game, subject, out var entity))
            return false;
        switch (kind)
        {
            case IntelKind.Geo:
                return GrantGeo(game, buyerId, entity, at);
            case IntelKind.Pin:
                return GrantPin(buyerId, entity);
            case IntelKind.Jump:
                JumpPointReveal.Grant(game, buyerId, entity, at);
                return true;
            default:
                return false;
        }
    }

    static bool GrantGeo(Game game, int buyerId, Entity body, DateTime at)
    {
        if (!body.TryGetDataBlob<GeoSurveyableDB>(out var geo))
            return false;
        geo.GeoSurveyStatus[buyerId] = 0;
        if (body.TryGetDataBlob<MineralsDB>(out var minerals)
            && game.Factions.TryGetValue(buyerId, out var buyer)
            && buyer.TryGetDataBlob<FactionInfoDB>(out var info))
        {
            minerals.GrantFactionPartialAccess(info.FactionMask);
        }

        EventManager.Instance.Publish(
            Event.Create(
                EventType.GeoSurveyCompleted,
                at,
                $"Geo Survey of {body.GetName(buyerId)} complete",
                buyerId,
                body.Manager?.ManagerID,
                body.Id));
        return true;
    }

    static bool GrantPin(int buyerId, Entity pin)
    {
        if (!pin.TryGetDataBlob<JPSurveyableDB>(out var survey))
            return false;
        survey.SurveyPointsRemaining[buyerId] = 0;
        pin.Manager?.HideNeutralEntityFromFaction(buyerId, pin.Id);
        return true;
    }

    static bool TrySubject(Game game, string subject, out Entity entity)
    {
        entity = null!;
        if (string.IsNullOrEmpty(subject))
            return false;
        if (!int.TryParse(subject, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            return false;
        return game.GlobalManager.TryGetGlobalEntityById(id, out entity!);
    }

    static string? SystemIdOf(Entity entity)
        => entity.Manager is StarSystem system ? system.ID : entity.Manager?.ManagerID;

    static string SystemName(Entity entity)
    {
        if (entity.Manager is StarSystem system && system.NameDB != null && !string.IsNullOrEmpty(system.NameDB.DefaultName))
            return system.NameDB.DefaultName;
        return "system";
    }
}

# Colony administrator

Status: notes from 2026-10-03. Not an implementation plan. Do not build personality, bids, or construction from this file. Refine-one-batch does not wait on it. A later plan names tests and the bid number before any of that is coded.

Goal rules stay in `agents-and-goals-design.md`. The refining slice is `GameEngine/Industry/plans/refine-one-batch.md`.

## What this is for

Friendly factions grow, and the system feels like other people are choosing. The player starts by editing listings and queueing work. Assigning a captain, a fleet commander, or a colony administrator is how that same work moves up a level.

## One intent on the colony

The colony has one active goal. The industry pass and the market pass are tools that goal calls. Offer stock is that goal today: it lists surplus, then the market posts the book. A supply order still replaces it.

A second goal for industry or for the market would replace the standing one. `AssignGoal(RunIndustry)` stays out.

A faction with one colony does not need a governor entity above it. When a faction has several colonies, command span is the width, the same way a supply order already covers owned offices.

## Who writes the book

An empty chair on a player colony leaves the book to the player. Offer stock lists the largest warehouse piles at ask 0 and bid 0. A refining line that already exists may still run one batch. The empty chair does not invent a buy.

A seated administrator writes the wants. A bid is a policy row with a reserve above the stock on hand and a bid above zero, inside the office's listing slots. The market pass posts it. An owned freighter needs the buy quantity. A trader needs the bid above the seller's ask. A bid of zero does not start friendly trade.

The owner can still edit an owned book while someone sits the chair. Whether a player edit survives the next wake, once the administrator is also writing policy, is open.

A friendly faction uses the same intent, because a person is in the chair. Luna Concord feels like a mining specialist when its starting civilian exists, sits, and rolled a high specialization. An empty chair is the midpoint.

There is no catalog price. The number on the first bid is not chosen.

The player's ladder uses goals that already exist once the books contain real buys:

- No administrator: the player edits rows, queues jobs, and queues buildings. Earth's chair starts here.
- A colony administrator: that person's patience and specialization write the bids, and later the next building.
- A captain: the ship may pick trade or freight.
- A fleet commander: the fleet parcels that work, clipped by the flagship's bridge.
- Several of the player's colonies: a supply order remains the order that covers them.

## The person

`AgentDB` is the personality. No commander is given that blob today. `CommanderFactory.CreateAdmin` fills `CommanderDB` only. A missing personality reads as 1 on every trait.

Specialization is how narrow the book is. High spends the office on one production chain the colony already has. Low spreads the same slots across more chains. At 1 the book stays in the middle. The number moves with the person. The chain is whatever installations and deposits are in front of them, read again each wake. An order to focus on one good is an order. Loyalty is how strictly they keep it.

Patience replaces Greed. High waits: hold stock, bid for an input, run a batch, and later raise a building. Low takes what the book is paying this week: sell the pile, take the spread. Caution stays risk. Skill stays quality and is not a gate. A poor specialist is still narrow.

Greed today multiplies MakeProfit, Trade, and Freighter only. It does not touch a bid, a batch, or a stockpile. Replacing it retunes that weight. The new curve is not chosen.

## The office is a short list

Office capacity is how many cargo ids may be listed. Earth and Luna start at 5. Offer stock fills Earth's five from the largest piles, so a new bid needs a slot that surplus currently holds. Luna's four warehouse goods leave one slot. The standing intent ranks which goods get the slots.

## Expansion

A new installation waits until a seated administrator can place a bid, that bid has brought the goods in, and construction spends those goods. `LocalConstructionProcessor` adds the component and does not take cargo. A factory goal, a shipyard goal, and any other installation choice stay their own plans. Each one has to pick a design.

Mines already fill the warehouse on their processor. Population already grows against life support. Neither places an order.

## When a plan is worth writing

1. Personality on the person. Add specialization. Replace Greed with Patience. Attach `AgentDB` to commanders.
2. Seat a starting civilian on Luna Concord so that personality is in the game on day one. Earth's chair stays empty until the player assigns someone. The commanders window has no assign button today.
3. The bid pass. A seated administrator writes buy rows, then the market posts them. Choose the bid number first.
4. Construction that spends cargo, then one installation plan.

## Open

- The bid number.
- Whether a player edit sticks while the seated administrator rewrites policy each wake.
- What Patience and Specialization change at the high end, in slots and in reserve depth.
- How the trade and freight weights move when Greed becomes Patience.

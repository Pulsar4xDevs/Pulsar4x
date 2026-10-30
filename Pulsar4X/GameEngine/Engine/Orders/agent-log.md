# Ship's log

Status: implemented 2026-10-26. Written 2026-10-26 from near-term item 14. Tests: `Pulsar4X.Tests/ShipLogTests.cs`.

A ship's log for one owned ship, fleet, or colony. Fleet Management and Colony Management each gain a Log tab for the thing selected there. The book is kept for the life of the game and is saved. A later pass can archive old lines. This pass does not.

The debug panel stays a live inspector. It is not this log.

## What the player reads

The tab is named Log. The book of the selected ship, or of the fleet when no ship is selected. Colony Management shows the selected colony's book.

Oldest line at the top. The tab opens scrolled to the latest line. Each line is the game date, then the text. Nothing on the tab edits the book.

A line this entity decided is flush left. An order it handed down is indented once and starts with `To <name> —`. A report, or an order it was given, is indented once and starts with `From <name> —`. A ship or a colony also logs each action it queues, indented once, with no `To` or `From`. A fleet does not list those actions. They stay in the ship's book.

```
2145-03-12 08:00  Survey the inner planets.
2145-03-12 08:20    To Prospect — Survey Mars.
2145-03-14 16:10    From Prospect — Mars surveyed.
```

The other book carries the mirror, written at the same moment:

```
2145-03-12 08:20    From 1st Survey Fleet — Survey Mars.
2145-03-12 08:21  Moving to Mars.
                    Warp to Mars
                    Circularise at Mars
2145-03-14 16:00  Mars surveyed.
```

A colony is the same shape. The goal stays flush left, and each install, mine, or refine it queues is indented under it.

The parent does not copy the child's book. It gets one line when that handed goal finishes or fails.

Only the owner sees the book. Another faction's snapshot has no log view. The same rule as orders.

## What gets a line

A line is a change. A wake that finds the same work still running adds nothing. The same text as the previous line is not written again.

- The player gives a goal. `Ordered: Survey the inner planets.`
- The agent picks its own task. `Refuel at Earth.` The line names the task and the target. It does not print weights or personality numbers. Those numbers are not a sentence yet.
- The plan commits a new route. `Moving to Ceres.` `Mining Ceres.`
- A ship or a colony queues an action, whether a captain planned it or the player plotted it on an empty chair. The line is the action name and its detail, indented like an order. `Warp to Mars.` `Install mine.` The line is written when the action is queued. Finishing that action does not add a second line. A failure does. The line is the action name, an em dash, and the reason the order already carries. `Warp Move to Mars — not enough ΔV.` An empty reason still notes that it failed. The goal's own finish line covers a success.
- A goal is handed down. The parent writes `To <ship> — <goal> <target>.` The child writes `From <parent> — <goal> <target>.`
- That handed goal finishes or fails. The child writes its own result. The parent writes `From <ship> — <result>.`
- This entity's goal finishes, fails, or the player cancels it. `Mars surveyed.` `Could not reach the asteroid.` `Cancelled.` A failure keeps that lead sentence and adds the reason when a player can read it. `Could not reach Mars. Not enough ΔV.` `Could not finish. Nothing to load.` An internal planner token (`no path`, `fleet only`, `a move action failed`) stays off the line. The lead sentence still says whether it was reach or finish. A cancel stays `Cancelled.`
- A commander takes or leaves a chair. `Captain Ada took command.` `Captain Ada left command.` A colony post is `Ada took the post.` A scientist is `Ada took the lab.` on the colony that holds the lab. Seating the same person again adds nothing. The starting roster is not copied into the book.

`Goal.Message` stays the one live status line the card and Fleet Management already show. The log does not copy an internal planner token. On a failure it appends a player-facing reason the order already stated, as a second sentence. Planners often return an empty message, and an empty message leaves the old line in place. Some of those strings are internal (`no path`, `fleet only`). The rest of the sentence is written at the commit, from the goal, the target, and the action name.

## Where it is stored

A `ShipLogDB` on the ship, fleet, or colony. One entry is the time of the decision (`atDateTime`), a kind (`Own`, `To`, `From`, `Action`), the other entity's id when there is one, and the text. `Action` is indented like `To` and `From`. The list is appended only. It is not capped.

The blob is saved with the entity, the same way `GoalsDB` is. `Clone` copies the list. A new game starts with an empty book.

`AssignGoal` is the player-order line, when it is asked to run now. The loop that hands `plan.SubGoals` writes the To and From pair, and sets the issuer entity id on the child goal so the later report can name the parent. Completion, failure, and cancel write their lines in `AgentProcessor` and `GoalCancel`. `EntityAction.Fail` writes the action line at the moment the order fails, including an empty-chair plot that never becomes a goal. `SubmitActions`, and an empty-chair plot, write one `Action` line per queued action on a ship or a colony. A fleet does not write `Action` lines.

The owner snapshot gains a `ShipLogView` of those entries, names already resolved. The client tab only reads that view.

## Tests

A captain given a move writes the goal line and one indented action line for each queued action, and those lines are still there after a save and a load. A colony that queues an install writes the goal line and one indented action line. A fleet book has no action lines. A second wake with the work unchanged does not add a line. A fleet that hands one survey writes `To` on the fleet and `From` on the ship. When the ship finishes, the fleet gains one `From` line and does not gain the ship's earlier lines. A faction that does not own the ship has no `ShipLogView`. A failed order writes the reason on the ship, and the fleet's report carries that same sentence. An internal token such as `a move action failed` does not.

## Out of this plan

Archiving or deleting old lines. Printing why a weight won. Rewriting every planner message. The debug panel. A log on the entity card.

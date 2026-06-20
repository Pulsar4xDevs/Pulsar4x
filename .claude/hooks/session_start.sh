#!/bin/bash
# Emits a Pulsar4x session context block at startup.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PLAN_FILE="$PROJECT_ROOT/PLAN.md"

echo "════════════════════════════════════════════════════"
echo "PULSAR4X SESSION START"
echo "════════════════════════════════════════════════════"
echo ""
echo "  Objective: bring planetary/ground combat and infrastructure"
echo "  to the same depth as existing space combat systems."
echo ""

# Show phase summary from PLAN.md
if [ -f "$PLAN_FILE" ]; then
    echo "── PLAN PHASES ─────────────────────────────────────"
    grep "^## Phase\|^### Phase\|^\*\*Status" "$PLAN_FILE" | head -20
    echo ""
fi

# Show recent commits
echo "── RECENT COMMITS ──────────────────────────────────"
cd "$PROJECT_ROOT" && git log --oneline -5 2>/dev/null || echo "  (no git log available)"
echo ""

echo "── PRE-FLIGHT RULES ────────────────────────────────"
echo "  1. Read the subsystem CLAUDE.md before touching any subsystem"
echo "  2. Run /build-check before your first edit"
echo "  3. Run /damage-audit before any Phase 1 combat work"
echo "  4. dotnet test must pass before pushing"
echo "  5. Update CLAUDE.md in same commit as code changes"
echo "════════════════════════════════════════════════════"

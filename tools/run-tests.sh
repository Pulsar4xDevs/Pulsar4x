#!/usr/bin/env bash
# Build once, then run the Pulsar4X test suite without the Slow tests (optionally sharded).
#
#   tools/run-tests.sh                  build, then run everything not marked [Category("Slow")]
#   tools/run-tests.sh --all            ... include the Slow tests too (milestone run)
#   tools/run-tests.sh --slow-only      ... run only the Slow tests
#   tools/run-tests.sh --shards 3       split the tests over 3 separate dotnet processes (2-8)
#   tools/run-tests.sh --no-build       skip the build (already built with the same settings)
#   tools/run-tests.sh --filter EXPR    AND an extra vstest filter onto the category filter
#   tools/run-tests.sh -h               this help
#
# Environment (all optional; nothing is hardcoded to a particular machine):
#   DOTNET          dotnet executable (default: dotnet)
#   ARTIFACTS_PATH  passed as --artifacts-path to build and test (default: the project's own output)
#   CONFIGURATION   build configuration (default: Debug)
#   RESULTS_DIR     where .trx files and shard logs go (default: ${TMPDIR:-/tmp}/pulsar4x-test-results)
#   TMPDIR          honoured as usual: every dotnet/testhost process inherits it
#   TEST_TIMEOUT    seconds before a build or a test process is killed (default: 3600; 0 = no limit)
#
# Shards are separate processes, so the engine's global singletons stay safe. Each shard runs a disjoint
# set of test classes, balanced by test count; the exit code is non-zero if the build or any shard failed.
# Each testhost needs about 1-2 GB, so keep --shards at 3-4 on a small machine. (The vstest list does not
# honour --filter, so shards are balanced on all tests per class; each shard still applies the filter itself.)
set -u -o pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
sln_dir="$here/../Pulsar4X"
project="$sln_dir/Pulsar4X.Tests/Pulsar4X.Tests.csproj"
dotnet_bin="${DOTNET:-dotnet}"
configuration="${CONFIGURATION:-Debug}"
results_dir="${RESULTS_DIR:-${TMPDIR:-/tmp}/pulsar4x-test-results}"
test_timeout="${TEST_TIMEOUT:-3600}"

mode=fast        # fast | all | slow
shards=1
build=1
extra_filter=""

usage() { sed -n '2,/^set -u/p' "${BASH_SOURCE[0]}" | sed '$d' | sed 's/^# \{0,1\}//'; }

while [ $# -gt 0 ]; do
    case "$1" in
        --all|--slow) mode=all ;;
        --slow-only) mode=slow ;;
        --shards) shift; shards="${1:-}" ;;
        --shards=*) shards="${1#*=}" ;;
        --no-build) build=0 ;;
        --filter) shift; extra_filter="${1:-}" ;;
        --filter=*) extra_filter="${1#*=}" ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument: $1" >&2; usage >&2; exit 2 ;;
    esac
    shift
done

if ! [[ "$shards" =~ ^[0-9]+$ ]] || [ "$shards" -lt 1 ] || [ "$shards" -gt 8 ]; then
    echo "--shards takes a number from 1 to 8" >&2
    exit 2
fi

run() {   # run <command...> under the timeout, if one is set and available
    if [ "$test_timeout" != 0 ] && command -v timeout >/dev/null 2>&1; then
        timeout "$test_timeout" "$@"
    else
        "$@"
    fi
}

art_args=()
[ -n "${ARTIFACTS_PATH:-}" ] && art_args=(--artifacts-path "$ARTIFACTS_PATH")

# [Explicit] tests are meant to be run by name, but the NUnit adapter also runs them when a class-name filter
# (shard mode) selects them, so they carry Category("Explicit") and are always left out here. Run one by name:
#   dotnet test ... --filter "Name~WarpTech_TwoYearsHeadless_ZeroDeaths"
case "$mode" in
    fast) base_filter='Category!=Slow&Category!=Explicit' ;;
    slow) base_filter='Category=Slow&Category!=Explicit' ;;
    all)  base_filter='Category!=Explicit' ;;
esac
filter="$base_filter"
if [ -n "$extra_filter" ]; then
    if [ -n "$filter" ]; then filter="($filter)&($extra_filter)"; else filter="$extra_filter"; fi
fi
filter_args=()
[ -n "$filter" ] && filter_args=(--filter "$filter")

mkdir -p "$results_dir"
start=$SECONDS

if [ "$build" = 1 ]; then
    echo "== build ($configuration)"
    if ! run "$dotnet_bin" build "$project" -c "$configuration" ${art_args[@]+"${art_args[@]}"} -nologo -v q -clp:ErrorsOnly; then
        echo "BUILD FAILED" >&2
        exit 1
    fi
fi

test_cmd=("$dotnet_bin" test "$project" --no-build -c "$configuration" ${art_args[@]+"${art_args[@]}"} --results-directory "$results_dir")

summary_line() { grep -aE '^(Passed|Failed)!|No test matches' "$1" | tail -1; }

if [ "$shards" = 1 ]; then
    echo "== test: ${filter:-<all tests>}"
    run "${test_cmd[@]}" ${filter_args[@]+"${filter_args[@]}"} --logger "trx;LogFileName=all.trx" > "$results_dir/all.log" 2>&1
    rc=$?
    grep -aE '^\s+Failed ' "$results_dir/all.log" || true
    echo "== $(summary_line "$results_dir/all.log")"
    echo "== wall time $((SECONDS - start)) s, results in $results_dir"
    exit "$rc"
fi

# ---- sharded: list the tests, group them by class, balance the classes over N processes ----
echo "== listing tests (${filter:-<all tests>})"
list_file="$results_dir/list.txt"
if ! run "$dotnet_bin" test "$project" --no-build -c "$configuration" ${art_args[@]+"${art_args[@]}"} ${filter_args[@]+"${filter_args[@]}"} \
        --list-tests -- NUnit.DisplayName=FullName > "$list_file" 2>&1; then
    echo "listing tests failed, see $list_file" >&2
    exit 1
fi

# Fully qualified names with the case arguments cut off, then the method off: namespace.Class, counted.
mapfile -t classes < <(
    sed -n 's/^    //p' "$list_file" | grep -a '^[A-Za-z_]' | sed 's/(.*//' | sed 's/\.[^.]*$//' \
    | sort | uniq -c | sort -k1,1nr -k2,2 | awk '{print $1 " " $2}')
if [ "${#classes[@]}" = 0 ]; then
    echo "no tests match the filter"
    exit 0
fi

# Longest-processing-time-first: each class goes to the lightest shard so far.
declare -a load shard_filter
for ((i = 0; i < shards; i++)); do load[i]=0; shard_filter[i]=""; done
for entry in "${classes[@]}"; do
    count="${entry%% *}"; cls="${entry#* }"
    lightest=0
    for ((i = 1; i < shards; i++)); do [ "${load[i]}" -lt "${load[lightest]}" ] && lightest=$i; done
    load[lightest]=$(( load[lightest] + count ))
    term="FullyQualifiedName~${cls}."
    shard_filter[lightest]="${shard_filter[lightest]:+${shard_filter[lightest]}|}$term"
done

echo "== ${#classes[@]} classes over $shards shards: ${load[*]} tests"
pids=()
for ((i = 0; i < shards; i++)); do
    [ -z "${shard_filter[i]}" ] && { pids[i]=""; continue; }
    f="(${shard_filter[i]})"
    [ -n "$filter" ] && f="($filter)&$f"
    ( run "${test_cmd[@]}" --filter "$f" --logger "trx;LogFileName=shard$((i + 1)).trx" > "$results_dir/shard$((i + 1)).log" 2>&1 ) &
    pids[i]=$!
done

rc=0
for ((i = 0; i < shards; i++)); do
    [ -z "${pids[i]}" ] && continue
    wait "${pids[i]}"; src=$?
    [ "$src" -ne 0 ] && rc=$src
    echo "== shard $((i + 1)): $(summary_line "$results_dir/shard$((i + 1)).log") (exit $src)"
    grep -aE '^\s+Failed ' "$results_dir/shard$((i + 1)).log" | sed 's/^/     /' || true
done
echo "== wall time $((SECONDS - start)) s, results and logs in $results_dir"
exit "$rc"

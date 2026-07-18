using RimTestRedux;
using static FarmingHysteresis.ColonyManagerRedux.ManagerJob_FarmingHysteresis;

namespace FarmingHysteresis.ColonyManagerRedux.Tests;

// Regression guard for the grower-exclusivity mechanism: a grower must never end up managed by
// more than one Farming Hysteresis manager job at once. See Docs/CMRIntegrationRework.md, Step 2.
[HotSwappable]
[TestSuite]
internal static class ManagerJobFarmingHysteresisScopeTests
{
    [Test]
    public static void AllModeIgnoresAreaAndSpecificSelection()
    {
        Assert
            .That(IsGrowerInScope(GrowerAssignmentMode.All, inArea: false, isSpecificallySelected: false))
            .Is.True();
        Assert
            .That(IsGrowerInScope(GrowerAssignmentMode.All, inArea: true, isSpecificallySelected: true))
            .Is.True();
    }

    [Test]
    public static void AreaModeFollowsAreaMembership()
    {
        Assert
            .That(
                IsGrowerInScope(GrowerAssignmentMode.Area, inArea: true, isSpecificallySelected: false)
            )
            .Is.True();
        Assert
            .That(
                IsGrowerInScope(GrowerAssignmentMode.Area, inArea: false, isSpecificallySelected: true)
            )
            .Is.False();
    }

    [Test]
    public static void SpecificModeFollowsExplicitSelection()
    {
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.Specific,
                    inArea: true,
                    isSpecificallySelected: false
                )
            )
            .Is.False();
        Assert
            .That(
                IsGrowerInScope(
                    GrowerAssignmentMode.Specific,
                    inArea: false,
                    isSpecificallySelected: true
                )
            )
            .Is.True();
    }
}

// FindOwningJob/ManagedGrowers themselves need a live Manager/JobTracker to exercise end to end,
// but the "first claim wins" exclusion they're built on is plain set arithmetic - covered here
// with plain fakes standing in for growers and per-job scopes.
[HotSwappable]
[TestSuite]
internal static class GrowerClaimResolutionTests
{
    private static List<string> ManagedByFirstClaim(params IReadOnlyList<string>[] jobScopesInOrder)
    {
        var claimed = new HashSet<string>();
        List<string> managedByLastJob = [];
        foreach (var scope in jobScopesInOrder)
        {
            managedByLastJob = [.. scope.Where(g => !claimed.Contains(g))];
            foreach (var grower in scope)
            {
                _ = claimed.Add(grower);
            }
        }
        return managedByLastJob;
    }

    [Test]
    public static void NonOverlappingScopesAreBothFullyManaged()
    {
        var managed = ManagedByFirstClaim(["zoneA"], ["zoneB"]);

        Assert.ThatCollection(managed).Has.Count(1);
        Assert.ThatCollection(managed).Does.Contain("zoneB");
    }

    [Test]
    public static void LaterJobLosesGrowerAlreadyClaimedByEarlierJob()
    {
        var laterJobManaged = ManagedByFirstClaim(["zoneA", "zoneB"], ["zoneB", "zoneC"]);

        Assert.ThatCollection(laterJobManaged).Does.Not.Contain("zoneB");
        Assert.ThatCollection(laterJobManaged).Does.Contain("zoneC");
    }
}

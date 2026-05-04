using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class CostDetailResourceGroupTagsTests
{
    [Fact]
    public void MergeByResourceGroup_CombinesTagsFromMultipleResources()
    {
        var rows = new (string ResourceGroup, string? TagsJson)[]
        {
            ("rg1", """{"GAR_ID":"g1","COST_CENTER":"cc"}"""),
            ("rg1", """{"GAR_ID":"g1","COST_CENTER":"cc"}""")
        };

        var map = CostDetailResourceGroupTags.MergeByResourceGroup(rows);

        Assert.True(map.TryGetValue("rg1", out var tags));
        Assert.Equal("g1", tags["GAR_ID"]);
        Assert.Equal("cc", tags["COST_CENTER"]);
    }

    [Fact]
    public void MergeByResourceGroup_MarksConflictingValues()
    {
        var rows = new (string ResourceGroup, string? TagsJson)[]
        {
            ("rg1", """{"GAR_ID":"a"}"""),
            ("rg1", """{"GAR_ID":"b"}""")
        };

        var map = CostDetailResourceGroupTags.MergeByResourceGroup(rows);

        Assert.True(map.TryGetValue("rg1", out var tags));
        Assert.Equal(CostDetailResourceGroupTags.MultipleValuesSentinel, tags["GAR_ID"]);
    }

    [Fact]
    public void MergeTagDictionaries_ReturnsNullWhenEmptyInput()
    {
        Assert.Null(CostDetailResourceGroupTags.MergeTagDictionaries([]));
    }

    [Fact]
    public void MergeByAnalysisRunAndResourceGroup_SeparatesRunsWithSameResourceGroupName()
    {
        var rows = new (int AnalysisRunId, string ResourceGroup, string? TagsJson)[]
        {
            (1, "rg1", """{"GAR_ID":"run1"}"""),
            (2, "rg1", """{"GAR_ID":"run2"}""")
        };

        var map = CostDetailResourceGroupTags.MergeByAnalysisRunAndResourceGroup(rows);

        Assert.Equal(2, map.Count);
        Assert.Equal("run1", map[(1, "rg1")]["GAR_ID"]);
        Assert.Equal("run2", map[(2, "rg1")]["GAR_ID"]);
    }
}

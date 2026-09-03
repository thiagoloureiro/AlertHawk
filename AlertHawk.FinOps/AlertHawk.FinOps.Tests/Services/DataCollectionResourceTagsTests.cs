using FinOpsToolSample.Models;
using FinOpsToolSample.Services;

namespace AlertHawk.FinOps.Tests.Services;

public class DataCollectionResourceTagsTests
{
    [Fact]
    public void ApplyMergedFromArm_CanonicalizesApplicationTagCasing()
    {
        var resource = new ResourceInfo();

        DataCollectionResourceTags.ApplyMergedFromArm(
            resource,
            resourceGroupTags: [new KeyValuePair<string, string>("application", "Payments")],
            resourceTags: null);

        Assert.Equal("Payments", resource.Tags[DataCollectionResourceTags.ApplicationTag]);
        Assert.False(resource.Tags.ContainsKey("application"));
    }

    [Fact]
    public void ApplyMergedFromArm_ResourceTagsOverrideResourceGroupApplication()
    {
        var resource = new ResourceInfo();

        DataCollectionResourceTags.ApplyMergedFromArm(
            resource,
            resourceGroupTags: [new KeyValuePair<string, string>("APPLICATION", "FromRg")],
            resourceTags: [new KeyValuePair<string, string>("Application", "FromResource")]);

        Assert.Equal("FromResource", resource.Tags[DataCollectionResourceTags.ApplicationTag]);
        Assert.Single(resource.Tags);
    }

    [Fact]
    public void ApplyMergedFromArm_CanonicalizesGarIdAndKeepsOtherKeys()
    {
        var resource = new ResourceInfo();

        DataCollectionResourceTags.ApplyMergedFromArm(
            resource,
            resourceGroupTags: null,
            resourceTags:
            [
                new KeyValuePair<string, string>("gar_id", "g-1"),
                new KeyValuePair<string, string>("Owner", "team-a")
            ]);

        Assert.Equal("g-1", resource.Tags[DataCollectionResourceTags.GarIdTag]);
        Assert.Equal("team-a", resource.Tags["Owner"]);
    }

    [Theory]
    [InlineData("APPLICATION", "APPLICATION")]
    [InlineData("application", "APPLICATION")]
    [InlineData("Application", "APPLICATION")]
    [InlineData("GAR_ID", "GAR_ID")]
    [InlineData("gar_id", "GAR_ID")]
    [InlineData("COST_CENTER", "COST_CENTER")]
    [InlineData("Owner", "Owner")]
    public void CanonicalKey_MapsWellKnownTags(string input, string expected)
    {
        Assert.Equal(expected, DataCollectionResourceTags.CanonicalKey(input));
    }
}

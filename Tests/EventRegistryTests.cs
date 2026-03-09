using SayTheSpire2.Settings;

namespace SayTheSpire2.Tests;

[EventSettings("test_combat", "Combat Events")]
public class TestCombatEvent;

[EventSettings("test_dialogue", "Dialogue", defaultBuffer: false)]
public class TestDialogueEvent;

public class EventRegistryTests
{
    [Fact]
    public void Register_CreatesDescriptor()
    {
        EventRegistry.Register(typeof(TestCombatEvent));

        Assert.True(EventRegistry.Descriptors.ContainsKey("test_combat"));
        var desc = EventRegistry.Descriptors["test_combat"];
        Assert.Equal("test_combat", desc.Key);
        Assert.True(desc.DefaultAnnounce);
        Assert.True(desc.DefaultBuffer);
    }

    [Fact]
    public void Register_CreatesSettingsEntries()
    {
        EventRegistry.Register(typeof(TestDialogueEvent));

        var announce = ModSettings.GetSetting<BoolSetting>("events.test_dialogue.announce");
        var buffer = ModSettings.GetSetting<BoolSetting>("events.test_dialogue.buffer");

        Assert.NotNull(announce);
        Assert.NotNull(buffer);
        Assert.True(announce!.Get());
        Assert.False(buffer!.Get());
    }
}

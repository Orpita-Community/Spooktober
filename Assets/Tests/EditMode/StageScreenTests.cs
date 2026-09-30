using NUnit.Framework;

public class StageScreenTests
{
    private TestContent content;

    [SetUp]
    public void SetUp() => content = new TestContent();

    [TearDown]
    public void TearDown() => content.Dispose();

    private StageScreen Build(StageSnapshot snapshot, Dialogue_SpeakerSO speaker) =>
        StageScreen.Build(snapshot, speaker, content.FindSpeaker, content.FindImage);

    [Test]
    public void ResolvesTheBackgroundPortraitsAndWhoIsSpeaking()
    {
        Dialogue_SpeakerSO adam = content.Speaker("Adam", PortraitExpression.Sad);
        Dialogue_SpeakerSO vance = content.Speaker("Vance");
        Stage_ImageSO shop = content.Image("bg-shop");

        StageSnapshot snapshot = new StageSnapshot { backgroundID = "bg-shop" };
        snapshot.actors.Add(new StageActor { speakerID = adam.saveID, slot = StageSlot.Left, expression = PortraitExpression.Sad, flip = true });
        snapshot.actors.Add(new StageActor { speakerID = vance.saveID, slot = StageSlot.Right });

        StageScreen screen = Build(snapshot, adam);

        Assert.AreEqual(shop, screen.background);
        Assert.IsNull(screen.closeUp);
        Assert.AreEqual(adam.GetPortrait(PortraitExpression.Sad), screen.GetActor(StageSlot.Left).sprite);
        Assert.IsTrue(screen.GetActor(StageSlot.Left).flip);
        Assert.IsTrue(screen.GetActor(StageSlot.Left).speaking);
        Assert.IsFalse(screen.GetActor(StageSlot.Right).speaking);
        Assert.IsTrue(screen.SomeoneSpeaking);
        Assert.IsNull(screen.GetActor(StageSlot.Center));
    }

    [Test]
    public void NarrationLightsEveryone()
    {
        Dialogue_SpeakerSO adam = content.Speaker("Adam");
        StageSnapshot snapshot = new StageSnapshot();
        snapshot.actors.Add(new StageActor { speakerID = adam.saveID, slot = StageSlot.Center });

        StageScreen screen = Build(snapshot, null);

        Assert.IsFalse(screen.SomeoneSpeaking);
        Assert.IsFalse(screen.GetActor(StageSlot.Center).speaking);
    }

    [Test]
    public void ContentDeletedSinceTheSaveIsLeftOff()
    {
        StageSnapshot snapshot = new StageSnapshot { backgroundID = "deleted-bg", closeUpID = "deleted-cu" };
        snapshot.actors.Add(new StageActor { speakerID = "deleted-speaker" });

        StageScreen screen = Build(snapshot, null);

        Assert.IsNull(screen.background);
        Assert.IsNull(screen.closeUp);
        Assert.AreEqual(0, screen.actors.Count);
    }
}

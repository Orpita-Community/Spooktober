using System.Linq;
using NUnit.Framework;

public class StageSnapshotTests
{
    private TestContent content;
    private Dialogue_SpeakerSO adam;
    private Dialogue_SpeakerSO vance;
    private Stage_ImageSO shop;
    private Stage_ImageSO letter;

    [SetUp]
    public void SetUp()
    {
        content = new TestContent();
        adam = content.Speaker("Adam");
        vance = content.Speaker("Vance");
        shop = content.Image("bg-shop");
        letter = content.Image("cu-letter");
    }

    [TearDown]
    public void TearDown() => content.Dispose();

    private StageSnapshot AdamAndVanceInTheShop()
    {
        StageDirection direction = TestContent.Cast(TestContent.On(adam, StageSlot.Left), TestContent.On(vance, StageSlot.Right));
        direction.background = shop;
        return new StageSnapshot().With(direction, null, PortraitExpression.Normal);
    }

    [Test]
    public void AnUntouchedDirectionKeepsEverything()
    {
        StageSnapshot before = AdamAndVanceInTheShop();

        StageSnapshot after = before.With(new StageDirection(), null, PortraitExpression.Normal);

        Assert.AreEqual("bg-shop", after.backgroundID);
        Assert.AreEqual(2, after.actors.Count);
        Assert.AreEqual("", after.closeUpID);
    }

    [Test]
    public void SetCastReplacesEveryoneAndClearEmptiesTheStage()
    {
        StageSnapshot stage = AdamAndVanceInTheShop();

        StageSnapshot vanceGone = stage.With(TestContent.Cast(TestContent.On(adam, StageSlot.Center)), null, PortraitExpression.Normal);
        Assert.AreEqual(1, vanceGone.actors.Count);
        Assert.AreEqual(StageSlot.Center, vanceGone.FindActor(adam.saveID).slot);
        Assert.IsFalse(vanceGone.HasActor(vance.saveID));

        StageSnapshot empty = stage.With(new StageDirection { characters = StageChange.Clear }, null, PortraitExpression.Normal);
        Assert.AreEqual(0, empty.actors.Count);
        Assert.AreEqual("bg-shop", empty.backgroundID);
    }

    [Test]
    public void OneCharacterPerSlotAndOneSlotPerCharacterTheLastEntryWins()
    {
        StageDirection direction = TestContent.Cast(
            TestContent.On(adam, StageSlot.Left),
            TestContent.On(vance, StageSlot.Left),   // Pushes Adam out of the left slot
            TestContent.On(vance, StageSlot.Right)); // Vance can only stand in one place

        StageSnapshot stage = new StageSnapshot().With(direction, null, PortraitExpression.Normal);

        Assert.AreEqual(1, stage.actors.Count);
        Assert.AreEqual(StageSlot.Right, stage.FindActor(vance.saveID).slot);
    }

    [Test]
    public void CloseUpCanBeSetKeptAndCleared()
    {
        StageSnapshot shown = AdamAndVanceInTheShop().With(new StageDirection { closeUp = StageChange.Set, closeUpImage = letter }, null, PortraitExpression.Normal);
        StageSnapshot kept = shown.With(new StageDirection(), null, PortraitExpression.Normal);
        StageSnapshot cleared = kept.With(new StageDirection { closeUp = StageChange.Clear }, null, PortraitExpression.Normal);

        Assert.AreEqual("cu-letter", shown.closeUpID);
        Assert.AreEqual("cu-letter", kept.closeUpID);
        Assert.AreEqual("", cleared.closeUpID);
    }

    [Test]
    public void ASpeakerOnStageShowsTheirLinesExpression()
    {
        StageSnapshot stage = AdamAndVanceInTheShop();

        StageSnapshot angry = stage.With(null, vance, PortraitExpression.Angry);
        Assert.AreEqual(PortraitExpression.Angry, angry.FindActor(vance.saveID).expression);
        Assert.AreEqual(PortraitExpression.Normal, angry.FindActor(adam.saveID).expression);

        // The expression stays after the line, until they speak again or the cast is set
        StageSnapshot narration = angry.With(new StageDirection(), null, PortraitExpression.Normal);
        Assert.AreEqual(PortraitExpression.Angry, narration.FindActor(vance.saveID).expression);
    }

    [Test]
    public void ASpeakerOffStageChangesNothing()
    {
        Dialogue_SpeakerSO stranger = content.Speaker("Stranger");
        StageSnapshot stage = AdamAndVanceInTheShop();

        StageSnapshot after = stage.With(null, stranger, PortraitExpression.Angry);

        Assert.AreEqual(2, after.actors.Count);
        Assert.IsFalse(after.HasActor(stranger.saveID));
    }

    [Test]
    public void WithNeverChangesTheOriginal()
    {
        StageSnapshot before = AdamAndVanceInTheShop();

        before.With(TestContent.Cast(), vance, PortraitExpression.Angry);
        before.With(null, vance, PortraitExpression.Angry);

        Assert.AreEqual(2, before.actors.Count);
        Assert.IsTrue(before.actors.All(actor => actor.expression == PortraitExpression.Normal));
    }
}

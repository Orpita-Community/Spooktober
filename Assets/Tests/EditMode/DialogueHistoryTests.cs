using System.Collections.Generic;
using NUnit.Framework;

public class DialogueHistoryTests
{
    private static HistoryEntry Line(string id) => new HistoryEntry { type = HistoryEntryType.Line, conversationID = "c", itemID = id };

    private static DialogueHistory SessionWith(int lines)
    {
        DialogueHistory history = new DialogueHistory();
        history.BeginSession();

        for (int i = 0; i < lines; i++)
            history.Append(Line("line" + i));

        return history;
    }

    [Test]
    public void StepBackStopsAtTheStartOfTheSession()
    {
        DialogueHistory history = SessionWith(3);

        Assert.IsTrue(history.StepBack());
        Assert.AreEqual(1, history.ViewIndex);
        Assert.IsTrue(history.StepBack());
        Assert.AreEqual(0, history.ViewIndex);
        Assert.IsFalse(history.StepBack());
        Assert.AreEqual(0, history.ViewIndex);
    }

    [Test]
    public void StepForwardReturnsToThePresentAndNeverGoesFurther()
    {
        DialogueHistory history = SessionWith(3);

        Assert.IsFalse(history.StepForward()); // Already at the present: never advances live dialogue

        history.StepBack();
        history.StepBack();
        Assert.IsTrue(history.StepForward());
        Assert.IsTrue(history.IsRollingBack);
        Assert.IsTrue(history.StepForward());
        Assert.IsFalse(history.IsRollingBack);
        Assert.AreEqual(2, history.ViewIndex);
        Assert.IsFalse(history.StepForward());
    }

    [Test]
    public void CantRollBackIntoAnEarlierSession()
    {
        DialogueHistory history = SessionWith(3);
        history.EndSession();

        history.BeginSession();
        history.Append(Line("new"));

        Assert.IsFalse(history.StepBack());
        Assert.AreEqual(4, history.Entries.Count); // The backlog still has everything
    }

    [Test]
    public void CantRollBackOutsideASession()
    {
        DialogueHistory history = SessionWith(3);
        history.EndSession();

        Assert.IsNull(history.Present);
        Assert.IsFalse(history.StepBack());
    }

    [Test]
    public void AppendingReturnsToThePresent()
    {
        DialogueHistory history = SessionWith(3);
        history.StepBack();

        history.Append(Line("next"));

        Assert.IsFalse(history.IsRollingBack);
        Assert.AreEqual("next", history.Present.itemID);
    }

    [Test]
    public void TrimmingOldEntriesKeepsTheSessionWorking()
    {
        DialogueHistory history = SessionWith(DialogueHistory.MaxEntries + 20);

        Assert.AreEqual(DialogueHistory.MaxEntries, history.Entries.Count);
        Assert.AreEqual(0, history.SessionStartIndex());

        int steps = 0;
        while (history.StepBack())
            steps++;

        Assert.AreEqual(DialogueHistory.MaxEntries - 1, steps);
    }

    [Test]
    public void SnapshotIsCapped()
    {
        DialogueHistory history = SessionWith(DialogueHistory.MaxSavedEntries + 5);

        List<HistoryEntry> snapshot = history.Snapshot();

        Assert.AreEqual(DialogueHistory.MaxSavedEntries, snapshot.Count);
        Assert.AreEqual("line5", snapshot[0].itemID);
    }

    [Test]
    public void LoadedHistoryResumesItsSessionAndNumbersNewOnesAfterIt()
    {
        DialogueHistory original = SessionWith(2);
        original.EndSession();
        original.BeginSession();
        original.Append(Line("second session"));
        int savedSession = original.CurrentSession;

        DialogueHistory loaded = new DialogueHistory();
        loaded.Load(original.Snapshot());
        loaded.ResumeSession(savedSession);

        Assert.AreEqual("second session", loaded.Present.itemID);
        Assert.IsFalse(loaded.StepBack()); // Only one line in this session

        loaded.EndSession();
        loaded.BeginSession();
        Assert.Greater(loaded.CurrentSession, savedSession);
    }
}

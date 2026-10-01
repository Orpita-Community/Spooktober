using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameDataJsonTests
{
    [Test]
    public void FullRoundTrip()
    {
        GameData data = new GameData
        {
            sceneName = "Shop",
            hasPlayerPosition = true,
            playerPosition = new Vector3(1, 2, 0),
            chapterID = "act-1",
            dialogue = new DialogueSaveState
            {
                isActive = true,
                conversationID = "intro",
                phase = DialoguePhase.Reaction,
                lineID = "l1",
                lineIndex = 1,
                reactionChoiceID = "c2",
                session = 4
            }
        };
        data.metadata.chapterText = "Act 1 — 10:30 PM";
        data.metadata.playTimeSeconds = 125.5f;
        data.storyValues["humanity"] = 2;
        data.history.Add(new HistoryEntry
        {
            type = HistoryEntryType.Line,
            session = 4,
            conversationID = "intro",
            itemID = "l1",
            choicesConversationID = "intro",
            shownChoiceIDs = new List<string> { "c1", "c2" },
            pickedChoiceID = "c2"
        });

        GameData loaded = JsonUtility.FromJson<GameData>(JsonUtility.ToJson(data, true));

        Assert.AreEqual(GameData.CurrentVersion, loaded.saveVersion);
        Assert.AreEqual("Shop", loaded.sceneName);
        Assert.AreEqual(new Vector3(1, 2, 0), loaded.playerPosition);
        Assert.AreEqual(2, loaded.storyValues["humanity"]);
        Assert.AreEqual("Act 1 — 10:30 PM", loaded.metadata.chapterText);
        Assert.AreEqual(125.5f, loaded.metadata.playTimeSeconds);
        Assert.AreEqual(DialoguePhase.Reaction, loaded.dialogue.phase);
        Assert.AreEqual("c2", loaded.dialogue.reactionChoiceID);
        Assert.AreEqual(1, loaded.history.Count);
        CollectionAssert.AreEqual(new[] { "c1", "c2" }, loaded.history[0].shownChoiceIDs);
        Assert.AreEqual("c2", loaded.history[0].pickedChoiceID);
    }

    [Test]
    public void HistoryKeepsTheStageBehindEachScreen()
    {
        GameData data = new GameData();
        StageSnapshot stage = new StageSnapshot { backgroundID = "bg-shop", closeUpID = "cu-letter", musicID = "music-theme", ambienceID = "amb-rain" };
        stage.actors.Add(new StageActor { speakerID = "adam", slot = StageSlot.Left, expression = PortraitExpression.Sad, flip = true });
        data.history.Add(new HistoryEntry { conversationID = "intro", itemID = "l1", stage = stage });

        GameData loaded = JsonUtility.FromJson<GameData>(JsonUtility.ToJson(data));
        StageSnapshot loadedStage = loaded.history[0].stage;

        Assert.AreEqual("bg-shop", loadedStage.backgroundID);
        Assert.AreEqual("cu-letter", loadedStage.closeUpID);
        Assert.AreEqual("music-theme", loadedStage.musicID);
        Assert.AreEqual("amb-rain", loadedStage.ambienceID);
        Assert.AreEqual(1, loadedStage.actors.Count);
        Assert.AreEqual(StageSlot.Left, loadedStage.actors[0].slot);
        Assert.AreEqual(PortraitExpression.Sad, loadedStage.actors[0].expression);
        Assert.IsTrue(loadedStage.actors[0].flip);
    }

    [Test]
    public void EntriesSavedBeforeTheStageExistedLoadWithAnEmptyStage()
    {
        GameData loaded = JsonUtility.FromJson<GameData>("{\"history\":[{\"conversationID\":\"intro\",\"itemID\":\"l1\"}]}");

        Assert.IsNotNull(loaded.history[0].stage);
        Assert.AreEqual("", loaded.history[0].stage.backgroundID);
        Assert.AreEqual(0, loaded.history[0].stage.actors.Count);
        Assert.AreEqual("", loaded.history[0].stage.musicID);
    }

    [Test]
    public void EmptyJsonStillHasEveryCollection()
    {
        GameData loaded = JsonUtility.FromJson<GameData>("{}");

        Assert.IsNotNull(loaded.metadata);
        Assert.IsNotNull(loaded.storyValues);
        Assert.IsNotNull(loaded.dialogue);
        Assert.IsNotNull(loaded.history);
        Assert.IsFalse(loaded.dialogue.isActive);
        Assert.AreEqual(GameData.CurrentVersion, loaded.saveVersion);
    }
}

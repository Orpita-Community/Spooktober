using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class StoryStateTests
{
    private TestContent content;
    private StoryState story;
    private Story_VariableSO humanity;
    private Story_VariableSO cynicism;

    [SetUp]
    public void SetUp()
    {
        content = new TestContent();
        story = new StoryState();
        humanity = content.Variable("humanity");
        cynicism = content.Variable("cynicism", 1);
    }

    [TearDown]
    public void TearDown() => content.Dispose();

    [Test]
    public void UnsetVariablesUseTheirDefault()
    {
        Assert.AreEqual(0, story.Get(humanity));
        Assert.AreEqual(1, story.Get(cynicism));
        Assert.IsTrue(story.GetBool(cynicism));
    }

    [Test]
    public void SetAddAndApplyEffects()
    {
        story.Set(humanity, 2);
        story.Add(humanity, 3);
        story.Apply(new List<StoryEffect>
        {
            TestContent.Effect(cynicism, EffectOperation.Add, 2),
            TestContent.Effect(humanity, EffectOperation.Set, 10)
        });

        Assert.AreEqual(10, story.Get(humanity));
        Assert.AreEqual(3, story.Get(cynicism));
    }

    [Test]
    public void ClearGoesBackToDefaults()
    {
        story.Set(cynicism, 7);
        story.SetChapter("act-2");

        story.Clear();

        Assert.AreEqual(1, story.Get(cynicism));
        Assert.AreEqual("", story.ChapterID);
    }

    [TestCase(ComparisonType.Equal, 2, true)]
    [TestCase(ComparisonType.Equal, 3, false)]
    [TestCase(ComparisonType.NotEqual, 3, true)]
    [TestCase(ComparisonType.Greater, 1, true)]
    [TestCase(ComparisonType.Greater, 2, false)]
    [TestCase(ComparisonType.GreaterOrEqual, 2, true)]
    [TestCase(ComparisonType.Less, 3, true)]
    [TestCase(ComparisonType.Less, 2, false)]
    [TestCase(ComparisonType.LessOrEqual, 2, true)]
    public void ComparesAgainstAConstant(ComparisonType comparison, int constant, bool expected)
    {
        story.Set(humanity, 2);
        Assert.AreEqual(expected, story.Evaluate(TestContent.Condition(humanity, comparison, constant)));
    }

    [Test]
    public void ComparesAgainstAnotherVariable()
    {
        StoryCondition humanityWins = TestContent.Condition(humanity, ComparisonType.Greater, cynicism);

        Assert.IsFalse(story.Evaluate(humanityWins)); // 0 > 1

        story.Set(humanity, 2);
        Assert.IsTrue(story.Evaluate(humanityWins)); // 2 > 1
    }

    [Test]
    public void NoConditionsAlwaysPass()
    {
        Assert.IsTrue(story.Evaluate(new List<StoryCondition>()));
        Assert.IsTrue(story.Evaluate((List<StoryCondition>)null));
    }

    [Test]
    public void AllConditionsMustPass()
    {
        story.Set(humanity, 2);

        Assert.IsFalse(story.Evaluate(new List<StoryCondition>
        {
            TestContent.Condition(humanity, ComparisonType.Equal, 2),
            TestContent.Condition(cynicism, ComparisonType.Equal, 5)
        }));
    }

    [Test]
    public void ConditionWithoutAVariableFails()
    {
        LogAssert.Expect(LogType.Warning, new Regex("no variable assigned"));
        Assert.IsFalse(story.Evaluate(new StoryCondition()));
    }

    [Test]
    public void VariableWithoutASaveIDIsIgnored()
    {
        Story_VariableSO broken = content.Variable("");

        LogAssert.Expect(LogType.Warning, new Regex("has no saveID"));
        story.Set(broken, 5);

        Assert.AreEqual(0, story.Get(broken));
    }

    [Test]
    public void RaisesEventsOnlyWhenSomethingChanges()
    {
        int valueEvents = 0;
        int chapterEvents = 0;
        story.OnValueChanged += (variable, value) => valueEvents++;
        story.OnChapterChanged += chapter => chapterEvents++;

        story.Set(humanity, 1);
        story.Set(humanity, 1);
        story.SetChapter("act-1");
        story.SetChapter("act-1");

        Assert.AreEqual(1, valueEvents);
        Assert.AreEqual(1, chapterEvents);
    }

    [Test]
    public void SavesAndLoadsThroughGameData()
    {
        story.Set(humanity, 4);
        story.SetChapter("act-3");

        GameData data = new GameData();
        story.SaveTo(data);
        GameData loaded = JsonUtility.FromJson<GameData>(JsonUtility.ToJson(data));

        StoryState restored = new StoryState();
        restored.LoadFrom(loaded);

        Assert.AreEqual(4, restored.Get(humanity));
        Assert.AreEqual(1, restored.Get(cynicism));
        Assert.AreEqual("act-3", restored.ChapterID);
    }
}

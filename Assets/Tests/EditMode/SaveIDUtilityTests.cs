using System.Collections.Generic;
using NUnit.Framework;

public class SaveIDUtilityTests
{
    private class Item
    {
        public string id;
    }

    private static bool Ensure(List<Item> items, HashSet<string> used = null) =>
        SaveIDUtility.EnsureUniqueIds(items, item => item.id, (item, id) => item.id = id, used);

    [Test]
    public void FillsEmptyIds()
    {
        List<Item> items = new List<Item> { new Item(), new Item { id = "" } };

        Assert.IsTrue(Ensure(items));
        Assert.IsNotEmpty(items[0].id);
        Assert.IsNotEmpty(items[1].id);
        Assert.AreNotEqual(items[0].id, items[1].id);
    }

    [Test]
    public void DuplicatesGetANewIdAndTheFirstKeepsItsOwn()
    {
        // What Unity's list "+" button produces: a copy of the last element, id included
        List<Item> items = new List<Item> { new Item { id = "abc" }, new Item { id = "abc" } };

        Assert.IsTrue(Ensure(items));
        Assert.AreEqual("abc", items[0].id);
        Assert.AreNotEqual("abc", items[1].id);
    }

    [Test]
    public void ReportsNoChangeWhenIdsAreAlreadyUnique()
    {
        List<Item> items = new List<Item> { new Item { id = "a" }, new Item { id = "b" } };

        Assert.IsFalse(Ensure(items));
        Assert.AreEqual("a", items[0].id);
        Assert.AreEqual("b", items[1].id);
    }

    [Test]
    public void SharedSetKeepsIdsUniqueAcrossLists()
    {
        HashSet<string> used = new HashSet<string>();
        List<Item> lines = new List<Item> { new Item { id = "x" } };
        List<Item> choices = new List<Item> { new Item { id = "x" } };

        Ensure(lines, used);
        Assert.IsTrue(Ensure(choices, used));
        Assert.AreNotEqual(lines[0].id, choices[0].id);
    }

    [Test]
    public void ConversationEnsureIdsCoversLinesAndChoices()
    {
        using TestContent content = new TestContent();
        Dialogue_ConversationSO conversation = content.Conversation("conv", DialogueEndType.Choices);
        conversation.lines.Add(new DialogueLine());
        conversation.lines.Add(new DialogueLine());
        conversation.choices.Add(new DialogueChoice());

        Assert.IsTrue(conversation.EnsureIds());
        Assert.IsFalse(conversation.EnsureIds());

        HashSet<string> ids = new HashSet<string> { conversation.lines[0].id, conversation.lines[1].id, conversation.choices[0].id };
        Assert.AreEqual(3, ids.Count);
    }
}

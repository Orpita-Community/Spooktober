using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Creates the sample story: speakers, story variables, chapters and a short branching Vance encounter
// that exercises every dialogue feature (jumps, choices, reactions, locked choices, endings, autosave).
// Assets keep their paths and line/choice ids between runs, so saves made with the sample stay valid.
public static class SampleStoryBuilder
{
    public const string DialogueFolder = "Assets/Data/Dialogue Data";
    public const string StoryFolder = "Assets/Data/Story Data";
    public const string DatabasePath = DialogueFolder + "/Dialogue Database.asset";

    // Asset paths, not objects: opening a scene unloads unused assets, which would turn held references into nulls
    public class Result
    {
        public string databasePath;
        public string introPath;
        public string introPlayedFlagPath;

        public T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    }

    public static Result Build()
    {
        // Story variables
        Story_VariableSO humanity = Variable("Humanity", 0, "Goes up with compassionate choices. Humanity > Cynicism at midnight = the Humanity ending.");
        Story_VariableSO cynicism = Variable("Cynicism", 0, "Goes up with self-serving choices.");
        Story_VariableSO clock = Variable("Clock", 2230, "Story time as HHMM. Advanced by lines, shown by future UI.");
        Story_VariableSO introPlayed = Variable("Flag Intro Played", 0, "1 once the opening scene has played (stops it replaying).");
        Story_VariableSO readContract = Variable("Flag Read Contract", 0, "1 once Adam has read the contract.");

        // Chapters (shown on save slots)
        Story_ChapterSO act1 = Chapter("Act 1", "Act 1 — 10:30 PM");
        Story_ChapterSO act2 = Chapter("Act 2", "Act 2 — 11:47 PM");
        Story_ChapterSO act3 = Chapter("Act 3", "Act 3 — 11:59 PM");

        // Speakers with placeholder portraits
        Dialogue_SpeakerSO adam = Speaker("Adam", new Color(.62f, .78f, 1f), PlaceholderArt.Character.Adam,
            PortraitExpression.Normal, PortraitExpression.Sad, PortraitExpression.Scared, PortraitExpression.Shocked, PortraitExpression.Angry);
        Dialogue_SpeakerSO vance = Speaker("Mr. Vance", new Color(.84f, .56f, 1f), PlaceholderArt.Character.Vance,
            PortraitExpression.Normal, PortraitExpression.Smirk, PortraitExpression.Angry, PortraitExpression.Happy);

        // Create every conversation first so they can reference each other
        Dialogue_ConversationSO intro = Conversation("01 Shop Intro");
        Dialogue_ConversationSO arrives = Conversation("02 Vance Arrives");
        Dialogue_ConversationSO give = Conversation("03a Give Costume");
        Dialogue_ConversationSO deposit = Conversation("03b Ask Deposit");
        Dialogue_ConversationSO snaps = Conversation("03c Vance Snaps");
        Dialogue_ConversationSO apology = Conversation("03d Apology");
        Dialogue_ConversationSO contract = Conversation("04 The Contract");
        Dialogue_ConversationSO midnight = Conversation("05 Midnight");
        Dialogue_ConversationSO router = Conversation("06 Ending Router");
        Dialogue_ConversationSO humanityEnding = Conversation("07a Humanity Ending");
        Dialogue_ConversationSO cynicismEnding = Conversation("07b Cynicism Ending");

        // 01: Linear lines, then a Jump. Starts Act 1 and autosaves.
        Setup(intro, act1, autosave: true, DialogueEndType.Jump);
        Line(intro, null, PortraitExpression.Normal, "Rain hammers the windows of Alexander's Costume Emporium. The sign on the door still says <i>OPEN</i>.");
        Line(intro, adam, PortraitExpression.Normal, "...He really sat at that sewing machine every single night?");
        Line(intro, adam, PortraitExpression.Sad, "One year gone, Uncle. The shop still smells like you.");
        Line(intro, null, PortraitExpression.Normal, "The bell above the door rings. Nobody should be out in this weather.",
            Effect(clock, EffectOperation.Set, 2245));
        intro.defaultNext = arrives;

        // 02: Choices, one with a reaction, one locked until the contract has been read
        Setup(arrives, null, autosave: false, DialogueEndType.Choices);
        Line(arrives, vance, PortraitExpression.Smirk, "Good evening. You must be the nephew.");
        Line(arrives, adam, PortraitExpression.Scared, "We're closed. How did you even get in?");
        Line(arrives, vance, PortraitExpression.Normal, "Your uncle never locked the door for me. I believe you have something that belongs to me.");

        Choice(arrives, "Give him the costume.", give, Effect(humanity, EffectOperation.Add, 1));
        Choice(arrives, "What about the deposit he paid?", deposit, Effect(cynicism, EffectOperation.Add, 1));
        DialogueChoice rude = Choice(arrives, "Get out of my uncle's shop, old man.", snaps, Effect(cynicism, EffectOperation.Add, 1));
        rude.reactionExpression = PortraitExpression.Angry;
        rude.reactionLine = "Such manners. Alexander would be <b>so</b> disappointed in you.";
        DialogueChoice truth = Choice(arrives, "Tell him what the contract says.", contract);
        truth.requirements.Add(new StoryCondition { variable = readContract, comparison = ComparisonType.Equal, constant = 1 });
        truth.showWhenLocked = true;

        // 03a-d: Branches. 03c has no lines, so its choices appear right under Vance's reaction.
        Setup(give, null, autosave: false, DialogueEndType.Jump);
        Line(give, vance, PortraitExpression.Happy, "How generous. He raised you well.");
        Line(give, adam, PortraitExpression.Sad, "Take it and go.");
        give.defaultNext = contract;

        Setup(deposit, null, autosave: false, DialogueEndType.Jump);
        Line(deposit, vance, PortraitExpression.Smirk, "Ah, a businessman after all. The deposit was never money, boy.");
        Line(deposit, adam, PortraitExpression.Shocked, "Then what was it?");
        deposit.defaultNext = contract;

        Setup(snaps, null, autosave: false, DialogueEndType.Choices);
        Choice(snaps, "I'm sorry. It's been a long night.", apology, Effect(humanity, EffectOperation.Add, 1));
        DialogueChoice standFirm = Choice(snaps, "I said get out.", contract, Effect(cynicism, EffectOperation.Add, 1));
        standFirm.reactionExpression = PortraitExpression.Smirk;
        standFirm.reactionLine = "Very well. But I'll be back before midnight.";

        Setup(apology, null, autosave: false, DialogueEndType.Jump);
        Line(apology, vance, PortraitExpression.Normal, "Apology accepted. For now.");
        apology.defaultNext = contract;

        // 04: New chapter + autosave, line effects set the clock and a flag
        Setup(contract, act2, autosave: true, DialogueEndType.Choices);
        Line(contract, null, PortraitExpression.Normal, "Vance is gone. On the counter lies an old contract, signed in something darker than ink.",
            Effect(clock, EffectOperation.Set, 2347), Effect(readContract, EffectOperation.Set, 1));
        Line(contract, adam, PortraitExpression.Shocked, "October 31st, 11:59 PM... That's the exact minute Uncle Alexander disappeared.");
        Line(contract, adam, PortraitExpression.Scared, "And the line for the next signature already has my name on it.");
        Choice(contract, "Remember him fixing costumes for kids who couldn't pay.", midnight, Effect(humanity, EffectOperation.Add, 1));
        Choice(contract, "Remember the unpaid bills piling up on this counter.", midnight, Effect(cynicism, EffectOperation.Add, 1));

        // 05: Final choice
        Setup(midnight, act3, autosave: true, DialogueEndType.Choices);
        Line(midnight, null, PortraitExpression.Normal, "The clock strikes. The mask on the counter is <i>breathing</i>.",
            Effect(clock, EffectOperation.Set, 2359));
        Line(midnight, vance, PortraitExpression.Smirk, "Time's up. Sign, and this shop will never be empty again.");
        Choice(midnight, "Tear up the contract.", router, Effect(humanity, EffectOperation.Add, 1));
        Choice(midnight, "Sign it.", router, Effect(cynicism, EffectOperation.Add, 1));

        // 06: No lines, just decides the ending
        Setup(router, null, autosave: false, DialogueEndType.Jump);
        router.branches.Add(new DialogueBranch
        {
            conditions = new List<StoryCondition>
            {
                new StoryCondition { variable = humanity, comparison = ComparisonType.Greater, compareTo = CompareTarget.Variable, otherVariable = cynicism }
            },
            next = humanityEnding
        });
        router.defaultNext = cynicismEnding;

        // 07: Endings
        Setup(humanityEnding, null, autosave: false, DialogueEndType.End);
        Line(humanityEnding, null, PortraitExpression.Normal, "The paper burns without fire. Somewhere, a bell rings for the last time.");
        Line(humanityEnding, adam, PortraitExpression.Sad, "Goodbye, Uncle.");
        Line(humanityEnding, null, PortraitExpression.Normal, "<b>ENDING — The Last Customer</b>");

        Setup(cynicismEnding, null, autosave: false, DialogueEndType.End);
        Line(cynicismEnding, vance, PortraitExpression.Happy, "Welcome to the family business.");
        Line(cynicismEnding, null, PortraitExpression.Normal, "The mask fits perfectly.");
        Line(cynicismEnding, null, PortraitExpression.Normal, "<b>ENDING — Open Forever</b>");

        foreach (Dialogue_ConversationSO conversation in new[] { intro, arrives, give, deposit, snaps, apology, contract, midnight, router, humanityEnding, cynicismEnding })
            EditorUtility.SetDirty(conversation);

        // Database collects everything and stamps saveIDs
        Dialogue_DatabaseSO database = LoadOrCreate<Dialogue_DatabaseSO>(DatabasePath);
        database.AutoFillAndValidate();

        return new Result
        {
            databasePath = AssetDatabase.GetAssetPath(database),
            introPath = AssetDatabase.GetAssetPath(intro),
            introPlayedFlagPath = AssetDatabase.GetAssetPath(introPlayed)
        };
    }

    private static Story_VariableSO Variable(string name, int defaultValue, string description)
    {
        Story_VariableSO variable = LoadOrCreate<Story_VariableSO>($"{StoryFolder}/Variables/Var - {name}.asset");
        variable.defaultValue = defaultValue;
        variable.description = description;
        EditorUtility.SetDirty(variable);
        return variable;
    }

    private static Story_ChapterSO Chapter(string name, string label)
    {
        Story_ChapterSO chapter = LoadOrCreate<Story_ChapterSO>($"{StoryFolder}/Chapters/Chapter - {name}.asset");
        chapter.label = label;
        EditorUtility.SetDirty(chapter);
        return chapter;
    }

    private static Dialogue_SpeakerSO Speaker(string name, Color nameColor, PlaceholderArt.Character art, params PortraitExpression[] expressions)
    {
        Dialogue_SpeakerSO speaker = LoadOrCreate<Dialogue_SpeakerSO>($"{DialogueFolder}/Speakers/Speaker - {art}.asset");
        speaker.speakerName = name;
        speaker.nameColor = nameColor;
        speaker.defaultPortrait = PlaceholderArt.Portrait(art, PortraitExpression.Normal);
        speaker.portraits.Clear();

        foreach (PortraitExpression expression in expressions)
            speaker.portraits.Add(new SpeakerPortrait { expression = expression, sprite = PlaceholderArt.Portrait(art, expression) });

        EditorUtility.SetDirty(speaker);
        return speaker;
    }

    private static Dialogue_ConversationSO Conversation(string name) =>
        LoadOrCreate<Dialogue_ConversationSO>($"{DialogueFolder}/Conversations/Conv - {name}.asset");

    private static void Setup(Dialogue_ConversationSO conversation, Story_ChapterSO chapter, bool autosave, DialogueEndType endType)
    {
        conversation.chapter = chapter;
        conversation.autosaveOnStart = autosave;
        conversation.endType = endType;
        conversation.lines.Clear();
        conversation.choices.Clear();
        conversation.branches.Clear();
        conversation.defaultNext = null;
    }

    private static void Line(Dialogue_ConversationSO conversation, Dialogue_SpeakerSO speaker, PortraitExpression expression, string text, params StoryEffect[] effects)
    {
        conversation.lines.Add(new DialogueLine
        {
            id = $"l{conversation.lines.Count + 1:00}", // Deterministic, so re-running keeps saved line ids valid
            speaker = speaker,
            expression = expression,
            text = text,
            effects = new List<StoryEffect>(effects)
        });
    }

    private static DialogueChoice Choice(Dialogue_ConversationSO conversation, string text, Dialogue_ConversationSO next, params StoryEffect[] effects)
    {
        DialogueChoice choice = new DialogueChoice
        {
            id = $"c{conversation.choices.Count + 1:00}",
            text = text,
            next = next,
            effects = new List<StoryEffect>(effects)
        };

        conversation.choices.Add(choice);
        return choice;
    }

    private static StoryEffect Effect(Story_VariableSO variable, EffectOperation operation, int value) =>
        new StoryEffect { variable = variable, operation = operation, value = value };

    public static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        AssetFolders.EnsureParent(path);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}

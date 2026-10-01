using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Imports the game's script (the team's dev scenario: Prologue, Act 1, Act 2 "The Three Boxes", Act 3 and both endings)
// as speakers, chapters, stage images and conversations. It reads like the script: Portrait() is PORTRAIT:,
// Stage() carries BG:, close-ups, cast changes and transitions, and choices carry the dev scenario's scores.
//
// It's a one-time import. Afterwards the assets are the source of truth: edit the text in the inspector.
// Running it again rewrites every conversation below (ids stay the same, so saves keep working) and loses those edits.
public static class StoryBuilder
{
    public const string DialogueFolder = "Assets/Data/Dialogue Data";
    public const string StoryFolder = "Assets/Data/Story Data";
    public const string StageFolder = "Assets/Data/Stage Data";
    public const string DatabasePath = DialogueFolder + "/Dialogue Database.asset";

    // Dev scenario scoring: every cynical answer is worth 3, every humane one 2
    private const int CynicismPoints = 3;
    private const int HumanityPoints = 2;

    private const string Ember = "#FF8A3D"; // The inscription burning into the wood

    // Asset paths, not objects: opening a scene unloads unused assets, which would turn held references into nulls
    public class Result
    {
        public string databasePath;
        public string openingPath;
        public string introPlayedFlagPath;

        public T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    }

    public static Result Build() => new Writer().Write();

    // An existing asset is reused, so its GUID (and the saveID stamped from it) never changes
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

    private class Writer
    {
        private Story_VariableSO humanity;
        private Story_VariableSO cynicism;
        private Story_VariableSO introPlayed;

        private Dialogue_SpeakerSO adam;
        private Dialogue_SpeakerSO adamMasked;
        private Dialogue_SpeakerSO youngAdam;
        private Dialogue_SpeakerSO vance;
        private Dialogue_SpeakerSO alexander;

        private Stage_ImageSO street, shopFront, shopWithNotice, shop, workshopCape, workshopDress, shopMemory, shopDawn, shopCold, shopFrontDawn;
        private Stage_ImageSO finalNotice, maskBox, maskInBox, mask, signedPaper;

        private Audio_SoundSO theme, rainOutside, rainInside;
        private Audio_SoundSO thunder, door, maskBoxOpen, paper, pen;

        private Story_ChapterSO prologue, act1, act2, act3;

        // Prologue
        private Dialogue_ConversationSO p01, p02;
        // Act 1
        private Dialogue_ConversationSO a01, a01Cynical, a01Humane, a02, a03, a03Humane, a03Cynical, a04, a04Humane, a05, a06, a07;
        // Act 2
        private Dialogue_ConversationSO b1, b1Cynical, b1Humane, b1End, b2, b2Humane, b2Cynical, b2End, b3, b3Cynical, b3Humane, b3End, b4;
        // Act 3
        private Dialogue_ConversationSO c01, c02, humanityEnding, dawn, cynicismEnding, finalShot;

        private readonly List<Dialogue_ConversationSO> written = new List<Dialogue_ConversationSO>();
        private readonly Dictionary<Dialogue_SpeakerSO, PortraitExpression> faces = new Dictionary<Dialogue_SpeakerSO, PortraitExpression>();
        private readonly List<StageCharacter> cast = new List<StageCharacter>();
        private readonly HashSet<Dialogue_SpeakerSO> changedFaces = new HashSet<Dialogue_SpeakerSO>();
        private Dialogue_ConversationSO current;

        public Result Write()
        {
            CreateVariables();
            CreateSpeakers();
            CreateImages();
            CreateSounds();
            CreateChapters();
            CreateConversations(); // All of them first, so they can point at each other

            // In script order: Portrait() and the cast carry over from one line to the next, like in the script
            WritePrologue();
            WriteAct1();
            WriteAct2();
            WriteAct3();

            foreach (Dialogue_ConversationSO conversation in written)
                EditorUtility.SetDirty(conversation);

            Dialogue_DatabaseSO database = LoadOrCreate<Dialogue_DatabaseSO>(DatabasePath);
            database.AutoFillAndValidate();

            return new Result
            {
                databasePath = AssetDatabase.GetAssetPath(database),
                openingPath = AssetDatabase.GetAssetPath(p01),
                introPlayedFlagPath = AssetDatabase.GetAssetPath(introPlayed)
            };
        }

        #region The script

        private void WritePrologue()
        {
            // P-01 — EXTERIOR
            Begin(p01, prologue, autosave: true);
            Narrate("October 31st.", Stage().Bg(street).Music(theme).Ambience(rainOutside));
            Narrate("Halloween night.", Stage().Sound(thunder)); // SFX_02_THUNDER
            Narrate("Five years ago, on this exact night, the owner of this shop vanished without a trace.", Stage().Bg(shopFront));
            Narrate("The shop stayed.");
            Narrate("The debt stayed.");
            Narrate("And somehow, Adam stayed too.");
            JumpTo(p02);

            // P-02 — INSIDE THE SHOP
            Begin(p02);
            Portrait(adam, PortraitExpression.Normal);
            Say(adam, "Tomorrow.", Stage().Fade().Bg(shopWithNotice).Cast(On(adam, StageSlot.Center)).Ambience(rainInside));
            Say(adam, "Nine AM.");
            Say(adam, "Final deadline.");
            Say(adam, "After that, this place isn't mine anymore.");
            Narrate("The final notice sits on the counter.", Stage().CloseUp(finalNotice));
            Narrate("Adam has read it enough times to know every word.");
            Portrait(adam, PortraitExpression.Bitter);
            Say(adam, "Five years.", Stage().NoCloseUp());
            Say(adam, "Five years paying for a dream that was never mine.");
            Say(adam, "Five Halloweens doing the same thing.");
            Say(adam, "Sitting here.");
            Say(adam, "Fixing costumes.");
            Say(adam, "Paying bills.");
            Say(adam, "Waiting for someone who isn't coming back.");
            Portrait(adam, PortraitExpression.Sad);
            Say(adam, "My uncle vanished five years ago tonight.");
            Say(adam, "No warning.");
            Say(adam, "No call.");
            Say(adam, "No letter.");
            Say(adam, "Nothing.");
            Say(adam, "He left me the shop.");
            Say(adam, "The debt.");
            Say(adam, "And no explanation.");
            Say(adam, "I miss him.");
            Say(adam, "I still think he was selfish for leaving me like this.");
            Say(adam, "Sometimes I wonder where he went.");
            Say(adam, "And whether he'll ever come back.");
            Narrate("Adam closes the ledger.");
            Portrait(adam, PortraitExpression.Normal);
            Say(adam, "Enough.");
            Narrate("Silence.");
            Narrate("The doorbell rings.", Stage().Sound(door)); // SFX_04_DOORBELL: someone has come in
            Narrate("Adam freezes.");
            Narrate("It rings again."); // SFX_04_DOORBELL (the door sound already played)
            Portrait(adam, PortraitExpression.Shocked);
            Narrate("The door was locked.");
            JumpTo(a01);
        }

        private void WriteAct1()
        {
            // A-01 — VANCE
            Begin(a01, act1, autosave: true);
            Portrait(adam, PortraitExpression.Normal);
            Portrait(vance, PortraitExpression.Normal);
            Narrate("Vance is standing inside the shop.", Stage().Bg(shop).Cast(On(adam, StageSlot.Left), On(vance, StageSlot.Right)));
            Narrate("His coat is soaked.");
            Narrate("Half his face remains in shadow.");
            Say(adam, "We're closed.");
            Say(adam, "Can't you read the sign?");
            Say(vance, "The door was locked.");
            Say(adam, "I know.");
            Say(vance, "I noticed.");
            Say(adam, "Then how did you get in?");
            Say(vance, "That's a good question.");
            Say(adam, "Are you going to answer it?");
            Say(vance, "No.");
            // CHOICE 1
            Cynical("Get out. Now. Before I call security.", a01Cynical);
            Humane("Look, we really are closed. Could you come back tomorrow?", a01Humane);

            Begin(a01Cynical);
            Say(vance, "You don't have to be frightened.");
            Say(adam, "I'm not frightened.");
            Say(vance, "I didn't say you were.");
            JumpTo(a02);

            Begin(a01Humane);
            Say(vance, "Tomorrow would be too late.");
            Say(adam, "Too late for what?");
            Narrate("Vance doesn't answer.");
            JumpTo(a02);

            // A-02 — THE BOX
            Begin(a02);
            Say(vance, "I'm here to return something.");
            Narrate("Vance places a wooden box on the counter.", Stage().CloseUp(maskBox));
            Portrait(adam, PortraitExpression.Wary);
            Say(vance, "I took it from this shop five years ago.");
            Say(vance, "Tonight.");
            Say(adam, "You kept it for five years?");
            Say(vance, "I was told to.");
            Narrate("Vance opens the box.", Stage().Sound(maskBoxOpen));
            Narrate("Inside is a glossy black obsidian mask.", Stage().CloseUp(maskInBox));
            Narrate("Its surface catches the light like dark glass.");
            Say(adam, "That's...");
            Say(adam, "That's new.");
            Say(adam, "Did you wear it?");
            Say(vance, "Once.");
            Say(adam, "And?");
            Say(vance, "That was enough.");
            Say(adam, "What does that mean?");
            Say(vance, "You'll understand.");
            Say(adam, "I don't like answers like that.");
            Say(vance, "Neither did I.");
            JumpTo(a03);

            // A-03 — THE LEDGER
            Begin(a03);
            Say(vance, "I paid a thousand-dollar deposit for it.", Stage().NoCloseUp());
            Say(adam, "A thousand?");
            Say(vance, "Check the ledger.");
            Narrate("Adam opens the ledger."); // SFX_06_LEDGER
            Portrait(adam, PortraitExpression.Confused);
            Say(adam, "October 31st.");
            Say(adam, "11:45 PM.");
            Say(adam, "Obsidian mask.");
            Say(adam, "One thousand dollars.");
            Say(adam, "This was the night my uncle disappeared.");
            Say(vance, "Yes.");
            Say(adam, "You knew him.");
            Say(vance, "I knew of him.");
            Say(adam, "That's not what I asked.");
            // CHOICE 2
            Humane("Do you know something about my uncle? Tell me.", a03Humane);
            Cynical("Never mind. I don't have time for this.", a03Cynical);

            Begin(a03Humane);
            Say(vance, "I know he stood exactly where you're standing.");
            Say(vance, "He read that same page.");
            Say(adam, "And?");
            Say(vance, "The rest isn't mine to tell.");
            Say(adam, "Then whose is it?");
            Say(vance, "His.");
            JumpTo(a04);

            Begin(a03Cynical);
            Say(adam, "And I definitely don't have a thousand dollars.");
            Say(vance, "I know.");
            Say(adam, "Then why bring it back?");
            Say(vance, "Because I don't want the money anymore.");
            JumpTo(a04);

            // A-04 — THE OFFER
            Begin(a04);
            Say(vance, "Put the mask in the shop window.");
            Say(adam, "What?");
            Say(vance, "Someone will pay for it.");
            Say(adam, "You're giving it to me?");
            Say(vance, "I'm forgiving the thousand-dollar deposit.");
            Say(adam, "Why?");
            Say(vance, "One condition.");
            Narrate("Vance places a paper on the counter.", Stage().Sound(paper)); // SFX_06_PAPER
            Say(vance, "Sign that you received it from me tonight.");
            Say(adam, "That's it?");
            Say(vance, "That's it.");
            Say(adam, "Why do you need my signature?");
            Say(vance, "Because I needed someone else's.");
            Say(adam, "Whose?");
            Narrate("Vance doesn't answer.");
            // CHOICE 3
            Cynical("Fine. I'll sign. Let's get this over with.", a05);
            Humane("Can't I wait until tomorrow? I could see what it's actually worth first.", a04Humane);

            Begin(a04Humane);
            Say(vance, "Tomorrow it won't exist.");
            Say(adam, "What?");
            Say(vance, "Opportunities like this come once.");
            Say(vance, "Like everything of value.");
            Narrate("Adam looks at the final notice.");
            Narrate("Then the mask.");
            Say(adam, "Fine.");
            Say(adam, "I'll sign.");
            JumpTo(a05);

            // A-05 — THE SIGNATURE
            Begin(a05);
            Portrait(adam, PortraitExpression.Normal);
            Narrate("Adam signs his name.", Stage().CloseUp(signedPaper).Sound(pen)); // SFX_07_PEN
            Narrate("Silence.");
            Portrait(vance, PortraitExpression.Sad);
            Say(vance, "I thought signing would be easy for me too.", Stage().NoCloseUp());
            Say(adam, "What?");
            Say(vance, "It wasn't easy at all.");
            // Short fade to black. Return to shop. Vance is gone.
            Portrait(adam, PortraitExpression.Confused);
            Say(adam, "Mr. Vance?", Stage().Fade().Cast(On(adam, StageSlot.Left)));
            Narrate("Silence.");
            Say(adam, "Mister Vance?");
            Say(adam, "Where did you go?");
            Narrate("Adam looks at the door.");
            Narrate("It is still locked.");
            JumpTo(a06);

            // A-06 — THE MIRROR (no mirror art yet: the mask appears on Adam himself)
            Begin(a06);
            Portrait(adam, PortraitExpression.Wary);
            Narrate("Adam passes the old mirror.", Stage().Cast(On(adam, StageSlot.Center)));
            Narrate("He stops.");
            Narrate("Looks back.");
            Say(adam, "...");
            Portrait(adamMasked, PortraitExpression.Shocked);
            // Thunder stands in for SFX_08_SUPERNATURAL_LOW, which has no file yet: with the flash it reads as lightning
            Narrate("The reflection is wearing the mask.", Stage().Flash().Cast(On(adamMasked, StageSlot.Center)).Sound(thunder));
            Say(adamMasked, "No.");
            Say(adamMasked, "What the hell?");
            Say(adamMasked, "Get it off.");
            Say(adamMasked, "Get it off me!");
            JumpTo(a07);

            // A-07 — THE INSCRIPTION (no inscription art yet: it burns in as centered text)
            Begin(a07);
            Narrate("Where Vance stood, words burn themselves into the wood, letter by letter.");
            Caption(Burning("THE MASK IS WORN ONCE."));
            Caption(Burning("THE ONE WHO WEARS IT SEES WHAT IS ALREADY INSIDE."));
            Caption(Burning("THREE BOXES."));
            Caption(Burning("WHAT HE LEFT BEHIND."));
            Caption(Burning("YOU HAVE UNTIL DAWN."));
            Portrait(adamMasked, PortraitExpression.Scared);
            Say(adamMasked, "Three boxes?");
            Narrate("Adam looks across the shop.");
            Narrate("Three old costume boxes sit where they have always been.");
            Say(adamMasked, "I've seen these my whole life.");
            Say(adamMasked, "How did I never notice them?");
            Say(adamMasked, "What did you leave behind, Uncle?");
            Narrate("Adam approaches the first box.");
            JumpTo(b1); // Fade to black: Act 2's title card
        }

        private void WriteAct2()
        {
            // BOX 1 — THE CAPE
            Begin(b1, act2, autosave: true);
            Caption("BOX ONE.", Stage().Bg(workshopCape).NoCast().NoAmbience()); // No rain in the memories
            Portrait(alexander, PortraitExpression.Happy);   // WARM
            Portrait(youngAdam, PortraitExpression.Curious);
            // Alexander by his sewing table, facing the boy, so the cape on the mannequin stays in view
            Say(alexander, "You have one job.", Stage().Cast(On(alexander, StageSlot.Left, flip: true), On(youngAdam, StageSlot.Center)));
            Say(youngAdam, "What?");
            Say(alexander, "Hand me the scissors.");
            Say(youngAdam, "You said one job.");
            Say(alexander, "Exactly.");
            Narrate("Young Adam hands him the scissors.");
            Say(youngAdam, "Uncle, this invoice.");
            Say(alexander, "Don't start.");
            Say(youngAdam, "You crossed the number out.");
            Say(alexander, "I negotiated it.");
            Say(youngAdam, "With a pen?");
            Say(alexander, "A firm pen.");
            Say(youngAdam, "That's not negotiating.");
            Say(alexander, "It worked.");
            Say(youngAdam, "Did Samira pay?");
            Say(alexander, "Eleven dollars.");
            Say(youngAdam, "That's not enough.");
            Say(alexander, "And a jar of pickles.");
            Say(youngAdam, "...");
            Say(youngAdam, "Pickles?");
            Say(alexander, "Very good pickles.");
            Narrate("Alexander continues sewing.");
            Say(youngAdam, "Why are you making it?");
            Portrait(alexander, PortraitExpression.Normal);
            Say(alexander, "Samira's boy needs a cape tomorrow.");
            Say(youngAdam, "He could use an old one.");
            Say(alexander, "Last year they put him in a locker wearing a bedsheet.");
            Narrate("Young Adam stops.");
            Say(alexander, "A bedsheet doesn't hide anybody.");
            Say(alexander, "Not from bullies.");
            Say(alexander, "Not from anything.");
            // CHOICE 4
            Cynical("Don't make it. We can't afford it.", b1Cynical);
            Humane("Make it. He needs it.", b1Humane);

            Begin(b1Cynical);
            Narrate("Alexander pauses.");
            Say(alexander, "We'll manage.");
            Narrate("He keeps sewing.");
            JumpTo(b1End);

            Begin(b1Humane);
            Portrait(alexander, PortraitExpression.Happy);
            Narrate("Alexander smiles.");
            Say(alexander, "That's what I thought.");
            JumpTo(b1End);

            Begin(b1End);
            Narrate("The cape is finished.");
            Narrate("Alexander folds it carefully.");
            Narrate("Adam remembers the shop differently now.");
            Narrate("Not as a place that kept losing money.");
            Narrate("As a place people came to when they had nowhere else to go.");
            JumpTo(b2);

            // BOX 2 — THE DRESS (after a warm fade)
            Begin(b2, autosave: true);
            Caption("BOX TWO.", Stage().Fade().Bg(workshopDress).NoCast());
            Portrait(alexander, PortraitExpression.Happy);   // AMUSED
            Portrait(youngAdam, PortraitExpression.Curious);
            // Same spot as the first box, which leaves the dress on the mannequin in plain sight
            Say(alexander, "Which layer goes first?", Stage().Cast(On(alexander, StageSlot.Left, flip: true), On(youngAdam, StageSlot.Center)));
            Say(youngAdam, "The one leaving or the one coming?");
            Say(alexander, "The dress.");
            Say(youngAdam, "I know.");
            Say(alexander, "Good.");
            Say(youngAdam, "Why don't you use new fabric?");
            Say(alexander, "Because I know where this tear lives.");
            Say(youngAdam, "It's a tear.");
            Say(alexander, "Exactly.");
            Narrate("Alexander starts sewing a star-shaped patch over it.");
            Say(youngAdam, "You're making it more obvious.");
            Say(alexander, "Yes.");
            Say(youngAdam, "Why?");
            Portrait(alexander, PortraitExpression.Normal);
            Say(alexander, "Because fixing something doesn't mean pretending it was never broken.");
            Say(youngAdam, "That's weird.");
            Portrait(alexander, PortraitExpression.Happy);
            Say(alexander, "Probably.");
            Portrait(alexander, PortraitExpression.Normal);
            Say(alexander, "A mask that hides everything isn't protecting you.");
            Say(alexander, "It's erasing you.");
            Say(youngAdam, "Who wears this?");
            Say(alexander, "A girl who's been coming here for three years.");
            Say(youngAdam, "Why?");
            Say(alexander, "She doesn't like people looking at her.");
            Say(youngAdam, "Then why make it pretty?");
            Portrait(alexander, PortraitExpression.Happy);
            Narrate("Alexander smiles.");
            Say(alexander, "Maybe one day she'll look at herself.");
            // CHOICE 5
            Humane("Leave the patch. It belongs there.", b2Humane);
            Cynical("Use new fabric. It'll be faster.", b2Cynical);

            Begin(b2Humane);
            Portrait(alexander, PortraitExpression.Happy);
            Narrate("Alexander smiles.");
            Say(alexander, "Exactly.");
            JumpTo(b2End);

            Begin(b2Cynical);
            Portrait(alexander, PortraitExpression.Normal);
            Narrate("Alexander looks at the dress.");
            Say(alexander, "Maybe.");
            Narrate("He continues sewing the patch anyway.");
            JumpTo(b2End);

            Begin(b2End);
            Narrate("The dress is finished.");
            Narrate("A small star covers the tear.");
            Narrate("She wore it.");
            Narrate("She came back the next Halloween.");
            Narrate("And the one after that.");
            Narrate("She never explained why.");
            Narrate("She didn't have to.");
            JumpTo(b3);

            // BOX 3 — THE SHOP (the warmth fades: a washed-out memory of the shop)
            Begin(b3, autosave: true);
            Caption("BOX THREE.", Stage().Fade().Bg(shopMemory).NoCast());
            Narrate("No costume.");
            Narrate("No customer.");
            Narrate("Just the shop.");
            // The dev scenario says "Five years ago." here, but this first memory has Adam as a child.
            // The line moved to the jump forward below, where the memory really is five years ago.
            Narrate("Late at night.");
            Narrate("The chairs are upside down.");
            Portrait(alexander, PortraitExpression.Worried);  // CONCERNED
            Narrate("Alexander is closing the ledger.", Stage().Cast(On(alexander, StageSlot.Right)));
            Narrate("Young Adam stands near the counter.", Stage().Cast(On(youngAdam, StageSlot.Left, flip: true), On(alexander, StageSlot.Right)));
            Say(youngAdam, "Uncle?");
            Say(alexander, "You're supposed to be asleep.");
            Say(youngAdam, "I heard you talking.");
            Say(alexander, "With who?");
            Say(youngAdam, "The landlord.");
            Narrate("Alexander stops.");
            Say(youngAdam, "Are you selling the shop?");
            Say(alexander, "I don't know.");
            Say(youngAdam, "You said we'd keep it.");
            Say(alexander, "I know.");
            Say(youngAdam, "Then why are you talking about selling it?");
            Say(alexander, "Because sometimes keeping something costs more than you can afford.");
            Say(youngAdam, "But you love this place.");
            Say(alexander, "That's the problem.");
            Narrate("Young Adam doesn't understand.");
            // MEMORY JUMPS FORWARD
            Portrait(adam, PortraitExpression.Angry);        // ADAM_5_YEARS_AGO_ANGRY
            Narrate("Five years ago.", Stage().Fade().Cast(On(adam, StageSlot.Left), On(alexander, StageSlot.Right)));
            Say(adam, "Then sell it.");
            Narrate("Alexander looks at him.");
            Say(alexander, "What?");
            Say(adam, "Sell the shop.");
            Say(alexander, "Adam—");
            Say(adam, "You can't keep doing this.");
            Say(alexander, "Doing what?");
            Say(adam, "Losing money.");
            Say(adam, "Borrowing.");
            Say(adam, "Pretending next Halloween will fix everything.");
            Narrate("Alexander closes the ledger.");
            Say(alexander, "You think that's what I'm doing?");
            Say(adam, "What else am I supposed to think?");
            Say(alexander, "This place isn't just a building.");
            Say(adam, "It's a shop.");
            Say(alexander, "It's people's lives.");
            Say(adam, "That's not our responsibility.");
            Say(alexander, "Maybe it is.");
            Say(adam, "No, it isn't.");
            Narrate("Silence.");
            Say(alexander, "You think I'm afraid of losing the shop.");
            Say(adam, "Aren't you?");
            Say(alexander, "I'm afraid of what happens after.");
            Say(adam, "To the shop?");
            Say(alexander, "To people.");
            Say(adam, "They'll find somewhere else.");
            Say(alexander, "Will they?");
            Narrate("Adam doesn't answer.");
            Say(alexander, "That's what you're not seeing.");
            Say(adam, "And what are you seeing?");
            Narrate("Alexander looks around the shop.");
            Say(alexander, "Everyone who won't have somewhere else.");
            Narrate("Silence.");
            Say(adam, "You can't save everyone.");
            Say(alexander, "I know.");
            Say(adam, "Then stop trying.");
            Narrate("Alexander looks at him.");
            Say(alexander, "Maybe.");
            Say(alexander, "But if I stop...");
            Narrate("He doesn't finish.");
            // CHOICE 6 (the most important choice in the game)
            Cynical("You should have sold it. You should've chosen yourself for once.", b3Cynical);
            Humane("I'm sorry. I didn't understand.", b3Humane);

            Begin(b3Cynical);
            Narrate("Alexander looks at him.");
            Say(alexander, "Maybe I should have.");
            JumpTo(b3End);

            Begin(b3Humane);
            Portrait(adam, PortraitExpression.Sad);
            Say(adam, "I thought you were just being stubborn.");
            Say(adam, "I didn't know you were scared too.");
            Narrate("Alexander looks at him.");
            Say(alexander, "I was.");
            JumpTo(b3End);

            Begin(b3End);
            Narrate("The argument ends.");
            Narrate("Nobody wins.");
            Narrate("Nobody changes the other's mind.");
            Narrate("Alexander closes the ledger.");
            Narrate("Adam leaves.", Stage().Cast(On(alexander, StageSlot.Right)).Sound(door)); // "Young Adam leaves." in the dev scenario, but it's the grown-up Adam here
            Narrate("Alexander remains alone in the shop.", Stage().Cast(On(alexander, StageSlot.Center)));
            Portrait(alexander, PortraitExpression.Sad);
            Narrate("For the first time, Adam notices what he didn't notice five years ago.");
            Narrate("Alexander is crying.");
            Narrate("He quickly wipes his face.");
            Say(alexander, "I'm fine.");
            Narrate("Silence.");
            Narrate("The memory ends.");
            JumpTo(b4);

            // ACT 2 — RETURN TO PRESENT
            Begin(b4);
            Portrait(adamMasked, PortraitExpression.Sad);
            Say(adamMasked, "That was our last argument.", Stage().Fade().Bg(shop).Cast(On(adamMasked, StageSlot.Center)).Ambience(rainInside));
            Say(adamMasked, "I forgot most of it.");
            Say(adamMasked, "Or maybe I didn't want to remember.");
            Narrate("Adam looks around the shop.");
            Say(adamMasked, "He wanted to keep it.");
            Say(adamMasked, "I wanted to sell it.");
            Say(adamMasked, "Five years later...");
            Narrate("Adam looks at the final notice.");
            Say(adamMasked, "I'm still trying to sell it.");
            Narrate("Silence.");
            Say(adamMasked, "I spent five years thinking he left me with his problem.");
            Say(adamMasked, "Maybe he was trying to leave me something else.");
            Narrate("Adam looks at the mask.");
            Say(adamMasked, "So what do you want from me?");
            Narrate("Midnight."); // SFX_10_CLOCK_CHIME
            JumpTo(c01);
        }

        private void WriteAct3()
        {
            // C-01 — VANCE
            Begin(c01, act3, autosave: true);
            Portrait(adamMasked, PortraitExpression.Normal);
            Portrait(vance, PortraitExpression.Normal);
            Narrate("Vance stands near the door.", Stage().Bg(shop).Cast(On(adamMasked, StageSlot.Left), On(vance, StageSlot.Right)).Sound(door));
            Say(vance, "Time's up, Adam.");
            Say(adamMasked, "It's midnight.");
            Say(vance, "Yes.");
            Say(adamMasked, "You said I had until dawn.");
            Say(vance, "I did.");
            Say(adamMasked, "Then why are you here?");
            Say(vance, "To see what you found.");
            Say(adamMasked, "Three boxes.");
            Say(vance, "And?");
            Say(adamMasked, "My uncle wasn't who I thought he was.");
            Say(vance, "Nobody is.");
            Say(adamMasked, "He wasn't selfish.");
            Say(vance, "That's not what I asked.");
            Say(adamMasked, "Then what are you asking?");
            Say(vance, "Who are you?");
            JumpTo(c02);

            // C-02 — THE MASK (no mirror art yet: the mask itself is the reflection)
            Begin(c02);
            Say(vance, "Look at yourself.");
            Portrait(adamMasked, PortraitExpression.Confused);
            Say(adamMasked, "I see the mask.", Stage().CloseUp(mask));
            Say(vance, "Look past it.");
            Say(adamMasked, "I don't know what you want me to see.");
            Say(vance, "You do.");
            Say(adamMasked, "No.");
            Say(vance, "Then tell me what you feel.");
            Narrate("Silence.", Stage().NoCloseUp());
            Say(adamMasked, "I'm angry.");
            Say(vance, "At who?");
            Say(adamMasked, "Everyone.");
            Say(vance, "That's convenient.");
            Say(adamMasked, "My uncle left me a shop I never wanted.");
            Say(adamMasked, "A debt I couldn't pay.");
            Say(adamMasked, "And five years of wondering if I mattered enough for him to say goodbye.");
            Narrate("Vance says nothing.");
            Say(adamMasked, "And now you're standing here asking me who I am?");
            Say(vance, "Yes.");
            // The ending depends on every choice so far
            BranchOnHumanity(humanityEnding, cynicismEnding);

            // IF HUMANITY IS HIGHER
            Begin(humanityEnding);
            Portrait(adamMasked, PortraitExpression.Sad);    // HONEST
            Say(adamMasked, "I'm still angry.");
            Say(adamMasked, "I think I always will be.");
            Say(adamMasked, "But I loved him.");
            Say(adamMasked, "I just didn't know how to say that while I was angry.");
            Portrait(vance, PortraitExpression.Sad);
            Narrate("Vance's expression changes slightly.");
            Say(vance, "That's enough.");
            Say(adamMasked, "Enough for what?");
            Say(vance, "For the mask.");
            Portrait(adam, PortraitExpression.Shocked);
            Narrate("The mask is broken away from Adam.", Stage().Flash().Cast(On(adam, StageSlot.Left), On(vance, StageSlot.Right)));
            Narrate("Warm light begins returning to the shop.", Stage().Bg(shopDawn));
            Portrait(adam, PortraitExpression.Normal);
            Say(adam, "My uncle.");
            Say(adam, "Where is he?");
            Say(vance, "I don't know.");
            Say(adam, "You knew him.");
            Say(vance, "I knew what he refused.");
            Say(adam, "What did he refuse?");
            Say(vance, "The same thing you almost accepted.");
            Say(adam, "What?");
            Narrate("Vance looks at the signed paper.", Stage().CloseUp(signedPaper).Sound(paper));
            Say(vance, "A way out.");
            Narrate("Silence.", Stage().NoCloseUp());
            Narrate("Vance disappears.", Stage().Cast(On(adam, StageSlot.Left)));
            JumpTo(dawn);

            // HUMANITY ENDING — DAWN
            Begin(dawn);
            Narrate("The rain has stopped.", Stage().Fade().Bg(shopDawn).Cast(On(adam, StageSlot.Center)).NoAmbience());
            Narrate("The shop is still standing.");
            Narrate("For the first time, it doesn't feel like a prison.");
            Narrate("Adam picks up the final notice.", Stage().CloseUp(finalNotice));
            Narrate("He folds it.");
            Narrate("Not tears it.");
            Narrate("Not throws it away.");
            Narrate("Just folds it.");
            Portrait(adam, PortraitExpression.Happy);        // SOFT_SMILE
            Say(adam, "I still don't know what I'm going to do with this place.", Stage().NoCloseUp());
            Say(adam, "But I know I'm not going to spend another five years hating him for it.");
            Narrate("Adam steps outside.", Stage().Fade().Bg(shopFrontDawn).NoCast().Sound(door));
            Narrate("The rain has stopped.");
            Narrate("The broken neon flickers once.");
            Narrate("The sign stays on."); // SFX_03_NEON
            Narrate("He never found out where Alexander went.");
            Narrate("But for the first time, Adam stopped asking why he left.");
            Narrate("He started asking what he had left behind.");
            Caption("Some things don't need to be solved in one night.");
            Caption("Some things only need to be faced.");
            EndTheGame();

            // IF CYNICISM IS HIGHER
            Begin(cynicismEnding);
            Portrait(adamMasked, PortraitExpression.Angry);
            Say(adamMasked, "You want to know who I am?");
            Say(vance, "Yes.");
            Say(adamMasked, "I'm tired.");
            Say(adamMasked, "I'm angry.");
            Say(adamMasked, "And I'm done carrying his mistakes.");
            Say(vance, "His mistakes?");
            Say(adamMasked, "The shop.");
            Say(adamMasked, "The debt.");
            Say(adamMasked, "Everything.");
            Say(adamMasked, "He chose this.");
            Say(adamMasked, "I didn't.");
            Narrate("Vance watches him.");
            Say(vance, "Are you sure?");
            Say(adamMasked, "Yes.");
            Say(vance, "Then look again.");
            Portrait(adamMasked, PortraitExpression.Scared); // FRIGHTENED
            Narrate("Adam's reflection now wears Vance's grey coat.", Stage().Flash().Sound(thunder)); // Stand-in for SFX_08_SUPERNATURAL_LOW
            Say(adamMasked, "What did you do?");
            Say(vance, "Nothing.");
            Say(adamMasked, "Then why does it look like you?");
            Say(vance, "It doesn't.");
            Say(vance, "It looks like what you gave it.");
            Narrate("The warm photographs in the shop fade to grey.", Stage().Bg(shopCold));
            Say(adamMasked, "Where is my uncle?");
            Say(vance, "I don't know.");
            Say(adamMasked, "You knew him!");
            Say(vance, "Yes.");
            Say(adamMasked, "Then tell me!");
            Say(vance, "He wanted to sell the shop too.");
            Narrate("Adam freezes.");
            Say(vance, "He just couldn't make himself do it.");
            Say(adamMasked, "So what happened to him?");
            Say(vance, "I don't know.");
            Say(adamMasked, "Then what do you know?");
            Narrate("Vance looks at the signed paper.", Stage().CloseUp(signedPaper).Sound(paper));
            Say(vance, "He said no.");
            Say(adamMasked, "No to what?");
            Say(vance, "The same thing you just said yes to.");
            Narrate("Silence.", Stage().NoCloseUp());
            Narrate("Vance disappears.", Stage().Cast(On(adamMasked, StageSlot.Left)).NoMusic()); // SFX: All BGM cuts.
            JumpTo(finalShot);

            // CYNICISM ENDING — FINAL SHOT
            Begin(finalShot);
            Portrait(adamMasked, PortraitExpression.Normal);
            Narrate("The shop is silent.", Stage().Fade().Bg(shopCold).Cast(On(adamMasked, StageSlot.Center)).NoAmbience()); // No music, no rain
            Narrate("The final notice is still on the counter.");
            Narrate("The clock keeps moving toward nine.");
            Narrate("Adam looks into the mirror.");
            Narrate("His reflection doesn't look away.");
            Caption("Some doors don't close when you leave.");
            Caption("Sometimes they simply wait for someone else to walk through.");
            EndTheGame();
        }

        #endregion

        #region Content set-up

        private void CreateChapters()
        {
            prologue = Chapter("Prologue", "Prologue — October 31", "PROLOGUE", "October 31");
            act1 = Chapter("Act 1", "Act 1 — The Man in the Rain", "ACT 1", "The Man in the Rain");
            act2 = Chapter("Act 2", "Act 2 — The Three Boxes", "ACT 2", "The Three Boxes",
                "The mask will show you the memory you have of the costume in the box and might let you change something from the past.");
            act3 = Chapter("Act 3", "Act 3 — The Final Notice", "ACT 3", "The Final Notice");
        }

        private void CreateConversations()
        {
            p01 = Conversation("Prologue", "P-01 Exterior");
            p02 = Conversation("Prologue", "P-02 Inside the Shop");

            a01 = Conversation("Act 1", "A-01 Vance");
            a01Cynical = Conversation("Act 1", "A-01a Get Out");
            a01Humane = Conversation("Act 1", "A-01b Come Back Tomorrow");
            a02 = Conversation("Act 1", "A-02 The Box");
            a03 = Conversation("Act 1", "A-03 The Ledger");
            a03Humane = Conversation("Act 1", "A-03a Ask About Uncle");
            a03Cynical = Conversation("Act 1", "A-03b No Time");
            a04 = Conversation("Act 1", "A-04 The Offer");
            a04Humane = Conversation("Act 1", "A-04b Wait Until Tomorrow");
            a05 = Conversation("Act 1", "A-05 The Signature");
            a06 = Conversation("Act 1", "A-06 The Mirror");
            a07 = Conversation("Act 1", "A-07 The Inscription");

            b1 = Conversation("Act 2", "B-1 The Cape");
            b1Cynical = Conversation("Act 2", "B-1a Don't Make It");
            b1Humane = Conversation("Act 2", "B-1b Make It");
            b1End = Conversation("Act 2", "B-1c The Cape Is Finished");
            b2 = Conversation("Act 2", "B-2 The Dress");
            b2Humane = Conversation("Act 2", "B-2a Leave the Patch");
            b2Cynical = Conversation("Act 2", "B-2b New Fabric");
            b2End = Conversation("Act 2", "B-2c The Dress Is Finished");
            b3 = Conversation("Act 2", "B-3 The Shop");
            b3Cynical = Conversation("Act 2", "B-3a Should Have Sold It");
            b3Humane = Conversation("Act 2", "B-3b I'm Sorry");
            b3End = Conversation("Act 2", "B-3c The Memory Ends");
            b4 = Conversation("Act 2", "B-4 Return to Present");

            c01 = Conversation("Act 3", "C-01 Vance");
            c02 = Conversation("Act 3", "C-02 The Mask");
            humanityEnding = Conversation("Act 3", "C-03a Humanity");
            dawn = Conversation("Act 3", "C-04a Humanity Ending - Dawn");
            cynicismEnding = Conversation("Act 3", "C-03b Cynicism");
            finalShot = Conversation("Act 3", "C-04b Cynicism Ending - Final Shot");
        }

        private void CreateVariables()
        {
            humanity = Variable("Humanity", 0, "Humane answers add 2. Humanity higher than Cynicism at midnight = the Humanity ending (dawn).");
            cynicism = Variable("Cynicism", 0, "Cynical answers add 3. Otherwise the Cynicism ending (the grey coat).");
            introPlayed = Variable("Flag Intro Played", 0, "1 once the prologue has started (stops the Shop scene replaying it).");
        }

        private void CreateSpeakers()
        {
            Color adamBlue = new Color(.62f, .78f, 1f);

            adam = Speaker("Adam", "Adam", adamBlue, 1560f, -570f,
                (PortraitExpression.Normal, StoryArt.Character(StoryArt.Adam)));
            adamMasked = Speaker("Adam Masked", "Adam", adamBlue, 1560f, -570f,
                (PortraitExpression.Normal, StoryArt.Character(StoryArt.AdamMasked)));
            youngAdam = Speaker("Young Adam", "Young Adam", new Color(.52f, .86f, .8f), 1180f, -400f,
                (PortraitExpression.Normal, StoryArt.Character(StoryArt.YoungAdam)));
            vance = Speaker("Vance", "Mr. Vance", new Color(.84f, .56f, 1f), 1640f, -630f,
                (PortraitExpression.Normal, StoryArt.Character(StoryArt.Vance)));
            alexander = Speaker("Alexander", "Alexander", new Color(1f, .74f, .45f), 1640f, -632f,
                (PortraitExpression.Normal, StoryArt.Character(StoryArt.AlexanderNormal)),
                (PortraitExpression.Happy, StoryArt.Character(StoryArt.AlexanderSmile)),
                (PortraitExpression.Sad, StoryArt.Character(StoryArt.AlexanderCry)));
        }

        private void CreateImages()
        {
            street = Image("Backgrounds", "BG - Street", StoryArt.Background(StoryArt.Street));
            shopFront = Image("Backgrounds", "BG - Shop Front", StoryArt.Background(StoryArt.ShopFront));
            shopWithNotice = Image("Backgrounds", "BG - Shop With Final Notice", StoryArt.Background(StoryArt.ShopWithNotice));
            shop = Image("Backgrounds", "BG - Shop", StoryArt.Background(StoryArt.Shop));
            workshopCape = Image("Backgrounds", "BG - Workshop Cape", StoryArt.Background(StoryArt.WorkshopCape));
            workshopDress = Image("Backgrounds", "BG - Workshop Dress", StoryArt.Background(StoryArt.WorkshopDress));
            shopDawn = Image("Backgrounds", "BG - Shop Dawn", StoryArt.Background(StoryArt.ShopDawn));
            shopCold = Image("Backgrounds", "BG - Shop Cold", StoryArt.Background(StoryArt.ShopCold));

            // Stand-ins until the art exists: the dev scenario's desaturated flashback and dawn exterior
            shopMemory = Image("Backgrounds", "BG - Shop Memory", StoryArt.Background(StoryArt.Shop), new Color(.95f, .9f, .82f), .15f);
            shopFrontDawn = Image("Backgrounds", "BG - Shop Front Dawn", StoryArt.Background(StoryArt.ShopFront), new Color(1f, .88f, .76f));

            finalNotice = Image("Close-ups", "CU - Final Notice", StoryArt.CloseUp(StoryArt.FinalNotice));
            maskBox = Image("Close-ups", "CU - Mask Box", StoryArt.CloseUp(StoryArt.MaskBox));
            maskInBox = Image("Close-ups", "CU - Mask in Box", StoryArt.CloseUp(StoryArt.MaskInBox));
            mask = Image("Close-ups", "CU - Mask", StoryArt.CloseUp(StoryArt.Mask));
            signedPaper = Image("Close-ups", "CU - Signed Paper", StoryArt.CloseUp(StoryArt.SignedPaper));
        }

        private void CreateSounds()
        {
            theme = StoryAudio.MainTheme();
            rainOutside = StoryAudio.RainOutsideLoop();
            rainInside = StoryAudio.RainInsideLoop();
            thunder = StoryAudio.ThunderClap();
            door = StoryAudio.DoorOpenClose();
            maskBoxOpen = StoryAudio.MaskBox();
            paper = StoryAudio.Paper();
            pen = StoryAudio.Pen();
        }

        #endregion

        #region Script helpers

        private void Begin(Dialogue_ConversationSO conversation, Story_ChapterSO chapter = null, bool autosave = false)
        {
            current = conversation;
            conversation.chapter = chapter;
            conversation.autosaveOnStart = autosave;
            conversation.endType = DialogueEndType.End;
            conversation.lines.Clear();
            conversation.choices.Clear();
            conversation.branches.Clear();
            conversation.defaultNext = null;
            conversation.endCard = new TitleCard { title = "THE END" };
        }

        // PORTRAIT: the speaker's face for their next lines. If they're on stage, it changes on the very next line.
        private void Portrait(Dialogue_SpeakerSO speaker, PortraitExpression expression)
        {
            faces[speaker] = expression;

            foreach (StageCharacter character in cast)
            {
                if (character.character == speaker && character.expression != expression)
                {
                    character.expression = expression;
                    changedFaces.Add(speaker);
                }
            }
        }

        private void Say(Dialogue_SpeakerSO speaker, string text, Direction stage = null) =>
            AddLine(speaker, FaceOf(speaker), text, DialogueLineStyle.Box, stage);

        private void Narrate(string text, Direction stage = null) =>
            AddLine(null, PortraitExpression.Normal, text, DialogueLineStyle.Box, stage);

        private void Caption(string text, Direction stage = null) =>
            AddLine(null, PortraitExpression.Normal, text, DialogueLineStyle.Centered, stage);

        private static string Burning(string text) => $"<color={Ember}>{text}</color>";

        private void AddLine(Dialogue_SpeakerSO speaker, PortraitExpression expression, string text, DialogueLineStyle style, Direction stage)
        {
            current.lines.Add(new DialogueLine
            {
                id = $"l{current.lines.Count + 1:00}", // Deterministic, so re-running keeps saved line ids valid
                speaker = speaker,
                expression = expression,
                text = text,
                style = style,
                stage = Direct(stage, speaker)
            });
        }

        // Turns a Direction into the line's StageDirection, and keeps track of who is on stage
        private StageDirection Direct(Direction stage, Dialogue_SpeakerSO speaker)
        {
            StageDirection direction = new StageDirection();
            bool castChanged = false;

            if (stage != null)
            {
                direction.transition = stage.transition;
                direction.background = stage.background;

                if (stage.clearCast || stage.cast != null)
                {
                    cast.Clear();
                    if (stage.cast != null)
                        cast.AddRange(stage.cast);

                    castChanged = true;
                }

                if (stage.clearCloseUp)
                    direction.closeUp = StageChange.Clear;
                else if (stage.closeUp != null)
                {
                    direction.closeUp = StageChange.Set;
                    direction.closeUpImage = stage.closeUp;
                }

                direction.music = stage.music;
                direction.musicTrack = stage.musicTrack;
                direction.ambience = stage.ambience;
                direction.ambienceTrack = stage.ambienceTrack;
                direction.sound = stage.sound;
            }

            // A face change shows up by itself when that character speaks. Anyone else's needs the cast re-set.
            changedFaces.Remove(speaker);
            if (changedFaces.Count > 0)
                castChanged = true;

            changedFaces.Clear();

            if (castChanged)
            {
                direction.characters = cast.Count > 0 ? StageChange.Set : StageChange.Clear;
                direction.cast = cast.Select(c => new StageCharacter { character = c.character, slot = c.slot, expression = c.expression, flip = c.flip }).ToList();
            }

            return direction;
        }

        private StageCharacter On(Dialogue_SpeakerSO speaker, StageSlot slot, bool flip = false) =>
            new StageCharacter { character = speaker, slot = slot, expression = FaceOf(speaker), flip = flip };

        private PortraitExpression FaceOf(Dialogue_SpeakerSO speaker) =>
            faces.TryGetValue(speaker, out PortraitExpression expression) ? expression : PortraitExpression.Normal;

        private static Direction Stage() => new Direction();

        private void Cynical(string text, Dialogue_ConversationSO next) => AddChoice(text, next, cynicism, CynicismPoints);
        private void Humane(string text, Dialogue_ConversationSO next) => AddChoice(text, next, humanity, HumanityPoints);

        private void AddChoice(string text, Dialogue_ConversationSO next, Story_VariableSO variable, int points)
        {
            current.endType = DialogueEndType.Choices;
            current.choices.Add(new DialogueChoice
            {
                id = $"c{current.choices.Count + 1:00}",
                text = text,
                next = next,
                effects = new List<StoryEffect> { new StoryEffect { variable = variable, operation = EffectOperation.Add, value = points } }
            });
        }

        private void JumpTo(Dialogue_ConversationSO next)
        {
            current.endType = DialogueEndType.Jump;
            current.defaultNext = next;
        }

        // Humanity > Cynicism goes to the first, anything else (ties included) to the second
        private void BranchOnHumanity(Dialogue_ConversationSO humane, Dialogue_ConversationSO cynical)
        {
            current.endType = DialogueEndType.Jump;
            current.branches.Add(new DialogueBranch
            {
                conditions = new List<StoryCondition>
                {
                    new StoryCondition { variable = humanity, comparison = ComparisonType.Greater, compareTo = CompareTarget.Variable, otherVariable = cynicism }
                },
                next = humane
            });
            current.defaultNext = cynical;
        }

        private void EndTheGame()
        {
            current.endType = DialogueEndType.EndGame;
            current.endCard = new TitleCard { title = "THE END" };
        }

        #endregion

        #region Assets

        private Dialogue_ConversationSO Conversation(string act, string name)
        {
            Dialogue_ConversationSO conversation = LoadOrCreate<Dialogue_ConversationSO>($"{DialogueFolder}/Conversations/{act}/Conv - {name}.asset");
            written.Add(conversation);
            return conversation;
        }

        private static Story_VariableSO Variable(string name, int defaultValue, string description)
        {
            Story_VariableSO variable = LoadOrCreate<Story_VariableSO>($"{StoryFolder}/Variables/Var - {name}.asset");
            variable.defaultValue = defaultValue;
            variable.description = description;
            EditorUtility.SetDirty(variable);
            return variable;
        }

        private static Story_ChapterSO Chapter(string name, string label, string title, string subtitle, string note = "")
        {
            Story_ChapterSO chapter = LoadOrCreate<Story_ChapterSO>($"{StoryFolder}/Chapters/Chapter - {name}.asset");
            chapter.label = label;
            chapter.titleCard = new TitleCard { title = title, subtitle = subtitle, note = note };
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        private static Dialogue_SpeakerSO Speaker(string assetName, string displayName, Color nameColor, float stageHeight, float stageOffsetY,
            params (PortraitExpression expression, Sprite sprite)[] portraits)
        {
            Dialogue_SpeakerSO speaker = LoadOrCreate<Dialogue_SpeakerSO>($"{DialogueFolder}/Speakers/Speaker - {assetName}.asset");
            speaker.speakerName = displayName;
            speaker.nameColor = nameColor;
            speaker.stageHeight = stageHeight;
            speaker.stageOffsetY = stageOffsetY;
            speaker.defaultPortrait = portraits[0].sprite;
            speaker.portraits.Clear();

            foreach ((PortraitExpression expression, Sprite sprite) in portraits)
                speaker.portraits.Add(new SpeakerPortrait { expression = expression, sprite = sprite });

            EditorUtility.SetDirty(speaker);
            return speaker;
        }

        private static Stage_ImageSO Image(string folder, string name, Sprite sprite, Color? tint = null, float saturation = 1f)
        {
            Stage_ImageSO image = LoadOrCreate<Stage_ImageSO>($"{StageFolder}/{folder}/{name}.asset");
            image.sprite = sprite;
            image.tint = tint ?? Color.white;
            image.saturation = saturation;
            EditorUtility.SetDirty(image);
            return image;
        }

        #endregion
    }

    // What a line does to the stage, written fluently: Stage().Fade().Bg(shop).Cast(On(adam, Left)).Sound(door)
    private class Direction
    {
        public StageTransition transition;
        public Stage_ImageSO background;
        public List<StageCharacter> cast;
        public bool clearCast;
        public Stage_ImageSO closeUp;
        public bool clearCloseUp;
        public StageChange music;
        public Audio_SoundSO musicTrack;
        public StageChange ambience;
        public Audio_SoundSO ambienceTrack;
        public Audio_SoundSO sound;

        public Direction Fade() { transition = StageTransition.Fade; return this; }
        public Direction Flash() { transition = StageTransition.Flash; return this; }
        public Direction Bg(Stage_ImageSO image) { background = image; return this; }
        public Direction Cast(params StageCharacter[] characters) { cast = new List<StageCharacter>(characters); return this; }
        public Direction NoCast() { clearCast = true; return this; }
        public Direction CloseUp(Stage_ImageSO image) { closeUp = image; return this; }
        public Direction NoCloseUp() { clearCloseUp = true; return this; }
        public Direction Music(Audio_SoundSO track) { music = StageChange.Set; musicTrack = track; return this; }
        public Direction NoMusic() { music = StageChange.Clear; return this; }
        public Direction Ambience(Audio_SoundSO track) { ambience = StageChange.Set; ambienceTrack = track; return this; }
        public Direction NoAmbience() { ambience = StageChange.Clear; return this; }
        public Direction Sound(Audio_SoundSO effect) { sound = effect; return this; }
    }
}

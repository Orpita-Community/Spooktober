using NUnit.Framework;

public class TitleCardTests
{
    [Test]
    public void ACardWithOnlyANoteStillShows()
    {
        Assert.IsTrue(new TitleCard().IsEmpty);
        Assert.IsTrue(new TitleCard { title = " ", subtitle = "", note = "\n" }.IsEmpty);
        Assert.IsFalse(new TitleCard { note = "The mask will show you the memory." }.IsEmpty);
    }
}

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

public class SerializableDictionaryTests
{
    [Serializable]
    private class Holder
    {
        public SerializableDictionary<string, int> values = new SerializableDictionary<string, int>();
    }

    [Test]
    public void RoundTripsThroughJson()
    {
        Holder holder = new Holder();
        holder.values["humanity"] = 2;
        holder.values["cynicism"] = -1;

        Holder loaded = JsonUtility.FromJson<Holder>(JsonUtility.ToJson(holder));

        Assert.AreEqual(2, loaded.values.Count);
        Assert.AreEqual(2, loaded.values["humanity"]);
        Assert.AreEqual(-1, loaded.values["cynicism"]);
    }

    [Test]
    public void MismatchedCountsLoadWhatTheyCanInsteadOfThrowing()
    {
        string json = "{\"values\":{\"keys\":[\"a\",\"b\",\"c\"],\"values\":[1,2]}}";

        LogAssert.Expect(LogType.Warning, new Regex("3 keys and 2 values"));
        Holder loaded = JsonUtility.FromJson<Holder>(json);

        Assert.AreEqual(2, loaded.values.Count);
        Assert.AreEqual(2, loaded.values["b"]);
    }

    [Test]
    public void DuplicateKeysKeepTheLastValue()
    {
        string json = "{\"values\":{\"keys\":[\"a\",\"a\"],\"values\":[1,5]}}";

        Holder loaded = JsonUtility.FromJson<Holder>(json);

        Assert.AreEqual(1, loaded.values.Count);
        Assert.AreEqual(5, loaded.values["a"]);
    }
}

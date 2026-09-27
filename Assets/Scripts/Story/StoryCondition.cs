using System;

[Serializable]
public class StoryCondition
{
    public Story_VariableSO variable;
    public ComparisonType comparison;
    public CompareTarget compareTo;
    public int constant;
    public Story_VariableSO otherVariable; // Used when compareTo is Variable, e.g. Humanity > Cynicism
}

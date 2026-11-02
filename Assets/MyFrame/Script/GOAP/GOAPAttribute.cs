using System;

[AttributeUsage(AttributeTargets.Class,Inherited = false)]
public class GOAPActionAttribute : Attribute
{
    public string ActionName;

    public GOAPActionAttribute(string actionName)
    {
        ActionName = actionName;
    }
}

[AttributeUsage(AttributeTargets.Class,Inherited = false)]
public class GOAPCheckerAttribute : Attribute
{
    public string CheckerName;

    public GOAPCheckerAttribute(string checkerName)
    {
        CheckerName = checkerName;
    }
}


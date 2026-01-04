using System;

namespace fbognini.EfCoreLocalization;


[AttributeUsage(AttributeTargets.Class)]
public class LocalizationKeyAttribute : Attribute
{
    public string Key;

    public LocalizationKeyAttribute(string key)
    {
        Key = key;
    }
}

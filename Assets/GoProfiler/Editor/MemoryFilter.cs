using System.Collections.Generic;
using System;
using GoProfiler;
[Serializable]
public class RegexElement
{
    public string key;
    public List<string> regexList = new List<string>();
    public RegexElement() { }
}
[Serializable]
public class MemoryFilter
{
	public ClassIDMap classID;
    public List<RegexElement> regexElementList = new List<RegexElement>();
}
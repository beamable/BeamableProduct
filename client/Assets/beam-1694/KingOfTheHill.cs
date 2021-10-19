using System;
using System.Collections.Generic;
using Beamable;
using Beamable.Common.Content;

[Serializable]
[Agnostic]
public class KingOfTheHillRef : ContentRef<KingOfTheHill>
{
}

[Serializable]
[Agnostic]
public class KingOfTheHillLink : ContentLink<KingOfTheHill>
{
}


[Serializable]
[Agnostic]
[ContentType("KingOfTheHill")]
public class KingOfTheHill : ContentB
{
    public string id;
    public string version;
    public Properties properties;
}

[Serializable]
public class MaxPlayers
{
    public int data;
}

[Serializable]
public class Datum
{
    public string name;
    public int maxPlayers;
    public string property;
    public int maxDelta;
}

[Serializable]
public class Teams
{
    public List<Datum> data;
}

[Serializable]
public class NumericRules
{
    public List<Datum> data;
}

[Serializable]
public class StringRules
{
    public List<string> data;
}

[Serializable]
public class LeaderboardUpdates
{
    public List<string> data;
}

[Serializable]
public class Rewards
{
    public List<string> data;
}

[Serializable]
public class Properties
{
    public MaxPlayers maxPlayers;
    public Teams teams;
    public NumericRules numericRules;
    public StringRules stringRules;
    public LeaderboardUpdates leaderboardUpdates;
    public Rewards rewards;
}
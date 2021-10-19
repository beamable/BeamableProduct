using Beamable;
using Beamable.Common.Content;

[System.Serializable]
[Agnostic]
public class ContentBRef : ContentRef<ContentB> { }

[System.Serializable]
[Agnostic]
public class ContentBLink : ContentLink<ContentB> { }


[System.Serializable]
[Agnostic]
public class ContentB : ContentObject
{
   // public KingOfTheHillLink KingOfTheHillLink;
   // public KingOfTheHillRef KingOfTheHillRef;
   
   
}


using Beamable.Common.Content;
using UnityEngine;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
    /// <summary>
    /// A <see cref="SimGameType"/> content that defines the data for the draft system. Only supports same-size teams.  
    /// </summary>
    [ContentType("DraftSimType")]
    public class DraftSimType : SimGameType
    {
        [Tooltip("The number of players in each team.")]
        public int TeamSize;

        [Tooltip("The total number of draft picks. Usually, this  will be \"= TeamSize x 2\".")]
        public int TotalNumberOfDraftPicks;
        
        [Tooltip("For each draft pick (0 ~ TotalNumberOfDraftPicks), define which team (0-NumberOfTeams) owns the pick.")]
        public int[] PerPickIdxTeams;
        
        [Tooltip("For each draft pick (0 ~ TotalNumberOfDraftPicks), define which player in a team (0-TeamSize) owns the pick.")]
        public int[] PerPickIdxPlayerIdx;
    }

    /// <summary>
    /// <see cref="ContentRef{TContent}"/> declaration to resolving a <see cref="DraftSimType"/> on-demand (at runtime).
    /// </summary>
    [System.Serializable]
    public class DraftSimTypeRef : ContentRef<DraftSimType> { }

}

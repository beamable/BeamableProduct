using Beamable.Common.Leaderboards;
using UnityEngine;


/// <summary>
/// This is a scriptable object with a property that represents the <see cref="BeamableDefaultLeaderboardSystem"/>'s necessary configuration data.
/// TODO: I'm still unsure whether <see cref="BeamableDefaultLeaderboardSystem.SerializableConfig"/> should be declared in the <see cref="IBeamableView"/> implementing type or in the underlying system.
/// TODO: I'm confident they should be a separate type from the separate object if they remain in the system, but if we stop thinking about this as:
/// TODO:   - These are system specific configurations.
/// TODO: And start thinking about them as:
/// TODO:   - These are just a convenient way to configure a view with data it needs to correctly communicate with it's <see cref="IBeamableViewDeps"/> (such as <see cref="LeaderboardView.ILeaderboardDeps"/>)...
/// TODO:     I think this may be a better avenue.
/// TODO: What I'm worried about is that by making this a system declaration we will be forcing all <see cref="IBeamableViewDeps"/> concrete implementations to have to know about this type...
/// TODO: Or, even worse, make the <see cref="IBeamableViewDeps"/>'s API dependent on these types in some way...
/// </summary>
[CreateAssetMenu(menuName = "Beamable Systems/Leaderboard/Basic", fileName = "BasicBeamableLeaderboardSystemConfig", order = 0)]
public class LeaderboardViewFeatureConfig : ScriptableObject
{
    public LeaderboardRef LeaderboardRef;
    public int EntriesAmount;
    [Header("Debug")] public bool TestMode;
}

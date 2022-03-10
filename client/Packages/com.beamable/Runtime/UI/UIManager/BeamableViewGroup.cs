using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable;
using Beamable.Common;
using Beamable.Common.Dependencies;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Playables;


/// <summary>
/// This is the common interface that the <see cref="BeamableViewGroup"/> talks too when it's <see cref="BeamContext"/> are configured on start or <see cref="BeamableViewGroup.Enrich"/>
/// (or <see cref="BeamableViewGroup.EnrichWithPlayerCodes"/>) gets called via code.
///
/// The underlying type should control one View and be a <see cref="MonoBehaviour"/>.
/// In a game-specific way, this means one Scene/Prefab --- any group of Visual game objects and components that together solve the problem of "rendering my specific game's interactivity layer (UX)".
///
/// Every <see cref="IBeamableView"/> will have nested interface types (such as <see cref="LeaderboardView.IViewDependencies"/> that must implement <see cref="IBeamableViewDeps"/>) declaring
/// it's dependencies. It expects these dependencies to have been registered with <see cref="BeamContextSystemAttribute"/> and <see cref="Beamable.Common.Dependencies.RegisterBeamableDependenciesAttribute"/>.
/// In your user code, you can ignore this part and not declare any <see cref="IBeamableViewDeps"/>. Instead, you can depend directly on your game-specific types if you wish so, making this process simpler.
///
/// To enable customization of the <see cref="IBeamableView"/>s that ship with Beamable, we declare these interfaces as we believe there's value provided by their existence.
/// Our future marketplace users may wish to do so too.
/// </summary>
public interface IBeamableView
{
    /// <summary>
    /// A read-only property that defines the intent of the person declaring whether or not the <see cref="BeamableViewGroup"/> knows what to do with multiple <see cref="BeamContext"/>.
    /// </summary>
    BeamableViewGroup.PlayerCountMode SupportedMode { get; }

    /// <summary>
    /// <see cref="BeamableViewGroup"/> sorts it's managed <see cref="IBeamableView"/> by this value before calling Enrich.
    /// </summary>
    int GetEnrichOrder();

}

/// <summary>
/// Implement this interface if your enrich method can be synchronous OR a fire-and-forget (async void) method.
/// </summary>
public interface ISyncBeamableView : IBeamableView
{
    /// <summary>
    /// Gets called by <see cref="BeamableViewGroup"/> on start or <see cref="BeamableViewGroup.Enrich"/> (or <see cref="BeamableViewGroup.EnrichWithPlayerCodes"/>) gets called via code.
    /// This version is called when the <see cref="SupportedMode"/> is set to <see cref="BeamableViewGroup.PlayerCountMode.SinglePlayerUI"/>.
    /// If you don't explicitly support <see cref="BeamableViewGroup.PlayerCountMode.SinglePlayerUI"/>, throw a <see cref="NotSupportedException"/> from this implementation.
    /// </summary>
    /// <param name="currentContext">The <see cref="BeamContext"/> for the player that is currently set as <see cref="BeamableViewGroup.MainPlayerCode"/>.</param>
    void EnrichWithContext(BeamContext currentContext);

    /// <summary>
    /// Gets called by <see cref="BeamableViewGroup"/> on start or <see cref="BeamableViewGroup.Enrich"/> (or <see cref="BeamableViewGroup.EnrichWithPlayerCodes"/>) gets called via code.
    /// This version is called once per <see cref="BeamableViewGroup.AllPlayerContexts"/> when the <see cref="SupportedMode"/> is set to <see cref="BeamableViewGroup.PlayerCountMode.MultiplayerUI"/>.
    /// If you don't explicitly support <see cref="BeamableViewGroup.PlayerCountMode.MultiplayerUI"/>, throw a <see cref="NotSupportedException"/> from this implementation.
    /// </summary>
    /// <param name="currentContext">The <see cref="BeamContext"/> at the current <paramref name="playerIndex"/>.</param>
    /// <param name="playerIndex">The index for this <see cref="BeamContext"/> in <see cref="BeamableViewGroup.AllPlayerContexts"/>.</param>
    void EnrichWithContext(BeamContext currentContext, int playerIndex);
}

/// <summary>
/// Implement this interface if your enrich should be awaited on before releasing control to the next <see cref="IBeamableView"/> in <see cref="BeamableViewGroup.ManagedViews"/>.
/// </summary>
public interface IAsyncBeamableView : IBeamableView
{
    /// <summary>
    /// Gets called by <see cref="BeamableViewGroup"/> on start or <see cref="BeamableViewGroup.Enrich"/> (or <see cref="BeamableViewGroup.EnrichWithPlayerCodes"/>) gets called via code.
    /// This version is called when the <see cref="SupportedMode"/> is set to <see cref="BeamableViewGroup.PlayerCountMode.SinglePlayerUI"/>.
    /// If you don't explicitly support <see cref="BeamableViewGroup.PlayerCountMode.SinglePlayerUI"/>, throw a <see cref="NotSupportedException"/> from this implementation.
    /// </summary>
    /// <param name="currentContext">The <see cref="BeamContext"/> for the player that is currently set as <see cref="BeamableViewGroup.MainPlayerCode"/>.</param>
    Promise EnrichWithContext(BeamContext currentContext);

    /// <summary>
    /// Gets called by <see cref="BeamableViewGroup"/> on start or <see cref="BeamableViewGroup.Enrich"/> (or <see cref="BeamableViewGroup.EnrichWithPlayerCodes"/>) gets called via code.
    /// This version is called once per <see cref="BeamableViewGroup.AllPlayerContexts"/> when the <see cref="SupportedMode"/> is set to <see cref="BeamableViewGroup.PlayerCountMode.MultiplayerUI"/>.
    /// If you don't explicitly support <see cref="BeamableViewGroup.PlayerCountMode.MultiplayerUI"/>, throw a <see cref="NotSupportedException"/> from this implementation.
    /// </summary>
    /// <param name="currentContext">The <see cref="BeamContext"/> at the current <paramref name="playerIndex"/>.</param>
    /// <param name="playerIndex">The index for this <see cref="BeamContext"/> in <see cref="BeamableViewGroup.AllPlayerContexts"/>.</param>
    Promise EnrichWithContext(BeamContext currentContext, int playerIndex);
}

/// <summary>
/// This is just a simple tag interface that all views must declare.
/// These are what the views should use <see cref="BeamContext.ServiceProvider"/> to get.
/// By doing this, you can easily swap out implementations via <see cref="BeamContextSystemAttribute"/>s and <see cref="Beamable.Common.Dependencies.RegisterBeamableDependenciesAttribute"/>,
/// adding data to existing systems keeping the UI.
/// </summary>
public interface IBeamableViewDeps
{
}

/// <summary>
/// This is a Beamable Component you can add to any <see cref="GameObject"/> you have to gain access to it's API that:
/// <list type="bullet">
/// <item>
/// Can be used to manage which <see cref="BeamContext"/>s are being used to get the data that will populate this view (which player's data should I be displaying?)
/// </item>
/// <item>
/// Can be used to rebuild and modify the UI dynamically as new players are added and removed.
/// Think about a Party UI in couch co-op games --- it takes X data from each player and displays it as player's "press start to join".
/// </item>
/// <item>
/// Can be used to define and change who is the main player driving data for the UI.
/// This is useful in cases where we want to swap between a main player's PoV with other players being secondary, but you want players to select which player is currently the Main PoV.
/// Think about score screens in couch co-op games where you could display the winner's scores and stat's more prominently and allow swapping between others.  
/// </item>
/// </list>
///
/// This is meant to provide additive behaviour and work with other <see cref="MonoBehaviour"/> scripts that implement <see cref="IBeamableView"/> in the same <see cref="GameObject"/> hierarchy.
/// </summary>
public class BeamableViewGroup : MonoBehaviour
{
    /// <summary>
    /// Defines the two primary ways you can use <see cref="BeamContext"/>s to get data into your <see cref="IBeamableView"/>s using the <see cref="BeamableViewGroup"/>.
    /// </summary>
    public enum PlayerCountMode
    {
        /// <summary>
        /// Single player exists for this UI --- it ignores any other BeamContexts it has other than Main
        /// </summary>
        SinglePlayerUI,

        /// <summary>
        /// Assumes information from multiple players populate this View.
        /// Think MMO Item Trade UIs, Victory/Defeat UIs and others.
        /// </summary>
        MultiplayerUI,
    }

    /// <summary>
    /// List of all <see cref="BeamContext"/>s currently being used by this <see cref="BeamableViewGroup"/> to get it's dependencies from.
    /// </summary>
    public List<BeamContext> AllPlayerContexts;

    /// <summary>
    /// List of all <see cref="BeamContext.PlayerCode"/> that are used to identify <see cref="BeamContext"/>s within Beamable's player-centric SDK.
    /// </summary>
    public List<string> AllPlayerCodes;

    /// <summary>
    /// List of all <see cref="IBeamableView"/>s that exist as children of the <see cref="GameObject"/> holding this <see cref="BeamableViewGroup"/>.
    /// If you add/remove <see cref="IBeamableView"/> components from this hierarchy, call <see cref="RebuildManagedViews"/> and then <see cref="Enrich"/> to make sure each <see cref="IBeamableView"/>
    /// sees those changes.
    /// </summary>
    public List<IBeamableView> ManagedViews;

    /// <summary>
    /// The player that is defined as the "Main" player (the focused player).
    /// TODO: Think about whether this concept of "Main" player should be left to each individual <see cref="IBeamableView"/> implementation or not????
    /// </summary>
    public int MainPlayerIdx { get; set; } = 0;

    /// <summary>
    /// Accessor that returns the "Main" <see cref="BeamContext.PlayerCode"/> currently being used by the <see cref="IBeamableView"/>s managed by this component.
    /// TODO: Think about whether this concept of "Main" player should be left to each individual <see cref="IBeamableView"/> implementation or not????
    /// </summary>
    public string MainPlayerCode => AllPlayerCodes[MainPlayerIdx];

    /// <summary>
    /// Accessor that returns the "Main" <see cref="BeamContext"/> currently being used by the <see cref="IBeamableView"/>s managed by this component.
    /// TODO: Think about whether this concept of "Main" player should be left to each individual <see cref="IBeamableView"/> implementation or not????
    /// </summary>
    public BeamContext MainBeamContext => AllPlayerContexts[MainPlayerIdx];

    /// <summary>
    /// Accessor that returns all <see cref="BeamContext"/> that aren't the "Main" <see cref="BeamContext"/> currently being used by the <see cref="IBeamableView"/>s managed by this component.
    /// TODO: Think about whether this concept of "Main" player should be left to each individual <see cref="IBeamableView"/> implementation or not????
    /// </summary>
    public IEnumerable<BeamContext> SecondaryContexts => AllPlayerContexts.Except(new[] { AllPlayerContexts[MainPlayerIdx] });

    /// <summary>
    /// Whether or not this <see cref="BeamableViewGroup"/> instance should always rebuild when it gets enabled.
    /// If you turn this off, remember that: You must manually call <see cref="RebuildManagedViews"/> and either <see cref="Enrich"/> or <see cref="EnrichWithPlayerCodes"/> to
    /// initialize this <see cref="BeamableViewGroup"/>. 
    /// </summary>
    public bool RebuildOnEnable = true;

    /// <summary>
    /// Whether or not this component should rebuild itself whenever it becomes enabled/disabled.
    /// </summary>
    public virtual async void OnEnable()
    {
        if (!RebuildOnEnable) return;

        RebuildManagedViews();

        // TODO: This is not True. Instead of this, add a hint that informs users the potential consequences of doing this, but we shouldn't constrain this.
        // TODO: If we don't, the Main + Secondary mode can be used to implement an Inspector view that can display information about content's owner, independent of who is the main player.
        Assert.IsTrue(AllPlayerCodes.Distinct().ToList().Count == AllPlayerCodes.Count, "All Player Codes must be unique for this thing to work.");

        var playerCodes = AllPlayerCodes;
        await RebuildPlayerContexts(playerCodes);
        EnrichWithPlayerCodes();
    }

    /// <summary>
    /// Rebuilds the list of managed <see cref="IBeamableView"/>s in this <see cref="BeamableViewGroup"/>'s <see cref="GameObject"/> hierarchy.  
    /// </summary>
    /// <param name="enrich">Whether or not you want to call <see cref="Enrich"/> after the list is updated.</param>
    public void RebuildManagedViews(bool enrich = false)
    {
        ManagedViews = GetComponentsInChildren(typeof(IBeamableView), true)
            .Cast<IBeamableView>()
            .ToList();

        if (enrich) Enrich();
    }

    /// <summary>
    /// Ensures that the <see cref="AllPlayerContexts"/> match the currently set <see cref="AllPlayerCodes"/> and that they are <see cref="BeamContext.OnReady"/>.
    /// Then, goes through all <see cref="ManagedViews"/> and calls either <see cref="IBeamableView.EnrichWithContext(Beamable.BeamContext)"/> or
    /// <see cref="IBeamableView.EnrichWithContext(Beamable.BeamContext, int)"/> based on <see cref="IBeamableView.SupportedMode"/>.
    /// </summary>
    public virtual async void Enrich()
    {
        ManagedViews.Sort((v1, v2) => v1.GetEnrichOrder().CompareTo(v2.GetEnrichOrder()));
        
        // For every view we have, call their appropriate EnrichWithContext function based on their supported mode.  
        foreach (var beamableUIView in ManagedViews)
        {
            var mode = beamableUIView.SupportedMode;
            // Based On UI Behaviour Type we invoke a couple of different callbacks
            switch (mode)
            {
                // In case the UI was built by taking information from a single player.
                case PlayerCountMode.SinglePlayerUI:
                {
                    if (AllPlayerContexts.Count > 1) // TODO: change this to hint.
                        Debug.LogWarning("Single player UI that is using multiple contexts!!! Maybe change this if you are not using them to reduce overhead.");

                    switch (beamableUIView)
                    {
                        case ISyncBeamableView syncBeamableView:
                            syncBeamableView.EnrichWithContext(MainBeamContext);
                            break;
                        case IAsyncBeamableView asyncBeamableView:
                            await asyncBeamableView.EnrichWithContext(MainBeamContext);
                            break;
                    }
                    break;
                }

                // In case the UI was built by taking information from two sets of players
                case PlayerCountMode.MultiplayerUI:
                {
                    switch (beamableUIView)
                    {
                        case ISyncBeamableView syncBeamableView:
                        {
                            for (var i = 0; i < AllPlayerContexts.Count; i++)
                                syncBeamableView.EnrichWithContext(AllPlayerContexts[i], i);
                            break;
                        }
                        case IAsyncBeamableView asyncBeamableView:
                        {
                            for (var i = 0; i < AllPlayerContexts.Count; i++)
                                await asyncBeamableView.EnrichWithContext(AllPlayerContexts[i], i);
                            break;
                        }
                    }

                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// Works the same as <see cref="Enrich"/>, but calls and awaits <see cref="RebuildPlayerContexts"/> before enriching.
    /// </summary>
    /// <param name="newPlayerCodes"></param>
    public async void EnrichWithPlayerCodes(List<string> newPlayerCodes = null)
    {
        // Rebuild the Player Contexts --- will do nothing if  newPlayerCodes is null or empty.
        await RebuildPlayerContexts(newPlayerCodes);
        Enrich();
    }

    /// <summary>
    /// Rebuilds the <see cref="AllPlayerCodes"/> and <see cref="AllPlayerContexts"/> codes based on the given <paramref name="playerCodes"/>.
    /// </summary>
    /// <param name="playerCodes">New <see cref="BeamContext.PlayerCode"/> representing the <see cref="BeamContext"/> that this View should get it's data from.</param>
    public async Task RebuildPlayerContexts(List<string> playerCodes)
    {
        AllPlayerCodes = playerCodes == null || playerCodes.Count == 0 ? AllPlayerCodes : playerCodes;
        AllPlayerContexts = AllPlayerCodes.Select(playerCode => playerCode == null ? BeamContext.Default : BeamContext.ForPlayer(playerCode)).ToList();
        foreach (var allPlayerContext in AllPlayerContexts)
            await allPlayerContext.OnReady;
    }
}


public static class BeamableDependencyInjectionExtensions
{
    public static void ReplaceAsSingleton<TInterface, TImplementation>(this IDependencyBuilder builder) where TImplementation : TInterface
    {
        builder.RemoveIfExists<TInterface>();
        builder.AddSingleton<TInterface, TImplementation>();
    }

    public static void ReplaceAsScoped<TInterface, TImplementation>(this IDependencyBuilder builder) where TImplementation : TInterface
    {
        builder.RemoveIfExists<TInterface>();
        builder.AddScoped<TInterface, TImplementation>();
    }

    public static void ReplaceAsTransient<TInterface, TImplementation>(this IDependencyBuilder builder) where TImplementation : TInterface
    {
        builder.RemoveIfExists<TInterface>();
        builder.AddTransient<TInterface, TImplementation>();
    }

    public static void RedirectAsSingleton<TInterface, TImplementation>(this IDependencyBuilder builder) where TImplementation : TInterface
    {
        builder.RemoveIfExists<TInterface>();
        builder.AddSingleton<TInterface, TImplementation>((provider) => provider.GetService<TImplementation>());
    }

    public static void RedirectAsScoped<TInterface, TImplementation>(this IDependencyBuilder builder) where TImplementation : TInterface
    {
        builder.RemoveIfExists<TInterface>();
        builder.AddScoped<TInterface, TImplementation>((provider) => provider.GetService<TImplementation>());
    }

    public static void RedirectAsTransient<TInterface, TImplementation>(this IDependencyBuilder builder) where TImplementation : TInterface
    {
        builder.RemoveIfExists<TInterface>();
        builder.AddTransient<TInterface, TImplementation>((provider) => provider.GetService<TImplementation>());
    }

    public static void SetupUnderlyingSystemSingleton<TUnderlyingSystem, TI1>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1
    {
        builder.AddSingleton<TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI1, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemScoped<TUnderlyingSystem, TI1>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1
    {
        builder.AddScoped<TUnderlyingSystem>();
        builder.RedirectAsScoped<TI1, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemTransient<TUnderlyingSystem, TI1>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1
    {
        builder.AddTransient<TUnderlyingSystem>();
        builder.RedirectAsTransient<TI1, TUnderlyingSystem>();
    }
    


    public static void SetupUnderlyingSystemSingleton<TUnderlyingSystem, TI1, TI2>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2
    {
        builder.AddSingleton<TUnderlyingSystem>();

        builder.RedirectAsSingleton<TI1, TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI2, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemScoped<TUnderlyingSystem, TI1, TI2>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2
    {
        builder.AddScoped<TUnderlyingSystem>();

        builder.RedirectAsScoped<TI1, TUnderlyingSystem>();
        builder.RedirectAsScoped<TI2, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemTransient<TUnderlyingSystem, TI1, TI2>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2
    {
        builder.AddTransient<TUnderlyingSystem>();

        builder.RedirectAsTransient<TI1, TUnderlyingSystem>();
        builder.RedirectAsTransient<TI2, TUnderlyingSystem>();
    }


    public static void SetupUnderlyingSystemSingleton<TUnderlyingSystem, TI1, TI2, TI3>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2, TI3
    {
        builder.AddSingleton<TUnderlyingSystem>();

        builder.RedirectAsSingleton<TI1, TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI2, TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI3, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemScoped<TUnderlyingSystem, TI1, TI2, TI3>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2, TI3
    {
        builder.AddScoped<TUnderlyingSystem>();

        builder.RedirectAsScoped<TI1, TUnderlyingSystem>();
        builder.RedirectAsScoped<TI2, TUnderlyingSystem>();
        builder.RedirectAsScoped<TI3, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemTransient<TUnderlyingSystem, TI1, TI2, TI3>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2, TI3
    {
        builder.AddTransient<TUnderlyingSystem>();

        builder.RedirectAsTransient<TI1, TUnderlyingSystem>();
        builder.RedirectAsTransient<TI2, TUnderlyingSystem>();
        builder.RedirectAsTransient<TI3, TUnderlyingSystem>();
    }


    public static void SetupUnderlyingSystemSingleton<TUnderlyingSystem, TI1, TI2, TI3, TI4>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2, TI3, TI4
    {
        builder.AddSingleton<TUnderlyingSystem>();

        builder.RedirectAsSingleton<TI1, TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI2, TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI3, TUnderlyingSystem>();
        builder.RedirectAsSingleton<TI4, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemScoped<TUnderlyingSystem, TI1, TI2, TI3, TI4>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2, TI3, TI4
    {
        builder.AddScoped<TUnderlyingSystem>();

        builder.RedirectAsScoped<TI1, TUnderlyingSystem>();
        builder.RedirectAsScoped<TI2, TUnderlyingSystem>();
        builder.RedirectAsScoped<TI3, TUnderlyingSystem>();
        builder.RedirectAsScoped<TI4, TUnderlyingSystem>();
    }

    public static void SetupUnderlyingSystemTransient<TUnderlyingSystem, TI1, TI2, TI3, TI4>(this IDependencyBuilder builder) where TUnderlyingSystem : TI1, TI2, TI3, TI4
    {
        builder.AddTransient<TUnderlyingSystem>();

        builder.RedirectAsTransient<TI1, TUnderlyingSystem>();
        builder.RedirectAsTransient<TI2, TUnderlyingSystem>();
        builder.RedirectAsTransient<TI3, TUnderlyingSystem>();
        builder.RedirectAsTransient<TI4, TUnderlyingSystem>();
    }
}

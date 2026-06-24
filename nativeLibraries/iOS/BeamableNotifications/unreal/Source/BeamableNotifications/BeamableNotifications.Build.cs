using System.IO;
using UnrealBuildTool;

public class BeamableNotifications : ModuleRules
{
    public BeamableNotifications(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(new string[]
        {
            "Core", "CoreUObject", "Engine", "Json"
        });

        if (Target.Platform == UnrealTargetPlatform.IOS)
        {
            // Link the prebuilt Swift core. Build the DYNAMIC framework with
            // scripts/build-xcframework-dynamic.sh → ThirdParty/BeamableNotifications.embeddedframework.zip.
            // It is dynamic (install name @rpath/...), so it must be EMBEDDED into the app bundle
            // (FrameworkMode.LinkAndCopy) — otherwise dyld fails at launch with
            // "Library not loaded: @rpath/BeamableNotifications.framework/BeamableNotifications".
            PublicAdditionalFrameworks.Add(new Framework(
                "BeamableNotifications",
                Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "BeamableNotifications.embeddedframework.zip"),
                Framework.FrameworkMode.LinkAndCopy
            ));

            // The UPL injects entitlements (push, App Group), background modes, and the
            // Notification Service Extension into the generated Xcode project.
            AdditionalPropertiesForReceipt.Add("IOSPlugin",
                Path.Combine(ModuleDirectory, "IOS", "BeamableNotifications_UPL.xml"));

            PublicFrameworks.AddRange(new string[] { "UserNotifications" });
        }
    }
}

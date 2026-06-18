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
            // Link the prebuilt Swift core. UE expects an xcframework delivered as a .zip.
            // Build it with scripts/build-xcframework.sh, then zip the .xcframework into
            // ThirdParty/ as BeamableNotifications.embeddedframework.zip.
            PublicAdditionalFrameworks.Add(new Framework(
                "BeamableNotifications",
                Path.Combine(ModuleDirectory, "..", "..", "ThirdParty", "BeamableNotifications.embeddedframework.zip"),
                null,
                false   // static xcframework: link, don't embed as a bundle
            ));

            // The UPL injects entitlements (push, App Group), background modes, and the
            // Notification Service Extension into the generated Xcode project.
            AdditionalPropertiesForReceipt.Add("IOSPlugin",
                Path.Combine(ModuleDirectory, "IOS", "BeamableNotifications_UPL.xml"));

            PublicFrameworks.AddRange(new string[] { "UserNotifications" });
        }
    }
}

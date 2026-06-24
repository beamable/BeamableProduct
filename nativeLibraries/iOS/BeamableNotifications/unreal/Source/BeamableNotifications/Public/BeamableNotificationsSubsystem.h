#pragma once

#include "CoreMinimal.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "BeamableNotificationsSubsystem.generated.h"

/// Normalized notification payload delivered to Blueprints. `RawJson` carries the full
/// payload (including arbitrary userInfo) for advanced use; common fields are lifted out.
USTRUCT(BlueprintType)
struct FBMNNotificationData
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString Id;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString Title;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString Body;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString Subtitle;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString DeepLink;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString ActionId;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") bool bWasLaunch = false;
    UPROPERTY(BlueprintReadOnly, Category = "Notifications") FString RawJson;
};

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FBMNOnPermissionResult, bool, bGranted, const FString&, Status);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FBMNOnString, const FString&, Value);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FBMNOnNotification, const FBMNNotificationData&, Notification);

/// Blueprint-facing API and event hub for BeamableNotifications. As a
/// UGameInstanceSubsystem it has a clear lifetime and is easy to reach from Blueprints
/// via "Get Game Instance Subsystem".
UCLASS()
class BEAMABLENOTIFICATIONS_API UBeamableNotificationsSubsystem : public UGameInstanceSubsystem
{
    GENERATED_BODY()

public:
    virtual void Initialize(FSubsystemCollectionBase& Collection) override;
    virtual void Deinitialize() override;

    // Events (feature 3)
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnPermissionResult OnPermissionResult;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnString OnTokenReceived;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnString OnTokenError;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnNotification OnNotificationPresented;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnNotification OnNotificationReceived;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnNotification OnNotificationTapped;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnString OnPendingNotifications;
    UPROPERTY(BlueprintAssignable, Category = "Notifications") FBMNOnString OnDeliveryReceipts;

    // Permission (feature 5)
    UFUNCTION(BlueprintCallable, Category = "Notifications")
    void RequestPermission(bool bAlert = true, bool bBadge = true, bool bSound = true);

    UFUNCTION(BlueprintCallable, Category = "Notifications")
    void GetPermissionStatus();

    // Local notifications (feature 1)
    UFUNCTION(BlueprintCallable, Category = "Notifications")
    void ScheduleLocalNotification(const FString& Id, const FString& Title, const FString& Body,
                                   float DelaySeconds = 0.f, const FString& DeepLink = TEXT(""),
                                   const FString& CategoryId = TEXT(""));

    /// Advanced: schedule from a full LocalRequest JSON string (see docs for the schema).
    UFUNCTION(BlueprintCallable, Category = "Notifications")
    void ScheduleLocalJson(const FString& RequestJson);

    UFUNCTION(BlueprintCallable, Category = "Notifications") void CancelLocal(const FString& Id);
    UFUNCTION(BlueprintCallable, Category = "Notifications") void CancelAllLocal();
    UFUNCTION(BlueprintCallable, Category = "Notifications") void GetPending();

    // Remote notifications (feature 2)
    UFUNCTION(BlueprintCallable, Category = "Notifications") void RegisterForRemote();
    UFUNCTION(BlueprintCallable, Category = "Notifications") void UnregisterForRemote();

    // Templates / categories / delivery receipts (features 4, 7)
    UFUNCTION(BlueprintCallable, Category = "Notifications") void RegisterTemplateJson(const FString& TemplateJson);
    UFUNCTION(BlueprintCallable, Category = "Notifications") void RegisterCategoryJson(const FString& CategoryJson);
    UFUNCTION(BlueprintCallable, Category = "Notifications") void GetDeliveryReceipts();

    // Badge
    UFUNCTION(BlueprintCallable, Category = "Notifications") void SetBadge(int32 Count);
    UFUNCTION(BlueprintCallable, Category = "Notifications") void ClearDelivered();

    // Get intent (feature 6)
    UFUNCTION(BlueprintCallable, Category = "Notifications")
    bool GetLaunchNotification(FBMNNotificationData& OutNotification);

    /// The active instance, used by the C-callback trampolines. Set in Initialize.
    static UBeamableNotificationsSubsystem* Active;

    // Internal: invoked (on the game thread) by the native callback trampolines.
    void HandlePermission(const FString& Json);
    void HandleTokenReceived(const FString& Json);
    void HandleTokenError(const FString& Json);
    void HandlePresented(const FString& Json);
    void HandleReceived(const FString& Json);
    void HandleTapped(const FString& Json);
    void HandlePending(const FString& Json);
    void HandleReceipts(const FString& Json);

    static FBMNNotificationData ParseNotification(const FString& Json);
};

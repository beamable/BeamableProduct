#include "BeamableNotificationsSubsystem.h"
#include "Async/Async.h"
#include "Dom/JsonObject.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"

UBeamableNotificationsSubsystem* UBeamableNotificationsSubsystem::Active = nullptr;

// ---------------------------------------------------------------------------
// C ABI imported from the Swift core (see BeamableNotifications.h). On non-iOS
// platforms these are stubbed so the module still compiles for the editor.
// ---------------------------------------------------------------------------
#if PLATFORM_IOS
extern "C" {
    typedef void (*bmn_callback)(const char* json);
    void bmn_initialize();
    void bmn_requestPermission(const char* optionsJson);
    void bmn_getPermissionStatus();
    void bmn_scheduleLocal(const char* requestJson);
    void bmn_cancelLocal(const char* id);
    void bmn_cancelAllLocal();
    void bmn_getPending();
    void bmn_registerForRemote();
    void bmn_unregisterForRemote();
    void bmn_configureAnalytics(const char* configJson);
    void bmn_getDeliveryReceipts();
    void bmn_registerTemplate(const char* templateJson);
    void bmn_registerCategory(const char* categoryJson);
    void bmn_setBadge(int count);
    void bmn_clearDelivered();
    const char* bmn_getLaunchNotification();
    void bmn_free(const char* ptr);
    void bmn_setOnPermissionResult(bmn_callback cb);
    void bmn_setOnTokenReceived(bmn_callback cb);
    void bmn_setOnTokenError(bmn_callback cb);
    void bmn_setOnNotificationPresented(bmn_callback cb);
    void bmn_setOnNotificationReceived(bmn_callback cb);
    void bmn_setOnNotificationTapped(bmn_callback cb);
    void bmn_setOnPendingNotifications(bmn_callback cb);
    void bmn_setOnDeliveryReceipts(bmn_callback cb);
}

// Trampolines: marshal the C string to FString and hop to the game thread before
// touching UObjects / broadcasting Blueprint delegates.
namespace
{
    void Bounce(const char* json, void (UBeamableNotificationsSubsystem::*handler)(const FString&))
    {
        FString Payload = json ? UTF8_TO_TCHAR(json) : TEXT("");
        AsyncTask(ENamedThreads::GameThread, [Payload, handler]()
        {
            if (UBeamableNotificationsSubsystem::Active)
            {
                (UBeamableNotificationsSubsystem::Active->*handler)(Payload);
            }
        });
    }
}

static void CB_Permission(const char* j) { Bounce(j, &UBeamableNotificationsSubsystem::HandlePermission); }
static void CB_TokenRecv(const char* j)  { Bounce(j, &UBeamableNotificationsSubsystem::HandleTokenReceived); }
static void CB_TokenErr(const char* j)   { Bounce(j, &UBeamableNotificationsSubsystem::HandleTokenError); }
static void CB_Presented(const char* j)  { Bounce(j, &UBeamableNotificationsSubsystem::HandlePresented); }
static void CB_Received(const char* j)   { Bounce(j, &UBeamableNotificationsSubsystem::HandleReceived); }
static void CB_Tapped(const char* j)     { Bounce(j, &UBeamableNotificationsSubsystem::HandleTapped); }
static void CB_Pending(const char* j)    { Bounce(j, &UBeamableNotificationsSubsystem::HandlePending); }
static void CB_Receipts(const char* j)   { Bounce(j, &UBeamableNotificationsSubsystem::HandleReceipts); }
#endif // PLATFORM_IOS

// Small helper so call sites stay tidy regardless of platform.
#if PLATFORM_IOS
  #define BMN_CALL(expr) expr
  #define BMN_CSTR(fstr) (const char*)TCHAR_TO_UTF8(*(fstr))
#else
  #define BMN_CALL(expr)
  #define BMN_CSTR(fstr) ((const char*)nullptr)
#endif

void UBeamableNotificationsSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
    Super::Initialize(Collection);
    Active = this;

#if PLATFORM_IOS
    bmn_setOnPermissionResult(&CB_Permission);
    bmn_setOnTokenReceived(&CB_TokenRecv);
    bmn_setOnTokenError(&CB_TokenErr);
    bmn_setOnNotificationPresented(&CB_Presented);
    bmn_setOnNotificationReceived(&CB_Received);
    bmn_setOnNotificationTapped(&CB_Tapped);
    bmn_setOnPendingNotifications(&CB_Pending);
    bmn_setOnDeliveryReceipts(&CB_Receipts);
    bmn_initialize();
#endif
}

void UBeamableNotificationsSubsystem::Deinitialize()
{
    if (Active == this) Active = nullptr;
    Super::Deinitialize();
}

// MARK: API

void UBeamableNotificationsSubsystem::RequestPermission(bool bAlert, bool bBadge, bool bSound)
{
    const FString Json = FString::Printf(
        TEXT("{\"alert\":%s,\"badge\":%s,\"sound\":%s}"),
        bAlert ? TEXT("true") : TEXT("false"),
        bBadge ? TEXT("true") : TEXT("false"),
        bSound ? TEXT("true") : TEXT("false"));
    BMN_CALL(bmn_requestPermission(BMN_CSTR(Json)));
}

void UBeamableNotificationsSubsystem::GetPermissionStatus()
{
    BMN_CALL(bmn_getPermissionStatus());
}

void UBeamableNotificationsSubsystem::ScheduleLocalNotification(const FString& Id, const FString& Title,
    const FString& Body, float DelaySeconds, const FString& DeepLink, const FString& CategoryId)
{
    TSharedRef<FJsonObject> Root = MakeShared<FJsonObject>();
    Root->SetStringField(TEXT("id"), Id);
    Root->SetStringField(TEXT("title"), Title);
    Root->SetStringField(TEXT("body"), Body);
    if (!CategoryId.IsEmpty()) Root->SetStringField(TEXT("categoryId"), CategoryId);

    TSharedRef<FJsonObject> Trigger = MakeShared<FJsonObject>();
    if (DelaySeconds > 0.f)
    {
        Trigger->SetStringField(TEXT("type"), TEXT("timeInterval"));
        Trigger->SetNumberField(TEXT("seconds"), DelaySeconds);
        Trigger->SetBoolField(TEXT("repeats"), false);
    }
    else
    {
        Trigger->SetStringField(TEXT("type"), TEXT("immediate"));
    }
    Root->SetObjectField(TEXT("trigger"), Trigger);

    if (!DeepLink.IsEmpty())
    {
        TSharedRef<FJsonObject> UserInfo = MakeShared<FJsonObject>();
        UserInfo->SetStringField(TEXT("deepLink"), DeepLink);
        Root->SetObjectField(TEXT("userInfo"), UserInfo);
    }

    FString Out;
    TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Out);
    FJsonSerializer::Serialize(Root, Writer);
    ScheduleLocalJson(Out);
}

void UBeamableNotificationsSubsystem::ScheduleLocalJson(const FString& RequestJson)
{
    BMN_CALL(bmn_scheduleLocal(BMN_CSTR(RequestJson)));
}

void UBeamableNotificationsSubsystem::CancelLocal(const FString& Id) { BMN_CALL(bmn_cancelLocal(BMN_CSTR(Id))); }
void UBeamableNotificationsSubsystem::CancelAllLocal() { BMN_CALL(bmn_cancelAllLocal()); }
void UBeamableNotificationsSubsystem::GetPending() { BMN_CALL(bmn_getPending()); }
void UBeamableNotificationsSubsystem::RegisterForRemote() { BMN_CALL(bmn_registerForRemote()); }
void UBeamableNotificationsSubsystem::UnregisterForRemote() { BMN_CALL(bmn_unregisterForRemote()); }

void UBeamableNotificationsSubsystem::RegisterTemplateJson(const FString& TemplateJson) { BMN_CALL(bmn_registerTemplate(BMN_CSTR(TemplateJson))); }
void UBeamableNotificationsSubsystem::RegisterCategoryJson(const FString& CategoryJson) { BMN_CALL(bmn_registerCategory(BMN_CSTR(CategoryJson))); }
void UBeamableNotificationsSubsystem::ConfigureAnalyticsJson(const FString& ConfigJson) { BMN_CALL(bmn_configureAnalytics(BMN_CSTR(ConfigJson))); }
void UBeamableNotificationsSubsystem::GetDeliveryReceipts() { BMN_CALL(bmn_getDeliveryReceipts()); }

void UBeamableNotificationsSubsystem::SetBadge(int32 Count) { BMN_CALL(bmn_setBadge((int)Count)); }
void UBeamableNotificationsSubsystem::ClearDelivered() { BMN_CALL(bmn_clearDelivered()); }

bool UBeamableNotificationsSubsystem::GetLaunchNotification(FBMNNotificationData& OutNotification)
{
#if PLATFORM_IOS
    const char* ptr = bmn_getLaunchNotification();
    if (!ptr) return false;
    FString Json = UTF8_TO_TCHAR(ptr);
    bmn_free(ptr);
    if (Json.IsEmpty()) return false;
    OutNotification = ParseNotification(Json);
    return true;
#else
    return false;
#endif
}

// MARK: Inbound handlers (game thread)

void UBeamableNotificationsSubsystem::HandlePermission(const FString& Json)
{
    TSharedPtr<FJsonObject> Obj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Json);
    if (FJsonSerializer::Deserialize(Reader, Obj) && Obj.IsValid())
    {
        OnPermissionResult.Broadcast(Obj->GetBoolField(TEXT("granted")), Obj->GetStringField(TEXT("status")));
    }
}

void UBeamableNotificationsSubsystem::HandleTokenReceived(const FString& Json)
{
    TSharedPtr<FJsonObject> Obj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Json);
    if (FJsonSerializer::Deserialize(Reader, Obj) && Obj.IsValid())
    {
        OnTokenReceived.Broadcast(Obj->GetStringField(TEXT("token")));
    }
}

void UBeamableNotificationsSubsystem::HandleTokenError(const FString& Json)
{
    TSharedPtr<FJsonObject> Obj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Json);
    if (FJsonSerializer::Deserialize(Reader, Obj) && Obj.IsValid())
    {
        OnTokenError.Broadcast(Obj->GetStringField(TEXT("error")));
    }
}

void UBeamableNotificationsSubsystem::HandlePresented(const FString& Json) { OnNotificationPresented.Broadcast(ParseNotification(Json)); }
void UBeamableNotificationsSubsystem::HandleReceived(const FString& Json)  { OnNotificationReceived.Broadcast(ParseNotification(Json)); }
void UBeamableNotificationsSubsystem::HandleTapped(const FString& Json)    { OnNotificationTapped.Broadcast(ParseNotification(Json)); }
void UBeamableNotificationsSubsystem::HandlePending(const FString& Json)   { OnPendingNotifications.Broadcast(Json); }
void UBeamableNotificationsSubsystem::HandleReceipts(const FString& Json)  { OnDeliveryReceipts.Broadcast(Json); }

FBMNNotificationData UBeamableNotificationsSubsystem::ParseNotification(const FString& Json)
{
    FBMNNotificationData Data;
    Data.RawJson = Json;

    TSharedPtr<FJsonObject> Obj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Json);
    if (FJsonSerializer::Deserialize(Reader, Obj) && Obj.IsValid())
    {
        Obj->TryGetStringField(TEXT("id"), Data.Id);
        Obj->TryGetStringField(TEXT("title"), Data.Title);
        Obj->TryGetStringField(TEXT("body"), Data.Body);
        Obj->TryGetStringField(TEXT("subtitle"), Data.Subtitle);
        Obj->TryGetStringField(TEXT("deepLink"), Data.DeepLink);
        Obj->TryGetStringField(TEXT("actionId"), Data.ActionId);
        Obj->TryGetBoolField(TEXT("wasLaunch"), Data.bWasLaunch);
    }
    return Data;
}

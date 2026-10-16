#include <CoreFoundation/CoreFoundation.h>
#include <Security/Security.h>
#include <TargetConditionals.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#define BSS_EXPORT __attribute__((visibility("default")))
#define BSS_KEY_LENGTH 64

static void BSS_Clear(void *data, size_t length)
{
	volatile uint8_t *bytes = (volatile uint8_t *)data;
	while (length--) { *bytes++ = 0; }
}

static CFMutableDictionaryRef BSS_Query(const char *service, const char *account)
{
	CFStringRef serviceString = CFStringCreateWithCString(NULL, service, kCFStringEncodingUTF8);
	CFStringRef accountString = CFStringCreateWithCString(NULL, account, kCFStringEncodingUTF8);
	if (!serviceString || !accountString)
	{
		if (serviceString) { CFRelease(serviceString); }
		if (accountString) { CFRelease(accountString); }
		return NULL;
	}
	CFMutableDictionaryRef query = CFDictionaryCreateMutable(NULL, 0, &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
	if (query)
	{
		CFDictionarySetValue(query, kSecClass, kSecClassGenericPassword);
		CFDictionarySetValue(query, kSecAttrService, serviceString);
		CFDictionarySetValue(query, kSecAttrAccount, accountString);
		CFDictionarySetValue(query, kSecAttrSynchronizable, kCFBooleanFalse);
	}
	CFRelease(serviceString);
	CFRelease(accountString);
	return query;
}

static OSStatus BSS_Read(CFMutableDictionaryRef query, uint8_t **output, int *length)
{
	CFDictionarySetValue(query, kSecReturnData, kCFBooleanTrue);
	CFDictionarySetValue(query, kSecMatchLimit, kSecMatchLimitOne);
	CFTypeRef result = NULL;
	OSStatus status = SecItemCopyMatching(query, &result);
	CFDictionaryRemoveValue(query, kSecReturnData);
	CFDictionaryRemoveValue(query, kSecMatchLimit);
	if (status == errSecSuccess)
	{
		if (!result || CFGetTypeID(result) != CFDataGetTypeID() || CFDataGetLength((CFDataRef)result) != BSS_KEY_LENGTH)
		{
			status = errSecDecode;
		}
		else
		{
			*output = (uint8_t *)malloc(BSS_KEY_LENGTH);
			if (!*output) { status = errSecAllocate; }
			else
			{
				memcpy(*output, CFDataGetBytePtr((CFDataRef)result), BSS_KEY_LENGTH);
				*length = BSS_KEY_LENGTH;
			}
		}
	}
	if (result) { CFRelease(result); }
	return status;
}

BSS_EXPORT int BSS_GetKey(const char *service, const char *account, int allowCreate, uint8_t **output, int *length)
{
	if (!service || !account || !output || !length) { return errSecParam; }
	*output = NULL;
	*length = 0;
	CFMutableDictionaryRef query = BSS_Query(service, account);
	if (!query) { return errSecAllocate; }
	OSStatus status = BSS_Read(query, output, length);
	if (status == errSecItemNotFound && allowCreate)
	{
		uint8_t key[BSS_KEY_LENGTH];
		status = SecRandomCopyBytes(kSecRandomDefault, sizeof(key), key);
		if (status == errSecSuccess)
		{
			CFDataRef data = CFDataCreate(NULL, key, sizeof(key));
			if (!data) { status = errSecAllocate; }
			else
			{
				CFDictionarySetValue(query, kSecValueData, data);
#if TARGET_OS_IPHONE
				// Available for background flushes after the first unlock, bound to this device.
				CFDictionarySetValue(query, kSecAttrAccessible, kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly);
#endif
				status = SecItemAdd(query, NULL);
				CFDictionaryRemoveValue(query, kSecValueData);
				CFDictionaryRemoveValue(query, kSecAttrAccessible);
				CFRelease(data);
				if (status == errSecSuccess || status == errSecDuplicateItem)
				{
					status = BSS_Read(query, output, length);
				}
			}
		}
		BSS_Clear(key, sizeof(key));
	}
	CFRelease(query);
	return (int)status;
}

BSS_EXPORT void BSS_FreeKey(uint8_t *data, int length)
{
	if (data)
	{
		if (length > 0) { BSS_Clear(data, (size_t)length); }
		free(data);
	}
}

// Bind an existing key to a stable identity without ever replacing a different item.
BSS_EXPORT int BSS_ImportKey(const char *service, const char *account, const uint8_t *key, int length)
{
	if (!service || !account || !key || length != BSS_KEY_LENGTH) { return errSecParam; }
	CFMutableDictionaryRef query = BSS_Query(service, account);
	if (!query) { return errSecAllocate; }
	CFDataRef data = CFDataCreate(NULL, key, length);
	if (!data) { CFRelease(query); return errSecAllocate; }
	CFDictionarySetValue(query, kSecValueData, data);
#if TARGET_OS_IPHONE
	CFDictionarySetValue(query, kSecAttrAccessible, kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly);
#endif
	OSStatus status = SecItemAdd(query, NULL);
	CFRelease(data);
	CFRelease(query);
	return status == errSecDuplicateItem ? errSecSuccess : (int)status;
}

BSS_EXPORT int BSS_DeleteKey(const char *service, const char *account)
{
	if (!service || !account) { return errSecParam; }
	CFMutableDictionaryRef query = BSS_Query(service, account);
	if (!query) { return errSecAllocate; }
	OSStatus status = SecItemDelete(query);
	CFRelease(query);
	return (int)status;
}

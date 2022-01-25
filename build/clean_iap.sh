
#!/bin/sh
echo "Unity=[${UNITY_VERSION}]. Checking to see if we should destroy the Unity IAP Plugin, because it gets auto-generated in later versions of Unity."

if [ "$UNITY_VERSION" != "2018.4.36f1" ]
then
    echo "Deleting Unity IAP Plugin folder"
    rm -rf ./client/Assets/Plugins/UnityPurchasing
    rm -f ./client/Assets/Plugins/UnityPurchasing.meta
else
    echo "Leaving Unity IAP Plugin alone"
fi

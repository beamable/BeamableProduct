
#!/bin/sh
echo "Hello world"
echo "UNITY ${UNITY_VERSION}"

if [ "$UNITY_VERSION" != "2018.4.18f1" ]
then
    echo "Deleting Unity IAP Plugin folder"
    rm -rf ./client/Assets/Plugins/UnityPurchasing
else
    echo "Leaving Unity IAP Plugin alone"
fi

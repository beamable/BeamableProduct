
#!/bin/sh
echo "Unity=[${UNITY_VERSION}]. Checking to see which mainTemplate.gradle file we should use based on Unity version."

if [ "$UNITY_VERSION" == "2020.3.19f1" ]
then
    echo "Using the 2020 gradle templates, and deleting the other ones..."
    rename -f ./client/Assets/Plugins/Android/gradleTemplate.2020.gradle ./client/Assets/Plugins/Android/gradleTemplate.gradle
    rename -f ./client/Assets/Plugins/Android/gradleTemplate.2020.properties ./client/Assets/Plugins/Android/gradleTemplate.properties
    rm -f ./client/Assets/Plugins/Android/gradleTemplate.2020.gradle.meta
    rm -f ./client/Assets/Plugins/Android/gradleTemplate.2020.properties.meta
else
    echo "Leaving gradle files alone"
fi

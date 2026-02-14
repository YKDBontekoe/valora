plugins {
    id("com.android.application")
    // id("kotlin-android") // Removed as it is now built-in with AGP 9+
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

android {
    namespace = "nl.valora.valora_app"
    compileSdk = 36
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    // kotlinOptions block is deprecated/removed in newer AGP versions with built-in Kotlin support.
    // JVM target is usually inferred from compileOptions, or configured via kotlin { ... } if needed.
    // For AGP 9 migration with simple setup, we might not need this block if compileOptions is set.
    // Or we try the new syntax if required. For now, commenting it out as per error.
    // kotlinOptions {
    //    jvmTarget = JavaVersion.VERSION_17.toString()
    // }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = "nl.valora.valora_app"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = 34
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    buildTypes {
        release {
            // TODO: Add your own signing config for the release build.
            // Signing with the debug keys for now, so  works.
            signingConfig = signingConfigs.getByName("debug")
        }
    }
}

flutter {
    source = "../.."
}

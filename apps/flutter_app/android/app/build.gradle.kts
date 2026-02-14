import java.util.Properties
import java.io.FileInputStream

plugins {
    id("com.android.application")
    // id("kotlin-android") // Removed as it is now built-in with AGP 9+
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val keystoreProperties = Properties()
val keystorePropertiesFile = rootProject.file("key.properties")
var hasValidSigningConfig = false

if (keystorePropertiesFile.exists()) {
    FileInputStream(keystorePropertiesFile).use { keystoreProperties.load(it) }
    val requiredProperties = listOf("keyAlias", "keyPassword", "storeFile", "storePassword")
    val missingProperties = requiredProperties.filter { keystoreProperties.getProperty(it).isNullOrBlank() }

    if (missingProperties.isEmpty()) {
        hasValidSigningConfig = true
    } else {
        project.logger.warn("key.properties exists but is missing required properties: ${missingProperties.joinToString()}. Falling back to debug signing.")
    }
}

android {
    namespace = "nl.valora.valora_app"
    compileSdk = 36
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    // defaultConfig { ... } remains same
    defaultConfig {
        applicationId = "nl.valora.valora_app"
        minSdk = flutter.minSdkVersion
        targetSdk = 34
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        create("release") {
            keyAlias = keystoreProperties.getProperty("keyAlias")
            keyPassword = keystoreProperties.getProperty("keyPassword")
            storeFile = keystoreProperties.getProperty("storeFile")?.let { file(it) }
            storePassword = keystoreProperties.getProperty("storePassword")
        }
    }

    buildTypes {
        release {
            signingConfig = if (hasValidSigningConfig) {
                signingConfigs.getByName("release")
            } else {
                signingConfigs.getByName("debug")
            }
        }
    }
}

flutter {
    source = "../.."
}

allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")

    // Force all plugins to use the same compileSdk as the app
    // Use plugins.withId to safely configure plugins when they are applied
    project.plugins.withId("com.android.application") {
        project.extensions.configure<com.android.build.gradle.BaseExtension> {
            compileSdkVersion(36)
        }
    }
    project.plugins.withId("com.android.library") {
        project.extensions.configure<com.android.build.gradle.BaseExtension> {
            compileSdkVersion(36)
        }
    }
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}

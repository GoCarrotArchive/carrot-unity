require "rake/clean"
CLEAN.include "**/.DS_Store"

desc "Build libraries and copy into Unity project"
task :default

#
# Helper methods
#

CARROT_JAR = File.expand_path("../carrot-android/build/Carrot.jar")

def android_java?
  # Return true if we should build the Android jar
  ENV["ANDROID_SDK"] && File.exist?(ENV["ANDROID_SDK"])
end

def ios?
  # Return true if we should build the iOS libraries.
  true
end

def unity(*args)
  # Run Unity.
  sh "/Applications/Unity/Unity.app/Contents/MacOS/Unity #{args.join(' ')}"
end

def unity?
  # Return true if we can run Unity.
  File.exist? "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
end

#
# Android build tasks
#

if android_java?
  task :default => "android:jar"
else
  puts "WARNING: Not building for Android."
end

namespace :android do
  desc "Build Android jar"
  task :jar do
    chdir "../carrot-android" do
      sh "ant compile && ant jar"
    end
  end
end

#
# iOS build tasks
#

if ios?
  task :default => "ios:library"
else
  puts "WARNING: Not building for iOS."
end

namespace :ios do
  desc "Build the iOS library"
  task :library do
    chdir "../carrot-ios" do
      sh "xcodebuild -alltargets -project Carrot-iOS.xcodeproj"
      sh [
        "/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/lipo",
        "-create",
        "build/Release-iphoneos/libCarrot.a",
        "build/Release-iphonesimulator/libCarrot.a",
        "-output build/libCarrot.a"
      ].join(" ")
    end
  end
end

#
# Unity build tasks
#

task :default => "unity:package"

desc "Build Unity Package"
task :unity => "unity:package"
namespace :unity do
  task :package do
    # Copy stuff in
    if ios?
      cp "../carrot-ios/build/libCarrot.a", "Assets/Plugins/iOS/libCarrot.a"
      cp "../carrot-ios/Src/Carrot.h", "Assets/Plugins/iOS/Carrot.h"
    end

    if android_java?
      cp "#{CARROT_JAR}", "Assets/Plugins/Android/Carrot.jar"
    end

    # Build .unitypackage
    project_path = File.expand_path("./")
    package_path = File.expand_path("./Carrot.unitypackage")
    mv "#{project_path}/Assets/Example", "#{project_path}/Assets/.Example"
    unity "-quit -batchmode -projectPath #{project_path} -exportPackage Assets #{package_path}"
    mv "#{project_path}/Assets/.Example", "#{project_path}/Assets/Example"
  end
end

require "rake/clean"
CLEAN.include "**/.DS_Store"

desc "Build Unity package"
task :default

#
# Helper methods
#
def unity(*args)
  # Run Unity.
  sh "/Applications/Unity/Unity.app/Contents/MacOS/Unity #{args.join(' ')}"
end

def unity?
  # Return true if we can run Unity.
  File.exist? "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
end

# Docs task
DOXYGEN_BINARY = "/Applications/Doxygen.app/Contents/Resources/doxygen"

def doxygen?
  return if not File.exist?(DOXYGEN_BINARY)
  return true
end

if doxygen?
  task :docs do
    sh DOXYGEN_BINARY
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
    project_path = File.expand_path("./")
    package_path = File.expand_path("./Carrot.unitypackage")
    mv "#{project_path}/Assets/Example", "#{project_path}/Assets/.Example"
    begin
      unity "-quit -batchmode -projectPath #{project_path} -exportPackage Assets #{package_path}"
    rescue
      puts "Unity build failed."
    end
    mv "#{project_path}/Assets/.Example", "#{project_path}/Assets/Example"
  end
end

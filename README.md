# Building
You will need clones of carrot-ios and carrot-android as well as this repo to build the unitypackage from source.

# Android
Add or modify Assets/Plugins/Android/AndroidManifest.xml to include the following code in the `<application>` section:

	<service android:name="org.openudid.OpenUDID_service">
		<intent-filter>
			<action android:name="org.openudid.GETUDID" />
		</intent-filter>
	</service>

# iOS
Ensure the following frameworks are linked in the generated Xcode project:
* FacebookSDK.framework
* libsqlite3.dylib
* Accounts.framework
* Social.framework
* AdSupport.framework
* SystemConfiguration.framework (Should be included by Unity)

Add this line to the main() function in your 'main.mm' file: `[Carrot plant:@"your_app_id" inApplication:NSClassFromString(@"AppController") withSecret:@"your_app_secret"];` after the line: `NSAutoreleasePool* pool = [NSAutoreleasePool new];` Also add `#import "../Libraries/Carrot.h"` to the top of the file.

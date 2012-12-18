/* Carrot -- Copyright (C) 2012 Carrot Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#import "Carrot.h"

extern void UnitySendMessage(const char* objectName, const char* methodName, const char* message);

@interface UnityCarrotDelegate : NSObject <CarrotDelegate>
@property (strong, nonatomic) NSString* objectName;
@end

@implementation UnityCarrotDelegate

+ (UnityCarrotDelegate*)sharedInstance
{
   static UnityCarrotDelegate* sharedInstance = nil;
   static dispatch_once_t onceToken;
   dispatch_once(&onceToken, ^{
      sharedInstance = [[UnityCarrotDelegate alloc] init];
   });
   return sharedInstance;
}

- (void)authenticationStatusChanged:(int)status withError:(NSError*)error
{
   UnitySendMessage([_objectName UTF8String], "authenticationStatusChanged",
                    [[NSString stringWithFormat:@"%d", status] UTF8String]);
}

- (void)applicationLinkRecieved:(NSURL*)targetURL
{
   UnitySendMessage([_objectName UTF8String], "applicationLinkRecieved",
                    [[targetURL absoluteString] UTF8String]);
}

@end

void Carrot_AssignUnityDelegate(const char* objectName)
{
   [UnityCarrotDelegate sharedInstance].objectName = [NSString stringWithUTF8String:objectName];
   [Carrot sharedInstance].delegate = [UnityCarrotDelegate sharedInstance];
}

void Carrot_GetUserAchievementsUnity(const char* objectName)
{
   [[Carrot sharedInstance] getUserAchievementsEx:^(NSHTTPURLResponse* response, NSData* data) {
      UnitySendMessage(objectName, "userAchievementListReceived",
         [[[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] UTF8String]);
   }];
}

void Carrot_GetFriendScoresUnity(const char* objectName)
{
   [[Carrot sharedInstance] getFriendScoresEx:^(NSHTTPURLResponse* response, NSData* data) {
      UnitySendMessage(objectName, "friendHighScoresReceived",
         [[[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] UTF8String]);
   }];
}

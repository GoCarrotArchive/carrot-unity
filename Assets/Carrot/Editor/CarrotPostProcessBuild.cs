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

using PlistCS;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;

public static class CarrotPostProcessBuild
{
   [PostProcessBuild]
   public static void OnPostProcessBuild(BuildTarget target, string path)
   {
      if(target == BuildTarget.iPhone)
      {
         #region Modifications to 'Info.plist'
         string plistPath = path + "/Info.plist";
         Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist(plistPath);
         if(plist != null)
         {
            // FacebookAppID
            if(!plist.ContainsKey("FacebookAppID"))
            {
               plist.Add("FacebookAppID", CarrotSettings.CarrotAppId);
            }

            // Change 'identifier' to 'rfc1034identifier'
            string bundleId = (string)plist["CFBundleIdentifier"];
            plist["CFBundleIdentifier"] = bundleId.Replace(":identifier", ":rfc1034identifier");

            // Facebook/Carrot URL handlers for deep-linking and < iOS6 SSO
            List<object> bundleURLArray = null;
            object tempObj = null;
            if(!plist.TryGetValue("CFBundleURLTypes", out tempObj))
            {
               bundleURLArray = new List<object>();
               plist.Add("CFBundleURLTypes", bundleURLArray);
            }
            else
            {
               bundleURLArray = (List<object>)tempObj;
            }

            Dictionary<string, object> bundleURLSchemes = null;
            if(bundleURLArray.Count > 0)
            {
               bundleURLSchemes = (Dictionary<string, object>)bundleURLArray[0];
            }
            else
            {
               bundleURLSchemes = new Dictionary<string, object>();
               bundleURLArray.Add(bundleURLSchemes);
            }

            List<object> urlSchemeArray = null;
            if(!bundleURLSchemes.TryGetValue("CFBundleURLSchemes", out tempObj))
            {
               urlSchemeArray = new List<object>();
               bundleURLSchemes.Add("CFBundleURLSchemes", urlSchemeArray);
            }
            else
            {
               urlSchemeArray = (List<object>)tempObj;
            }

            string fbURLScheme = "fb" + CarrotSettings.CarrotAppId;
            if(!urlSchemeArray.Contains(fbURLScheme))
            {
               urlSchemeArray.Add(fbURLScheme);
            }

            string ctURLScheme = "carrot" + CarrotSettings.CarrotAppId;
            if(!urlSchemeArray.Contains(ctURLScheme))
            {
               urlSchemeArray.Add(ctURLScheme);
            }

            Plist.writeXml(plist, plistPath);
         }
         #endregion

         #region Modifications to 'Classes/main.mm'
         string mainmmPath = path + "/Classes/main.mm";
         string tempMainmmPath = mainmmPath + ".tmp";
         FileStream tempMainmm = File.Open(tempMainmmPath, FileMode.Create, FileAccess.Write);
         StreamWriter mainmmWriter = new StreamWriter(tempMainmm);
         bool mainModified = false;
         foreach(string line in File.ReadAllLines(mainmmPath))
         {
            if(line.Contains("// Carrot Modifications"))
            {
               mainModified = true;
               mainmmWriter.WriteLine(line);
            }
            else if(line.Contains("// Hack to work") && !mainModified)
            {
               mainmmWriter.WriteLine("// Carrot Modifications\n#include \"../Libraries/Carrot.h\"");
               mainmmWriter.WriteLine("#define CARROT_APP_ID @\"" + CarrotSettings.CarrotAppId + "\"");
               mainmmWriter.WriteLine("#define CARROT_APP_SECRET @\"" + CarrotSettings.CarrotAppSecret + "\"\n");
               mainmmWriter.WriteLine(line);
            }
            else if(line.Contains("[NSAutoreleasePool new]") && !mainModified)
            {
               mainmmWriter.WriteLine(line);
               mainmmWriter.WriteLine("   [Carrot plant:CARROT_APP_ID");
               mainmmWriter.WriteLine("   inApplication:NSClassFromString(@\"AppController\")");
               mainmmWriter.WriteLine("      withSecret:CARROT_APP_SECRET];");
            }
            else if(line.Contains("#define CARROT_APP_ID"))
            {
               mainmmWriter.WriteLine("#define CARROT_APP_ID @\"" + CarrotSettings.CarrotAppId + "\"");
            }
            else if(line.Contains("#define CARROT_APP_SECRET"))
            {
               mainmmWriter.WriteLine("#define CARROT_APP_SECRET @\"" + CarrotSettings.CarrotAppSecret + "\"");
            }
            else
            {
               mainmmWriter.WriteLine(line);
            }
         }

         mainmmWriter.Close();
         FileUtil.ReplaceFile(tempMainmmPath, mainmmPath);
         #endregion

         #region Modify Xcode Project
         System.Diagnostics.Process p = new System.Diagnostics.Process();
         p.StartInfo.UseShellExecute = false;
         p.StartInfo.RedirectStandardOutput = true;
         p.StartInfo.FileName = "python";
         p.StartInfo.Arguments = System.String.Format("-B \"{0}\" -i \"{1}\"",
            Application.dataPath + "/Carrot/Editor/Python/CarrotXcodeFrameworks.py",
            path + "/Unity-iPhone.xcodeproj/project.pbxproj");
         p.Start();
         string output = p.StandardOutput.ReadToEnd().Trim();
         if(!System.String.IsNullOrEmpty(output))
         {
            Debug.LogWarning("Xcode Project Updated, you must save/reload the Xcode project in order for Append builds to work from Unity.\n" + output);
         }
         p.WaitForExit();
         #endregion
      }
   }
}

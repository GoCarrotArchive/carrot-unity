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

public static class CarrotPostBuildTrigger
{
   public static string FacebookAppID = "280914862014150";
   public static string CarrotAppSecret = "";

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
               Debug.Log("Adding FacebookAppID to '" + plistPath + "'");
               plist.Add("FacebookAppID", FacebookAppID);
            }

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

            string fbURLScheme = "fb" + FacebookAppID;
            if(!urlSchemeArray.Contains(fbURLScheme))
            {
               Debug.Log("Adding Facebook URL scheme to '" + plistPath + "'");
               urlSchemeArray.Add(fbURLScheme);
            }

            string ctURLScheme = "carrot" + FacebookAppID;
            if(!urlSchemeArray.Contains(ctURLScheme))
            {
               Debug.Log("Adding Carrot URL scheme to '" + plistPath + "'");
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
         bool moveTempMainmm = true;
         foreach(string line in File.ReadAllLines(mainmmPath))
         {
            mainmmWriter.WriteLine(line);
            if(line.Contains("RegisterMonoModules.h"))
            {
               mainmmWriter.WriteLine("#include \"../Libraries/Carrot.h\"");
            }
            else if(line.Contains("[NSAutoreleasePool new]"))
            {
               mainmmWriter.WriteLine("   [Carrot plant:@\"" + FacebookAppID + "\"");
               mainmmWriter.WriteLine("   inApplication:NSClassFromString(@\"AppController\")");
               mainmmWriter.WriteLine("      withSecret:@\"" + CarrotAppSecret + "\"];");
            }
            else if(line.Contains("Carrot"))
            {
               mainmmWriter.Close();
               File.Delete(tempMainmmPath);
               moveTempMainmm = false;
               break;
            }
         }

         if(moveTempMainmm)
         {
            mainmmWriter.Close();
            File.Delete(mainmmPath);
            File.Move(tempMainmmPath, mainmmPath);
         }

         #endregion
      }
   }
}

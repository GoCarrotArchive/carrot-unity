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

using System.IO;
using UnityEngine;
using UnityEditor;

public class CarrotSettings : EditorWindow
{
   public static string CarrotAppId
   {
      get
      {
         LoadSettings();
         return mCarrotAppId;
      }
   }

   public static string CarrotAppSecret
   {
      get
      {
         LoadSettings();
         return mCarrotAppSecret;
      }
   }

   [MenuItem("Edit/Carrot")]
   public static void ShowWindow()
   {
      CarrotSettings settingsWindow = (CarrotSettings)GetWindow<CarrotSettings>(false, "Carrot Settings", false);
      settingsWindow.Show();
   }

   void OnGUI()
   {
      GUILayout.Label("Settings", EditorStyles.boldLabel);
      mCarrotAppId = EditorGUILayout.TextField("Carrot App Id", mCarrotAppId);
      mCarrotAppSecret = EditorGUILayout.TextField("Carrot App Secret", mCarrotAppSecret);

      if(!CarrotPostProcessScene.WillCreatePrefab)
      {
         if(GUILayout.Button("Create Carrot GameObject", GUILayout.Height(25)))
         {
            mCarrotGameObject = GameObject.Find("CarrotGameObject");
            if(mCarrotGameObject == null)
            {
               Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Carrot/CarrotGameObject.prefab", typeof(GameObject));
               mCarrotGameObject =  PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
            }
            UpdateCarrotGameObject();
         }
      }
   }

   void UpdateCarrotGameObject()
   {
      mCarrotGameObject = GameObject.Find("CarrotGameObject");
      if(mCarrotGameObject)
      {
         Carrot carrot = mCarrotGameObject.GetComponent<Carrot>();
         carrot.FacebookAppId = mCarrotAppId;
         carrot.CarrotAppSecret = mCarrotAppSecret;
      }
   }

   void OnFocus()
   {
      LoadSettings();
      UpdateCarrotGameObject();
   }

   void OnSelectionChange()
   {
      SaveSettings();
      UpdateCarrotGameObject();
   }

   void OnLostFocus()
   {
      SaveSettings();
      UpdateCarrotGameObject();
   }

   static void LoadSettings()
   {
      mCarrotAppId = EditorPrefs.GetString(ProjectName + "-CarrotAppId");
      mCarrotAppSecret = EditorPrefs.GetString(ProjectName + "-CarrotAppSecret");
   }

   static void SaveSettings()
   {
      EditorPrefs.SetString(ProjectName + "-CarrotAppId", mCarrotAppId);
      EditorPrefs.SetString(ProjectName + "-CarrotAppSecret", mCarrotAppSecret);
   }

   static string ProjectName
   {
      get
      {
         string[] dp = Application.dataPath.Split('/');
         return dp[dp.Length - 2];
      }
   }

   static string mCarrotAppId = "";
   static string mCarrotAppSecret = "";
   GameObject mCarrotGameObject = null;
}

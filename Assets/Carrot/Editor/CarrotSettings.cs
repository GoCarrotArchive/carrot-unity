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
   public static string CarrotAppId = "280914862014150";
   public static string CarrotAppSecret = "aa37313c6062762980f95a0711af0537";

   [MenuItem("Edit/Carrot")]
   public static void ShowWindow()
   {
      CarrotSettings settingsWindow = (CarrotSettings)GetWindow<CarrotSettings>(false, "Carrot Settings", false);
      settingsWindow.Show();
   }

   void OnGUI()
   {
      GUILayout.Label("Settings", EditorStyles.boldLabel);
      CarrotAppId = EditorGUILayout.TextField("Carrot App Id", CarrotAppId);
      CarrotAppSecret = EditorGUILayout.TextField("Carrot App Secret", CarrotAppSecret);
   }

   void OnFocus()
   {
      Debug.Log("CarrotSettings::OnFocus");
   }

   void OnLostFocus()
   {
      Debug.Log("CarrotSettings::OnLostFocus");
   }
}


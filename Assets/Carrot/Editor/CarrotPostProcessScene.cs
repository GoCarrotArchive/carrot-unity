/* Carrot -- Copyright (C) 2012 GoCarrot Inc.
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

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class CarrotPostProcessScene
{
   [PostProcessScene]
   public static void OnPostprocessScene()
   {
      Carrot carrot = Object.FindObjectOfType(typeof(Carrot)) as Carrot;
      if(carrot == null)
      {
         if(CarrotPostProcessScene.WillCreatePrefab)
         {
            GameObject carrotGameObject = GameObject.Find("CarrotGameObject");
            if(carrotGameObject == null)
            {
               Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Carrot/CarrotGameObject.prefab", typeof(GameObject));
               carrotGameObject =  PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
            }
            carrot = carrotGameObject.GetComponent<Carrot>();
         }
         else
         {
            Debug.LogWarning("No Carrot prefab found in: '" + EditorApplication.currentScene + "'");
         }
      }

      if(carrot != null)
      {
         if(CarrotPostProcessScene.WillCreatePrefab)
         {
            carrot.FacebookAppId = CarrotSettings.CarrotAppId;
            carrot.CarrotAppSecret = CarrotSettings.CarrotAppSecret;
         }
         else if(carrot.FacebookAppId != CarrotSettings.CarrotAppId ||
                 carrot.CarrotAppSecret != CarrotSettings.CarrotAppSecret)
         {
            Debug.LogWarning("Carrot prefab in: '" + EditorApplication.currentScene + "' has different credentials than the Carrot Settings in the Editor.");
         }
      }
   }

   public static bool WillCreatePrefab
   {
      get
      {
         // https://fogbugz.unity3d.com/default.asp?501928_jte739hnp32m9ebb
#if UNITY_X_X_VERSION_WHEN_BUG_FIXED
         return true;
#else
         return false;
#endif
      }
   }
}

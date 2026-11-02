
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if  UNITY_EDITOR
public static class GOAPEditorUtility
{
   public static GOAPAgent Agent;
   public static GOAPGlobal Global { get; private set; }

   [InitializeOnLoadMethod]
   public static void Init()
   {
      TryGetGlobal();
      EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
   }
   
   public static void SaveData()
   {
      if(EditorApplication.isPlaying) return;
      EditorUtility.SetDirty(Agent.GOAPConfig);
      EditorUtility.SetDirty(Agent);
   }
   
   public static void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
   {
      GetGlobal();
   }
   private static void TryGetGlobal()
   {
      if (Global == null) GetGlobal();
   }

   private static void GetGlobal()
   {
      Global = Object.FindAnyObjectByType<GOAPGlobal>();
   }
}
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class EditorAudioUtility
{
    private static MethodInfo _playClipMethodInfo;
    private static MethodInfo _stopClipMethodInfo;
    static EditorAudioUtility()
    {
       var editorAssembly = typeof(UnityEditor.AudioImporter).Assembly;
       var utilClassType = editorAssembly.GetType("UnityEditor.AudioUtil");
  
       _playClipMethodInfo = utilClassType.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,null,
           new Type[]{typeof(AudioClip),typeof(int),typeof(bool)},null);
       _stopClipMethodInfo = utilClassType.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioClip">音频文件</param>
    /// <param name="start">初始播放进度</param>
    public static void PlayAudio(AudioClip audioClip,float start)
    {
        _playClipMethodInfo.Invoke(null, new object[] { audioClip, (int)(start * 50000), false });
    }

    public static void StopAllAudio()
    {
        _stopClipMethodInfo.Invoke(null, null);
    }
}

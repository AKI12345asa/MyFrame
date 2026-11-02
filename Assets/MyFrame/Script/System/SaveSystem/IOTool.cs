using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MyFrame
{
    public static class IOTool
    {
        private static BinaryFormatter  binaryFormatter = new();

        public static void SaveJson(string jsonString, string path)
        {
            File.WriteAllText(path, jsonString);
        }

        public static T LoadJson<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                return null;
            }
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        
        public static void SaveFile(object saveObject, string path)
        {
            using var f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            // 二进制的方式把对象写进文件
            binaryFormatter.Serialize(f, saveObject);
        }

        public static T LoadFile<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                return null;
            }
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length == 0)
            {
                Debug.LogError($"LoadFile failed: file is empty. path={path}");
                return null;
            }
            try
            {
                using var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return (T)binaryFormatter.Deserialize(f);
            }
            catch (SerializationException e)
            {
                Debug.LogError($"LoadFile deserialize failed: {path}\n{e}");
                return null;
            }
        }
    }
}

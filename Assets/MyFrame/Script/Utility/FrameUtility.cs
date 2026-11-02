using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace MyFrame
{
    public static class FrameUtility
    {
        public static List<Type> GetTypesByAttribute<T>(out List<T> attributes) where T : Attribute
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> result = new List<Type>();
            attributes = new List<T>();
            foreach (Assembly assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    var attribute = type.GetCustomAttribute<T>();
                    if (attribute != null)
                    {
                        result.Add(type);
                        attributes.Add(attribute);
                    }
                }
            }
            return result;
        }
        public static List<Type> GetTypesByBaseTypeAndAttribute<B,T>(out List<T> attributes) where T : Attribute
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> result = new List<Type>();
            Type baseType = typeof(B);
            attributes = new List<T>();
            foreach (Assembly assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.BaseType == null || !baseType.IsAssignableFrom(type.BaseType)) continue;
                    var attribute = type.GetCustomAttribute<T>();
                    if (attribute != null)
                    {
                        result.Add(type);
                        attributes.Add(attribute);
                    }
                }
            }
            return result;
        }

        public static List<T> FindAllInFolders<T>(string[] folders) where T : UnityEngine.Object
        {
            var result = new List<T>();
            var assetFolders = folders
                .Select(folder => Path.IsPathRooted(folder)
                    ? FileUtil.GetProjectRelativePath(folder)
                    : folder.Replace('\\', '/'))
                .ToArray();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", assetFolders);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                result.Add(asset);
            }
            return result;
        }
        
        public static List<Type> GetTypesByBaseType<B>()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> result = new List<Type>();
            Type baseType = typeof(B);
            foreach (Assembly assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.BaseType == null || !baseType.IsAssignableFrom(type.BaseType)) continue;
                    result.Add(type);
                }
            }
            return result;
        }


        public static Dictionary<Type, MethodInfo> GetInterfaceMethods<I>(string targetMethodName,bool containAbstract = false)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Dictionary<Type, MethodInfo> result = new Dictionary<Type, MethodInfo>();
            Type targetInterface = typeof(I);
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!targetInterface.IsAssignableFrom(type) || type == targetInterface) continue;
                    var interfaceMap = type.GetInterfaceMap(targetInterface);
                    MethodInfo targetMethod = null;
                    var index =Array.FindIndex(interfaceMap.InterfaceMethods, method => method.Name == targetMethodName);
                    if(index >= 0) targetMethod = interfaceMap.TargetMethods[index];
                    if(targetMethod == null) continue;
                    if(!containAbstract && targetMethod.IsAbstract) continue;
                    result.Add(type, targetMethod);
                }
            }
            return result;
        }
    }
}

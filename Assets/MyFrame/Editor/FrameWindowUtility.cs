using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyFrame.Editor
{
    public static class FrameWindowUtility
    {
        #region Config部分
        
        public static Dictionary<Type,FrameConfigInspectorBase> ConfigInspectorDic=new();
        public static Dictionary<string,ConfigGroupItem> ConfigGroupDic=new();
        private static int groupIndex;
        [InitializeOnLoadMethod]
        public static void InitConfigInspector()
        {
            FrameUtility.GetTypesByBaseTypeAndAttribute<FrameConfigBase,FrameConfigAttribute>(out var attributes);
            foreach (var attr in attributes)
            {
                var groupNames = attr.ConfigGroupName.Split("/", StringSplitOptions.RemoveEmptyEntries);
                if (ConfigGroupDic.TryGetValue(groupNames[0], out ConfigGroupItem groupItem))
                {
                    var newGroupNames = new string[groupNames.Length - 1];
                    for (int i = 0; i < newGroupNames.Length; i++)
                    {
                        newGroupNames[i] = groupNames[i + 1];
                    }
                    groupItem.RefreshChildGroups(newGroupNames,attr.ConfigType);
                }
                else
                {
                    groupItem = new ConfigGroupItem(groupNames, attr.ConfigType);
                    ConfigGroupDic.Add(groupNames[0], groupItem);
                }
            }
            FrameUtility.GetTypesByBaseTypeAndAttribute<FrameConfigInspectorBase, FrameConfigInspectorAttribute>(out var inspectorsAttributes);
            foreach (var attr in inspectorsAttributes)
            {
                if (ConfigInspectorDic.ContainsKey(attr.ConfigType)) continue;
                var inspector = Activator.CreateInstance(attr.InspectorType) as FrameConfigInspectorBase;
                ConfigInspectorDic.Add(attr.ConfigType, inspector);
            }

        }

        public static List<TreeViewItemData<ConfigGroupItem>> GenerateTreeViewData()
        {
            var treeViewItems = new List<TreeViewItemData<ConfigGroupItem>>();
            foreach (var groupItem in ConfigGroupDic.Values)
            {
                var index = groupIndex++;
                var childTreeViewItem =  GenerateTreeViewData(groupItem);
                var treeViewItem = new TreeViewItemData<ConfigGroupItem>(index, groupItem, childTreeViewItem);
                treeViewItems.Add(treeViewItem);
            }
            groupIndex = 0;
            return  treeViewItems;
        }
        private static List<TreeViewItemData<ConfigGroupItem>> GenerateTreeViewData(ConfigGroupItem groupItem)
        {
            var treeViewItems = new List<TreeViewItemData<ConfigGroupItem>>();
            foreach(var childItem in groupItem.ChildInspectorDic.Values)
            {
                var index = groupIndex++;
                var childTreeViewItem = GenerateTreeViewData(childItem);
                var treeViewItem = new TreeViewItemData<ConfigGroupItem>(index, childItem, childTreeViewItem);
                treeViewItems.Add(treeViewItem);
            }
            return treeViewItems;
        }

        public static List<FrameConfigBase> FindAllConfigsInFolders()
        {
            if (MyFrameRoot.Setting == null || MyFrameRoot.Setting.ConfigFolderPathList.Count == 0) return null;
             return FrameUtility.FindAllInFolders<FrameConfigBase>(MyFrameRoot.Setting.ConfigFolderPathList.ToArray());
        }
        #endregion
    }

  public class ConfigGroupItem
        {
            public string Name;
            public HashSet<Type> ConfigTypes;
            public Dictionary<string, ConfigGroupItem> ChildInspectorDic;

            public ConfigGroupItem(string[] groupNames,Type type)
            {
                if (groupNames != null && groupNames.Length > 0)
                {
                    Name = groupNames[0];
                    ChildInspectorDic = new Dictionary<string, ConfigGroupItem>();
                    ConfigTypes = new HashSet<Type>();
                    ConfigTypes.Add(type);
                    if (groupNames.Length > 1)
                    {
                        var newGroupNames = new string[groupNames.Length - 1];
                        for (int i = 0; i < newGroupNames.Length; i++)
                        {
                            newGroupNames[i] = groupNames[i + 1];
                        }
                        var groupItem = new ConfigGroupItem(newGroupNames,type);
                        ChildInspectorDic.Add(groupItem.Name, groupItem);
                    }
                }
            }
            public void RefreshChildGroups(string[] groupNames, Type type) //能进入该方法就说明首字段已经被匹配过了自动添加类型就好了
            {
                ConfigTypes.Add(type);
                if (groupNames != null && groupNames.Length > 0)
                {
                    if (ChildInspectorDic.TryGetValue(groupNames[0], out ConfigGroupItem groupItem))
                    {
                        var newGroupNames = new string[groupNames.Length - 1];
                        for (int i = 0; i < newGroupNames.Length; i++)
                        {
                            newGroupNames[i] = groupNames[i + 1];
                        }
                        groupItem.RefreshChildGroups(newGroupNames, type);
                    }
                    else
                    {
                        groupItem = new ConfigGroupItem(groupNames, type);
                        ChildInspectorDic.Add(groupNames[0], groupItem);
                    }
                }
            }
        }

}
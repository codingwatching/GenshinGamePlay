﻿using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Unity.Code.NinoGen;
namespace TaoTie
{
    public class DecisionTreeEditor: BaseEditorWindow<ConfigAIDecisionTree>
    {
        protected override string fileName => "DecisionTree";
        protected override string folderPath => base.folderPath + "/EditConfig/AITree";
        protected override byte[] Serialize(ConfigAIDecisionTree data)
        {
            return Serializer.Serialize(data);
        }
        protected override ConfigAIDecisionTree CreateInstance()
        {
            return new ConfigAIDecisionTree();
        }
        
              
        [MenuItem("Tools/配置编辑器/AI行为树")]
        static void OpenDecisionTree()
        {
            EditorWindow.GetWindow<DecisionTreeEditor>().Show();
        }
        [OnOpenAsset(0)]
        public static bool OnBaseDataOpened(int instanceID, int line)
        {
            var data = EditorUtility.InstanceIDToObject(instanceID) as TextAsset;
            var path = AssetDatabase.GetAssetPath(data);
            return InitializeData(data,path);
        }

        public static bool InitializeData(TextAsset asset,string path)
        {
            if (asset == null) return false;
            if (path.EndsWith(".json") && JsonHelper.TryFromJson<ConfigAIDecisionTree>(asset.text,out var decisionTreeJson))
            {
                var win = EditorWindow.GetWindow<DecisionTreeEditor>();
                win.Init(decisionTreeJson,path,true);
                return true;
            }
            return false;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TaoTie
{
    public class FsmTableEditor: OdinEditorWindow
    {
        protected virtual string fileName => "FsmConfig";
        protected virtual string folderPath => "Assets/AssetsPackage/Unit";

        private string oldJson;

        public void Init(ConfigFsmTable data, string searchPath)
        {
            this.data = data;
            data.ChangeLayer();
            oldJson = JsonHelper.ToJson(data);
            filePath = searchPath;
        }

        protected virtual ConfigFsmTable CreateInstance()
        {
            return Activator.CreateInstance<ConfigFsmTable>();
        }
#if RoslynAnalyzer
        protected abstract byte[] Serialize(T data);
#endif
        [ShowIf("@data!=null")] [ReadOnly] public string filePath;
        [ShowIf("@data!=null")] [HideReferenceObjectPicker] public ConfigFsmTable data;

        #region Create

        [Button("打开")]
        public void Open()
        {
            string searchPath = EditorUtility.OpenFilePanel($"选择{typeof(ConfigFsmTable).Name}配置文件", folderPath, "json");
            if (!string.IsNullOrEmpty(searchPath))
            {
                var text = File.ReadAllText(searchPath);
                try
                {
                    data = JsonHelper.FromJson<ConfigFsmTable>(text);
                    data.ChangeLayer();
                    filePath = searchPath;
                    return;
                }
                catch (Exception ex)
                {
                }
                data = null;
                filePath = null;
                ShowNotification(new GUIContent($"非{typeof(ConfigFsmTable).Name}文件或内容损坏"));
            }
        }

        [Button("新建")]
        public void CreateJson()
        {
            string searchPath = EditorUtility.SaveFilePanel($"新建{typeof(ConfigFsmTable).Name}配置文件", folderPath, fileName, "json");
            if (!string.IsNullOrEmpty(searchPath))
            {
                data = CreateInstance();
                data.ChangeLayer();
                filePath = searchPath;
                var jStr = JsonHelper.ToJson(data);
                oldJson = jStr;
                File.WriteAllText(filePath, jStr);
                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region Save

        [Button("保存")]
        [ShowIf("@data!=null")]
        public void SaveJson()
        {
            if (data != null && !string.IsNullOrEmpty(filePath))
            {
                var jStr = JsonHelper.ToJson(data);
                oldJson = jStr;
                File.WriteAllText(filePath, jStr);
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("保存Json成功"));
            }
        }

        [Button("另存为")]
        [ShowIf("@data!=null")]
        public void SaveNewJson()
        {
            var names = filePath.Split('/', '.');
            string name = names[names.Length - 2];
            var paths = filePath.Split(name);
            string searchPath = EditorUtility.SaveFilePanel($"新建{typeof(ConfigFsmTable).Name}配置文件", paths[0], name, "json");
            if (!string.IsNullOrEmpty(searchPath))
            {
                var jStr = JsonHelper.ToJson(data);
                oldJson = jStr;
                File.WriteAllText(searchPath, jStr);
                AssetDatabase.Refresh();
                filePath = searchPath;
            }
        }

        [Button("导出(包括Animator)")]
        [ShowIf("@data!=null")]
        public void ExportWithAnimator()
        {
            Export(true);
        }
        [Button("导出(不包括Animator)")]
        [ShowIf("@data!=null")]
        public void ExportWithoutAnimator()
        {
            Export(false);
        }

        private void Export(bool withAnim)
        {
            string editDir = Path.GetDirectoryName(filePath);
            var fsmTimelineDict = FsmExporter.LoadFsmTimeline(editDir);
            ConfigFsmController config = new ConfigFsmController();
            config.ParamDict = new Dictionary<string, ConfigParam>();
            config.FsmConfigs = new ConfigFsm[data.Layers.Length];
            for (int layerIndex = 0; layerIndex < data.Layers.Length; layerIndex++)
            {
                if (data.Layers[layerIndex].FsmStates == null ||
                    data.Layers[layerIndex].FsmStates.Length <= 0) continue;
                ConfigFsm fsm = config.FsmConfigs[layerIndex] = new ConfigFsm();
                fsm.Name = data.Layers[layerIndex].Name;
                fsm.Entry = data.Layers[layerIndex].FsmStates[0].Name;
                fsm.LayerIndex = layerIndex;
                fsm.StateDict = new Dictionary<string, ConfigFsmState>();
                for (int i = 0; i < data.Layers[layerIndex].DataTable.GetLength(0); i++)
                {
                    var clip = data.Layers[layerIndex].FsmStates[i];
                    var fsmState = new ConfigFsmState();
                    fsmState.Data = clip.Data;
                    fsmState.Name = clip.Name;
                    if (clip.Clip != null)
                    {
                        fsmState.StateDuration = clip.Clip.length;
                        fsmState.StateLoop = clip.Clip.wrapMode == WrapMode.Loop;
                    }
                    else
                    {
                        fsmState.StateDuration = 1;
                        fsmState.StateLoop = true;
                    }

                    if (fsmTimelineDict.TryGetValue(fsmState.Name, out var tl))
                    {
                        fsmState.Timeline = tl;
                    }

                    List<ConfigTransition> transitions = new List<ConfigTransition>();
                    fsm.StateDict.Add(clip.Name, fsmState);
                    for (int j = 0; j < data.Layers[layerIndex].DataTable.GetLength(1); j++)
                    {
                        var trans = data.Layers[layerIndex].DataTable[j, i];
                        if (trans != null && trans.Transitions != null)
                        {
                            for (int k = 0; k < trans.Transitions.Length; k++)
                            {
                                transitions.Add(trans.Transitions[k]);
                                for (int l = 0; l < (trans.Transitions[k].Conditions?.Length??0); l++)
                                {
                                    var condition = trans.Transitions[k].Conditions[l];
                                    if (condition is ConfigConditionByDataTrigger trigger &&
                                        !string.IsNullOrEmpty(trigger.Key))
                                    {
                                        if (!config.ParamDict.ContainsKey(trigger.Key))
                                        {
                                            config.ParamDict.Add(trigger.Key, new ConfigParamTrigger()
                                            {
                                                Key = trigger.Key,
                                                ParameterType = AnimatorFsmType.Trigger
                                            });
                                        }
                                    }
                                    else if (condition is ConfigConditionByDataBool boolean &&
                                             !string.IsNullOrEmpty(boolean.Key))
                                    {
                                        if (!config.ParamDict.ContainsKey(boolean.Key))
                                        {
                                            config.ParamDict.Add(boolean.Key, new ConfigParamBool()
                                            {
                                                Key = boolean.Key,
                                                ParameterType = AnimatorFsmType.Bool
                                            });
                                        }
                                    }
                                    else if (condition is ConfigConditionByDataFloat floater &&
                                             !string.IsNullOrEmpty(floater.Key))
                                    {
                                        if (!config.ParamDict.ContainsKey(floater.Key))
                                        {
                                            config.ParamDict.Add(floater.Key, new ConfigParamFloat()
                                            {
                                                Key = floater.Key,
                                                ParameterType = AnimatorFsmType.Float
                                            });
                                        }
                                    }
                                    else if (condition is ConfigConditionByDataInt inter &&
                                             !string.IsNullOrEmpty(inter.Key))
                                    {
                                        if (!config.ParamDict.ContainsKey(inter.Key))
                                        {
                                            config.ParamDict.Add(inter.Key, new ConfigParamInt()
                                            {
                                                Key = inter.Key,
                                                ParameterType = AnimatorFsmType.Int
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    fsmState.Transitions = transitions.ToArray();
                }
            }
            var exportPath = filePath.Replace("/Edit/", "/");
            File.WriteAllText(exportPath, JsonHelper.ToJson(config));
            if (withAnim)
            {
                var acPath = filePath.Replace("/Edit/", "/Animations/").Replace(".json", ".controller");
                AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(acPath);
                for (int i = 0; i < data.Layers.Length; i++)
                {
                    AnimatorControllerLayer layer = animatorController.layers[i];
                    AnimatorStateMachine stateMachine = layer.stateMachine;

                    for (int j = 0; j < data.Layers[i].FsmStates.Length; j++)
                    {
                        var clip = data.Layers[i].FsmStates[j];
                        AnimatorState startState = stateMachine.AddState(clip.Name,
                            new Vector3(stateMachine.entryPosition.x + 230 * j, stateMachine.entryPosition.y + 100, 0));
                        startState.motion = clip.Clip;
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (data != null)
            {
                var jStr = JsonHelper.ToJson(data);
                if (oldJson != jStr)
                {
                    var res = EditorUtility.DisplayDialog("提示", "是否需要保存？", "是", "否");
                    if (res)
                    {
                        SaveJson();
                    }
                }
            }
        }
        #endregion
        
        [MenuItem("Tools/配置编辑器/状态机编辑器")]
        static void OpenFsmTable()
        {
            EditorWindow.GetWindow<FsmTableEditor>().Show();
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
            if (path.EndsWith(".json") && JsonHelper.TryFromJson<ConfigFsmTable>(asset.text,out var aiJson))
            {
                var win = GetWindow<FsmTableEditor>();
                win.Init(aiJson,path);
                return true;
            }
            return false;
        }
    }
}
﻿using System;
using Nino.Serialization;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace TaoTie
{
    [LabelText("且 运算节点")]
    [NinoSerialize]
    public partial class ConfigSceneGroupAndAction:ConfigSceneGroupAction
    {
        [NinoMember(10)]
        [LabelText("条件")]
        [TypeFilter("@OdinDropdownHelper.GetFilteredConditionTypeList(handleType)")]
        public ConfigSceneGroupCondition[] conditions;
        [NinoMember(11)]
        [LabelText("所有条件都满足后执行")]
#if UNITY_EDITOR
        [OnCollectionChanged(nameof(Refresh))]
        [OnStateUpdate(nameof(Refresh))]
#endif
        [TypeFilter("@OdinDropdownHelper.GetFilteredActionTypeList(handleType)")]
        public ConfigSceneGroupAction[] success;
        [NinoMember(12)]
        [LabelText("任意一个条件不满足执行")]
#if UNITY_EDITOR
        [OnCollectionChanged(nameof(Refresh))]
        [OnStateUpdate(nameof(Refresh))]
#endif
        [TypeFilter("@OdinDropdownHelper.GetFilteredActionTypeList(handleType)")]
        public ConfigSceneGroupAction[] fail;

#if UNITY_EDITOR
        
        public void Refresh()
        {
            if (success!= null)
            {
                for (int i = 0; i <  success.Length; i++)
                {
                    if(success[i]!=null)
                        success[i].handleType = handleType;
                }
                success.Sort((a, b) => { return a.localId - b.localId;});
            }

            if (fail != null)
            {
                for (int i = 0; i <  fail.Length; i++)
                {
                    if(fail[i]!=null)
                        fail[i].handleType = handleType;
                }
                fail.Sort((a, b) => { return a.localId - b.localId;});
            }
        }
#endif
        
        protected override void Execute(IEventBase evt, SceneGroup aimSceneGroup, SceneGroup fromSceneGroup)
        {
            bool isSuc = true;
            if (conditions == null || conditions.Length == 0)
            {
                isSuc = true;
            }
            else
            {
                for (int i = 0; i < conditions.Length; i++)
                {
                    isSuc &= conditions[i].IsMatch(evt,aimSceneGroup);
                }
            }

            if (isSuc)
            {
                for (int i = 0; i < (success == null ? 0 : success.Length); i++)
                {
                    success[i]?.ExecuteAction(evt,aimSceneGroup);
                }
            }
            else
            {
                for (int i = 0; i < (fail==null?0:fail.Length); i++)
                {
                    fail[i]?.ExecuteAction(evt,aimSceneGroup);
                }
            }
        }
    }
}
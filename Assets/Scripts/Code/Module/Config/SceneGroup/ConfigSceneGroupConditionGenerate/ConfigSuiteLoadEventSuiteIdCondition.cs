using System;
using Nino.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TaoTie
{
    [TriggerType(typeof(ConfigSuiteLoadEventTrigger))]
    [NinoType(false)]
    [LabelText("组Id")]
    public partial class ConfigSuiteLoadEventSuiteIdCondition : ConfigSceneGroupCondition<SuiteLoadEvent>
    {
        [Tooltip(SceneGroupTooltips.CompareMode)]
#if UNITY_EDITOR
        [OnValueChanged("@"+nameof(CheckModeType)+"("+nameof(Value)+","+nameof(Mode)+")")]
#endif
        [NinoMember(1)]
        [LabelText("判断类型")]
        public CompareMode Mode;
        [NinoMember(2)]
#if UNITY_EDITOR
        [ValueDropdown("@"+nameof(OdinDropdownHelper)+"."+nameof(OdinDropdownHelper.GetSceneGroupSuiteIds)+"()",AppendNextDrawer = true)]
        [LabelText("阶段Id")]
#endif
        public Int32 Value;

        public override bool IsMatch(SuiteLoadEvent obj, SceneGroup sceneGroup)
        {
            return IsMatch(Value, obj.SuiteId, Mode);
        }
#if UNITY_EDITOR
        protected override bool CheckModeType<T>(T t, CompareMode mode)
        {
            if (!base.CheckModeType(t, mode))
            {
                mode = CompareMode.Equal;
                return false;
            }

            return true;
        }
#endif
    }
}

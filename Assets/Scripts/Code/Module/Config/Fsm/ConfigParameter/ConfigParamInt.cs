﻿namespace TaoTie
{
    public class ConfigParamInt : ConfigParam<int>
    {
        public ConfigParamInt(){}
        public ConfigParamInt(string key, int value, bool needSyncAnimator = false) : base(key, value, needSyncAnimator) { }

        public override void SetDefaultValue(FsmComponent ctrl)
        {
            if (this.defaultValue != default(int))
            {
                SetValue(ctrl, this.defaultValue);
            }
        }
    }
}
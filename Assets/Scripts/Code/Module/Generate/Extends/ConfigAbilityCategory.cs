﻿using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace TaoTie
{
    public class ConfigAbilityCategory: IManager
    {
        public static ConfigAbilityCategory Instance { get; private set; }

        #region IManager

        private Dictionary<string, ConfigAbility> _dict;
        private List<ConfigAbility> _list;

        public void Init()
        {
            Instance = this;
            _list = new List<ConfigAbility>();
            _dict = new Dictionary<string, ConfigAbility>();
            var sceneGroups = YooAssets.GetAssetInfos("ability");
            for (int i = 0; i < sceneGroups.Length; i++)
            {
                if (sceneGroups[i].AssetPath.EndsWith("json") && Define.ConfigType != 0)
                {
                    continue;
                }
                if (sceneGroups[i].AssetPath.EndsWith("bytes") && Define.ConfigType != 1)
                {
                    continue;
                }
                var op = YooAssets.LoadAssetSync(sceneGroups[i]);
                if (op.AssetObject is TextAsset textAsset)
                {
                    if (Define.ConfigType == 0)
                    {
                        if (JsonHelper.TryFromJson<ConfigAbility[]>(textAsset.text, out var list))
                        {
                            for (int j = 0; j < list.Length; j++)
                            {
                                var item = list[j];
                                if (!_dict.ContainsKey(item.AbilityName))
                                {
                                    _list.Add(item);
                                    _dict[item.AbilityName] = item;
                                }
                                else
                                {
                                    Log.Error("ConfigAbilityList AbilityName重复 "+item.AbilityName);
                                }
                            }
                        }
                    }
                    else if (Define.ConfigType == 1)
                    {
                        try
                        {
                            var list = ProtobufHelper.FromBytes<ConfigAbility[]>(textAsset.bytes);
                            for (int j = 0; j < list.Length; j++)
                            {
                                var item = list[j];
                                if (!_dict.ContainsKey(item.AbilityName))
                                {
                                    _list.Add(item);
                                    _dict[item.AbilityName] = item;
                                }
                                else
                                {
                                    Log.Error("ConfigAbilityList AbilityName重复 "+item.AbilityName);
                                }
                            }
                        }
                        catch{}
                    }
                }
                op.Release();
            }
        }

        public void Destroy()
        {
            Instance = null;
            _dict = null;
            _list = null;
        }

        #endregion

        public ConfigAbility Get(string name)
        {
            return _dict[name];
        }
        public Dictionary<string, ConfigAbility> GetAll()
        {
            return _dict;
        }
        public List<ConfigAbility> GetAllList()
        {
            return _list;
        }
        
        public ListComponent<ConfigAbility> GetList(string[] names)
        {
            ListComponent<ConfigAbility> res = ListComponent<ConfigAbility>.Create();
            if (names != null)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    res.Add(Get(names[i]));
                }
            }

            return res;
        }
    }
}
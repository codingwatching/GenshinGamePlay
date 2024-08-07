﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using YooAsset;
namespace TaoTie
{
    public static class BuildHelper
    {
        const string relativeDirPrefix = "Release";

        public static readonly Dictionary<PlatformType, BuildTarget> buildmap = new Dictionary<PlatformType, BuildTarget>(PlatformTypeComparer.Instance)
        {
            { PlatformType.Android , BuildTarget.Android },
            { PlatformType.Windows , BuildTarget.StandaloneWindows64 },
            { PlatformType.IOS , BuildTarget.iOS },
            { PlatformType.MacOS , BuildTarget.StandaloneOSX },
            { PlatformType.Linux , BuildTarget.StandaloneLinux64 },
        };

        public static readonly Dictionary<PlatformType, BuildTargetGroup> buildGroupmap = new Dictionary<PlatformType, BuildTargetGroup>(PlatformTypeComparer.Instance)
        {
            { PlatformType.Android , BuildTargetGroup.Android },
            { PlatformType.Windows , BuildTargetGroup.Standalone },
            { PlatformType.IOS , BuildTargetGroup.iOS },
            { PlatformType.MacOS , BuildTargetGroup.Standalone },
            { PlatformType.Linux , BuildTargetGroup.Standalone },
        };
        public static void KeystoreSetting()
        {
            PlayerSettings.Android.keystoreName = "TaoTie.keystore";
            PlayerSettings.Android.keyaliasName = "taitie";
            PlayerSettings.keyaliasPass = "123456";
            PlayerSettings.keystorePass = "123456";
        }
        
        private static string[] cdnList =
        {
            "http://127.0.0.1:8081/cdn",
            "http://127.0.0.1:8081/cdn",
            "http://127.0.0.1:8081/cdn"
        };
        /// <summary>
        /// 设置打包模式
        /// </summary>
        public static void SetCdnConfig(string channel, int mode = 1)
        {
            var cdn = Resources.Load<CDNConfig>("CDNConfig");
            cdn.Channel = channel;

            cdn.DefaultHostServer = cdnList[mode];
            cdn.FallbackHostServer = cdnList[mode];
            cdn.UpdateListUrl = cdnList[mode];
            cdn.TestUpdateListUrl = cdnList[mode];
            EditorUtility.SetDirty(cdn);
            AssetDatabase.SaveAssetIfDirty(cdn);
        }

        public static void Build(PlatformType type, BuildOptions buildOptions, bool isBuildExe,bool clearFolder,
            bool isBuildAll,bool packAtlas,bool isContainsAb)
        {
            if (buildmap[type] == EditorUserBuildSettings.activeBuildTarget)
            {
                //pack
                BuildHandle(type, buildOptions, isBuildExe,clearFolder,isBuildAll,packAtlas,isContainsAb);
            }
            else
            {
                EditorUserBuildSettings.activeBuildTargetChanged = delegate ()
                {
                    if (EditorUserBuildSettings.activeBuildTarget == buildmap[type])
                    {
                        //pack
                        BuildHandle(type, buildOptions, isBuildExe, clearFolder, isBuildAll,packAtlas,isContainsAb);
                    }
                };
                if(buildGroupmap.TryGetValue(type,out var group))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(group, buildmap[type]);
                }
                else
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(buildmap[type]);
                }
               
            }
        }
         public static void BuildPackage(PlatformType type, string packageName)
        {
            string platform = "";
            BuildTarget buildTarget = BuildTarget.StandaloneWindows;
            switch (type)
            {
                case PlatformType.Windows:
                    buildTarget = BuildTarget.StandaloneWindows64;
 
                    platform = "pc";
                    break;
                case PlatformType.Android:
                    KeystoreSetting();
                    buildTarget = BuildTarget.Android;
                    platform = "android";
                    break;
                case PlatformType.IOS:
                    buildTarget = BuildTarget.iOS;
                    platform = "ios";
                    break;
                case PlatformType.MacOS:
                    buildTarget = BuildTarget.StandaloneOSX;
                    platform = "pc";
                    break;
                case PlatformType.Linux:
                    buildTarget = BuildTarget.StandaloneLinux64;
                    platform = "pc";
                    break;
            }

            string jstr = File.ReadAllText("Assets/AssetsPackage/packageConfig.bytes");
            var packageConfig = JsonHelper.FromJson<PackageConfig>(jstr);
            if (!packageConfig.packageVer.TryGetValue(packageName, out var version))
            {
                Debug.LogError("指定分包版本号不存在");
                return;
            }
            if (buildmap[type] == EditorUserBuildSettings.activeBuildTarget)
            {
                //pack
                BuildPackage(buildTarget, true, false, version, packageName);
            }
            else
            {
                EditorUserBuildSettings.activeBuildTargetChanged = delegate()
                {
                    if (EditorUserBuildSettings.activeBuildTarget == buildmap[type])
                    {
                        //pack
                        BuildPackage(buildTarget, true, false, version, packageName);
                    }
                };
                if (buildGroupmap.TryGetValue(type, out var group))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(group, buildmap[type]);
                }
                else
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(buildmap[type]);
                }

            }
            
            jstr = File.ReadAllText("Assets/AssetsPackage/config.bytes");
            var obj = JsonHelper.FromJson<BuildInConfig>(jstr);

            string fold = $"{AssetBundleBuilderHelper.GetDefaultOutputRoot()}/{buildTarget}";
            var config = Resources.Load<CDNConfig>("CDNConfig");
            string targetPath = Path.Combine(relativeDirPrefix, $"{config.Channel}_{platform}");
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            FileHelper.CleanDirectory(targetPath);
            var dir = $"{fold}/{packageName}/{version}";
            FileHelper.CopyFiles(dir, targetPath);
            UnityEngine.Debug.Log("完成cdn资源打包");
#if UNITY_EDITOR
            Application.OpenURL($"file:///{targetPath}");
#endif
        }
         private static void BuildInternal(BuildTarget buildTarget, bool isBuildExe, bool isBuildAll, bool isContainsAb)
         {
             string jstr = File.ReadAllText("Assets/AssetsPackage/config.bytes");
             var obj = JsonHelper.FromJson<BuildInConfig>(jstr);
             int buildVersion = obj.Resver;
             Debug.Log($"开始构建 : {buildTarget}");
             BuildPackage(buildTarget, isBuildExe, isBuildAll, buildVersion, YooAssetsMgr.DefaultName);
             if (isContainsAb)
             {
                 jstr = File.ReadAllText("Assets/AssetsPackage/packageConfig.bytes");
                 var packageConfig = JsonHelper.FromJson<PackageConfig>(jstr);
                 if (packageConfig.packageVer != null)
                 {
                     foreach (var item in packageConfig.packageVer)
                     {
                         BuildPackage(buildTarget, isBuildExe, isBuildAll, item.Value, item.Key);
                     }
                 }
             }
         }

         public static void BuildPackage(BuildTarget buildTarget, bool isBuildExe, bool isBuildAll, int buildVersion,
             string packageName)
         {
             // 构建参数
             string defaultOutputRoot = AssetBundleBuilderHelper.GetDefaultOutputRoot();
             BuildParameters buildParameters = new BuildParameters();
             buildParameters.OutputRoot = defaultOutputRoot;
             buildParameters.BuildTarget = buildTarget;
             buildParameters.PackageName = packageName;
             buildParameters.BuildPipeline =
                 isBuildExe ? EBuildPipeline.BuiltinBuildPipeline : EBuildPipeline.ScriptableBuildPipeline;
             buildParameters.SBPParameters = new BuildParameters.SBPBuildParameters();
             buildParameters.BuildMode = isBuildExe ? EBuildMode.ForceRebuild : EBuildMode.IncrementalBuild;
             buildParameters.PackageVersion = buildVersion.ToString();
             buildParameters.CopyBuildinFileTags = "buildin";
             buildParameters.VerifyBuildingResult = true;
             if (packageName == YooAssetsMgr.DefaultName)
             {
                 buildParameters.CopyBuildinFileOption = isBuildAll
                     ? ECopyBuildinFileOption.ClearAndCopyAll
                     : ECopyBuildinFileOption.ClearAndCopyByTags;
             }
             else
             {
                 buildParameters.CopyBuildinFileOption =
                     isBuildAll ? ECopyBuildinFileOption.OnlyCopyAll : ECopyBuildinFileOption.None;
             }

             buildParameters.EncryptionServices = new FileOffsetEncryption();
             buildParameters.CompressOption = ECompressOption.LZ4;
             buildParameters.DisableWriteTypeTree = true; //禁止写入类型树结构（可以降低包体和内存并提高加载效率）
             buildParameters.IgnoreTypeTreeChanges = false;
             buildParameters.SharedPackRule = new ZeroRedundancySharedPackRule();
             if (buildParameters.BuildPipeline == EBuildPipeline.ScriptableBuildPipeline)
             {
                 buildParameters.SBPParameters = new BuildParameters.SBPBuildParameters();
                 buildParameters.SBPParameters.WriteLinkXML = true;
             }

             // 执行构建
             AssetBundleBuilder builder = new AssetBundleBuilder();
             var buildResult = builder.Run(buildParameters);
             if (buildResult.Success)
                 Debug.Log($"构建成功!");
         }

         public static void HandleAtlas()
        {
            //清除图集
            AtlasHelper.ClearAllAtlas();
            //生成图集
            AtlasHelper.GeneratingAtlas();
        }
        static void BuildHandle(PlatformType type, BuildOptions buildOptions, bool isBuildExe,bool clearFolder,
            bool isBuildAll,bool packAtlas,bool isContainsAb)
        {
            BuildTarget buildTarget = BuildTarget.StandaloneWindows;
            string programName = "TaoTie";
            string exeName = programName;
            string platform = "";
            switch (type)
            {
                case PlatformType.Windows:
                    buildTarget = BuildTarget.StandaloneWindows64;
                    exeName += ".exe";
                    platform = "pc";
                    break;
                case PlatformType.Android:
                    KeystoreSetting();
                    buildTarget = BuildTarget.Android;
                    exeName += ".apk";
                    platform = "android";
                    break;
                case PlatformType.IOS:
                    buildTarget = BuildTarget.iOS;
                    platform = "ios";
                    break;
                case PlatformType.MacOS:
                    buildTarget = BuildTarget.StandaloneOSX;
                    platform = "pc";
                    break;
                case PlatformType.Linux:
                    buildTarget = BuildTarget.StandaloneLinux64;
                    platform = "pc";
                    break;
            }
            
            PackagesManagerEditor.Clear("com.thridparty-moudule.hotreload");//HotReload存在时打包会报错
            //打程序集
            FileHelper.CleanDirectory(Define.HotfixDir);
            BuildAssemblieEditor.BuildCodeRelease();
            
            //处理图集资源
            if (packAtlas) HandleAtlas();
            
            if (isBuildExe)
            {
                if (Directory.Exists("Assets/StreamingAssets"))
                {
                    Directory.Delete("Assets/StreamingAssets", true);
                    Directory.CreateDirectory("Assets/StreamingAssets");
                }
                else
                {
                    Directory.CreateDirectory("Assets/StreamingAssets");
                }
                AssetDatabase.Refresh();
            }
                              
            //打ab
            BuildInternal(buildTarget, isBuildExe, isBuildAll, isContainsAb);

            if (clearFolder && Directory.Exists(relativeDirPrefix))
            {
                Directory.Delete(relativeDirPrefix, true);
                Directory.CreateDirectory(relativeDirPrefix);
            }
            else
            {
                Directory.CreateDirectory(relativeDirPrefix);
            }

            if (isBuildExe)
            {

                AssetDatabase.Refresh();
                string[] levels = {
                    "Assets/AssetsPackage/Scenes/InitScene/Init.unity",
                };
                UnityEngine.Debug.Log("开始EXE打包");
                BuildPipeline.BuildPlayer(levels, $"{relativeDirPrefix}/{exeName}", buildTarget, buildOptions);
                UnityEngine.Debug.Log("完成exe打包");
                
            }
            
            string jstr = File.ReadAllText("Assets/AssetsPackage/config.bytes");
            var obj = JsonHelper.FromJson<BuildInConfig>(jstr);
            
            string fold = $"{AssetBundleBuilderHelper.GetDefaultOutputRoot()}/{buildTarget}";
            var config = Resources.Load<CDNConfig>("CDNConfig");
            string targetPath = Path.Combine(relativeDirPrefix, $"{config.Channel}_{platform}");
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
            FileHelper.CleanDirectory(targetPath);
            var dirs = new DirectoryInfo(fold).GetDirectories();
            jstr = File.ReadAllText("Assets/AssetsPackage/packageConfig.bytes");
            var packageConfig = JsonHelper.FromJson<PackageConfig>(jstr);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = null;
                if (dirs[i].Name == YooAssetsMgr.DefaultName)
                {
                    dir = $"{fold}/{dirs[i].Name}/{obj.Resver}";
                }
                else
                {
                    if (packageConfig.packageVer != null)
                    {
                        foreach (var item in packageConfig.packageVer)
                        {
                            if (item.Key == dirs[i].Name)
                            {
                                dir = $"{fold}/{dirs[i].Name}/{item.Value}";
                            }
                        }
                    }
                }
                if (dir != null)
                {
                    FileHelper.CopyFiles(dir, targetPath);
                }
            }

            UnityEngine.Debug.Log("完成cdn资源打包");
#if UNITY_EDITOR
            Application.OpenURL($"file:///{targetPath}");
#endif
        }
        
    }
}

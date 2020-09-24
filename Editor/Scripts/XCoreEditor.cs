﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using TinaX;
using TinaX.IO;
using TinaXEditor.Const;
using TinaXEditor.Internal;

namespace TinaXEditor
{
    [InitializeOnLoad]
    public static class XCoreEditor
    {
        static XCoreEditor()
        {
            //检查 TinaX Profile
            XDirectory.CreateIfNotExists(XEditorConst.EditorProjectSettingRootFolder);

            RefreshXProfile();
        }

        private static XProfileModel mProfileModel;
        private static TinaX.Internal.XProfileConfig mProfileRuntimeConfig;

        public static string[] GetXProfileNames()
        {
            return mProfileModel.GetXProfileNames();
        }

        public static void RefreshXProfile()
        {
            if (mProfileModel == null)
            {
                //load or new
                bool create = false;
                if (File.Exists(XEditorConst.EditorXProfilePath))
                {
                    try
                    {
                        mProfileModel = XConfig.GetJson<XProfileModel>(XEditorConst.EditorXProfilePath, AssetLoadType.SystemIO, false);
                        mProfileModel.CheckDefaultProfile();
                    }
                    catch
                    {
                        create = true;
                    }
                }
                else
                    create = true;

                if (create)
                {
                    mProfileModel = XProfileModel.GetDefault();
                    //写出
                    XConfig.SaveJson(mProfileModel, XEditorConst.EditorXProfilePath, AssetLoadType.SystemIO);
                }

            }

            if (mProfileRuntimeConfig == null)
            {
                mProfileRuntimeConfig = XConfig.CreateConfigIfNotExists<TinaX.Internal.XProfileConfig>(TinaX.Const.FrameworkConst.XProfile_Config_Path, AssetLoadType.Resources);
                if (mProfileRuntimeConfig.ActiveProfileName.IsNullOrEmpty())
                    mProfileRuntimeConfig.ActiveProfileName = mProfileModel.CurrentProfileName;
                mProfileRuntimeConfig.DevelopMode = mProfileModel.IsDevelopMode(mProfileRuntimeConfig.ActiveProfileName);
            }
        }

        public static void SaveXProfiles()
        {
            if (mProfileModel == null || mProfileRuntimeConfig == null)
            {
                RefreshXProfile();
            }

            mProfileModel.ReadySave();
            XConfig.SaveJson(mProfileModel, XEditorConst.EditorXProfilePath, AssetLoadType.SystemIO);

            mProfileRuntimeConfig.ActiveProfileName = mProfileModel.CurrentProfileName;
            mProfileRuntimeConfig.DevelopMode = mProfileModel.IsDevelopMode(mProfileModel.CurrentProfileName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static bool AddXProfile(string profileName)
        {
            if (mProfileModel == null)
                RefreshXProfile();

            if (profileName.IsNullOrEmpty() || profileName.IsNullOrWhiteSpace())
            {
                Debug.LogError($"[TinaX] Add profile name error : profile name cannot be empty or whitespaces");
                return false;
            }

            if (mProfileModel.IsXProfileExists(profileName))
            {
                Debug.LogError($"[TinaX] Add profile name error : cannot add the same profile name \"{profileName}\"");
                return false;
            }

            mProfileModel.AddXProfile(profileName);
            return true;
        }

        public static void RemoveXProfile(string profileName)
        {
            if (mProfileModel.GetCount() < 1) return; //至少保留一个

            mProfileModel.RemoveXProfile(profileName);

        }

        public static string GetCurrentActiveXProfileName()
        {
            return mProfileModel.CurrentProfileName;
        }

        public static bool SetActiveXProfile(string profileName)
        {


            if (profileName.IsNullOrEmpty() || profileName.IsNullOrWhiteSpace())
            {
                Debug.LogError("[TinaX] set active profile name error : profile name cannot be empty or whitespaces");
                return false;
            }

            if (!mProfileModel.IsXProfileExists(profileName))
            {
                Debug.LogError($"[TinaX] set active profile name error : profile name \"{profileName}\" not found ");
                return false;
            }

            mProfileModel.CurrentProfileName = profileName;
            return true;
        }

        public static void SetXProfileDevelopMode(string profileName, bool isDevelopMode)
        {
            mProfileModel.SetDevelopMode(profileName, isDevelopMode);
        }

        public static bool IsXProfileDevelopMode(string profileName)
        {
            return mProfileModel.IsDevelopMode(profileName);
        }

        //不要轻易修改它的名字，它在XCore中被反射
        public static string GetDebugCommandLineArgs()
        {
            string filePath = XEditorConst.LaunchSettingsFilePath;
            if (File.Exists(filePath))
            {
                try
                {
                    string json_str = File.ReadAllText(filePath, Encoding.UTF8);
                    EditorLaunchSettingModel model = JsonUtility.FromJson<EditorLaunchSettingModel>(json_str);
                    return model.DebugCommandLineArgs;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[TinaX.Core]" + (TinaXEditor.Utils.EditorGUIUtil.IsCmnHans
                        ? "编辑器获取Debug命令行启动参数失败."
                        : "Editor load debug command line args failed.")
                        + "\n:" + e.Message);
                    return string.Empty;
                }
            }
            else
                return string.Empty;
        }

        public static void SaveDebugCommandLineArgs(string args)
        {
            string filePath = XEditorConst.LaunchSettingsFilePath;
            if (File.Exists(filePath))
            {
                try
                {
                    string json_str = File.ReadAllText(filePath, Encoding.UTF8);
                    EditorLaunchSettingModel model = JsonUtility.FromJson<EditorLaunchSettingModel>(json_str);
                    model.DebugCommandLineArgs = args;
                    json_str = JsonUtility.ToJson(model);
                    File.WriteAllText(filePath, json_str, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[TinaX.Core]" + (TinaXEditor.Utils.EditorGUIUtil.IsCmnHans
                        ? "编辑器保存Debug命令行启动参数失败."
                        : "Editor save debug command line args failed.")
                        + "\n:" + e.Message);
                }
            }
            else
            {
                try
                {
                    EditorLaunchSettingModel model = new EditorLaunchSettingModel();
                    model.DebugCommandLineArgs = args;
                    var json_str = JsonUtility.ToJson(model);
                    File.WriteAllText(filePath, json_str, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[TinaX.Core]" + (TinaXEditor.Utils.EditorGUIUtil.IsCmnHans
                        ? "编辑器保存Debug命令行启动参数失败."
                        : "Editor save debug command line args failed.")
                        + "\n:" + e.Message);
                }
            }
        }

    }
}

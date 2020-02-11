﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TinaX
{
    public static class XConfig
    {
#if UNITY_EDITOR
        public static string ConfigResourcesPath { get; set; } = $"Assets/Resources/XConfig";
#endif

        //private static List<>
        private const string mPathHeadText_LoadByResources = "ures:";
        private const string mPathHeadText_LoadByVFS = "xvfs:";

        /// <summary>
        /// key: path(附带头)， value: 缓存对象
        /// </summary>
        private static Dictionary<string, object> mDict_ConfigCache = new Dictionary<string, object>();

        public static void RefreshConfigs()
        {

        }
    
        /// <summary>
        /// Get Unity Config
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="loadType"></param>
        /// <param name="cachePrior"></param>
        /// <returns></returns>
        public static T GetConfig<T>(string configPath , AssetLoadType loadType = AssetLoadType.Resources, bool cachePrior = true) where T : UnityEngine.ScriptableObject
        {
            bool cache_flag = false;
            string cache_key = ((loadType == AssetLoadType.Resources) ? mPathHeadText_LoadByResources : mPathHeadText_LoadByVFS) + configPath;

            if (cachePrior && UnityEngine.Application.isPlaying)
            {
                //检查缓存
                if (mDict_ConfigCache.TryGetValue(cache_key, out object conf))
                    return conf as T;
                else
                    cache_flag = true; //加载之后要加入缓存字典
            }

            T final_asset;
            //load asset 
            if(loadType == AssetLoadType.Resources)
            {
#if UNITY_EDITOR
                //在编辑器下应该使用AssetDatabase加载
                if(configPath.IndexOf("Resources") != -1 && configPath.StartsWith("Assets/"))
                {
                    //本来给定的就是unity asset path, 直接加载
                    final_asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(configPath);
                }
                else
                {
                    //本来的路径是Resources的路径，要还原成Unity的路径
                    string uPath = ConfigResourcesPath + "/" + configPath;
                    if (!uPath.EndsWith(".asset"))
                        uPath = uPath + ".asset";

                    final_asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(uPath);
                }
#else
                final_asset = Resources.Load<T>(RemoveExtNameIfExists(configPath));
#endif
            }
            else if(loadType == AssetLoadType.VFS)
            {
#if UNITY_EDITOR
                if(Application.isPlaying && XCore.MainInstance != null && XCore.GetMainInstance().IsRunning)
                {
                    final_asset = XCore.GetMainInstance().GetService<TinaX.Services.IAssetService>().Load<T>(configPath);
                }
                else
                {
                    //editor only : load asset by assetdatabase.
                    final_asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(configPath);
                }

#else
                final_asset = XCore.GetMainInstance()?.GetService<TinaX.Services.IAssetService>()?.Load<T>(configPath);
#endif
            }
            else
            {
                throw new Exception($"[TinaX] XConfig System can't load asset by \"{loadType.ToString()}\"loadtype.");
            }

            if (cache_flag)
            {
                if (mDict_ConfigCache.ContainsKey(cache_key))
                    mDict_ConfigCache[cache_key] = final_asset;
                else
                    mDict_ConfigCache.Add(cache_key, final_asset);
            }

            return final_asset;

        }

        public static object GetConfig(string configPath, Type type, AssetLoadType loadType = AssetLoadType.Resources, bool cachePrior = true)
        {
            if (!type.IsSubclassOf(typeof(UnityEngine.ScriptableObject))) { Debug.LogError("[TinaX] XConfig : the config you want get is not valid \"ScriptableObject\"Type.  type: " + type.FullName + "  path:" + configPath); return null; }

            bool cache_flag = false;
            string cache_key = ((loadType == AssetLoadType.Resources) ? mPathHeadText_LoadByResources : mPathHeadText_LoadByVFS) + configPath;

            if (cachePrior && UnityEngine.Application.isPlaying)
            {
                //检查缓存
                if (mDict_ConfigCache.TryGetValue(cache_key, out object conf))
                    return conf;
                else
                    cache_flag = true; //加载之后要加入缓存字典
            }

            object final_asset;
            //load asset 
            if (loadType == AssetLoadType.Resources)
            {
#if UNITY_EDITOR
                //在编辑器下应该使用AssetDatabase加载
                if (configPath.IndexOf("Resources") != -1 && configPath.StartsWith("Assets/"))
                {
                    //本来给定的就是unity asset path, 直接加载
                    final_asset = UnityEditor.AssetDatabase.LoadAssetAtPath(configPath,type);
                }
                else
                {
                    //本来的路径是Resources的路径，要还原成Unity的路径
                    string uPath = ConfigResourcesPath + "/" + configPath;
                    if (!uPath.EndsWith(".asset"))
                        uPath += ".asset";

                    final_asset = UnityEditor.AssetDatabase.LoadAssetAtPath(uPath,type);
                }
#else
                final_asset = Resources.Load(RemoveExtNameIfExists(configPath),type);
#endif
            }
            else if (loadType == AssetLoadType.VFS)
            {
#if UNITY_EDITOR
                if (Application.isPlaying && XCore.MainInstance != null && XCore.GetMainInstance().IsRunning)
                {
                    final_asset = XCore.GetMainInstance().GetService<TinaX.Services.IAssetService>().Load(configPath,type);
                }
                else
                {
                    //editor only : load asset by assetdatabase.
                    final_asset = UnityEditor.AssetDatabase.LoadAssetAtPath(configPath,type);
                }

#else
                final_asset = XCore.GetMainInstance()?.GetService<TinaX.Services.IAssetService>()?.Load(configPath,type);
#endif
            }
            else
            {
                throw new Exception($"[TinaX] XConfig System can't load asset by \"{loadType.ToString()}\"loadtype.");
            }

            if (cache_flag)
            {
                if (mDict_ConfigCache.ContainsKey(cache_key))
                    mDict_ConfigCache[cache_key] = final_asset;
                else
                    mDict_ConfigCache.Add(cache_key, final_asset);
            }

            return final_asset;
        }



#if UNITY_EDITOR

        public static T CreateConfigIfNotExists<T>(string configPath, AssetLoadType pathType = AssetLoadType.Resources) where T: ScriptableObject
        {
            UnityEditor.AssetDatabase.ReleaseCachedFileHandles();
            //检查路径
            var final_path = (pathType == AssetLoadType.Resources) ? (ConfigResourcesPath + "/" + configPath + ".asset") : configPath;
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(final_path);
            if(asset == null)
            {
                //create
                EnsureDirectory(final_path);
                var config = ScriptableObject.CreateInstance<T>();
                UnityEditor.AssetDatabase.CreateAsset(config, final_path);

                return config;
            }
            else
            {
                return asset;
            }
        }


        private static void EnsureDirectory(string unityPath)
        {
            TinaX.IO.XDirectory.CreateIfNotExists(
                System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(), 
                    System.IO.Path.GetDirectoryName(unityPath.Replace("/", "\\"))
                    )
                );
        }

#endif

        /// <summary>
        /// 如果给定的路径有后缀名，则去掉后缀。
        /// </summary>
        /// <returns></returns>
        private static string RemoveExtNameIfExists(string source)
        {
            if (source.IsNullOrEmpty()) return source;
            int last_point = source.LastIndexOf('.');
            int last_slash = source.LastIndexOf('/');
            if (last_point > 0 && last_slash > 0 && last_slash > last_point)
                return source.Substring(0, last_point);
            else
                return source;
        }




    }
}
//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    public class ProcedurePreload : ProcedureBase
    {
        /*
         * 云存档接入参考（本地 Setting + 云端 JSON）
         *
         * 1) 运行时配置（建议在启动流程里做一次）
         *    GameEntry.Setting.ConfigureCloudSyncRuntime(new CloudSyncRuntimeOptions
         *    {
         *        EnableCloudSync = true,
         *        TrackOnLocalWrite = true,
         *        RemoveLocalKeysNotInCloudOnImport = false, // 保守合并
         *        SaveAfterImport = true,
         *        ForceMethodImportBehavior = false,
         *        CloudKeyPrefixes = new[] { "Setting." }
         *    });
         *
         * 2) 业务写入规范
         *    - 使用 SetCloudSyncedBool/Int/Float/String 写入
         *    - 删除键使用 RemoveCloudSyncedKey
         *    - 作用：写本地同时登记可云同步键
         *
         * 3) 上传流程（本地 -> 云端）
         *    string uploadJson = GameEntry.Setting.ExportCloudSyncJson();
         *    // 调用云端上传接口
         *
         * 4) 下载合并（云端 -> 本地）
         *    CloudSyncImportResult result = GameEntry.Setting.ImportCloudSyncJson(downloadJson);
         *    // result.UpsertedCount / result.RemovedCount 可用于日志与监控
         *
         * 5) 字段增减兼容
         *    - 云端新增键：自动写入并注册
         *    - 云端缺失键：可按策略删除本地（严格模式）或保留（保守模式）
         */

        private Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();

        public static readonly string[] DataTableNames = new string[]
        {
            "Scene",
            "Entity",
            "Music",
            "Sound",
            "UIForm",
            "UISound",
            "UITranslation",
            "WorldUI",
            "Character","Weapon","Effect"
        };



        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            m_LoadedFlag.Clear();

            PreloadResources();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
            {
                if (!loadedFlag.Value)
                {
                    return;
                }
            }

            // 初始进入游戏时直接进入游戏场景与游戏流程
            procedureOwner.SetData<VarInt32>(ProcedureChangeScene.NextProcedureIdKey, m_EnterProcedure);
            procedureOwner.SetData<VarString>(ProcedureChangeScene.NextSceneNameKey, m_EnterScene);
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        private void PreloadResources()
        {
            // Preload configs
            // LoadConfig("DefaultConfig");


            // Preload data tables（UITranslation 加载成功后会在 OnLoadDataTableSuccess 中调用 LocationExtension.BuildUiTranslationKeyToIdMap）
            foreach (string dataTableName in DataTableNames)
            {
                LoadDataTable(dataTableName);
            }

            // Preload dictionaries 旧版xml字典已废弃，暂不加载
            // LoadDictionary("Default");

            // Preload fonts
            //LoadFont("MainFont.otf");//旧版字体

            LoadFont("ziti.asset");

            LoadSpriteAtlas("UISpriteAtlas");
        }

        private void LoadConfig(string configName)
        {
            string configAssetName = AssetUtility.GetConfigAsset(configName, false);
            m_LoadedFlag.Add(configAssetName, false);
            GameEntry.Config.ReadData(configAssetName, this);
        }

        private void LoadDataTable(string dataTableName)
        {
            string dataTableAssetName = AssetUtility.GetByteDataTableAsset(dataTableName);
            m_LoadedFlag.Add(dataTableAssetName, false);
            GameEntry.DataTable.LoadDataTable(dataTableName, dataTableAssetName, this);
        }

        private void LoadDictionary(string dictionaryName)
        {
            string dictionaryAssetName = AssetUtility.GetDictionaryAsset(dictionaryName, false);
            m_LoadedFlag.Add(dictionaryAssetName, false);
            GameEntry.Localization.ReadData(dictionaryAssetName, this);
        }

        private void LoadFont(string fontName)
        {
            m_LoadedFlag.Add(Utility.Text.Format("Font.{0}", fontName), false);
            GameEntry.Resource.LoadAsset(AssetUtility.GetFontAsset(fontName), Constant.AssetPriority.FontAsset, new LoadAssetCallbacks(
                (assetName, asset, duration, userData) =>
                {
                    m_LoadedFlag[Utility.Text.Format("Font.{0}", fontName)] = true;
                    //UGuiForm.SetMainFont((Font)asset);
                    UGuiForm.SetMainTMPFontAsset((TMP_FontAsset)asset);
                    Log.Info("Load font '{0}' OK.", fontName);
                },

                (assetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not load font '{0}' from '{1}' with error message '{2}'.", fontName, assetName, errorMessage);
                }));
        }

        private void LoadSpriteAtlas(string spriteAtlasName)
        {
            m_LoadedFlag.Add(Utility.Text.Format("SpriteAtlas.{0}", spriteAtlasName), false);
            GameEntry.Resource.LoadAsset(AssetUtility.GetSpriteAtlasAsset(spriteAtlasName), Constant.AssetPriority.FontAsset, new LoadAssetCallbacks(
                (assetName, asset, duration, userData) =>
                {
                    m_LoadedFlag[Utility.Text.Format("SpriteAtlas.{0}", spriteAtlasName)] = true;
                    SpriteAtlas atlas = (SpriteAtlas)asset;
                    GameEntry.BuiltinData.UISpriteAtlas = atlas;
                    UGuiForm.SetMainSpriteAtlas(atlas);
                    Log.Info("Load sprite atlas '{0}' OK.", spriteAtlasName);
                },

                (assetName, status, errorMessage, userData) =>
                {
                    Log.Error("Can not load sprite atlas '{0}' from '{1}' with error message '{2}'.", spriteAtlasName, assetName, errorMessage);
                }));
        }

        private void OnLoadConfigSuccess(object sender, GameEventArgs e)
        {
            LoadConfigSuccessEventArgs ne = (LoadConfigSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.ConfigAssetName] = true;
            Log.Info("Load config '{0}' OK.", ne.ConfigAssetName);
        }

        private void OnLoadConfigFailure(object sender, GameEventArgs e)
        {
            LoadConfigFailureEventArgs ne = (LoadConfigFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load config '{0}' from '{1}' with error message '{2}'.", ne.ConfigAssetName, ne.ConfigAssetName, ne.ErrorMessage);
        }

        private void OnLoadDataTableSuccess(object sender, GameEventArgs e)
        {
            LoadDataTableSuccessEventArgs ne = (LoadDataTableSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.DataTableAssetName] = true;
            // Log.Info("Load data table '{0}' OK.", ne.DataTableAssetName);

            if (ne.DataTableAssetName == AssetUtility.GetByteDataTableAsset("UITranslation"))
            {
                LocationExtension.BuildUiTranslationKeyToIdMap();
            }
        }

        private void OnLoadDataTableFailure(object sender, GameEventArgs e)
        {
            LoadDataTableFailureEventArgs ne = (LoadDataTableFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load data table '{0}' from '{1}' with error message '{2}'.", ne.DataTableAssetName, ne.DataTableAssetName, ne.ErrorMessage);
        }

        private void OnLoadDictionarySuccess(object sender, GameEventArgs e)
        {
            LoadDictionarySuccessEventArgs ne = (LoadDictionarySuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_LoadedFlag[ne.DictionaryAssetName] = true;
            Log.Info("Load dictionary '{0}' OK.", ne.DictionaryAssetName);
        }

        private void OnLoadDictionaryFailure(object sender, GameEventArgs e)
        {
            LoadDictionaryFailureEventArgs ne = (LoadDictionaryFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load dictionary '{0}' from '{1}' with error message '{2}'.", ne.DictionaryAssetName, ne.DictionaryAssetName, ne.ErrorMessage);
        }
    }
}

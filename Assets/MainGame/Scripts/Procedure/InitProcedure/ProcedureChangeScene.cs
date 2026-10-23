//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Event;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    public class ProcedureChangeScene : ProcedureBase
    {
        public const string NextProcedureIdKey = "NextProcedureId";
        public const string NextSceneNameKey = "NextSceneName";

        private bool m_IsChangeSceneComplete = false;
        private int m_BackgroundMusicId = 0;
        private ProcedureEnum m_ProcedureId = ProcedureEnum.菜单流程;
        private string m_TargetSceneAssetName = null;
        private int m_PendingUnloadCount = 0;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_IsChangeSceneComplete = false;
            m_PendingUnloadCount = 0;
            m_TargetSceneAssetName = null;

            GameEntry.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            GameEntry.Event.Subscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
            GameEntry.Event.Subscribe(LoadSceneDependencyAssetEventArgs.EventId, OnLoadSceneDependencyAsset);
            GameEntry.Event.Subscribe(UnloadSceneSuccessEventArgs.EventId, OnUnloadSceneSuccess);
            GameEntry.Event.Subscribe(UnloadSceneFailureEventArgs.EventId, OnUnloadSceneFailure);

            GameEntry.Base.ResetNormalGameSpeed();

            int procedureId = procedureOwner.GetData<VarInt32>(NextProcedureIdKey);
            string overrideSceneName = procedureOwner.GetData<VarString>(NextSceneNameKey);

            m_ProcedureId = (ProcedureEnum)procedureId;
            if (m_ProcedureId == ProcedureEnum.Unknown)
            {
                Log.Warning("Can not change scene with invalid Procedure '{0}'.", procedureId);
                return;
            }

            if (string.IsNullOrEmpty(overrideSceneName))
            {
                Log.Error("Scene name is invalid when change scene.");
                return;
            }

            m_TargetSceneAssetName = AssetUtility.GetSceneAsset(overrideSceneName);
            UnloadLoadedScenesExceptTarget();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoadSceneSuccess);
            GameEntry.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnLoadSceneFailure);
            GameEntry.Event.Unsubscribe(LoadSceneUpdateEventArgs.EventId, OnLoadSceneUpdate);
            GameEntry.Event.Unsubscribe(LoadSceneDependencyAssetEventArgs.EventId, OnLoadSceneDependencyAsset);
            GameEntry.Event.Unsubscribe(UnloadSceneSuccessEventArgs.EventId, OnUnloadSceneSuccess);
            GameEntry.Event.Unsubscribe(UnloadSceneFailureEventArgs.EventId, OnUnloadSceneFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_IsChangeSceneComplete)
            {
                return;
            }

            ChangeToTargetProcedure(procedureOwner, m_ProcedureId);
        }

        private void OnLoadSceneSuccess(object sender, GameEventArgs e)
        {
            LoadSceneSuccessEventArgs ne = (LoadSceneSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Info("Load scene '{0}' OK.", ne.SceneAssetName);

            if (m_BackgroundMusicId > 0)
            {
                // GameEntry.Sound.PlayMusic(m_BackgroundMusicId);
            }

            m_IsChangeSceneComplete = true;
        }

        private void OnLoadSceneFailure(object sender, GameEventArgs e)
        {
            LoadSceneFailureEventArgs ne = (LoadSceneFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Load scene '{0}' failure, error message '{1}'.", ne.SceneAssetName, ne.ErrorMessage);
        }

        private void OnLoadSceneUpdate(object sender, GameEventArgs e)
        {
            LoadSceneUpdateEventArgs ne = (LoadSceneUpdateEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Info("Load scene '{0}' update, progress '{1}'.", ne.SceneAssetName, ne.Progress.ToString("P2"));
        }

        private void OnLoadSceneDependencyAsset(object sender, GameEventArgs e)
        {
            LoadSceneDependencyAssetEventArgs ne = (LoadSceneDependencyAssetEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Info("Load scene '{0}' dependency asset '{1}', count '{2}/{3}'.", ne.SceneAssetName, ne.DependencyAssetName, ne.LoadedCount.ToString(), ne.TotalCount.ToString());
        }

        private void UnloadLoadedScenesExceptTarget()
        {
            string[] loadedSceneAssetNames = GameEntry.Scene.GetLoadedSceneAssetNames();
            for (int i = 0; i < loadedSceneAssetNames.Length; i++)
            {
                string loadedSceneAssetName = loadedSceneAssetNames[i];
                if (loadedSceneAssetName == m_TargetSceneAssetName)
                {
                    continue;
                }

                m_PendingUnloadCount++;
                GameEntry.Scene.UnloadScene(loadedSceneAssetName, this);
            }

            if (m_PendingUnloadCount == 0)
            {
                TryLoadTargetScene();
            }
        }

        private void TryLoadTargetScene()
        {
            if (m_IsChangeSceneComplete || string.IsNullOrEmpty(m_TargetSceneAssetName))
            {
                return;
            }

            if (GameEntry.Scene.SceneIsLoaded(m_TargetSceneAssetName))
            {
                m_IsChangeSceneComplete = true;
                return;
            }

            if (GameEntry.Scene.SceneIsLoading(m_TargetSceneAssetName))
            {
                return;
            }

            GameEntry.Scene.LoadScene(m_TargetSceneAssetName, Constant.AssetPriority.SceneAsset, this);
        }

        private void OnUnloadSceneSuccess(object sender, GameEventArgs e)
        {
            UnloadSceneSuccessEventArgs ne = (UnloadSceneSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            m_PendingUnloadCount--;
            if (m_PendingUnloadCount <= 0)
            {
                m_PendingUnloadCount = 0;
                TryLoadTargetScene();
            }
        }

        private void OnUnloadSceneFailure(object sender, GameEventArgs e)
        {
            UnloadSceneFailureEventArgs ne = (UnloadSceneFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Unload scene '{0}' failure.", ne.SceneAssetName);
            m_PendingUnloadCount--;
            if (m_PendingUnloadCount <= 0)
            {
                m_PendingUnloadCount = 0;
                TryLoadTargetScene();
            }
        }
    }
}

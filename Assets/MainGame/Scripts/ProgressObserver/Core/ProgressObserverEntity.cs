using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 进度观察实体基类：只订阅游戏事件并检测条件，达成后调用 <see cref="CompleteObserver"/>。
    /// </summary>
    public abstract class ProgressObserverEntity : Entity
    {
        protected ProgressObserverEntityData ObserverData { get; private set; }

        protected string ObserverId => ObserverData?.ObserverId;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            ObserverData = userData as ProgressObserverEntityData;
            Name = "progressObserver_" + (ObserverId ?? GetType().Name);
            GameEntry.ProgressObserver?.RegisterActiveObserver(ObserverId);
            OnObserverStart();
        }

        /// <summary>实体创建后的初始化，订阅事件等。</summary>
        protected abstract void OnObserverStart();

        /// <summary>
        /// 观察任务达成：写入完成态存档、执行奖励、隐藏自身。
        /// </summary>
        protected void CompleteObserver(Action applyReward)
        {
            if (GameEntry.ProgressObserver != null && !string.IsNullOrEmpty(ObserverId))
            {
                GameEntry.ProgressObserver.MarkCompleted(ObserverId);
            }

            applyReward?.Invoke();
            HideSelf();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            OnObserverStop(isShutdown);
            GameEntry.ProgressObserver?.UnregisterActiveObserver(ObserverId);
            base.OnHide(isShutdown, userData);
        }

        protected virtual void OnObserverStop(bool isShutdown)
        {
        }

        protected void HideSelf()
        {
            if (GameEntry.Entity != null)
            {
                GameEntry.Entity.HideEntity(this);
            }
        }
    }
}

using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 默认平台服务。
    /// 当具体平台实现尚未接入时，用于兜底，避免业务层直接依赖 SDK。
    /// </summary>
    public class DefaultPlatformService : PlatformServiceBase
    {
        public override PlatformType PlatformType
        {
            get
            {
                return PlatformComponent != null ? PlatformComponent.RuntimePlatformType : PlatformType.Unknown;
            }
        }

        /// <summary>
        /// 默认平台：体力不足时直接视为已解决，便于本地调试；正式项目可改为弹窗/广告等。
        /// </summary>
        public override PlatformResourceResolveResult HandleInsufficientStamina(int requiredAmount, Action onResolved = null)
        {
            Log.Info("DefaultPlatform: stamina insufficient (required={0}), treat as resolved for local debug.", requiredAmount);
            return NotifyStaminaResolvedImmediately(onResolved);
        }

        /// <summary>
        /// 默认平台：金币不足时显示提示。
        /// </summary>
        public override PlatformResourceResolveResult HandleInsufficientGold(int requiredAmount, Action onResolved = null)
        {
            return base.HandleInsufficientGold(requiredAmount, onResolved);
        }
    }
}

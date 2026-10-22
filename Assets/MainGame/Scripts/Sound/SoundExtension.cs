//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using GameFramework;
using GameFramework.Sound;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static class SoundExtension
    {
        private const float FadeVolumeDuration = 1f;
        private static int? s_MusicSerialId = null;

        public static int? PlayMusic(this SoundComponent soundComponent, int musicId, object userData = null)
        {
            soundComponent.StopMusic();

            DRMusic drMusic = GameEntry.DataTable.GetDataRow<DRMusic>(musicId);
            if (drMusic == null)
            {
                Log.Warning("Can not load music '{0}' from data table.", musicId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = 64;
            playSoundParams.Loop = true;
            playSoundParams.VolumeInSoundGroup = 1f;
            playSoundParams.FadeInSeconds = FadeVolumeDuration;
            playSoundParams.SpatialBlend = 0f;
            s_MusicSerialId = soundComponent.PlaySound(AssetUtility.GetMusicAsset(drMusic.AssetName), "Music", Constant.AssetPriority.MusicAsset, playSoundParams, null, userData);
            return s_MusicSerialId;
        }

        public static void StopMusic(this SoundComponent soundComponent)
        {
            if (!s_MusicSerialId.HasValue)
            {
                return;
            }

            soundComponent.StopSound(s_MusicSerialId.Value, FadeVolumeDuration);
            s_MusicSerialId = null;
        }

        public static int? PlaySound(this SoundComponent soundComponent, int soundId, Entity bindingEntity = null, object userData = null)
        {
            if (soundId < 1000)
            {
                return null;
            }

            DRSound drSound = GameEntry.DataTable.GetDataRow<DRSound>(soundId);
            if (drSound == null)
            {
                Log.Warning("Can not load sound '{0}' from data table.", soundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = CreatePlaySoundParams(drSound);
            return soundComponent.PlaySound(AssetUtility.GetSoundAsset(drSound.AssetName), "Sound", Constant.AssetPriority.SoundAsset, playSoundParams, bindingEntity != null ? bindingEntity.Entity : null, userData);
        }

        public static int? PlaySound(this SoundComponent soundComponent, SoundEnum soundId, Entity bindingEntity = null, object userData = null)
        {
            if (!Enum.IsDefined(typeof(SoundEnum), soundId))
            {
                Log.Warning("Cant find sound type.", soundId.ToString());
                return null;
            }

            DRSound drSound = GameEntry.DataTable.GetDataRow<DRSound>((int)soundId);
            if (drSound == null)
            {
                Log.Warning("Can not load sound '{0}' from data table.", soundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = CreatePlaySoundParams(drSound);
            return soundComponent.PlaySound(AssetUtility.GetSoundAsset(drSound.AssetName), "Sound", Constant.AssetPriority.SoundAsset, playSoundParams, bindingEntity != null ? bindingEntity.Entity : null, userData);
        }

        /// <summary>播放音效，音量 = 配置表 Volume × volumeScale（0~1）。</summary>
        public static int? PlaySound(this SoundComponent soundComponent, SoundEnum soundId, Entity bindingEntity, float volumeScale, object userData = null)
        {
            return PlaySound(soundComponent, soundId, bindingEntity, volumeScale, fadeInSeconds: 0f, userData);
        }

        /// <summary>播放循环类音效；可带淡入，避免 AudioClip 挂上瞬间的硬切滋声。</summary>
        public static int? PlaySound(this SoundComponent soundComponent, SoundEnum soundId, Entity bindingEntity, float volumeScale, float fadeInSeconds, object userData = null)
        {
            if (!Enum.IsDefined(typeof(SoundEnum), soundId))
            {
                Log.Warning("Cant find sound type.", soundId.ToString());
                return null;
            }

            DRSound drSound = GameEntry.DataTable.GetDataRow<DRSound>((int)soundId);
            if (drSound == null)
            {
                Log.Warning("Can not load sound '{0}' from data table.", soundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = CreatePlaySoundParams(drSound);
            playSoundParams.VolumeInSoundGroup = Mathf.Clamp01(drSound.Volume * Mathf.Clamp01(volumeScale));
            playSoundParams.FadeInSeconds = Mathf.Max(0f, fadeInSeconds);
            return soundComponent.PlaySound(
                AssetUtility.GetSoundAsset(drSound.AssetName),
                "Sound",
                Constant.AssetPriority.SoundAsset,
                playSoundParams,
                bindingEntity != null ? bindingEntity.Entity : null,
                userData);
        }

        private static PlaySoundParams CreatePlaySoundParams(DRSound drSound)
        {
            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = drSound.Priority;
            playSoundParams.Loop = drSound.Loop;
            playSoundParams.VolumeInSoundGroup = drSound.Volume;
            playSoundParams.SpatialBlend = drSound.SpatialBlend;
            playSoundParams.MaxDistance = drSound.MaxDistance;
            return playSoundParams;
        }

        public static int? PlayUISound(this SoundComponent soundComponent, int uiSoundId, object userData = null)
        {
            DRUISound drUISound = GameEntry.DataTable.GetDataRow<DRUISound>(uiSoundId);
            if (drUISound == null)
            {
                Log.Warning("Can not load UI sound '{0}' from data table.", uiSoundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = drUISound.Priority;
            playSoundParams.Loop = false;
            playSoundParams.VolumeInSoundGroup = drUISound.Volume;
            playSoundParams.SpatialBlend = 0f;
            return soundComponent.PlaySound(AssetUtility.GetUISoundAsset(drUISound.AssetName), "UISound", Constant.AssetPriority.UISoundAsset, playSoundParams, userData);
        }

        public static bool IsMuted(this SoundComponent soundComponent, string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return true;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return true;
            }

            return soundGroup.Mute;
        }

        public static void Mute(this SoundComponent soundComponent, string soundGroupName, bool mute)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return;
            }

            soundGroup.Mute = mute;

            GameEntry.Setting.SetCloudSyncedBool(Utility.Text.Format(Constant.Setting.SoundGroupMuted, soundGroupName), mute);
            GameEntry.Setting.Save();
        }

        public static float GetVolume(this SoundComponent soundComponent, string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return 0f;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return 0f;
            }

            return soundGroup.Volume;
        }

        public static void SetVolume(this SoundComponent soundComponent, string soundGroupName, float volume)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return;
            }

            soundGroup.Volume = volume;

            GameEntry.Setting.SetCloudSyncedFloat(Utility.Text.Format(Constant.Setting.SoundGroupVolume, soundGroupName), volume);
            GameEntry.Setting.Save();
        }

        /// <summary>
        /// 根据场景中其它 AudioListener 自动开关 Sound 组件内置 Listener。
        /// 场景相机（实体）晚于场景加载生成时，需主动调用以避免重复 Listener 警告。
        /// </summary>
        public static void RefreshBuiltInAudioListener(this SoundComponent soundComponent)
        {
            AudioListener builtInListener = soundComponent.GetComponent<AudioListener>();
            if (builtInListener == null)
            {
                return;
            }

            AudioListener[] listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>();
            int otherEnabledCount = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null || listener == builtInListener || !listener.enabled)
                {
                    continue;
                }

                otherEnabledCount++;
            }

            builtInListener.enabled = otherEnabledCount <= 0;
        }
    }
}

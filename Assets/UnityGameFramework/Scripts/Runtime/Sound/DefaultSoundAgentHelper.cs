//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Sound;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 默认声音代理辅助器。
    /// </summary>
    public class DefaultSoundAgentHelper : SoundAgentHelperBase
    {
        private Transform m_CachedTransform = null;
        private AudioSource m_AudioSource = null;
        private EntityLogic m_BindingEntityLogic = null;
        private float m_VolumeWhenPause = 0f;
        private bool m_ApplicationPauseFlag = false;
        private EventHandler<ResetSoundAgentEventArgs> m_ResetSoundAgentEventHandler = null;

        /// <summary>
        /// Play 后宽限帧数：期内对外 <see cref="IsPlaying"/> 为 true（防止 SoundGroup 误判空闲抢走代理），
        /// 且 Update 不因 isPlaying 暂为 false 而 Reset。
        /// </summary>
        private const int PlayGraceFrameCount = 3;
        private int m_PlayGraceFrames = 0;

        /// <summary>
        /// 获取当前是否正在播放。
        /// </summary>
        public override bool IsPlaying
        {
            get
            {
                // Play 后若干帧内 isPlaying 可能仍为 false；若此时回报空闲，
                // SoundGroup 会把代理分给后来的低优先级音（如风声），直接掐掉刚发起的 one-shot。
                if (m_PlayGraceFrames > 0 && m_AudioSource != null && m_AudioSource.clip != null)
                {
                    return true;
                }

                return m_AudioSource.isPlaying;
            }
        }

        /// <summary>
        /// 获取声音长度。
        /// </summary>
        public override float Length
        {
            get
            {
                return m_AudioSource.clip != null ? m_AudioSource.clip.length : 0f;
            }
        }

        /// <summary>
        /// 获取或设置播放位置。
        /// </summary>
        public override float Time
        {
            get
            {
                return m_AudioSource.time;
            }
            set
            {
                m_AudioSource.time = value;
            }
        }

        /// <summary>
        /// 获取或设置是否静音。
        /// </summary>
        public override bool Mute
        {
            get
            {
                return m_AudioSource.mute;
            }
            set
            {
                m_AudioSource.mute = value;
            }
        }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public override bool Loop
        {
            get
            {
                return m_AudioSource.loop;
            }
            set
            {
                m_AudioSource.loop = value;
            }
        }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public override int Priority
        {
            get
            {
                return 128 - m_AudioSource.priority;
            }
            set
            {
                m_AudioSource.priority = 128 - value;
            }
        }

        /// <summary>
        /// 获取或设置音量大小。
        /// </summary>
        public override float Volume
        {
            get
            {
                return m_AudioSource.volume;
            }
            set
            {
                m_AudioSource.volume = value;
            }
        }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public override float Pitch
        {
            get
            {
                return m_AudioSource.pitch;
            }
            set
            {
                m_AudioSource.pitch = value;
            }
        }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public override float PanStereo
        {
            get
            {
                return m_AudioSource.panStereo;
            }
            set
            {
                m_AudioSource.panStereo = value;
            }
        }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public override float SpatialBlend
        {
            get
            {
                return m_AudioSource.spatialBlend;
            }
            set
            {
                m_AudioSource.spatialBlend = value;
            }
        }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public override float MaxDistance
        {
            get
            {
                return m_AudioSource.maxDistance;
            }

            set
            {
                m_AudioSource.maxDistance = value;
            }
        }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public override float DopplerLevel
        {
            get
            {
                return m_AudioSource.dopplerLevel;
            }
            set
            {
                m_AudioSource.dopplerLevel = value;
            }
        }

        /// <summary>
        /// 获取或设置声音代理辅助器所在的混音组。
        /// </summary>
        public override AudioMixerGroup AudioMixerGroup
        {
            get
            {
                return m_AudioSource.outputAudioMixerGroup;
            }
            set
            {
                m_AudioSource.outputAudioMixerGroup = value;
            }
        }

        /// <summary>
        /// 重置声音代理事件。
        /// </summary>
        public override event EventHandler<ResetSoundAgentEventArgs> ResetSoundAgent
        {
            add
            {
                m_ResetSoundAgentEventHandler += value;
            }
            remove
            {
                m_ResetSoundAgentEventHandler -= value;
            }
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public override void Play(float fadeInSeconds)
        {
            StopAllCoroutines();

            // DecompressOnLoad / 未预加载时，Play 后首帧 isPlaying 可能仍为 false。
            m_PlayGraceFrames = PlayGraceFrameCount;
            if (m_AudioSource.clip != null && m_AudioSource.clip.loadState == AudioDataLoadState.Unloaded)
            {
                m_AudioSource.clip.LoadAudioData();
            }

            // 必须先把音量置 0 再 Play：若先 Play 再改 volume，首个混音缓冲仍可能以满音量输出，WebGL 上听成很大的「滋」。
            float targetVolume = m_AudioSource.volume;
            m_AudioSource.volume = 0f;
            m_AudioSource.Play();
            if (fadeInSeconds > 0f)
            {
                StartCoroutine(FadeToVolume(m_AudioSource, targetVolume, fadeInSeconds));
            }
            else
            {
                // 无淡入时也延后一帧再恢复音量，避开同帧 Stop/Play 的首缓冲爆音。
                StartCoroutine(RestoreVolumeNextFrame(targetVolume));
            }
        }

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public override void Stop(float fadeOutSeconds)
        {
            StopAllCoroutines();
            m_PlayGraceFrames = 0;

            if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
            {
                StartCoroutine(StopCo(fadeOutSeconds));
            }
            else
            {
                // 同帧 Stop→换 clip→Play 时，硬 Stop 非零振幅采样会爆音；先静音再停。
                if (m_AudioSource != null)
                {
                    m_AudioSource.volume = 0f;
                    m_AudioSource.Stop();
                }
            }
        }

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public override void Pause(float fadeOutSeconds)
        {
            StopAllCoroutines();

            m_VolumeWhenPause = m_AudioSource.volume;
            if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
            {
                StartCoroutine(PauseCo(fadeOutSeconds));
            }
            else
            {
                m_AudioSource.Pause();
            }
        }

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public override void Resume(float fadeInSeconds)
        {
            StopAllCoroutines();

            m_AudioSource.UnPause();
            if (fadeInSeconds > 0f)
            {
                StartCoroutine(FadeToVolume(m_AudioSource, m_VolumeWhenPause, fadeInSeconds));
            }
            else
            {
                m_AudioSource.volume = m_VolumeWhenPause;
            }
        }

        /// <summary>
        /// 重置声音代理辅助器。
        /// </summary>
        public override void Reset()
        {
            StopAllCoroutines();
            m_PlayGraceFrames = 0;
            m_CachedTransform.localPosition = Vector3.zero;
            if (m_AudioSource != null)
            {
                // 先静音再 Stop：同帧 Stop→换 clip→Play 时，非零振幅硬切会产生很大「滋」声（WebGL 尤甚）。
                m_AudioSource.volume = 0f;
                m_AudioSource.Stop();
                m_AudioSource.clip = null;
            }

            m_BindingEntityLogic = null;
            m_VolumeWhenPause = 0f;
        }

        /// <summary>
        /// 设置声音资源。
        /// </summary>
        /// <param name="soundAsset">声音资源。</param>
        /// <returns>是否设置声音资源成功。</returns>
        public override bool SetSoundAsset(object soundAsset)
        {
            AudioClip audioClip = soundAsset as AudioClip;
            if (audioClip == null)
            {
                return false;
            }

            m_AudioSource.clip = audioClip;
            return true;
        }

        /// <summary>
        /// 设置声音绑定的实体。
        /// </summary>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        public override void SetBindingEntity(Entity bindingEntity)
        {
            // 绑定失败时不要 Reset：Play 已在回调前执行，Reset 会造成「clip 刚挂上就被清空」的滋一声。
            if (bindingEntity == null)
            {
                m_BindingEntityLogic = null;
                return;
            }

            m_BindingEntityLogic = bindingEntity.Logic;
            if (m_BindingEntityLogic != null && m_BindingEntityLogic.Available)
            {
                UpdateAgentPosition();
            }
        }

        /// <summary>
        /// 设置声音所在的世界坐标。
        /// </summary>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        public override void SetWorldPosition(Vector3 worldPosition)
        {
            m_CachedTransform.position = worldPosition;
        }

        private void Awake()
        {
            m_CachedTransform = transform;
            m_AudioSource = gameObject.GetOrAddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            // Custom 且未配置曲线时，3D 音量可能被衰减成近似静音，听感只剩 Stop/Reset 的爆音。
            m_AudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            // 跟车镜头约 5~6m；默认 minDistance=1 时对数衰减会把音量压到约 1/6，统一抬到 5 对齐常用听感。
            m_AudioSource.minDistance = 5f;
            // 绑实体/换位置时多普勒会把瞬间位移算成极端音高，听感像爆音。
            m_AudioSource.dopplerLevel = 0f;
        }

        private void Update()
        {
            if (m_PlayGraceFrames > 0)
            {
                // 这里必须看 AudioSource 真实状态：对外 IsPlaying 在宽限期内恒为 true，避免被 SoundGroup 抢走。
                if (!m_AudioSource.isPlaying && m_AudioSource.clip != null)
                {
                    AudioDataLoadState loadState = m_AudioSource.clip.loadState;
                    if (loadState == AudioDataLoadState.Loading)
                    {
                        // 仍在加载：不消耗宽限、不 Reset。
                        if (m_BindingEntityLogic != null)
                        {
                            UpdateAgentPosition();
                        }

                        return;
                    }

                    if (loadState == AudioDataLoadState.Loaded)
                    {
                        m_AudioSource.Play();
                    }

                    m_PlayGraceFrames--;
                    if (m_BindingEntityLogic != null)
                    {
                        UpdateAgentPosition();
                    }

                    return;
                }

                m_PlayGraceFrames--;
            }

            if (!m_ApplicationPauseFlag && !IsPlaying && m_AudioSource.clip != null && m_ResetSoundAgentEventHandler != null)
            {
                ResetSoundAgentEventArgs resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
                m_ResetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
                ReferencePool.Release(resetSoundAgentEventArgs);
                return;
            }

            if (m_BindingEntityLogic != null)
            {
                UpdateAgentPosition();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            m_ApplicationPauseFlag = pause;
        }

        private void UpdateAgentPosition()
        {
            if (m_BindingEntityLogic == null)
            {
                return;
            }

            if (m_BindingEntityLogic.Available)
            {
                m_CachedTransform.position = m_BindingEntityLogic.CachedTransform.position;
                return;
            }

            // 实体不可用时仅解除跟随，让残留 one-shot 播完，禁止 Reset 误杀。
            m_BindingEntityLogic = null;
        }

        private IEnumerator StopCo(float fadeOutSeconds)
        {
            yield return FadeToVolume(m_AudioSource, 0f, fadeOutSeconds);
            m_AudioSource.Stop();
        }

        private IEnumerator PauseCo(float fadeOutSeconds)
        {
            yield return FadeToVolume(m_AudioSource, 0f, fadeOutSeconds);
            m_AudioSource.Pause();
        }

        private IEnumerator FadeToVolume(AudioSource audioSource, float volume, float duration)
        {
            float time = 0f;
            float originalVolume = audioSource.volume;
            while (time < duration)
            {
                time += UnityEngine.Time.deltaTime;
                audioSource.volume = Mathf.Lerp(originalVolume, volume, time / duration);
                yield return new WaitForEndOfFrame();
            }

            audioSource.volume = volume;
        }

        private IEnumerator RestoreVolumeNextFrame(float volume)
        {
            yield return null;
            if (m_AudioSource != null)
            {
                m_AudioSource.volume = volume;
            }
        }
    }
}

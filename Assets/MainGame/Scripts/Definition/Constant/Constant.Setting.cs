//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace MainGame
{
    public static partial class Constant
    {
        public static class Setting
        {
            public const string Language = "Setting.Language";
            public const string SoundGroupMuted = "Setting.{0}Muted";
            public const string SoundGroupVolume = "Setting.{0}Volume";
            public const string MusicMuted = "Setting.MusicMuted";
            public const string MusicVolume = "Setting.MusicVolume";
            public const string SoundMuted = "Setting.SoundMuted";
            public const string SoundVolume = "Setting.SoundVolume";
            public const string UISoundMuted = "Setting.UISoundMuted";
            public const string UISoundVolume = "Setting.UISoundVolume";
            public const string CloudSyncRegistry = "Setting.CloudSync.Registry";
            public const string CloudSyncSchemaVersion = "Setting.CloudSync.Version";
            public const string CloudSyncDirty = "Setting.CloudSync.Dirty";
            /// <summary>是否使用触屏控制窗口。无此键时按平台自动判定。</summary>
            public const string UseTouchControl = "Setting.UseTouchControl";
        }
    }
}

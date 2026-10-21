//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace MainGame
{
    public static partial class Constant
    {
        /// <summary>
        /// 层。
        /// </summary>
        public static class Layer
        {
            public const string DefaultLayerName = "Default";
            public static readonly int DefaultLayerId = LayerMask.NameToLayer(DefaultLayerName);

            public const string UILayerName = "UI";
            public static readonly int UILayerId = LayerMask.NameToLayer(UILayerName);

            public const string CharacterLayerName = "Character";
            public static readonly int CharacterLayerId = LayerMask.NameToLayer(CharacterLayerName);

            public const string CameraLayerName = "Camera";
            public static readonly int CameraLyerId = LayerMask.NameToLayer(CameraLayerName);

            public const string WallLayerName = "Wall";
            public static readonly int WallLayerId = LayerMask.NameToLayer(WallLayerName);
        }
    }
}

//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    public abstract class ProcedureBase : GameFramework.Procedure.ProcedureBase
    {
        protected int m_EnterProcedure = (int)ProcedureEnum.游戏流程;
        protected string m_EnterScene = "Game";

        protected void ChangeToTargetProcedure(ProcedureOwner procedureOwner, ProcedureEnum procedureId)
        {
            switch (procedureId)
            {
                case ProcedureEnum.菜单流程:
                    ChangeState<ProcedureMenu>(procedureOwner);
                    break;
                case ProcedureEnum.游戏流程:
                    ChangeState<ProcedureGame>(procedureOwner);
                    break;

                default:
                    Log.Warning("procedure not exist '{0}'.", procedureId);
                    break;
            }
        }
    }

    /// <summary>
    /// Procedure枚举。
    /// </summary>
    public enum ProcedureEnum : int
    {
        /// <summary>
        /// 未知。
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 菜单流程
        /// </summary>
        菜单流程 = 1,

        /// <summary>
        /// 游戏流程
        /// </summary>
        游戏流程 = 2,
    }
}

using UnityGameFramework.Runtime;

namespace MainGame
{
    public static class ProcedureExtension
    {
        public static ProcedureFlowBase CurrentFlowProcedure(this ProcedureComponent procedureComponent)
        {
            if (procedureComponent.CurrentProcedure is ProcedureFlowBase procedureInstance)
            {
                return procedureInstance;
            }

            Log.Warning("Now procedure not flow procedure");
            return null;
        }
    }
}

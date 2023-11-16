namespace Unity.Muse.Common
{
    internal interface IMaskOperator : IOperator
    {
        public string GetMask();
        public bool IsClear();
    }
}
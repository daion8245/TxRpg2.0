namespace TxRpg.Core
{
    /// <summary>
    /// 오브젝트 풀에서 관리되는 객체가 구현하는 인터페이스.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}

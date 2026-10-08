namespace Game.Core
{
    /// <summary>
    /// 游戏全局状态枚举。
    /// 监听 GameFlow.OnStateChanged 时会拿到这个值。
    /// </summary>
    public enum GameState
    {
        Playing,   // 游戏中
        Won,       // 胜利
        Lost       // 失败（本期 S 级不实现，但留位）
    }
}
namespace HexGame.Tools
{
    /// <summary>
    /// Specialized interface for ongoing tools that stay selected and have an active state.
    /// </summary>
    public interface IActiveTool : ITool
    {
        bool IsEnabled { get; set; }
        void HandleHighlighting(Hex oldHex, Hex newHex);
    }
}
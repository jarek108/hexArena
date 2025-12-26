namespace HexGame.Tools
{
    /// <summary>
    /// Marker interface for tools that execute once and don't stay selected.
    /// Selection of such a tool triggers its OnActivate and immediately reverts 
    /// the ToolManager to the previous ongoing tool.
    /// </summary>
    public interface IToggleTool : ITool
    {
    }
}

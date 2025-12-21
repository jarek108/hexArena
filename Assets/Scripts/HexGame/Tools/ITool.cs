namespace HexGame.Tools
{
    public interface ITool
    {
        string ToolName { get; }
        void OnActivate();
        void OnDeactivate();
        void HandleInput(Hex hoveredHex);
    }
}

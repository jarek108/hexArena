namespace HexGame.Tools
{
    public interface ITool
    {
        void OnActivate();
        void OnDeactivate();
        void HandleInput(Hex hoveredHex);
    }
}
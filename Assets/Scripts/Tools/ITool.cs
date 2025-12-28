namespace HexGame.Tools
{
    public interface ITool
    {
        bool CheckRequirements(out string reason);
        void OnActivate();
        void OnDeactivate();
        void HandleInput(Hex hoveredHex);
    }
}
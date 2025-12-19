using NUnit.Framework;
using HexGame;

public class HexDataTests
{
    [Test]
    public void HexData_StoresCoordinatesCorrectly()
    {
        HexData data = new HexData(1, -2);
        Assert.AreEqual(1, data.Q);
        Assert.AreEqual(-2, data.R);
        Assert.AreEqual(1, data.S); // s = -q - r = -1 - (-2) = 1
    }

    [Test]
    public void HexData_Defaults_AreCorrect()
    {
        HexData data = new HexData(0, 0);
        Assert.AreEqual(0, data.Elevation);
        Assert.AreEqual(TerrainType.Plains, data.TerrainType);
    }
}
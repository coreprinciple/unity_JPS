public class JPSDotsDataLinker
{
    public int xSize = 30;
    public int ySize = 30;

    public int startX, startY;
    public int endX, endY;

    public float gridWidth;
    public float gridHeight;

    public static JPSDotsDataLinker Instance()
    {
        if (_instance == null)
            _instance = new JPSDotsDataLinker();
        return _instance;
    }

    private JPSDotsDataLinker() { }
    private static JPSDotsDataLinker _instance;
}

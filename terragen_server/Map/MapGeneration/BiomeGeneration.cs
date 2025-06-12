namespace terragen_server.Map.MapGeneration;

public class BiomeGeneration(int seed, string myEncodedTree2D, string myEncodedTree3D)
{
    private const float Frequency = (float)0.005;

    /// <summary>
    /// generates a 2D noise map based on the provided size and coordinates.
    /// </summary>
    /// <param name="size">int map size</param>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>float table with hight map</returns>
    public float[] GenerateNoise2D(int size, int x, int z)
    {
        var buffer = new float[size * size];
        var fn = FastNoise2.FromEncodedNodeTree(myEncodedTree2D);
        
        fn.GenUniformGrid2D(buffer, x*size, z*size, size, size, Frequency, seed);
        return buffer;
    }
    
    public float[] GenerateNoise2D(int size, int x, int z, float frequency, string myEncodedTree)
    {
        var buffer = new float[size * size];
        var fn = FastNoise2.FromEncodedNodeTree(myEncodedTree);
        
        fn.GenUniformGrid2D(buffer, x*size, z*size, size, size, frequency, seed);
        return buffer;
    }
    
    public float[] GenerateNoise2DForTrees(int size, int x, int z)
    {
        var buffer = new float[size * size];
        string myEncodedTree2DTree = "HgANAAMAAAAAAABAHgALAAEAAAAAAAAAAQAAAAAAAAABBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAETAFK4nj8IAAAAAAA/AAAAAAABEwBxPQpACAA=";
        const float TreesFrequency = 0.05f;
        
        var fn = FastNoise2.FromEncodedNodeTree(myEncodedTree2DTree);
        
        fn.GenUniformGrid2D(buffer, x*size, z*size, size, size, TreesFrequency, seed);
        return buffer;
    }

    /// <summary>
    /// generates a 3D noise map based on the provided size, height, and coordinates.
    /// </summary>
    /// <param name="size">int map size</param>
    /// <param name="sizeHeight"></param>
    /// <param name="x">X coordinate</param>
    /// <param name="z">Z coordinate</param>
    /// <returns>float table with generated map</returns>
    public float[] GenerateNoise3D(int size, int sizeHeight, int x, int z)
    {
        var buffer = new float[size * size * sizeHeight];
        var fn = FastNoise2.FromEncodedNodeTree(myEncodedTree3D);
        
        fn.GenUniformGrid3D(buffer, x*size, z*size, 0, size, size, sizeHeight, Frequency, seed);
        return buffer;
    }
}
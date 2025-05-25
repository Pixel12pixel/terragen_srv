namespace terragen_server.Map.MapGeneration;

public class BiomeGeneration(int seed, string myEncodedTree2D, string myEncodedTree3D)
{
    private const float Frequency = (float)0.005;
    
    public float[] GenerateNoise2D(int size, int x, int y)
    {
        var buffer = new float[size * size];
        var fn = FastNoise2.FromEncodedNodeTree(myEncodedTree2D);
        
        fn.GenUniformGrid2D(buffer, x*size, y*size, size, size, Frequency, seed);
        return buffer;
    }

    public float[] GenerateNoise3D(int size, int sizeHeight, int x, int y)
    {
        var buffer = new float[size * size * sizeHeight];
        var fn = FastNoise2.FromEncodedNodeTree(myEncodedTree3D);
        
        fn.GenUniformGrid3D(buffer, x*size, y*size, 0, size, size, sizeHeight, Frequency, seed);
        return buffer;
    }
}
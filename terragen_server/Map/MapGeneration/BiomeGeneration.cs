using terragen_server.DataModels.World;

namespace terragen_server.Map.MapGeneration;

public class BiomeGeneration(int seed, string myEncodedTree2D, string myEncodedTree3D, float caveDensity)
{
    private const float Frequency = (float)0.005;
    private const int ChunkHeight = 256;
    private const int ChunkSize = 16;

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

    public byte MountainsBlockValue(int x, int z, int y, int h, float caveValue, byte chunkData)
    {
        
        var index = (y * ChunkSize + z) * ChunkSize + x;
                    
        if(y==0) return (byte)BlockType.BEDROCK; //bedrock
        else if ((y <= h) && (caveValue < caveDensity))
        {
            if(y>200&&y<=230) return (byte)BlockType.STONE;
            else if(y>230) return (byte)BlockType.SNOW; //snow
            else if (y<=h-5) return (byte)BlockType.STONE;
            else if (y<=h-1) return (byte)BlockType.DIRT;
            else return (byte)BlockType.GRASS;
        }
        else if(chunkData!=(byte)BlockType.LEAVES) return (byte)BlockType.AIR;
        else return 0;
    }
    
    public byte ForestBlockValue(int x, int z, int y, int h, float caveValue, byte chunkData)
    {
        
        var index = (y * ChunkSize + z) * ChunkSize + x;
                    
        if(y==0) return (byte)BlockType.BEDROCK; //bedrock
        else if ((y <= h) && (caveValue < caveDensity))
        {
            if (y<=h-5) return (byte)BlockType.STONE;
            else if (y<=h-1) return (byte)BlockType.DIRT;
            else return (byte)BlockType.GRASS;
        }
        else if(chunkData!=(byte)BlockType.LEAVES) return (byte)BlockType.AIR; 
        return 0;
    }
    
    public byte DesertBlockValue(int x, int z, int y, int h, float caveValue, byte chunkData)
    {
        
        var index = (y * ChunkSize + z) * ChunkSize + x;
                    
        if(y==0) return (byte)BlockType.BEDROCK; //bedrock
        else if ((y <= h) && (caveValue < caveDensity))
        {
            if (y<=h-5) return (byte)BlockType.STONE;
            else return (byte)BlockType.SAND;
        }
        else if(chunkData!=(byte)BlockType.LEAVES) return (byte)BlockType.AIR;
        return 0;
    }
    
    public void GenTree(int x, int z, int h, ref ChunkModel chunk)
    {

        /*generowanie pnia*/
        for (int dy = 1; dy <= 4; ++dy) {
            int yy = h + dy;
            int index = (yy * ChunkSize + z) * ChunkSize + x;
            chunk.ChunkData[index] = (byte)BlockType.WOOD;
        }

        /*generowanie lisci*/
        for (int dx = -2; dx <= 2; ++dx) {
            for (int dy = 3; dy <= 6; ++dy) {
                for (int dz = -2; dz <= 2; ++dz) {

                    if (dx*dx + (dy-4)*(dy-4) + dz*dz > 8)
                        continue;

                    int xx = x + dx;
                    int yy = h + dy;
                    int zz = z + dz;

                    int idx = (yy * ChunkSize + zz) * ChunkSize + xx;

                    if (chunk.ChunkData[idx] == (byte)BlockType.AIR) {
                        chunk.ChunkData[idx] = (byte)BlockType.LEAVES;
                    }
                }
            }
        }
    }
}
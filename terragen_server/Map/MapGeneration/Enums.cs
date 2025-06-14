namespace terragen_server.Map.MapGeneration;

/// <summary>
/// BlockType enum representing different types of blocks in the terrain.
/// </summary>
public enum BlockType : byte
{
    AIR =0,
    GRASS =1,
    DIRT =2,
    STONE =3,
    WOOD =4,
    LEAVES =5,
    BEDROCK =6,
    SNOW =7,
    SAND =8
}

public enum Biome : byte
{
    Mountains =0,
    Forest =1,
    Desert =2
}
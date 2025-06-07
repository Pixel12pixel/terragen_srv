namespace terragen_server.Netcode;

public class ChunkSerializer
{
    
    private const int SectionSize = 16 * 16 * 16; // 4096

    public List<List<(byte value, ushort count)>> CompressChunkRle(byte[] chunkData)
    {
        if (chunkData.Length != 16 * SectionSize)
        {
            SrvLog.Console.Log("Chunk_Serializer", "Invalid chunk data length. Expected 65536 bytes.", SrvLog.LogTypes.ERROR);
            return null;
        }

        var compressedSections = new List<List<(byte value, ushort count)>>();

        for (var section = 0; section < 16; section++)
        {
            var offset = section * SectionSize;
            var sectionData = new Span<byte>(chunkData, offset, SectionSize);
            var compressed = CompressSectionRle(sectionData);
            compressedSections.Add(compressed);
        }

        return compressedSections;
    }

    private List<(byte value, ushort count)> CompressSectionRle(Span<byte> sectionData)
    {
        var result = new List<(byte value, ushort count)>();
        var i = 0;

        while (i < sectionData.Length)
        {
            var current = sectionData[i];
            ushort count = 1;

            while (i + count < sectionData.Length && sectionData[i + count] == current && count < ushort.MaxValue)
                count++;

            result.Add((current, count));
            i += count;
        }

        return result;
    }

    
}
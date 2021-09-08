using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MGF
{
    public enum Version
    {
        MechAssault = 4,
        MechAssault2LW = 2
    }

    public interface ISerializable
    {
        public abstract void Serialize(in BinaryWriter bin);
    }

    public class Header : ISerializable
    {
        public const uint Signature = 0x2066676d; // "mgf "
        public byte VersionMajor;
        public const byte VersionMinor = 1;
        public const ushort ZZ = 0x5A5A;
        public const uint Padding = 0;
        public int FileRecordCount;
        public int FileRecordChunkLength;
        public int FileRecordChunkOffset;
        public int DirectoryEntryCount;
        public int DirectoryEntryChunkLength;
        public int DirectoryEntryChunkOffset;
        public int StringChunkLength;
        public int StringChunkOffset;
        public int Unknown = 0;
        public int PageSize = 2048;

        public Header(Version gameVersion, int fileRecordCount, int directoryEntryCount, int dirNameChunkLength)
        {
            FileRecordCount = fileRecordCount;
            FileRecordChunkOffset = 64;
            DirectoryEntryChunkLength = directoryEntryCount * 24;
            DirectoryEntryCount = directoryEntryCount;

            if (gameVersion == MGF.Version.MechAssault)
            {
                VersionMajor = 2;
                FileRecordChunkLength = fileRecordCount * 28;

                PageSize = 2048;
            }
            else if (gameVersion == MGF.Version.MechAssault2LW)
            {
                VersionMajor = 4;
                FileRecordChunkLength = fileRecordCount * 32;
                PageSize = 65536;
            }

            DirectoryEntryChunkOffset = FileRecordChunkOffset + FileRecordChunkLength;
            StringChunkLength = dirNameChunkLength;
            StringChunkOffset = DirectoryEntryChunkOffset + DirectoryEntryChunkLength;
        }

        public void Serialize(in BinaryWriter bin)
        {
            bin.Write(Signature);
            bin.Write(VersionMajor);
            bin.Write(VersionMinor);
            bin.Write(ZZ);
            bin.Write(Padding);
            bin.Write(FileRecordCount);
            bin.Write(FileRecordChunkLength);
            bin.Write(FileRecordChunkOffset);
            bin.Write(DirectoryEntryCount);
            bin.Write(DirectoryEntryChunkLength);
            bin.Write(DirectoryEntryChunkOffset);
            bin.Write(StringChunkLength);
            bin.Write(StringChunkOffset);
            bin.Write(Unknown);
            bin.Write(PageSize);
            bin.Write(Encoding.ASCII.GetBytes("\0\0\0\0\0\0\0\0\0\0\0\0"));
        }
    }

    public abstract class FileRecord : ISerializable
    {
        public uint DirectoryEntryIndex;
        public uint LastModifiedTimeUnix;
        public uint FileChecksum;
        public int FilePathHash;
        public uint FileLength;
        public uint FileOffset;
        public FileInfo _fileInfo;

        public abstract void Serialize(in BinaryWriter bin);
    }
    public class FileRecordMA1 : FileRecord
    {
        public override void Serialize(in BinaryWriter bin)
        {
            bin.Write(0x00000000);
            bin.Write(DirectoryEntryIndex);
            bin.Write(LastModifiedTimeUnix);
            bin.Write(FileChecksum);
            bin.Write(FilePathHash);
            bin.Write(FileLength);
            bin.Write(FileOffset);
        }
    }

    public class FileRecordMA2 : FileRecord
    {
        public override void Serialize(in BinaryWriter bin)
        {
            bin.Write(DirectoryEntryIndex);
            bin.Write(FileChecksum);
            bin.Write(FilePathHash);
            bin.Write(FileLength);
            bin.Write(FileLength);
            bin.Write(LastModifiedTimeUnix);
            bin.Write(FileOffset);
            bin.Write(0x0012F700);
        }
    }

    public class DirectoryEntry : ISerializable
    {
        public int FilepathHash;
        public int ParentIndex;
        public int FirstChildIndex;
        public int SiblingIndex;
        public uint DirNameOffset;
        public int FileRecordIndex;

        public void Serialize(in BinaryWriter bin)
        {
            bin.Write(FilepathHash);
            bin.Write(ParentIndex);
            bin.Write(FirstChildIndex);
            bin.Write(SiblingIndex);
            bin.Write(DirNameOffset);
            bin.Write(FileRecordIndex);
        }
    }
}

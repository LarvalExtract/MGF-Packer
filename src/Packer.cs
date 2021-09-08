using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Force.Crc32;

namespace MGF
{
    class Packer<FileRecordType> where FileRecordType : MGF.FileRecord, new()
    {
        private string SourceFolder;
        private MGF.Version GameVersion;

        private List<MGF.FileRecord> FileRecords = new();
        private List<MGF.DirectoryEntry> DirectoryEntries = new();
        private MemoryStream DirectoryNameStringBuffer;

        private Dictionary<uint, uint> ExistingFileOffsets = new();

        private int FileIndex = 0;
        private int DirectoryIndex = 0;
        private uint LastFileOffset = 0;

        public Packer(string sourceFolder)
        {
            SourceFolder = sourceFolder;
            GameVersion = typeof(FileRecordType) == typeof(FileRecordMA1) ? MGF.Version.MechAssault : MGF.Version.MechAssault2LW;

            DirectoryNameStringBuffer = new MemoryStream();
            DirectoryNameStringBuffer.Write(Encoding.ASCII.GetBytes("MGI\0\\\0\0\0"), 0, 8);

            Console.WriteLine("Collating directories...");

            // Add implicit root directory entry
            DirectoryEntries.Add(new DirectoryEntry
            {
                FilepathHash = MGF.Hash.GenerateHash("\\"),
                ParentIndex = -1,
                FirstChildIndex = -1,
                SiblingIndex = -1,
                DirNameOffset = 4,
                FileRecordIndex = -1
            });

            if (IsPathDirectory(SourceFolder))
            {
                TraverseDirectory(SourceFolder, DirectoryIndex);
            }

            Console.WriteLine($"Packing {DirectoryEntries.Count} directories ({FileRecords.Count} files)...");

            ExistingFileOffsets.Clear();
        }

        private void TraverseDirectory(string path, int parentIndex)
        {
            int previousDirectoryIndex = parentIndex;
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                var relativePath = "\\" + Path.GetRelativePath(SourceFolder, entry);
                var filename = Path.GetFileName(relativePath) + char.MinValue;
                filename = string.Concat(filename, new string('\0', filename.Length % 4 == 0 ? 0 : 4 - filename.Length % 4));
                int relativePathHash = MGF.Hash.GenerateHash(relativePath);

                bool isFile = !IsPathDirectory(entry);

                DirectoryEntries.Add(new DirectoryEntry
                {
                    FilepathHash = !isFile ? relativePathHash : 0,
                    ParentIndex = parentIndex,
                    SiblingIndex = -1,
                    FirstChildIndex = -1,
                    DirNameOffset = (uint)DirectoryNameStringBuffer.Position,
                    FileRecordIndex = isFile ? FileIndex : -1
                });

                DirectoryNameStringBuffer.Write(Encoding.ASCII.GetBytes(filename), 0, filename.Length);
                
                previousDirectoryIndex = parentIndex == previousDirectoryIndex
                    ? DirectoryEntries[previousDirectoryIndex].FirstChildIndex = ++DirectoryIndex
                    : DirectoryEntries[previousDirectoryIndex].SiblingIndex = ++DirectoryIndex;

                if (!isFile)
                {
                    TraverseDirectory(entry, DirectoryIndex);
                }
                else
                {
                    AddFileRecord(entry, relativePathHash);
                }
            }
        }

        private void AddFileRecord(string filepath, int relativePathHash)
        {
            var fileInfo = new FileInfo(filepath);
            var checksum = ~Crc32Algorithm.Compute(File.ReadAllBytes(filepath));

            uint newFileOffset = LastFileOffset = Math.Max(
                LastFileOffset,
                FileRecords.Count > 0
                    ? FileRecords[FileIndex - 1].FileOffset + FileRecords[FileIndex - 1].FileLength
                    : 0
            );

            if (!ExistingFileOffsets.TryAdd(checksum, newFileOffset))
            {
                newFileOffset = ExistingFileOffsets[checksum];
            }

            FileRecords.Add(new FileRecordType
            {
                DirectoryEntryIndex = (uint)DirectoryIndex,
                LastModifiedTimeUnix = (uint)((DateTimeOffset)fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds(),
                FileChecksum = checksum,
                FilePathHash = relativePathHash,
                FileLength = (uint)fileInfo.Length,
                FileOffset = newFileOffset,
                _fileInfo = fileInfo
            });

            FileIndex++;
        }

        public void WriteToFile(string filename)
        {
            Console.WriteLine($"Writing {FileRecords.Count} files...");

            var mgfFile = new FileStream(filename, FileMode.Create);
            using (var binaryWriter = new BinaryWriter(mgfFile))
            {
                var mgfHeader = new MGF.Header(GameVersion, FileRecords.Count, DirectoryEntries.Count, (int)DirectoryNameStringBuffer.Length);
                mgfHeader.Serialize(binaryWriter);

                foreach (var fileRecord in FileRecords)
                {
                    uint offset = (uint)(mgfHeader.FileRecordChunkOffset + mgfHeader.FileRecordChunkLength + mgfHeader.DirectoryEntryChunkLength + mgfHeader.StringChunkLength);
                    fileRecord.FileOffset += offset;
                    fileRecord.Serialize(binaryWriter);
                }

                foreach (var directoryEntry in DirectoryEntries)
                {
                    directoryEntry.Serialize(binaryWriter);
                }

                DirectoryNameStringBuffer.Seek(0, SeekOrigin.Begin);
                DirectoryNameStringBuffer.CopyTo(mgfFile);
                DirectoryNameStringBuffer.Close();

                const int bufSize = 65536;
                var fileBuffer = new byte[bufSize];

                var filesWritten = new HashSet<uint>();

                foreach (var fileRecord in FileRecords)
                {
                    if (filesWritten.Contains(fileRecord.FileChecksum))
                    {
                        continue;
                    }

                    using (var file = new FileStream(fileRecord._fileInfo.FullName, FileMode.Open, FileAccess.Read))
                    {
                        int bytesRemaining = (int)fileRecord.FileLength;

                        while (bytesRemaining > 0)
                        {
                            int bytesToCopy = Math.Min(bufSize, bytesRemaining);
                            file.Read(fileBuffer, 0, bytesToCopy);
                            binaryWriter.Write(fileBuffer, 0, bytesToCopy);

                            bytesRemaining -= bufSize;
                        }

                        filesWritten.Add(fileRecord.FileChecksum);

                        Console.WriteLine($"Wrote {Path.DirectorySeparatorChar + Path.GetRelativePath(SourceFolder, fileRecord._fileInfo.FullName)} to offset {fileRecord.FileOffset} ({fileRecord.FileLength} bytes)");
                    }
                }

                int totalBytesWritten = (int)binaryWriter.BaseStream.Length;
                int remainder = totalBytesWritten % mgfHeader.PageSize;
                for (int bytesRemaining = remainder == 0 ? 0 : mgfHeader.PageSize - remainder; bytesRemaining > 0; bytesRemaining--)
                {
                    binaryWriter.Write((byte)0);
                }
            }
        }

        private static bool IsPathDirectory(string path)
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }
    }
}

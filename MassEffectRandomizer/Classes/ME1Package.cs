﻿using Gibbed.IO;
using MassEffectRandomizer.Classes.TLK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MassEffectRandomizer.Classes
{
    public enum MEGame
    {
        ME1 = 1,
        ME2,
        ME3,
        UDK
    }

    public sealed class ME1Package : MEPackage
    {
        public readonly MEGame Game = MEGame.ME1;
        const uint packageTag = 0x9E2A83C1;
        public override int NameCount
        {
            get => BitConverter.ToInt32(header, nameSize + 20);
            protected set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 20, sizeof(int));
        }
        public int NameOffset
        {
            get => BitConverter.ToInt32(header, nameSize + 24);
            private set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 24, sizeof(int));
        }
        public override int ExportCount
        {
            get => BitConverter.ToInt32(header, nameSize + 28);
            protected set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 28, sizeof(int));
        }
        public int ExportOffset
        {
            get => BitConverter.ToInt32(header, nameSize + 32);
            private set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 32, sizeof(int));
        }
        public override int ImportCount
        {
            get => BitConverter.ToInt32(header, nameSize + 36);
            protected set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 36, sizeof(int));
        }
        public int ImportOffset
        {
            get => BitConverter.ToInt32(header, nameSize + 40);
            private set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 40, sizeof(int));
        }
        int FreeZoneStart
        {
            get => BitConverter.ToInt32(header, nameSize + 44);
            set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, nameSize + 44, sizeof(int));
        }
        int Generations => BitConverter.ToInt32(header, nameSize + 64);
        int Compression
        {
            get => BitConverter.ToInt32(header, header.Length - 4);
            set => Buffer.BlockCopy(BitConverter.GetBytes(value), 0, header, header.Length - 4, sizeof(int));
        }
        /// <summary>
        /// Indicates an export has been modified in this file
        /// </summary>
        public bool ShouldSave { get; internal set; }
        public List<TalkFile> LocalTalkFiles { get; } = new List<TalkFile>();
        public bool TlksModified => LocalTalkFiles.Any(x => x.Modified);

        static bool isInitialized;
        public static Func<string, ME1Package> Initialize()
        {
            if (isInitialized)
            {
                throw new Exception(nameof(ME1Package) + " can only be initialized once");
            }
            else
            {
                isInitialized = true;
                return f => new ME1Package(f);
            }
        }

        public ME1Package(string path)
        {
            //Debug.WriteLine(" >> Opening me1 package " + path);
            FileName = Path.GetFullPath(path);
            MemoryStream tempStream = new MemoryStream();
            if (!File.Exists(FileName))
                throw new FileNotFoundException("PCC file not found");
            using (FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read))
            {
                FileInfo tempInfo = new FileInfo(FileName);
                tempStream.WriteFromStream(fs, tempInfo.Length);
                if (tempStream.Length != tempInfo.Length)
                {
                    throw new FileLoadException("File not fully read in. Try again later");
                }
            }

            tempStream.Seek(12, SeekOrigin.Begin);
            int tempNameSize = tempStream.ReadValueS32();
            tempStream.Seek(64 + tempNameSize, SeekOrigin.Begin);
            int tempGenerations = tempStream.ReadValueS32();
            tempStream.Seek(36 + tempGenerations * 12, SeekOrigin.Current);

            tempStream.ReadValueU32(); //Compression Type. We read this from header[] in MEPackage.cs intead when accessing value
            int tempPos = (int)tempStream.Position;
            tempStream.Seek(0, SeekOrigin.Begin);
            header = tempStream.ReadBytes(tempPos);
            tempStream.Seek(0, SeekOrigin.Begin);

            if (magic != packageTag)
            {
                throw new FormatException("This is not an ME1 Package file. The magic number is incorrect.");
            }
            MemoryStream listsStream;
            if (IsCompressed)
            {
                //Aquadran: Code to decompress package on disk.
                //Do not set the decompressed flag as some tools use this flag
                //to determine if the file on disk is still compressed or not
                //e.g. soundplorer's offset based audio access
                listsStream = CompressionHelper.DecompressME1orME2(tempStream);

                //Correct the header
                IsCompressed = false;
                listsStream.Seek(0, SeekOrigin.Begin);
                listsStream.WriteBytes(header);

                // Set numblocks to zero
                listsStream.WriteValueS32(0);
                //Write the magic number
                listsStream.WriteBytes(new byte[] { 0xF2, 0x56, 0x1B, 0x4E });
                // Write 4 bytes of 0
                listsStream.WriteValueS32(0);
            }
            else
            {
                //listsStream = tempStream;
                listsStream = new MemoryStream();
                tempStream.WriteTo(listsStream);
            }
            tempStream.Dispose();


            ReadNames(listsStream);
            ReadImports(listsStream);
            ReadExports(listsStream);
            ReadLocalTLKs();
        }

        private void ReadLocalTLKs()
        {
            LocalTalkFiles.Clear();
            var tlkFileSets = Exports.Where(x => x.ClassName == "BioTlkFileSet" && !x.ObjectName.StartsWith("Default__")).ToList();
            var exportsToLoad = new List<IExportEntry>();
            foreach (var tlkFileSet in tlkFileSets)
            {
                MemoryStream r = new MemoryStream(tlkFileSet.Data);
                r.Position = tlkFileSet.propsEnd();
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int langRef = r.ReadInt32();
                    r.ReadInt32(); //second half of name
                    string lang = getNameEntry(langRef);
                    int numTlksForLang = r.ReadInt32(); //I beleive this is always 2. Hopefully I am not wrong.
                    int maleTlk = r.ReadInt32();
                    int femaleTlk = r.ReadInt32();

                    if (lang.Equals("int", StringComparison.InvariantCultureIgnoreCase))
                    {
                        exportsToLoad.Add(getUExport(maleTlk));
                        exportsToLoad.Add(getUExport(femaleTlk));
                        break;
                    }

                    //r.ReadInt64();
                    //talkFiles.Add(new TalkFile(pcc, r.ReadInt32(), true, langRef, index));
                    //talkFiles.Add(new TalkFile(pcc, r.ReadInt32(), false, langRef, index));
                }
            }

            foreach (var exp in exportsToLoad)
            {
                //Debug.WriteLine("Loading local TLK: " + exp.GetIndexedFullPath);
                LocalTalkFiles.Add(new TalkFile(exp));
            }
        }

        private void ReadNames(MemoryStream fs)
        {
            names = new List<string>();
            fs.Seek(NameOffset, SeekOrigin.Begin);
            for (int i = 0; i < NameCount; i++)
            {
                int len = fs.ReadValueS32();
                string s = "";
                if (len > 0)
                {
                    s = fs.ReadString((uint)(len - 1));
                    fs.Seek(9, SeekOrigin.Current);
                }
                else
                {
                    len *= -1;
                    for (int j = 0; j < len - 1; j++)
                    {
                        s += (char)fs.ReadByte();
                        fs.ReadByte();
                    }
                    fs.Seek(10, SeekOrigin.Current);
                }
                names.Add(s);
            }
        }

        private void ReadImports(MemoryStream fs)
        {
            imports = new List<ImportEntry>();
            fs.Seek(ImportOffset, SeekOrigin.Begin);
            for (int i = 0; i < ImportCount; i++)
            {
                ImportEntry import = new ImportEntry(this, fs);
                import.Index = i;
                imports.Add(import);
            }
        }

        private void ReadExports(MemoryStream fs)
        {
            exports = new List<IExportEntry>();
            fs.Seek(ExportOffset, SeekOrigin.Begin);
            for (int i = 0; i < ExportCount; i++)
            {
                ME1ExportEntry exp = new ME1ExportEntry(this, fs);
                exp.Index = i;
                exports.Add(exp);
            }
        }

        /// <summary>
        ///     save PCC to same file by reconstruction if possible, append if not
        /// </summary>
        public void save()
        {
            save(FileName);
        }

        /// <summary>
        ///     save PCC by reconstruction if possible, append if not
        /// </summary>
        /// <param name="path">full path + file name.</param>
        public void save(string path)
        {
            if (CanReconstruct)
            {
                saveByReconstructing(path);
            }
            else
            {
                throw new Exception($"Cannot save ME1 packages with a SeekFreeShaderCache.");
            }
        }

        /// <summary>
        ///     save PCCObject to file by reconstruction from data
        /// </summary>
        /// <param name="path">full path + file name.</param>
        public void saveByReconstructing(string path)
        {
            try
            {
                this.IsCompressed = false;
                MemoryStream m = new MemoryStream();
                m.WriteBytes(header);

                // Set numblocks to zero
                m.WriteValueS32(0);
                //Write the magic number (What is this?)
                m.WriteBytes(new byte[] { 0xF2, 0x56, 0x1B, 0x4E });
                // Write 4 bytes of 0
                m.WriteValueS32(0);

                //name table
                NameOffset = (int)m.Position;
                NameCount = names.Count;
                foreach (string name in names)
                {
                    m.WriteValueS32(name.Length + 1);
                    m.WriteString(name);
                    m.WriteByte(0);
                    m.WriteValueS32(0);
                    m.WriteValueS32(458768);
                }
                //import table
                ImportOffset = (int)m.Position;
                ImportCount = imports.Count;
                foreach (ImportEntry e in imports)
                {
                    m.WriteBytes(e.Header);
                }
                //export table
                ExportOffset = (int)m.Position;
                ExportCount = exports.Count;
                for (int i = 0; i < exports.Count; i++)
                {
                    IExportEntry e = exports[i];
                    e.HeaderOffset = (uint)m.Position;
                    m.WriteBytes(e.Header);
                }
                //freezone
                int FreeZoneSize = expDataBegOffset - FreeZoneStart;
                FreeZoneStart = (int)m.Position;
                m.Write(new byte[FreeZoneSize], 0, FreeZoneSize);
                expDataBegOffset = (int)m.Position;
                //export data
                for (int i = 0; i < exports.Count; i++)
                {
                    IExportEntry e = exports[i];
                    e.DataOffset = (int)m.Position;
                    e.DataSize = e.Data.Length;
                    m.WriteBytes(e.Data);
                    long pos = m.Position;
                    m.Seek(e.HeaderOffset + 32, SeekOrigin.Begin);
                    m.WriteValueS32(e.DataSize);
                    m.WriteValueS32(e.DataOffset);
                    m.Seek(pos, SeekOrigin.Begin);
                }
                //update header
                m.Seek(0, SeekOrigin.Begin);
                m.WriteBytes(header);

                File.WriteAllBytes(path, m.ToArray());
                AfterSave();
                ShouldSave = false; //mark as no longer needing save
                LocalTalkFiles.ForEach(x => x.Modified = false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("PCC Save error:\n" + ex.Message);
            }
        }
    }
}

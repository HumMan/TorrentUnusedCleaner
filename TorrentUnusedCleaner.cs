using Alphaleonis.Win32.Filesystem;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TorrentUnusedCleaner
{
    class TorrentUnusedCleaner
    {
        public static string GetSavePath(string filename)
        {
            var parser = new BencodeParser();
            using (var stream = File.OpenRead(filename))
            {
                var torrent = parser.Parse<BDictionary>(stream);
                var save_path = torrent["save_path"].ToString();
                return save_path;
            }
        }

        public static List<string> ListTorrentFiles(string filename, bool includeDisplayNameInPath)
        {
            List<string> files = new List<string>();
            var parser = new BencodeParser();
            using (var stream = File.OpenRead(filename))
            {
                var torrent = parser.Parse<Torrent>(stream);
                if (torrent.File != null)
                    files.Add(torrent.File.FileName);
                else
                {
                    foreach (var f in torrent.Files)
                    {
                        if (includeDisplayNameInPath)
                            files.Add(Path.Combine(torrent.DisplayName, f.FullPath));
                        else
                            files.Add(f.FullPath);
                    }
                }
            }
            return files;
        }

        public struct TTorrentInfo
        {
            public string caption;
            public string[] labels;
            public string path;
        }

        public static Dictionary<string, TTorrentInfo> uTorrentListResume(string filename)
        {
            Dictionary<string, TTorrentInfo> files = new Dictionary<string, TTorrentInfo>();

            var parser = new BencodeParser();
            using (var stream = File.OpenRead(filename))
            {
                var resume = parser.Parse<BDictionary>(stream);
                foreach (var file in resume)
                {
                    if (file.Value is BDictionary)
                    {
                        var path = (file.Value as BDictionary)["path"].ToString();
                        var name = Path.GetFileName(file.Key.ToString());
                        var caption = (file.Value as BDictionary)["caption"].ToString();
                        var labels = (file.Value as BDictionary)["labels"] as BList;
                        var labels_list = new List<string>();
                        foreach (BString l in labels)
                            labels_list.Add(l.Value.ToString());

                        var new_tor = new TTorrentInfo();
                        new_tor.caption = caption;
                        new_tor.labels = labels_list.ToArray();
                        new_tor.path = path;
                        files.Add(name.ToLower(), new_tor);
                    }
                }
            }

            return files;
        }

        internal static void UTorrentFindUnusedFiles(string torrentsDirPath, string filesDirPath, string resumeFilePath, Action<int> reportProgress)
        {
            var resume = uTorrentListResume(resumeFilePath);

            var all_files = FilesListToHashSet(torrentsDirPath, "*.*");
            var all_torrents = FilesListToHashSet(torrentsDirPath, "*.torrent");

            var unused_torrens = new List<string>();
            var used_files = new HashSet<string>();

            foreach (var f in all_torrents)
            {
                if (!resume.ContainsKey(Path.GetFileName(f)))
                {
                    unused_torrens.Add(f);
                }
            }

            reportProgress(10);

            if (unused_torrens.Count > 0)
            {
                var result = MessageBox.Show(String.Format("Found {0} unused torrents, move them to the recycle?", unused_torrens.Count), "Torrents cleanup", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    DeleteFilesToRecycle(unused_torrens);
                    MessageBox.Show("Unused torrents were moved to the recycle");
                }
            }

            int processedTorrentsCount = 0;
            foreach (var file in all_torrents)
            {
                if (resume.ContainsKey(Path.GetFileName(file)))
                {
                    var r = resume[Path.GetFileName(file)];
                    var torrentFiles = ListTorrentFiles(file, false);
                    if (torrentFiles.Count == 0)
                    {
                        var full_path = r.path.ToLower();
                        if (all_files.Contains(full_path))
                        {
                            used_files.Add(full_path);
                        }
                    }
                    else
                        foreach (var f in torrentFiles)
                        {
                            var full_path = Path.Combine(r.path, f).ToLower();
                            if (all_files.Contains(full_path))
                            {
                                used_files.Add(full_path);
                            }
                        }
                }
                processedTorrentsCount++;

                if (processedTorrentsCount % 10 == 0)
                    reportProgress(10 + 80 * processedTorrentsCount / all_torrents.Count);
            }

            var unused_files = new List<string>();

            foreach (var f in all_files)
            {
                if (!used_files.Contains(f.ToLower()))
                {
                    unused_files.Add(f);
                }
            }

            reportProgress(100);

            UnusedFilesDialog(unused_files);
        }

        private static HashSet<string> FilesListToHashSet(string dirPath, string mask)
        {
            return new HashSet<string>(new DirectoryInfo(dirPath)
                .EnumerateFiles(mask, System.IO.SearchOption.AllDirectories)
                .Select(i => i.FullName.ToLower()));
        }

        private static void UnusedFilesDialog(List<string> unused_files)
        {
            if (unused_files.Count > 0)
            {
                var result = MessageBox.Show(String.Format("Found {0} unused files, move them to the recycle?", unused_files.Count), "Downloaded files cleanup", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    DeleteFilesToRecycle(unused_files);
                    MessageBox.Show("Unused files were moved to the recycle");
                }
            }
            else
                MessageBox.Show("Unused files not found");
        }

        private static void DeleteFilesToRecycle(List<string> list)
        {
            foreach (var f in list)
                FileSystem.DeleteFile(f, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        public static void QBittorentFindUnusedFiles(string filesDirPath, string torrentsDirPath, Action<int> reportProgress)
        {
            reportProgress(0);

            var all_torrents = FilesListToHashSet(torrentsDirPath, "*.torrent");
            var all_resume = FilesListToHashSet(torrentsDirPath, "*.fastresume");

            reportProgress(20);

            var all_files = new HashSet<string>(new DirectoryInfo(filesDirPath)
                .EnumerateFiles("*.*", System.IO.SearchOption.AllDirectories)
                .Select(i => i.FullName.ToLower()));

            reportProgress(70);

            var unused_torrens = new List<string>();
            var used_files = new HashSet<string>();

            foreach (var p in all_resume)
            {
                var savePath = GetSavePath(p);
                var torrentFiles = ListTorrentFiles(p.Replace(".fastresume", ".torrent"), true);
                foreach (var t in torrentFiles)
                {
                    used_files.Add(Path.Combine(savePath, t).ToLower());
                }
            }

            var unused_files = new List<string>();

            reportProgress(90);

            foreach (var f in all_files)
            {
                if (!used_files.Contains(f.ToLower()))
                {
                    unused_files.Add(f);
                }
            }

            reportProgress(100);

            UnusedFilesDialog(unused_files);
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace dscren
{
    static class Program
    {
        static readonly int LASTHOUR_OF_DAY = 2;
        static readonly string[] ftypes = { ".mp4", ".mov", ".avi", ".tif", ".raf", ".nef", ".cr2", ".arw" };

        static bool RenameFilename(string oldFilename, string newBasename)
        {
            string head = Path.GetDirectoryName(oldFilename) + "\\" + newBasename;
            string tail = Path.GetExtension(oldFilename);

            for (int i = 0; i < 100; i++)
            {
                string temp = (i == 0) ? "" : "(" + i.ToString() + ")";
                string path = head + temp + tail;

                if (path.CompareTo(oldFilename) == 0) return true;
                if (File.Exists(path)) continue;

                File.Move(oldFilename, path);
                return true;
            }
            return false;
        }

        static bool RenameFolderName(string folderName)
        {
            var files = Directory.GetFiles(folderName, "*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return false;

            string minFile = files.OrderBy(f => Path.GetFileName(f)).First();
            string minName = Path.GetFileName(minFile);
            if (minName.Length < 9) return false;

            string newFolderBase = minName.Substring(0, 9);
            string parentDir = Path.GetDirectoryName(folderName);
            string newFolderName = Path.Combine(parentDir, newFolderBase);

            string finalFolderName = newFolderName;
            for (int i = 0; i < 100; i++)
            {
                if (i > 0) finalFolderName = newFolderName + "(" + i + ")";
                if (!Directory.Exists(finalFolderName))
                {
                    Directory.Move(folderName, finalFolderName);
                    return true;
                }
            }
            return false;
        }

        static void RenameTarget(string target)
        {
            if (target.EndsWith(".jpg", true, null))
            {
                RenameByExifDateTime(target);
            }
            else
            {
                RenameByFileDateTime(target);
            }
        }

        static void RenameFileFolder(string target)
        {
            if (Directory.Exists(target))
            {
                try
                {
                    string[] orgFiles = Directory.GetFiles(target, "*");
                    foreach (string name in orgFiles)
                    {
                        if (Directory.Exists(name))
                        {
                            RenameFileFolder(name);
                        }
                        else
                        {
                            RenameTarget(name);
                        }
                    }
                    RenameFolderName(target);
                }
                catch (Exception e)
                {
                    CustomMessageBox.Show("GetFiles Error> " + e.ToString());
                }
            }
            else
            {
                RenameTarget(target);
            }
        }

        static string BuildDateTimeString(DateTime dt)
        {
            int hh = dt.Hour;
            if (hh <= LASTHOUR_OF_DAY)
            {
                hh += 24;
                dt = dt.AddDays(-1);
            }
            return string.Format("{0,4:0000}_{1,2:00}{2,2:00}_{3,2:00}{4,2:00}-{5,2:00}", dt.Year, dt.Month, dt.Day, hh, dt.Minute, dt.Second);
        }

        static bool RenameByFileDateTime(string orgFilename)
        {
            DateTime dt = System.IO.File.GetLastWriteTime(orgFilename);
            string filename = BuildDateTimeString(dt);
            return RenameFilename(orgFilename, filename);
        }

        static bool RenameByExifDateTime(string orgFilename)
        {
            Bitmap bmp = new Bitmap(orgFilename);
            foreach (System.Drawing.Imaging.PropertyItem item in bmp?.PropertyItems)
            {
                if ((item.Id == 0x9003) && (item.Type == 2))
                {
                    string tagstr = Encoding.ASCII.GetString(item.Value);
                    MatchCollection matches = Regex.Matches(tagstr, @"\d+");
                    if (matches.Count < 6) break;

                    DateTime dt = new DateTime(
                        Int32.Parse(matches[0].Value),
                        Int32.Parse(matches[1].Value),
                        Int32.Parse(matches[2].Value),
                        Int32.Parse(matches[3].Value),
                        Int32.Parse(matches[4].Value),
                        Int32.Parse(matches[5].Value)
                    );
                    string filename = BuildDateTimeString(dt);
                    bmp.Dispose();
                    return RenameFilename(orgFilename, filename);
                }
            }
            bmp?.Dispose();
            return RenameByFileDateTime(orgFilename);
        }

        static List<string> ImportDcfFolder()
        {
            var copiedFolders = new List<string>();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Removable || !drive.IsReady)
                    continue;
                string dcimPath = Path.Combine(drive.RootDirectory.FullName, "DCIM");
                if (!Directory.Exists(dcimPath))
                    continue;
                foreach (var dir in Directory.GetDirectories(dcimPath, "???*"))
                {
                    string folderName = Path.GetFileName(dir);
                    string destPath = Path.Combine(desktopPath, folderName);
                    string finalDest = destPath;
                    int idx = 1;
                    while (Directory.Exists(finalDest))
                    {
                        finalDest = destPath + "_" + idx;
                        idx++;
                    }
                    CopyDirectory(dir, finalDest);
                    copiedFolders.Add(finalDest);
                }
            }
            return copiedFolders;
        }

        static void EjectDcfDrive()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Removable || !drive.IsReady)
                    continue;
                string dcimPath = Path.Combine(drive.RootDirectory.FullName, "DCIM");
                if (!Directory.Exists(dcimPath))
                    continue;

                string driveLetter = drive.Name.TrimEnd('\\');
                DialogResult result = CustomMessageBox.Show("コピーし終えたメディアのドライブを取り外しますか？\n\nドライブ名　(" + driveLetter　+")",
                     "DCF Renamer",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information
                );
                if (result == DialogResult.OK)
                {
                    try
                    {
                        var proc = Process.Start("RemoveDrive.exe", driveLetter + " /L");
                        if(proc != null)
                        {
                            proc.WaitForExit(60000);  // 最大60秒待つ
                            if (proc.HasExited)
                            {
                                CustomMessageBox.Show("ドライブ名 (" + driveLetter + ") を取り外しました。");
                                return;
                            }
                            else {
                                proc.Kill();
                            }
                        }
                        CustomMessageBox.Show("ドライブ名 (" + driveLetter + ") を取り外せませんでした。\n手動で取り外し操作を行って下さい。");
                    }
                    catch (Exception e)
                    {
                        CustomMessageBox.Show("Failed to remove drive " + driveLetter + " -> " + e.Message);
                    }
                }
            }
            return;
        }

        static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }


        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string[] targets;
            bool bImported = false;

            if (args.Length < 1)
            {
                DialogResult result = CustomMessageBox.Show(
                    "usage> dcf_renamer フォルダ名\n  or\nusage> dcf_renamer file1 file2 file3 ...\n  or\nリムーバブルドライブの撮影画像フォルダを自動検出し\nデスクトップにリネームコピーしますか？",
                    //"リムーバブルドライブの撮影画像フォルダを自動検出し\nデスクトップにリネームコピーしますか？",
                    "DCF Renamer",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information
                );
                if (result == DialogResult.OK)
                {
                    targets = ImportDcfFolder().ToArray();
                    if (targets.Length == 0)
                    {
                        CustomMessageBox.Show("撮影画像（DCF）があるメディアが\nリムーバブルドライブから見つかりませんでした。", "DCF Renamer");
                        return;
                    }
                    bImported = true;
                }
                else
                {
                    return;
                }
            }
            else
            {
                targets = args;
            }

            foreach (string target in targets)
            {
                RenameFileFolder(target);
            }

            if (bImported) EjectDcfDrive();

           /*
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            */
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dscren
{
    static class Program
    {
        static readonly int LASTHOUR_OF_DAY = 2;
        static readonly string[] ftypes = { ".mp4", ".mov", ".avi", ".tif", ".raf", ".nef", ".cr2", ".arw" };

        static bool rename_filename( string old_filename, string new_basename )
        {
            string head = Path.GetDirectoryName(old_filename) + "\\" + new_basename ;
            string tail = Path.GetExtension(old_filename);

            // 重複による枝番追加は99まで
            for( int i=0; i<100; i++) {
                string temp = (i == 0) ? "" : "(" + i.ToString() + ")";
                string path = head + temp + tail;

                if (path.CompareTo(old_filename) == 0) return true; // すでに同じ名前であれば何もせずに終了する

                if (File.Exists(path)) continue;    // 既存のファイル名とぶつかるなら枝番繰り上げる
                
                File.Move(old_filename, path);  // リネーム実行
                //if (File.Exists(path))        // 成功したかどうかは確認省略
                return true;
            }
            // fail to rename
            return false;
        }

        // フォルダ名をフォルダ直下の最も若いファイル名（文字列として最小）の先頭9文字にリネームする
        //（但しフォルダ名衝突がある場合は枝番を付与する）
        static bool rename_foldername( string folderName )
        {
            // フォルダ直下のファイル名一覧を取得（サブフォルダは無視）
            var files = Directory.GetFiles(folderName, "*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return false;

            // 最も若いファイル名（文字列として最小）を取得
            string minFile = files.OrderBy(f => Path.GetFileName(f)).First();
            string minName = Path.GetFileName(minFile);
            if (minName.Length < 9) return false;

            string newFolderBase = minName.Substring(0, 9);
            string parentDir = Path.GetDirectoryName(folderName);
            string newFolderName = Path.Combine(parentDir, newFolderBase);

            // 枝番付与（最大99まで）
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
            // リネーム失敗
            return false;
        }

        // 対象のファイルをファイルタイプに応じてリネームする
        static void rename_target(string target)
        {
            if (target.EndsWith(".jpg", true, null)) // ignore case of extention
            {
                rename_by_exifdatetime(target);
            }
            else
            {
                rename_by_filedatetime(target);
            }
        }

        static void rename_filefolder(string target)
        {
            // 引数がフォルダのとき　（フォルダ直下の全てのファイルを対象とする）
            if (Directory.Exists(target))
            {
                try {
                    string[] org_files = Directory.GetFiles(target, "*");
                    foreach (string name in org_files)      // フォルダ直下の全てのファイルについて
                    {
                        if (Directory.Exists(name))
                        {
                            rename_filefolder(name); // サブフォルダがあれば再帰的に処理
                        }
                        else
                        {
                            rename_target(name);
                        }
                    }
                    // 最後にこのフォルダ名のリネーム
                    rename_foldername(target);
                }
                catch (Exception e) {
                    MessageBox.Show("GetFiles Error> " + e.ToString());
                }
            }
            // 引数がファイルのとき
            else
            {
                rename_target(target);
            }
        }
        
        static string build_datetimestring( DateTime dt )
        {
            int hh = dt.Hour;
            if( hh <= LASTHOUR_OF_DAY)
            {
                hh += 24;         // replace today's 01:30 to yesterday's 25:30
                dt = dt.AddDays(-1);
            }
            // ex. "2021_0731_2530-12"
            return string.Format("{0,4:0000}_{1,2:00}{2,2:00}_{3,2:00}{4,2:00}-{5,2:00}", dt.Year, dt.Month, dt.Day, hh, dt.Minute, dt.Second ); 
        }

        static bool rename_by_filedatetime(string org_filename)
        {
            // not CreationTime but LastWriteTime
            DateTime dt = System.IO.File.GetLastWriteTime(org_filename);
            string filename = build_datetimestring(dt);

            //MessageBox.Show("File :" + dt.ToString() + " -> " + filename );
            return rename_filename(org_filename, filename) ;
        }

        static bool rename_by_exifdatetime(string org_filename)
        {
            Bitmap bmp = new Bitmap(org_filename);
            // Exifタグを読んで撮影日付データを作成する
            foreach (System.Drawing.Imaging.PropertyItem item in bmp?.PropertyItems)
            {
                // Exif DateTimeOriginal(0x9003) and characters
                if(( item.Id == 0x9003) && (item.Type == 2) )
                {
                    string tagstr = Encoding.ASCII.GetString(item.Value);
                    //tagstr = tagstr.Trim(new char[] { '\0' });

                    MatchCollection matches = Regex.Matches(tagstr, @"\d+") ;
                    if (matches.Count < 6) break;

                    DateTime dt = new DateTime( Int32.Parse(matches[0].Value),      // Years
                                                Int32.Parse(matches[1].Value),      // Months
                                                Int32.Parse(matches[2].Value),      // Days
                                                Int32.Parse(matches[3].Value),      // Hours
                                                Int32.Parse(matches[4].Value),      // Minutes
                                                Int32.Parse(matches[5].Value) );    // Seconds
                    string filename = build_datetimestring(dt);

                    //MessageBox.Show(" Exif :" + dt.ToString() + " -> " + filename) ;
                    bmp.Dispose();
                    return rename_filename(org_filename, filename);
                }
            }
            // as jpeg without exif tag
            bmp?.Dispose();
            return rename_by_filedatetime(org_filename) ;
        }

        // システムのリムーバブルドライブから \DCIM\nnn* フォルダをデスクトップにコピーし、コピー先フォルダのリストを返す
        static List<string> import_dcf_folder()
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
                    // コピー先が既に存在する場合は枝番を付与
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

        // ディレクトリを再帰的にコピーする補助関数
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
        static void Main( string[] args )
        {
            string[] org_targets;

            if (args.Length < 1) {
                DialogResult result = MessageBox.Show(
                    "USAGE> dcf_renamer foldername\n  or\nUSAGE> dcf_renamer file1 file2 file3 ...\n  or\n[OK] to import DCF folder now, and rename them.",
                    "DCF Renamer",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information
                );
                if (result == DialogResult.OK)
                {
                    org_targets = import_dcf_folder().ToArray();
                    if (org_targets.Length == 0)
                    {
                        MessageBox.Show("No DCF folder found in removable drives.");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else {
                org_targets = args;
            }

            foreach (string target in org_targets)
            {
                rename_filefolder(target);
            }

            
           /*
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            */
        }
    }
}

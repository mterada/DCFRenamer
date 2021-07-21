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
        static string foldername = null ;

        static bool rename_execute( string old_filename, string new_basename )
        {
            string head = Path.GetDirectoryName(old_filename) + "\\" + new_basename ;
            string tail = Path.GetExtension(old_filename);

            // フォルダ名候補を最も若い日付文字列に更新する
            string str = new_basename.Substring(0, 9);  // length of "YYYY_MMDD" = 9
            if ((foldername == null) || (foldername.CompareTo(str) > 0) ) {
                foldername = str;
            }

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
            return rename_execute(org_filename, filename) ;
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
                    return rename_execute(org_filename, filename);
                }
            }
            // as jpeg without exif tag
            bmp?.Dispose();
            return rename_by_filedatetime(org_filename) ;
        }

        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            string[] org_files;
            bool bSuccess = false;

            if (args.Length < 1) {
                MessageBox.Show("USAGE> dcf_renamer foldername\n  or\nUSAGE> dcf_renamer file1 file2 file3 ...");
                return;
            }
            if( Directory.Exists(args[0])) {
                // フォルダが引数のとき　（先頭の引数のみ有効）
                try {
                    org_files = Directory.GetFiles(args[0], "*");
                    foreach( string name in org_files)      // フォルダ直下の全てのファイルについて
                    {
                        if (name.EndsWith(".jpg", true, null) ) // JPGならExifタグを読もうとする
                        {
                            bSuccess = rename_by_exifdatetime(name );
                        }
                        else {      // JPGでなければ拡張子リストに載っているもののみ対象とする
                            foreach( string ext in ftypes)
                            {
                                if( name.EndsWith(ext, true, null)) {
                                    bSuccess = rename_by_filedatetime(name );
                                    break;
                                }
                            }
                        }
                        // else  skip
                    }
                    // フォルダ名候補が保存されていたらフォルダ名も更新する
                    if ((foldername != null) && !args[0].Contains(foldername))
                    {
                        string head = Path.GetDirectoryName(args[0]) + "\\" + foldername;
                        for (int i = 0; i < 100; i++)
                        {
                            string path = (i == 0) ? head : string.Format("{0}({1})", head, i);
                            if (Directory.Exists(path)) continue;    // 既存のフォルダ名とぶつかるなら枝番繰り上げる

                            Directory.Move(args[0], path);  // リネーム実行
                            break;
                        }
                    }
                }
                catch( Exception e) {
                    MessageBox.Show("GetFiles Error> " + e.ToString());
                }
            }
            // ファイルが引数のとき　（全ての引数をファイルリストとして扱う）
            else {
                foreach( string name in args ) {
                    if (name.EndsWith(".jpg", true, null) ) // ignore case of extention
                    {
                        bSuccess = rename_by_exifdatetime(name );
                    }
                    else
                    {
                        bSuccess = rename_by_filedatetime(name );
                    }
                }
            }
            /*
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            */
        }
    }
}

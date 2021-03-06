DCF_Renamer.exe (Windows10/64bit）について
			2021/07/18
			Masahiro Terada

＜概要＞
デジカメのメディアからコピーしたフォルダに対して
撮影日時をファイル名文字列にして一括でリネームします。
YYYY_MMDD_hhmm-ss.拡張子

＜制約＞
このツールは私個人の日常ニーズにフィットさせた簡易実装ですので
・モード切替えなどのオプションはなし
・汎用的なツールへのアップデート予定なし
　（事情により機能改善や追加はするかも）
・使用によって生じた不都合に関しては一切責任を負いません
をご了承ください。

ーーーーーーーーーーーーーーーーーーーーーー
使い方
ーーーーーーーーーーーーーーーーーーーーーー
１．フォルダごと実行する
USAGE> DCF_Renamer.exe  foldername
DCF_Renamer（のショートカット）に画像フォルダをドロップする。

（１）ファイル名
　Exif JPEG は DateTimeOriginalタグの情報をもとにして
　"YYYY_MMDD_hhmm-ss.拡張子" 形式の名称にリネームします。
　Exif JPEG以外のファイルタイプはすべてタイムスタンプ情報で
　同様にリネームします。（手抜き仕様）

 【特別ルール 】
　時刻が深夜の2:00までは前日とみなす日付文字列を付与します。
　2021年7月19日の深夜1時59分は、
　2021_0719_0159  ではなく 2021_0718_2559 になります。
　（年末年始をまたいでも同様になる点にご注意ください）

（２）対象範囲
　ファイルは指定したフォルダ直下のみ。
　対象拡張子は jpg,mov,avi,mp4,tif,raf,nef,cr2,arw のみ。
　その他の拡張子のファイルはリネームしません。

（３）重複処理
　既に同名ファイルがある、時分秒が重複する（連写等）場合は、
　後発のファイルは末尾に(1),(2)...が付加されます。
　新しいファイル名と元のファイル名が同じ場合は変化なし。

（４）フォルダ名
　フォルダは、直下のファイルのリネームが終了したあとに
　対象ファイルの年月日の中で最も若かった値でリネームします。
　例）2021_0712
　ただし先頭9文字（年月日）が一致していたらリネームせず。
　例）"2021_0712公園散歩" が "2021_0712" にはならない。
   同名フォルダが既にあれば末尾に(1),(2)...が付加されます。

（５）留意点
　リネームしようとするファイルが使用中だとエラーで停止します。
　フォルダ内にあるリネーム対象外のファイルを使用中だと
　フォルダのリネームでエラーになります。
　複数のフォルダをドロップしても１つ目しかみません。
　エクスプローラーのUndoでリネーム前の状態には戻せません。
　⇒　意図に合致しているか、試用して確認してください。

２．ファイル単位で実行する
USAGE＞ DCF_Renamer.exe  file1  file2  file3 ...
DCF_Renamer（のショートカット）にファイルをドロップする。

（１）ファイル名
　上記１．と同様。

（２）対象範囲
　このモードではすべての拡張子のファイルをリネームする。
（ファイルとフォルダを混在してドロップしてしまうとエラー）　

（３）重複処理
　上記１．と同様

３．使用例
（１）カードリーダーでDCIMフォルダをデスクトップにコピー後、
　　DCIMの下にある101_FUJI を DCF_Renamerに落とす。
　　リネーム後のフォルダを一覧確認後にオリジナルを削除する。
（２）メール添付で受け取ったJPEG数個をローカルに保存後、
　　それをつかんで DCF_Renamerに落とす。
（３）右クリックで”新しいテキストドキュメント”を作成直後に
　　一旦重複しない名前として年月日時でリネームして使う。
などなど。

以上です。

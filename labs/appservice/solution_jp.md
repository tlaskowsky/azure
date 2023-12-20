# ラボ 解決策

ホームページの内容は `src/WebForms/WebApp/Default.aspx` にあります。以下は使用できる更新されたファイルです：

- [lab/Default.aspx](/labs/appservice/lab/Default.aspx) - HTMLコンテンツを変更

新しいファイルを元のファイルにコピーします：



```
cp labs/appservice/lab/Default.aspx src/WebForms/WebApp/
```


変更をコミットします：



```
git add src/WebForms/WebApp/Default.aspx

git commit -m 'Homepage update'
```


リモートにプッシュします：



```
git push webapp main
```


ビルドの出力が再度表示され、新しいコンテンツは数分でライブになるはずです。

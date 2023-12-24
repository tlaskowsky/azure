# ラボ解決策

ポータルで、ストレージアカウントからCDNエンドポイントを開くことができます。トップメニューでは、_パージ_ を使ってコンテンツをリフレッシュできます。これは全てのコンテンツや特定のパスに対して行うことができます。

またはCLIで：



```
az cdn endpoint purge --content-paths '/index.html' -g labs-storage-static --profile-name labs-storage-static -n <cdn-domain>
```


> これには時間がかかる場合があります - 通常、--no-wait フラグを使用します

これを実行した後、CDNドメインを確認してください。パージ前にコンテンツが更新されていなかった場合、今は更新されているはずです。

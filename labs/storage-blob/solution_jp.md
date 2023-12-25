# ラボ解決策

HotおよびCool階層のBlobはダウンロードできます。

CLIを使用してアーカイブ階層に変更します：



```
az storage blob set-tier --container-name labs --name 'storage-blob/README.md' --tier Archive --account-name <sa-name>
```


ポータルでそのBlobを確認してください - アーカイブされたBlobはすぐにアクセスする必要がない長期バックアップ用なので、今はダウンロードできません。

ダウンロードする前に、BlobをCoolまたはHot階層に再度活性化（リハイドレート）する必要があります。

> ポータルはBlobがダウンロードできるかどうかを示し、リハイドレートには数時間かかる可能性があると教えてくれます :)

[リハイドレートについて](https://learn.microsoft.com/ja-jp/azure/storage/blobs/archive-rehydrate-overview)

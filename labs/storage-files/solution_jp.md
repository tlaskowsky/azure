# ラボ解決策

## 標準共有

標準共有の容量は最大5TBまで変更できますが、作成時に異なる容量を設定しない場合、デフォルトは既に最大値です。

標準共有では、実際に保存しているデータ量に基づいて料金が発生し、容量は設定した上限に過ぎません。したがって、1TBの容量を持つ共有に1GBのデータが保存されている場合、1GB分の料金を支払います。

## プレミアム共有

`storage share create`には標準またはプレミアムを選択するオプションがなく、同じ種類の共有を作成する代替コマンドを使用する必要があります：



```
az storage share-rm create --help
```


標準SAでプレミアム共有を作成しようとするとエラーが発生します：



```
az storage share-rm create -n labs-premium --quota 100 --access-tier Premium --storage-account <sa-name>
```


> HTTPヘッダに関する奇妙で役に立たないエラーが表示されますが、問題はプレミアム共有にはプレミアムSAが必要であることです - ポータルで新しい共有を作成しようとすると確認できます

したがって、新しいSAを作成します - Premium SKUで、ファイルストレージ用にフラグを立てる必要があります：



```
az storage account create -g labs-storage-files  -l southeastasia --sku Premium_LRS --kind FileStorage -n <premium-sa-name>

az storage share-rm create -n labs-premium --quota 100 --access-tier Premium --storage-account <premium-sa-name>
```


共有の使用方法は同じですが、**プロビジョニングされた量に基づいて料金が発生します**。100GBの容量を持つプレミアム共有に1GBのデータがある場合、100GB分の料金を支払います。

このSAはファイルストレージ専用です - ポータルを開くと、Blobやその他のオプションがないことがわかります。

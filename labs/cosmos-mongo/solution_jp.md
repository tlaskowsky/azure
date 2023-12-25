# ラボ解答

これらは標準的なMongo Shellコマンドで、PortalやVS Code拡張機能で使用できます。

すべてのコレクションを見つけます：


```
use('AssetsDb');
db.getCollectionNames();
```


_Location_、_AssetTypes_、_Assets_ が別々のコレクションであることがわかります。これは、CosmosのNoSQL APIと異なり、ドキュメントが同じコンテナに格納されており、オブジェクトタイプを識別するために _Discriminator_ フィールドが使用される点が異なります。

すべてのロケーションを出力します：



```
db.Locations.find().pretty();
```


新しいロケーションを挿入します：



```
db.Locations.insertOne({
    "AddressLine1": "1 Parliament Place",
    "Country": "Singapore",
    "PostalCode": "178880"
});
```


アプリをリフレッシュすると、新しいデータが表示されるはずです。

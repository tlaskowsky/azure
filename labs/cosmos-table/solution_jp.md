_Data Explorer_ を開いて _FulfilmentLogs_ エンティティに移動します。

_クエリビルダー_ では、フィルタリングするプロパティを設定できます：

- デフォルトの `PartitionKey` および `RowKey` フィールドを削除します。
- `Level` フィールドが `Error` に等しい新しい条件を追加します。
- `Timestamp` フィールドが `Last Hour` 以上である新しい条件を追加します。

> Explorerはこれが日付/時間フィールドであることを認識し、選択するための限定された範囲セットを提供します。

_クエリテキスト_ を確認すると、これはTable Storageで使用したODataフィルタに非常に似ていることがわかります。

CosmosDBは、HTTPを介したODataクエリもサポートしていますが、CosmosにアクセスするためのSASトークンを生成することはできません。代わりに[認証ヘッダーを構築する](https://learn.microsoft.com/en-us/rest/api/storageservices/authorize-with-shared-key)必要がありますが、これは簡単ではありません。

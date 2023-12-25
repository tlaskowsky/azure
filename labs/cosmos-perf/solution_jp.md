# ラボ解答

ドキュメントをフェッチした時、結果の最後にオブジェクトIDが表示されます。私の参照データドキュメントはこのようになっています：



```
                "productId": "999",
                "name": "p999",
                "price": 4995
            }
        ],
        "id": "b9937d9c-fdd9-44c7-8e35-80d1befba841",
        "_rid": "3UsvAOBTm7cBAAAAAAAAAA==",
        "_self": "dbs/3UsvAA==/colls/3UsvAOBTm7c=/docs/3UsvAOBTm7cBAAAAAAAAAA==/",
        "_etag": "\"67019b48-0000-0d00-0000-635ca24e0000\"",
        "_attachments": "attachments/",
        "_ts": 1667015246
    }
]
```


参照データタイプはパーティションキーなので、次のようにポイントリードを試みることができます：



```
SELECT * 
FROM ReferenceData r 
WHERE r.refDataType="Products" 
AND r.id="b9937d9c-fdd9-44c7-8e35-80d1befba841"
```


これにより、次の結果が得られます：

| 取得したドキュメントの数 | 取得したドキュメントのサイズ | 実行時間 | RU |
|-|-|-|-|
|59958|60258|0.01ms|3.59|


これは予想外です - パーティションキーとオブジェクトIDを持っており、返されたドキュメントは小さいので、これは1RUであるべきです。

**しかし**、データエクスプローラはSQLクエリのみを実行でき、[ポイントリードはクライアントライブラリを使用して行う必要があります](https://devblogs.microsoft.com/cosmosdb/point-reads-versus-queries/)。

C#では、次のようにコードを書くことができます：



```
var refData = await container.ReadItemAsync<RefData>(id: "b9937d9c-fdd9-44c7-8e35-80d1befba841", partitionKey: new PartitionKey("Products"));
```


これにより1RUのコストがかかります。アプリの例に戻ると、これはスループット容量の2％になります（10 * 1 * 1 = 10 RU/500 RU/s）。

> データを効果的にナビゲートするためには、_id_ フィールドにカスタム値を設定し、Cosmosにランダムなものを生成させないようにするべきです。

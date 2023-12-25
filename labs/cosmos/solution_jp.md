# Lab Solution

There are some quirks to SQL support in Cosmos.

# ラボ解答

CosmosにおけるSQLサポートにはいくつか特異点があります。

これは問題ありません：



```
SELECT *
FROM AssetContext
```


しかし、これは失敗します：



```
SELECT Id
FROM AssetContext
```


なぜなら、識別子はテーブルにアンカーされる必要があるからです：



```
SELECT c.Id
FROM AssetContext c
```


アセットタイプリストはDiscriminatorに基づいて選択されます：



```
SELECT c.Id, c.Description
FROM AssetContext c
WHERE c.Discriminator = "AssetType"
```


そして、ロケーションのカウントは文字列比較を使用します：



```
SELECT COUNT(c.Id)
FROM AssetContext c
WHERE c.Discriminator = "Location"
AND c.PostalCode LIKE '%1%'
```


COUNT(*)を試すことができますが、同じ理由で失敗します。Cosmosは識別子が明示的にアンカーされることを望んでいます。

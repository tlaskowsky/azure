# Cosmos DB - パフォーマンスプロビジョニング

CosmosDBはストレージとコンピュートの両方に対して課金されます。ストレージは一定料金で、保存されているデータ量に対して支払い、使用するパフォーマンスレベルに関わらず料金は同じです。コンピュートは_Request Units_ (RU)の観点で課金され、全てのアクセス操作（読み取り、書き込み、削除、更新、クエリ）に対して支払います。サーバーレスモデル（消費したRUに対して支払う）と、プロビジョニングモデル（固定レベルのRUに対して支払う）の間で選択できます。

コストはCosmosの使用を阻害する要因になり得ますが、適切に計画すれば非常にコスト効果の高いデータベースになります。このラボでは、RU消費量をテストして測定する方法を見ていきます。

## 参照

- [Cosmos DBのサーバーレスモード](https://learn.microsoft.com/en-us/azure/cosmos-db/serverless)

- [整合性レベルの説明](https://docs.microsoft.com/en-gb/azure/cosmos-db/consistency-levels?toc=%2Fazure%2Fcosmos-db%2Fsql%2Ftoc.json#guarantees-associated-with-consistency-levels)

- [Cosmos DB内のSQLクエリ](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-query-getting-started)

- [Cosmos DB内のデータへの安全なアクセス](https://docs.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data?tabs=using-primary-key)

## 固定パフォーマンスのCosmosDBコンテナを作成する

秒間リクエストユニット（RU/s）はCosmosDBのパフォーマンスレベルを定義します。コストに影響を与える他の要素もあります - 例えばレプリケーションの量や複数書き込み機能など - ですが、RU消費は主要なコスト要因です。

新しいCosmos DBアカウントを作成します：



```
az group create -n labs-cosmos-perf  -l southeastasia --tags courselabs=azure

az cosmosdb create -g labs-cosmos-perf --enable-public-network --kind GlobalDocumentDB --default-consistency-level Eventual -n <cosmos-db-name>
```


特定のパフォーマンスレベルの500 RU/sでSQL APIデータベースを作成します：



```
az cosmosdb sql database create --name ShopDb -g labs-cosmos-perf --throughput 500 --account-name <cosmos-db-name>
```


> これは標準のプロビジョニングモデルを使用しており、RUパフォーマンスのレベルに対して支払い、そのレベルに達していなくても課金されます。

他の課金モデルは[サーバーレス](https://learn.microsoft.com/en-us/azure/cosmos-db/scripts/cli/nosql/serverless)と[オートスケール](https://learn.microsoft.com/en-us/azure/cosmos-db/scripts/cli/nosql/autoscale)です。

アプリを介さずに直接データベースで作業するため、ドキュメントコンテナを作成する必要があります。コンテナには、データベースの全スループットの一部を割り当てることができ、パフォーマンスを必要な場所に集中させることができます。

CosmosDBはドキュメント内の各フィールドにインデックスを設定し、挿入とストレージの負担をかけながらクエリを高速化します。私たちはIDフィールドのみにインデックスを設定するカスタムポリシーを使用します：

- [index-policies/products.json](/labs/cosmos-perf/index-policies/products.json) - これはJSONでポリシーを表現する方法です

📋 `Products`という名前のSQLコンテナをデータベースに作成します。コンテナのスループットを400 RU/sに設定し、`productId`をパーティションキーとして使用し、JSONファイルからカスタムインデックスポリシーを設定します。

<details>
  <summary>わからない場合はこちら</summary>

ヘルプを確認してください：



```
az cosmosdb sql container create --help
```


固定パフォーマンスには`throughput`を使用し、オートスケールには`max-throughput`を使用します。ドキュメントにはIDフィールドが必要で、`partition-key-path`で設定します：


```
az cosmosdb sql container create -n Products -g labs-cosmos-perf  -d ShopDb --partition-key-path '/productId' --throughput 400 --idx @labs/cosmos-perf/index-policies/products.json -a <cosmos-db-name>
```


</details><br/>

これで、データの追加とクエリを行う準備が整いました。

## RU使用量の推定

データのフォーマット方法によってRU消費に大きな違いが生じる場合があります。私たちはショップデータベースに保存する製品のリストを持っており、これは異なる方法でモデル化できる参照データです。ここでは、製品ごとに1つのドキュメントがあります：

- [items/products.json](/labs/cosmos-perf/items/products.json) - これにより1000個の小さなドキュメントができます

📋 `labs/cosmos-perf/items/products.json`のドキュメントをポータルを使用してコンテナにアップロードしてください。コンテナをクエリしてすべてのアイテムを選択すると、クエリのRUコストはどのくらいですか？

<details>
  <summary>わからない場合はこちら</summary>

コンテナを_Data Explorer_で開き、_Upload item_をクリックしてファイルを選択してください。

データがアップロードされたら、コンテナの省略記号をクリックし、_New SQL Query_を選択します。次のクエリを入力します：



```
SELECT * FROM Products
```


結果が表示されたら、_Query Stats_ページに切り替えてRUチャージを確認できます。

</details><br/>

> 私は7.46 RUsを得ました

製品名と価格のみを選択する場合、RUカウントは同じですか？新しいSQLクエリタブを開いて試してみてください：



```
SELECT p.name, p.price FROM Products p
```


> 私は8.22 RUsを得ました

両方のクエリの統計を比較すると、フィールドを選択するとクエリ実行時間が増加し、それによって処理時間とわずかなRU増加があることがわかります。私は以下の結果を得ました：

|フィールド| 取得ドキュメントサイズ | 出力ドキュメントサイズ | 実行時間 | RUs|
|-|-|-|-|-|
|*|59958|60258|0.09ms|7.46|
|p.name, p.price|59958|5568|0.15ms|8.22|

1つの製品をクエリする場合はどうでしょうか：



```
SELECT * FROM Products p WHERE p.name = 'p1'

SELECT * FROM Products p WHERE p.productId = '1'
```


> 最初のクエリでは18.59 RUs、2つ目のクエリでは2.82 RUsを得ました

ここでの違いはインデックスルックアップです - 名前フィールドにはインデックスがないため、Cosmosはすべての行を読まなければなりません：

|フィールド| ドキュメントロード | 取得ドキュメント数 | 実行時間 | RUs|
|-|-|-|-|-|
|名前|1.97ms|1000|0.28ms|18.59|
|id|0.02ms|1|0.01ms|2.82|

これらは小さな数字ですが、それでも大きな違いです。RUは多くの要素から計算されることがわかります。クエリ実行時間とインデックスルックアップ時間は、アイテムまたはフィールドをフィルタリングすると影響を受けます。

小さなデータセットの場合、1つのドキュメント内の配列としてデータを格納する方が安価かもしれません。

## 代替データモデリングとRUs

これは一括ロードのアプローチです。アプリケーションコードはCosmosから単一のドキュメント内のすべての製品を安価に取得し、次にメモリ内でリストをフィルタリングします。アプリは有効期限キャッシュを使用して、数分ごとにメモリ内のリストが更新されるようにします。

このアプローチを試すために、参照データ用として使用する代替コンテナを作成します：

- [items/refData.json](/labs/cosmos-perf/items/refData.json) - 同じ製品リストを、単一ドキュメント内の配列として表現したもの

📋 `ReferenceData`という名前の新しいコンテナを作成し、フィールド`refDataType`をパーティションキーとして使用します。このコンテナにはインデックスポリシーを提供しません。

<details>
  <summary>わからない場合はこちら</summary>



```
az cosmosdb sql container create -n ReferenceData -g labs-cosmos-perf  -d ShopDb --partition-key-path '/refDataType' -a <cosmos-db-name>
```


</details><br/>

> ポータルで開いて、両方のコンテナのインデックス設定を比較してください

デフォルトでは、すべてのフィールドがインデックスされています。インデックスもストレージを使用します - すべてのフィールドがインデックスされた場合、インデックスサイズはデータサイズよりも大きくなることがあります。

ポータルを使用して`labs/cosmos-perf/items/refData.json`のドキュメントを新しいコンテナにアップロードします。
すべての製品を取得し、1つの製品をフィルタリングするためにいくつかのクエリを実行します：



```
SELECT * FROM ReferenceData r 
WHERE r.refDataType='Products'

SELECT *
FROM p IN ReferenceData.items
WHERE p.productId='1'
```

> 全データを取得するのに3.59 RUが、単一商品を取得するのに4.94 RUがかかりました。

もしアプリがCosmosから個々の商品を個別にフェッチする場合、10インスタンスが1秒間に10クエリを行う大規模な使用では、スループットの限界（10 * 10 * 4.94 = 494/500 RU/s）に近づくでしょう。もしアプリが最初のクエリを使用し、インスタンスが結果を少なくとも1秒間キャッシュした場合、スループットの10％未満を使用することになります（10 * 1 * 3.59 = 35.9/500 RU/s）。

高いスケールでCosmosDBを使用するつもりなら、データをモデル化し、アプリケーションを慎重に設計する必要があります。

## ラボ

Cosmosから個々のドキュメントを読む最も安価な方法は、オブジェクトIDとパーティションキーを使用してフェッチすることです - これを _ポイントリード_ と呼び、1RUのコストがかかります（ドキュメントが100Kbまでの場合）。

2番目のアプローチで挿入した参照データ項目のオブジェクトIDを見つけ、IDとパーティションキーを使用してフェッチするRUコストを確認してください。1RUになりますか？クエリに高価な部分はありますか？

> 詰まったら [ヒント](hints_jp.md) を試すか、[解答](solution_jp.md) を確認してください。

___

## クリーンアップ

リソースグループとデータベースを削除し、すべてのデータも削除します：



```
az group delete -y --no-wait -n labs-cosmos-perf
```

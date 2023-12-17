# リソース グループ

リソース グループ (RGs) は、他のすべての Azure リソース - VM、SQL データベース、Kubernetes クラスターなどを含むコンテナです。アプリケーションごとに1つのリソース グループを持ち、そのアプリが必要とするすべてのコンポーネントを含むことができます。管理権限はリソース グループレベルで適用され、グループを削除することですべてのリソースを簡単に削除できます。

## 参照

- [リソース グループ](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/management/overview#resource-groups)
- [リージョンと地理](https://azure.microsoft.com/ja-jp/global-infrastructure/geographies/#overview)
- [`az group` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/group?view=azure-cli-latest)
- [JMESPath JSON クエリ言語](http://jmespath.org/)

## ポータルで新しい RG を作成

https://portal.azure.com を開き、必要に応じてサインインします。

_Azure サービス_ セクションから _リソースの作成_ を選択し、リソース グループを検索して新しいものを作成します。

- 名前は `labs-rg-1` とします
- 近くのリージョンを選択します（リストは _おすすめ_ と _その他_ に分かれています）
- タグを追加: `courselabs=azure`
- 作成をクリックし、リソースが準備できたというアラートを待ちます
- リソース グループに移動し、UI を探索します

> 各 _リージョン_ は近くのデータセンターの集まりです。通常、アプリのすべてのコンポーネントを同じリージョンに配置し、ネットワーク遅延を最小限に抑えます。高可用性のために他のリージョンに追加のデプロイメントを配置することもあります。

リソース グループ自体ではあまり作業できませんが、他のリソースを格納するために常に RG を作成します。

## Azure CLI で RG を作成

`az group` コマンドでリソース グループを管理します。利用可能なコマンドを確認するには、ヘルプを表示します。


```
az group --help
```

📋 新しい RG を作成するためのヘルプテキストを表示します。どのパラメーターを提供する必要がありますか？

<details>
  <summary>わからない場合は？</summary>

ヘルプはコマンドグループと個別のコマンドに適用されます：

```
az group create --help
```


唯一必要なパラメーターはグループ名とリージョンですが、他の `az` コマンドでは大抵 _ロケーション_ と呼ばれています。

</details><br/>

CLI のヘルプテキストでは、リージョンのリストの見つけ方も示されています。

📋 最初のものと異なるリージョンで `labs-rg-2` という名前の新しい RG を作成し、同じタグ `courselabs=azure` を付けます。

<details>
  <summary>方法がわからない場合は？</summary>

リージョンのリストを見つけます（このコマンドは `group create` ヘルプテキストにあります）：



```
az account list-locations -o table
```


グループを作成します、この例では West US 2 を使用します：



```
az group create -n labs-rg-2 -l westus2 --tags courselabs=azure
```

</details><br/>

CLI でリソースを作成すると、リソースが準備できるまで待機し、その後詳細を表示します。

## リソース グループの管理

`az` コマンドラインは、すべてのリソースに対して一貫した方法で動作します。同じ動詞を使用してそれらを作成、リスト表示、表示、削除します。

📋 すべての RG のリストを表示し、出力をテーブル形式で表示します。

<details>
  <summary>方法がわからない場合は？</summary>

```
az group list -o table 
```

</details><br/>


両方の RG に同じタグを追加しました。タグは、管理に役立つすべてのリソースに追加できる単純なキー値ペアです。開発環境や UAT 環境のリソースを識別するために `environment` タグを使用することがあります。

結果をフィルタリングするために、`list` コマンドにクエリパラメーターを追加できます。一致するタグを持つ RG を印刷するためにこのクエリを完成させます：



```
az group list -o table --query "[?tags.courselabs ...
```


> クエリパラメーターは [JMESPath](http://jmespath.org/) を使用します。これは JSON クエリ言語で、すべてのリージョンの一致する RG を見つける結果を返します。

## リソース グループの削除

`group delete` コマンドはリソース グループを削除します - そのグループ内のすべてのリソースも含みます。5つの Hadoop クラスターと数百の Docker コンテナを含む RG を持っていても、グループを削除するとサービスが停止され、データが削除されます。

リソースの削除は危険なため、`az` コマンドではクエリに基づいて複数のグループを削除することはできません。これを試してください - 失敗します：



```
# これはエラーを出し、グループ名が必要であると言います：
az group delete --query "[?tags.courselabs=='azure']"
```

📋 コマンドラインを使用して最初のリソース グループ `labs-rg-1` を削除します。

<details>
  <summary>方法がわからない場合は？</summary>


```
az group delete -n labs-rg-1
```


</details><br/>

> 削除の確認が求められ、その後グループが削除されるまでコマンドが待機します。

## ラボ

時には、クエリに一致するすべてのリソースを削除したい場合もあります。courselabs タグを持つすべての RG を単一のコマンドで削除するにはどうすればよいですか？


> 困ったときは、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。


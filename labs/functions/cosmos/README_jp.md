# Functions: CosmosDB to CosmosDB

CosmosDBをAzure Functionsのトリガーおよび出力として使用できます。トリガーはドキュメントの作成および編集のためであり、CosmosDBの潜在的なスケールのため、各関数への呼び出しには複数のドキュメントが含まれる可能性があるため、そのロジックはそれに対応する必要があります。一部のケースでは、入力と出力に同じデータベースコレクションを使用したい場合もありますが、これについても注意深く考える必要があります。

このラボでは、CosmosDBをトリガーおよび出力として使用する関数を作成します。

## リファレンス

- [CosmosDBトリガーおよびバインディングリファレンス](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2?tabs=in-process%2Cfunctionsv2&pivots=programming-language-csharp)

## CosmosDBからCosmosDBへの関数の作成

シナリオは翻訳エンジンです。CosmosDBに英語のメッセージを含むドキュメントが保存されると、関数はそれらをスペイン語に翻訳して翻訳されたドキュメントを同じCosmosDBデータベースに保存します。コードは `CosmosToCosmos` ディレクトリにあります。

- [CosmosToCosmos/Translator.cs](/labs/functions/cosmos/CosmosToCosmos/Translator.cs) - インカミングドキュメントを受け取り、特定のメッセージを英語からスペイン語に翻訳します。

これらの属性が関数をワイヤアップします：

- `[CosmosDBTrigger]` は `posts` コレクションに追加または更新があると、読み取り専用のドキュメントで発火します。

- `[CosmosDB]` は `posts` コレクションに一連のドキュメントを書き込む出力バインディングです。

ロジックは以前のラボよりもやや複雑ですが、複数の入力があるためです。ドキュメントコレクションが反復処理され、メッセージと言語が一致する場合、翻訳が実行され、出力に新しいドキュメントが追加されます。この新しいドキュメントはCosmosDBに作成されます。

<details>
  <summary>参考情報</summary>

以下は関数の作成方法です：



```
func init CosmosToCosmos --dotnet 

cd CosmosToCosmos

func new --name Translator --template "CosmosDBTrigger"

dotnet add package Microsoft.Azure.WebJobs.Extensions.CosmosDB --version 3.0.10
```


</details><br/>

## ローカルで関数をテスト

ローカルで実行できる[CosmosDBエミュレータ](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21)がありますが、セットアップが少し複雑です。CosmosDBを頻繁に使用する場合は、手順を実行する価値がありますが、このラボには少し過剰です。

代わりに、AzureでCosmosDBアカウントを作成します：

- NoSQL APIを使用
- `Test`という名前のデータベースを作成
- データベース内に`posts`というコレクションを作成
- コレクションのパーティションキーに `/id` を使用

次に、CosmosDB接続文字列を `labs/functions/cosmos/CosmosToCosmos/local.settings.json` に書き込みます：



```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "CosmosDbConnectionString" : "",
        "DatabaseName" : "Test"
    }
}
```


それ以外のコンポーネントはローカルで実行されます。Azure Storageエミュレータを起動します：



```
docker run -d -p 10000-10002:10000-10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


関数を実行します：



```
cd labs/functions/cosmos/CosmosToCosmos

func start
```


ポータルのCosmosDBデータエクスプローラを開き、新しいアイテムを挿入します：



```
{
    "id": "123",
    "message": "goodbye",
    "lang" : "en"
}
```

```
{
    "id": "897",
    "message": "hello",
    "lang" : "en"
}
```


次のような出力が表示されるはずです：



```
[2022-11-08T05:24:58.366Z] Processing: 1 documents
[2022-11-08T05:24:58.366Z] Translating message for document ID: test02
[2022-11-08T05:24:58.571Z] Added translated document ID: 62e768b9-1eb1-44bf-8985-ab4cb2386295
[2022-11-08T05:24:58.580Z] Executed 'Translator' (Succeeded, Id=9c74627b-3f01-4747-9930-005aa0fa1133, Duration=238ms)
```


Cosmos内のドキュメントを確認し、'hello' ドキュメントごとに 'hola' ドキュメントが翻訳されたことを確認できるはずです。

> 新しいアイテムを挿入すると、関数が再度実行されることに注意してください :)

## Azureにデプロイ

Azureで実行する場合、既存のCosmosDBデータベースを使用するか、新しいデータベースを作成できます。以下はコアのセットアップです：



```
az group create -n labs-functions-cosmos --tags courselabs=azure -l eastus

az storage account create -g labs-functions-cosmos --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-cosmos  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


関数の前提条件は次のとおりです：

- `posts` コレクションに追加または更新があると、`Prod` という名前のCosmosDBデータベースにトリガーが発生します。
- アプリ設定 `CosmosDbConnectionString` に設定されたCosmosDB接続文字列
- アプリ設定 `DatabaseName` にデータベース名を設定

準備ができたら、デプロイできます：



```
func azure functionapp publish <function-name>
```


`Prod` データベースにいくつかの新しいドキュメントを使用して関数をテストします。

## ラボ

ドキュメントを翻訳するときに関数が2回トリガーされないようにデータベースを設計する方法は何ですか？ロジックが異なる場合、トリガーされた関数の無限ループが発生する可能性がありますか？

> 行き詰まっていますか？[提案](suggestions_jp.md)を試してみてください。

---

## クリーンアップ

Azure Storageエミュレータを停止します：



```
docker rm -f azurite
```


ラボのリソース グループを削除します：



```
az group delete -y --no-wait -n labs-functions-cosmos
```

## Durable Functions: ファンアウト

Durable functionsはAzure内で状態が保持されます。オーケストレーターはアクティビティが完了するのを待ち、失敗したアクティビティをリトライするロジックを持っています。これは複数の第三者システムを含む長いトランザクションに最適です。

複数のシステム呼び出しを行い、集められた結果セットで作業するためにすべてが完了する必要がある場合、_ファンアウト/ファンイン_ パターンを使用できます。オーケストレーターは、すべてのアクティビティ関数を並列に開始し、すべてが終了するのを待ちます。

このラボでは、HTTPトリガーを使用するdurable functionを使用し、関数のステータスをチェックするための追加機能を見ていきます。

## 参考

- [ファンアウト用のDurable Functions](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview?tabs=csharp#human)

- [durable functionsのHTTP機能](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-http-features?tabs=csharp)

- [アクティビティ関数内のエラーハンドリングとリトライ](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-error-handling?tabs=csharp)

## HTTPトリガーとオーケストレーション

シナリオは、[Service Bus関数ラボ](/labs/functions/servicebus/README.md)における見積もりエンジンの改善された実装です。元の関数はいくつかのサプライヤーに注文の見積もりを依頼し、それらの回答を保存していました。このバージョンはすべての回答を待ち、最も安い見積もりを選択します。

コードは `QuoteEngine` フォルダにあります：

- [QuoteEngine/HttpOrchestratorStart.cs](/labs/functions-durable/fan-out/QuoteEngine/HttpOrchestratorStart.cs) - HTTPトリガーを使用し、クライアントが実行中の関数と連携するためのURLセットを返します。

- [DurableChained/ChainedOrchestrator.cs](/labs/functions-durable/fan-out/QuoteEngine/QuoteOrchestrator.cs) - オーケストレーターは3つのサプライヤー見積もりアクティビティを _非同期に_ 呼び出し、すべてが完了するまで待ちます。

これは複数のサービス呼び出しを管理する非常に効率的な方式です - 合計時間は最長の呼び出しの持続時間になりますが、同期呼び出しの場合はすべての持続時間の合計になります。

各アクティビティは見積もりレスポンスオブジェクトを返し、オーケストレーターは最も良い価格のものを選択します。サプライヤーの見積もりアクティビティはほぼ同じです：

- [Activities/Supplier1Quote.cs](/labs/functions-durable/fan-out/QuoteEngine/Activities/Supplier1Quote.cs) - ランダムな見積もり価格を生成して返します。

一つのアクティビティには遅延が含まれているため、オーケストレーターは最も遅いサービスが返すまで待ち続けることがわかります。

## ローカルでの関数テスト

この関数には、標準のストレージアカウント以外の依存関係はありません。

Docker Desktopを実行し、Azure Storageエミュレーターを開始します：


```
docker run -d -p 10000-10002:10000-10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```


ローカル設定ファイルがまだ必要なので、`labs/functions-durable/fan-out/QuoteEngine/local.settings.json`にテキストファイルを作成し、標準設定を追加します：


```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet"
    }
}
```


関数をローカルで実行します：


```
cd labs/functions-durable/fan-out/QuoteEngine

func start
```


これはHTTPトリガーを使用します。すべての関数がリストされ、オーケストレーターを開始するために呼び出すURLが表示されます。

別のターミナルを開き、HTTPトリガー関数を呼び出します：


```
curl http://localhost:7071/api/HttpOrchestratorStart
```


> 関数のターミナルにはオーケストレーターのログが表示され、以下のように始まります：


```
[2022-11-14T03:56:07.064Z] Executing 'Supplier1Quote' (Reason='(null)', Id=bf710bb4-164a-472b-acac-04d320913b7d)
[2022-11-14T03:56:07.064Z] SUPPLIER-1 calculating price for quote ID: 5a9b8cb3-055b-461b-8846-12f6d8f930e2
[2022-11-14T03:56:07.064Z] SUPPLIER-1 calculated quote: 480; for ID: 5a9b8cb3-055b-461b-8846-12f6d8f930e2
[2022-11-14T03:56:07.064Z] Executed 'Supplier1Quote' (Succeeded, Id=bf710bb4-164a-472b-acac-04d320913b7d, Duration=0ms)
```


そして、curlウィンドウには次のようなURLがいっぱいのJSONレスポンスが表示されるはずです：


```
{
    "id":"13ce7b3e0da8405cb12781acdacc7f1e",
    "statusQueryGetUri":"http://localhost:7071/runtime/webhooks/durabletask/instances/13ce7b3e0da8405cb12781acdacc7f1e?taskHub=TestHubName&connection=Storage&code=Jg2Pnt0EJU8OS2yXI7Zn5aBnHldpfGkvkwppeu6F2Xj2AzFuQ-TqjQ==",
    "sendEventPostUri":"http://localhost:7071/runtime/webhooks/durabletask/instances/13ce7b3e0da8405cb12781acdacc7f1e/raiseEvent/{eventName}?taskHub=TestHubName&connection=Storage&code=Jg2Pnt0EJU8OS2yXI7Zn5aBnHldpfGkvkwppeu6F2Xj2AzFuQ-TqjQ=="
    ...
}
```


> これらは関数の進捗状況を確認するために呼び出すURLです。

これはエンドユーザーが見るものではありませんが、Web UIで更新を確認し、レスポンスをきれいに整形することができます。レスポンスから `statusQueryGetUri` を呼び出すと、ステータスを確認できます：


```
# トリガーレスポンスからのURLを使用してください

curl "http://localhost:7071/runtime/webhooks/durabletask/instances/xyz?taskHub=TestHubName&connection=Storage&code=abc"
```


レスポンスには、最も価格の良いサプライヤーからの見積もりレスポンスが含まれます。例えば以下のようなものです：


```
    {
        "name": "QuoteOrchestrator",
        "instanceId": "13ce7b3e0da8405cb12781acdacc7f1e",
        "runtimeStatus": "Completed",
        "input": {
            "QuoteId": "5a9b8cb3-055b-461b-8846-12f6d8f930e2",
            "ProductCode": "P101",
            "Quantity": 32
        },
        "customStatus": null,
        "output": {
            "QuoteId": "5a9b8cb3-055b-461b-8846-12f6d8f930e2",
            "SupplierCode": "SUPPLIER-3",
            "Quote": 256.0
        },
        "createdTime": "2022-11-14T03:56:07Z",
        "lastUpdatedTime": "2022-11-14T03:56:22Z"
    }
```


すべてがうまくいっていると感じたら、Azureにデプロイすることができます。

## Azureへのデプロイ

Function Appの基本セットアップは以下の通りです：



```
az group create -n labs-functions-durable-fan-out --tags courselabs=azure -l eastus

az storage account create -g labs-functions-durable-fan-out --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-durable-fan-out  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


**依存関係はありません** - トリガーやバインディングに外部サービスは使用されていないので、すぐにデプロイすることができます：


```
func azure functionapp publish <function-name>
```

パブリックURLを使用して関数を試してみてください。同じ方法で機能し、ポータルの関数の _Monitor_ タブでオーケストレーションの進行状況を追跡できるはずです。

## ラボ

ファンアウトパターンを持つdurable functionは非常に強力ですが、個別の関数を使用するイベント駆動型のパターンに比べると柔軟性が低いです。このパターンで新しいサプライヤーを導入するにはどうすれば良いでしょうか？これを、durable functionを使用しない従来のpub-subパターンと比較するとどうなるでしょうか？また、どちらのパターンを使用しても、ワークフローが長引かないように、_x_ 秒以内に返された最安値の見積もりを採用するワークフローを実装することは可能ですか？

> 詰まった場合は、私の[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

Azure Storageエミュレーターを停止します：



```
docker rm -f azurite
```


ラボのリソースグループを削除します：



```
az group delete -y --no-wait -n labs-functions-durable-fan-out
```

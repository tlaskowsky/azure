# 機能: タイマーからBlobストレージへの変換

Azure関数の主要な特徴は、その_バインディング_です。関数コードはイベントによってトリガーされ、バインディングを使用して他のAzureサービスへの入力を受け取ったり、出力を書き込んだりすることができます。CosmosDBから読み取り、Service Busキューに書き込む必要がある場合、インフラストラクチャコードなしで行うことができます。

このラボでは、タイマートリガーを使用した関数をBlobストレージに出力する方法を使用します。また、Azure StorageエミュレータであるAzuriteの使用方法と、funcionsでの設定方法についても説明します。

## 参照

- [ローカルでの関数のコーディングとテスト](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-develop-local)

- [タイマートリガーとバインディングリファレンス](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-timer?tabs=in-process&pivots=programming-language-csharp)

- [関数バインディング式](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-expressions-patterns)

- [Blobストレージトリガーとバインディングサンプル](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/storage/Microsoft.Azure.WebJobs.Extensions.Storage.Blobs#examples)

## Blobストレージへの書き込みを行うスケジュールされた関数

関数コードは`TimerToBlob`ディレクトリにあります:

- [TimerToBlob/Hearbeat.cs](/labs/functions/timer/TimerToBlob/Heartbeat.cs) - 5分ごとに実行され、JSONファイルをBlobストレージに書き込みます

これは、すべての関数の機能が属性でリストされた標準的なC#コードです:

- `[FunctionName]`はメソッドを関数として識別し、名前を付けます
- `[StorageAccount]`はストレージアカウントの接続文字列設定の名前を指定します
- `[TimerTrigger]`は、[変更されたcron構文](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-timer?tabs=in-process&pivots=programming-language-csharp#ncrontab-expressions)を使用してタイマーで機能を実行するように設定します
- `[Blob]`は出力バインディングであり、関数内で設定された内容でblobファイルを作成します

<details>
  <summary>参考のために</summary>

こちらは関数が作成された方法です:





```
func init TimerToBlob --dotnet 

cd TimerToBlob

func templates list

func new --name Heartbeat --template "Timer trigger"

dotnet add package Microsoft.Azure.WebJobs.Extensions.Storage.Blobs
```



</details><br/>

コードは非常にシンプルです。イベントをトリガーするためのロジックを書く必要はありません。これはタイマーで全て処理されます。Blobストレージコンテナに接続してファイルをアップロードするためのコードを書く必要もありません。バインディングがすべてを処理します。

## ローカルでの関数のテスト

依存関係を持つ関数は、`local.settings.json`と呼ばれるファイルに設定値を設定することができます。これは、ローカルで関数を実行するときにのみ使用されます。

> このファイルは、機密性の高い接続文字列が含まれる可能性があるため、意図的にgitリポジトリから除外されています。

`labs/functions/timer/TimerToBlob`フォルダ内に`local.settings.json`というファイルを以下の値で作成します:





```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "HeartbeatOutputStorageConnectionString": "UseDevelopmentStorage=true"
    }
}
```



これにより、ストレージ接続文字列がローカルのAzureストレージエミュレータを使用するように設定されます。これを実行する最も簡単な方法はコンテナ内です。

**Docker Desktopが実行中であることを確認してください**、そしてストレージエミュレータを起動します:




```
docker run -d -p 10000-10002:10000-10002 --name azurite mcr.microsoft.com/azure-storage/azurite
```



これで、関数をローカルで実行できます（トリガーをより頻繁に実行するように変更する場合があります - 例えば、2分ごとは`0 */2 * * * *`です）



```
cd labs/functions/timer/TimerToBlob

func start
```



関数ホストの起動から、数分ごとに関数が発火するところまでの出力が表示されます。

コンテナのログをチェックして、blobストレージにファイルが書き込まれていることを確認できます（または、[Storage Explorer](https://learn.microsoft.com/ja-jp/azure/vs-azure-tools-storage-manage-with-storage-explorer)を使用してエミュレータストレージを閲覧することもできます）:



```
docker logs azurite
```


ローカルで関数を実行することで、迅速なフィードバックループを得ることができ、Azureへデプロイする際に必要となるすべての依存関係と設定を特定するのに役立ちます。

## Azureへのデプロイ

Function Appの標準的なセットアップから始めます:



```
# 消費プランをサポートしているあなたの近くの場所を使用することを忘れないでください
az group create -n labs-functions-timer --tags courselabs=azure -l eastus

az storage account create -g labs-functions-timer --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-timer  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


ここから先はあなたにお任せします。関数には以下が必要です:

- 出力用の別のストレージアカウントが必要で、ここにblobが書き込まれます
- そのストレージアカウントの接続文字列は、`HeartbeatOutputStorageConnectionString`という名前の設定アプリ設定に設定する必要があります

準備ができたら、ローカルフォルダから関数をデプロイできます:



```
func azure functionapp publish <function-name>
```

## ラボ

この関数はシステムのための（偽の）診断チェックを生成します。別のシステムのための診断チェックをどのように追加しますか？この関数に更なるコードを追加するか、それとも別の関数を作成するか？もし全てのコンポーネントの現在の状態を報告するAPIがあったら、それはどのようにデータを読み取るのでしょうか？

> 詰まったら、私の[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

Azure Storage エミュレータを停止します：



```
docker rm -f azurite
```


ラボRGを削除します：



```
az group delete -y --no-wait -n labs-functions-timer
```

# イベントハブ分割コンシューマ

分割されたイベントストリームを確実に処理するためには、注意深いロジックが必要です。Microsoftは、さまざまな言語のクライアントライブラリにこのロジックを組み込んでいます。このライブラリは、処理済みのオフセットを記録すること（Blobストレージを単純な状態保持のためのストアとして使用）、各コンシューマがストリームの中断した箇所から再開できるようにすること、そして複数のコンシューマーでのスケール実行をサポートすることを担当します。一つのコンシューマが失敗した場合、他のコンシューマがその作業を引き継ぎます。

このラボでは、この_分割コンシューマ_パターンを進行中に見ることができ、各インスタンスがどのように進行状況を記録し、高可用性とスケールを実現するかを確認します。

## 参照

- [イベントハブのパーティション負荷のバランス](https://learn.microsoft.com/ja-jp/azure/event-hubs/event-processor-balance-partition-load)

- [競合するコンシューマパターン](https://learn.microsoft.com/ja-jp/previous-versions/msp-n-p/dn568101(v=pandp.10))

- [`az eventhubs eventhub consumer-group` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/eventhubs/eventhub/consumer-group?view=azure-cli-latest)

## イベントハブ名前空間とストレージアカウントの作成

まず、コアリソースであるRGとイベントハブ名前空間を作成します。分割コンシューマパターンをサポートするためには、イベントハブは標準SKU以上である必要があります：


```
az group create -n labs-eventhubs-consumers --tags courselabs=azure -l southeastasia

az eventhubs namespace create --min-tls 1.2 --capacity 2 --sku Standard -g labs-eventhubs-consumers -l southeastasia -n <eh-name> 
```


また、コンシューマが進行状況を保存するために使用するBlobコンテナを含むストレージアカウントも必要です。2種類の処理があり、一方はオフセットを保存するためのコンテナが必要で、もう一方はすべてのイベントのコピーを保存します：


```
az storage account create --sku Standard_ZRS -g labs-eventhubs-consumers -l southeastasia -n <sa-name>

az storage container create -n checkpoints  -g labs-eventhubs-consumers --account-name <sa-name>

az storage container create -n devicelogs  -g labs-eventhubs-consumers --account-name <sa-name>
```


> イベントハブとストレージアカウントの間に直接的なリンクはありません - それらはコンシューマコードでのみまとめられます。

ポータルでイベントハブ名前空間を開きます - 左メニューには_ネットワーキング_という追加オプションがあります。イベントハブでプライベートネットワーキングをどのように使用するか？

## イベントハブとコンシューマグループの作成

イベントハブのパーティション数は作成時に設定され、変更はできません。パーティションの数を慎重に考える必要があります。パーティションが多いほどコストがかかりますが、パーティションが少ないとスケールの能力が制限されます。

📋 5つのパーティションと2日間の保持期間を持つ`devicelogs`というイベントハブを作成します。

<details>
  <summary>方法がわからない場合</summary>



```
# 標準SKUではメッセージ保持期間を長くすることができます：
az eventhubs eventhub create --name devicelogs --partition-count 5 --message-retention 2 -g labs-eventhubs-consumers --namespace-name <eh-name> 
```


</details><br/>

> より高価なSKUでは、より長い保持期間を設定できます。これは、プロデューサーが突発的である場合に便利です - 高負荷期間中にコンシューマがすべてのイベントを適切な時間内に処理できない場合がありますので、長い保持期間はピークを処理するためのより多くの時間を彼らに与えます。

ポータルでイベントハブを開き、_コンシューマグループ_タブを確認します。すべてのイベントハブには`$Default`コンシューマグループがあります - これを削除できますか？

このSKUでは複数のコンシューマグループを作成できます - 異なる機能に対して異なるグループを使用するでしょう。異なるスケールレベルで実行できます（例えば、複数のコンシューマを持つビジネス処理と、単一のコンシューマを持つ監査ログ）：


```
# オプションを確認します - これは名前空間レベルではなく、イベントハブレベルでの操作です：
az eventhubs eventhub consumer-group create --help

# 処理グループを作成します：
az eventhubs eventhub consumer-group create -n processing --eventhub-name devicelogs -g labs-eventhubs-consumers --namespace-name <eh-name>

# 監査グループも作成します
az eventhubs eventhub consumer-group create -n auditing --eventhub-name devicelogs -g labs-eventhubs-consumers --namespace-name <eh-name>
```


> ポータルで再度確認します - デフォルトとともに二つのコンシューマグループがリストされていますが、それらを操作することはできません（削除以外は）。

コンシューマグループはデータ読み取りを分離するメカニズムです。Service Busトピックに概念的に似ており、異なるコンポーネントが同じデータをすべて受け取ることができますが、異なる速度で処理します。イベントハブでは、コンシューマが読み取りを管理する責任があります。

## イベントの公開とキャプチャ

前回と同じパブリッシャーアプリを使用します。これには新しいイベントハブ名前空間の接続文字列が必要です：


```
# 接続文字列を取得します：
az eventhubs namespace authorization-rule keys list -n RootManageSharedAccessKey --query primaryConnectionString -o tsv -g labs-eventhubs-consumers --namespace-name <eh-name>

# これは100バッチの50イベントを送信します：
dotnet run --project ./src/eventhubs/producer -ProducerCount 100 -BatchSize 50 -cs '<connection-string>'
```


公開のセマンティクスは、複数のコンシューマグループがある場合でも、単一のコンシューマグループがある場合でも同じです（すべてのイベントハブには常にデフォルトのコンシューマグループがあります）。実際にこれを行う場合、このハブの送信権限のみを持つ専用のアクセスポリシーを作成します。

標準SKUには追加機能があります。これには、すべてのイベントをBlobストレージに保存する機能が含まれます。ポータルでイベントハブを開き、_キャプチャ_を設定します：

- Avro出力形式を選択し、_キャプチャ_を有効にするために_On_を選択します
- _サイズウィンドウ_を最小にスライドします
- _キャプチャ時間窓の間にイベントが発生しない場合、空のファイルを生成しない_をチェックします
- `devicelogs`Blobストレージコンテナを選択します

残りのフィールドはデフォルトのままにして、変更を保存してからストレージアカウントを開きます。

`devicelogs`コンテナで、Blobとして保存されたイベントを確認できるはずです（届くまで数分かかることがあります - もしなにも見えない場合は、キャプチャがそれらを見るには出版されてから時間が長すぎるかもしれません。もう一度パブリッシャーを実行してください）。フォルダ構造は何を意味していますか？Avroファイルの1つを開き、中に何が含まれていますか？

> サンプルは[13.avro](/labs/eventhubs-consumers/13.avro)にあります。

## 処理コンシューマの実行

監査用にキャプチャ機能を使用することができます。これにより、Blobストレージに効率的な形式ですべてのイベントのコピーが確実に保存されます。フォルダ構造は日付別に分かれているので、古いデータを定期的にクリアするプロセスを持つことができます。

カスタム処理には、分割コンシューマパターンを使用します：

- [processor/Program.cs](/src/eventhubs/processor/Program.cs) - 複雑に見えますが、主に標準ライブラリの設定であり、どのBlobストレージコンテナを状態記録用に使用するかを指定しています；_UpdateCheckpoint_の呼び出しは、処理したどこまでを記録するかです。

単一のプロセッサを実行すると、すべてのパーティションから読み取ります：


```
# Event Hub の接続文字列を表示します:
az eventhubs namespace authorization-rule keys list -n RootManageSharedAccessKey --query primaryConnectionString -o tsv -g labs-eventhubs-consumers --namespace-name <eh-name>

# ストレージアカウントの接続文字列を表示します:
az storage account show-connection-string --query connectionString -o tsv -g labs-eventhubs-consumers -n <sa-name>

# プロセッサを実行します:
dotnet run --project ./src/eventhubs/processor -cs '<event-hub-connection-string>' -scs '<storage-account-connection-string>'
```

100件のイベントごとに印刷されるログを確認できます。これには、コンシューマが読み取っているパーティションがリストされています。

> 最初からすべてのパーティションを読むわけではないので、5つのパーティションすべてが読み取られるまでには時間がかかる場合があります。

処理が終了したら、もう一度コンシューマを実行してください - 同じイベントが処理されますか？



```
dotnet run --project ./src/eventhubs/processor -cs '<event-hub-connection-string>' -scs '<storage-account-connection-string>'
```


> うまくいけば、同じイベントは処理されません :)

コードの追加の複雑さは、既に処理された内容を記録することに関するものです。イベントが二度処理されないという保証はありません - コンシューマはイベントのバッチを処理した後、チェックポイントを更新する前にクラッシュするかもしれません。その場合、コンシューマが再起動すると、それらのイベントを再び処理します。

しかし、すべてのイベントが処理され、一つも見逃されることがないように保証することは可能です。これを「少なくとも一度の処理保証」と呼びます。

## ラボ

このライブラリはスケールでの処理に関するものです。さらに2つのターミナルウィンドウを開き、それぞれに別のプロセッサを実行して、合計で3つを実行してください。その後、プロデューサを実行してさらにイベントのバッチを生成します。すべてのコンシューマが処理を行いますか？ストレージアカウントを調べて、彼らがどのように作業を共有しているかを探ります。

バッチの途中でプロセッサを停止してみてください。そのプロセッサのパーティションの残りのイベントはどうなりますか？そして、異なるコンシューマグループ（`-g`パラメータを使用）でコンシューマを開始した場合、どのイベントが処理されますか？

> 詰まったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除します：



```
az group delete -y --no-wait -n labs-eventhubs-consumers
```

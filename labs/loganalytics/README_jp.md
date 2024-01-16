# ログ分析

ログ分析は、Azureでのあらゆる種類のログ収集のためのデータストアです。アプリケーションインサイトはデータをログ分析に送信し、仮想マシンに直接使用することもでき、ストレージアカウントに保存されているログを取り込むためにも使用できます。

Azureポータルには、ログ分析のデータを照会するための豊富なUIがあり、Kustoクエリ言語（KQL）を使用します。他の監視ツール（ダッシュボードやアラートを動力するため）でログ分析データを使用する場合、KQLを使用し、それらのためのクエリを記述し、テストするためにログ分析を使用します - また、対話式の照会にも使用します。

このラボでは、アプリケーションインサイトによって収集されたデータにログ分析がどのようにアクセスするかを見て、KQLを使った経験を積みます。

## 参考資料

- [ログ分析の概要](https://learn.microsoft.com/ja-jp/azure/azure-monitor/logs/log-analytics-overview)

- [Kustoクエリチュートリアル](https://learn.microsoft.com/ja-jp/azure/azure-monitor/logs/get-started-queries)

- [Kustoクエリ言語リファレンス](https://learn.microsoft.com/ja-jp/azure/data-explorer/kusto/query/)


## ログデータの生成

**[アプリケーションインサイトのラボ](/labs/applicationinsights/README_jp.md)を実施しており、`labs-appinsights`リソースグループにアプリケーションインサイトとログ分析がまだ存在する場合は、この手順をスキップしてください。**

<details>
  <summary>ログ分析ワークスペースを作成し、ログを生成するコンテナを実行する</summary>

監視用のRGを作成し、ワークスペースとアプリインサイトを追加：



```
az group create -n labs-appinsights -l eastus --tags courselabs=azure

az monitor log-analytics workspace create -g labs-appinsights -n labsloganalytics -l eastus

az monitor app-insights component create --app labs --kind web -g labs-appinsights --workspace labsloganalytics -l eastus
```


アプリインサイトの接続文字列を取得：


```
az monitor app-insights component show --app labs -g labs-appinsights --query connectionString -o tsv
```


サンプルアプリ用の別のRGを作成し、アプリインサイトへの書き込みを行うACIコンテナを作成：


```
az group create -n labs-appinsights-apps --tags courselabs=azure -l eastus

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.0 --no-wait --name fp1 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.0 --no-wait --name fp2 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.0 --no-wait --name fp3 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"

az container create -g labs-appinsights-apps  --image courselabs/fulfilment-processor:appinsights-1.2 --no-wait --name fp4 --secure-environment-variables "ApplicationInsights__ConnectionString=<appinsights-connection-string>"
```


</details><br />

ACIコンテナは多くのログとメトリックを生成するので、作業に十分なデータが得られるはずです。

## アプリケーションログの照会

ポータルでログ分析ワークスペースを開き、_ログ_ ビューに移動します。多くのサンプルクエリが表示されますが、ここでは役に立たないので、そのウィンドウを閉じて構いません。

これでKQLクエリエディタに入ります。左側のメニューにはサンプルクエリのリストがありますが、ここでも役に立たないので、_テーブル_ ビューに切り替えます：

![ログ分析テーブル](/img/loganalytics-query-editor.png)

これらはワークスペース内の実際のデータテーブルです。_LogManagement_ を展開すると、アプリケーションインサイトから取得された多くのテーブルが表示されます。

KQLクエリの最も単純な形式は、テーブル名だけです。これらのテーブルからデータを選択してみてください：

- AppEvents
- AppDependencies
- AppTraces

これは、アプリケーションインサイトの異なるビューで表示されるのと同じデータです。`AppTraces`には、アプリケーションによって書き込まれた実際のログエントリが含まれています。

さらにKQLクエリを探索してみてください - SQLに似ていますが、はるかに多くの関数があり、構文はより厳密です。個々のクエリは改行を含むことができず、句はパイプ（`|`）で区切られます：


```
AppTraces
| limit 100

AppMetrics
| order by TimeGenerated desc 
| limit 10

AppTraces
| distinct SeverityLevel

AppTraces
| summarize LogsBySeverity = count() by(SeverityLevel)
```

ここで何を見ていますか？ 最初の100個のログエントリ；最も最近のログエントリ10個；すべてのログにわたるログ重大度のリスト；各ログ重大度のカウントでの内訳。重大度3のログはありますか？

📋 特定の重大度レベルの個々のログエントリを見つけるためのKQLを記述してください - 各エントリのメッセージ、アプリケーション名、インスタンス名のみを表示します。

📋 重大度3のログにエラーコード302が何個ありますか？

📋 あるアプリケーションインスタンスが他のインスタンスよりも多くの `Fulfilment.Failed` イベントを記録しているかどうかを確認できますか？

## アプリケーションメトリックの集計

App Insightsからのすべては、テーブル内の行として記録されます。つまり、個々のログエントリを詳細に調べるためにも、メトリックをまとめるためにも、同じクエリ言語を使用します。

`AppMetrics` テーブルは、アプリケーションによって報告されるカスタムメトリックを保存します。これらのクエリを試してみてください：



```
AppMetrics
| count

AppMetrics
| limit 10

AppMetrics
| where (Name == "QueueSize")
| summarize AvgQueueLength = avg(Sum)
```


それは何でしたか？ 記録されたすべてのメトリックのカウント；メトリックの最初の10行；`QueueSize` メトリックの平均。

その平均はすべての時間にわたる平均であるため、あまり役に立ちません。より有用なのは、時間の経過とともに平均キュー長を分解することで、上昇傾向か下降傾向か、または外れ値があるかどうかを確認できます。

📋 10分間隔で集計した場合の平均キューサイズはどのように見えますか？

📋 そのテーブルをより有用な視覚化で表示できますか？

## ラボ

ログ分析の視覚化はクイッククエリに適していますが、アプリケーションの健康状態とパフォーマンスについてより有用なビューが必要な場合は、ワークブックを作成する必要があります。AzureのワークブックUXには慣れる必要がありますが、視覚化はログ分析ワークスペース上のKQLクエリによって動力を得ています。

`Fulfilment.Processor` アプリのワークブックを作成し、以下を示します：

- 現在実行中のすべてのインスタンスのテーブル
- インスタンスごとに分割された `Fulfilment.Failed` イベントの内訳
- 10分間隔で平均された `QueueSize` のグラフ

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

RGを削除すると、すべてのデータも削除されます：



```
az group delete -y --no-wait -n labs-appinsights-apps

az group delete -y --no-wait -n labs-appinsights
```

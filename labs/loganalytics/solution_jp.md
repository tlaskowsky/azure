# ラボの解決策

ここにJSONテンプレートとしてエクスポートされたサンプルワークブックがあります：

- [loganalytics/Lab.workbook](/labs/loganalytics/Lab.workbook)

関連するKQLクエリは以下の通りです：

_アプリインスタンスごとの最新のタイムスタンプを表示し、分単位で経過時間を表示_



```
AppTraces
| summarize arg_max(TimeGenerated, AppRoleInstance) by AppRoleInstance
| project Instance=AppRoleInstance, LastSeen=datetime_diff('minute', now(), TimeGenerated)
```


_インスタンス別の失敗回数を表示_

```
AppTraces
| where Properties.EventType == "Fulfilment.Failed"
| summarize count() by AppRoleInstance
```


_時間経過に伴うキューサイズの内訳を表示_

```
AppMetrics
| where (Name == "QueueSize")
| summarize AvgQueueLength = avg(Sum) by bin(TimeGenerated, 10m)
```


このワークブックを新しいワークブックにインポートできます：

- 編集モードで、`</>`のような見た目の_アドバンスエディター_アイコンをクリックします。
- テンプレートJSONをアドバンスエディターにコピー＆ペーストします。

ワークブックがロードされるはずです：

![LogAnalytics Workbook](/img/loganalytics-workbook.png)

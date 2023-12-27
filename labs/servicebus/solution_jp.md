# ラボ解決策

任意の数のパブリッシャーを実行できます:



```
dotnet run --project src/servicebus/publisher -cs '<connection-string>'
```


メッセージは引き続きすべてのサブスクライバー間で共有されます。各サブスクライバーが各パブリッシャーからのメッセージを処理しているのを確認できます。

信頼性をチェックしたい場合、最も簡単な方法は、既存のすべてのサブスクライバーを終了させることですが、パブリッシャーは実行したままにします。

次に、ACKしない単一のサブスクライバーを実行します:



```
dotnet run --project src/servicebus/subscriber -ack False -cs '<connection-string>' 
```


メッセージが処理されているログは表示されますが、確認ログは消えます。

パブリッシャーを停止し、サブスクライバーがバッチの処理を終えるのを待ちます。これで、キューには未確認のメッセージが固定セットとして残っているはずです。

サブスクライバーを停止し、新しいインスタンスに置き換えます:



```
dotnet run --project src/servicebus/subscriber -ack False -cs '<connection-string>' 
```


同じバッチのメッセージが処理されているのを確認できるはずです。Service Bus がタイムアウトメカニズムを使用しているため、すべてのメッセージが再度配信されるべきかどうかを確認するために、すべてを見るまでに数分かかることがあります。

このインスタンスもメッセージのACKに失敗するため、Service Busではまだ処理済みとマークされていません。メッセージをACKするサブスクライバーに置き換えます:


```
dotnet run --project src/servicebus/subscriber -cs '<connection-string>' 
```


これでメッセージがACKされ、他のサブスクライバーには送信されません。

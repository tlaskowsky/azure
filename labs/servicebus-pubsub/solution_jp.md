# ラボ解決策

まず、一つのサブスクリプションに対してより多くのサブスクライバーを実行して開始します:



```
dotnet run --project src/servicebus/subscriber -topic broadcast -subscription desktop -cs '<subscriber-connection-string>'
```


新しいサブスクライバーは、他の消費者がサブスクリプションを処理しているため、バックログのメッセージを処理することから始まりません。

新しいメッセージバッチはサブスクライバーによって共有されます:

- 単一の `web` サブスクライバーは、そのサブスクリプションにメッセージが届いたときにすべてのメッセージを処理します。
- 2つの `desktop` サブスクライバーは、キュー上の複数のサブスクライバーと同じように、来るメッセージを共有します。

別のパブリッシャーを追加します:



```
dotnet run --project src/servicebus/publisher -topic broadcast -cs '<publisher-connection-string>'
```


両方のサブスクリプションは、両方のパブリッシャーからのすべてのメッセージのコピーを受け取ります。受信メッセージはサブスクリプションのすべての消費者間で共有されるのと同じ方法で処理が続きます。

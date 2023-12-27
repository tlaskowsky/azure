# Azure Cache for Redis

Redisは、メッセージキューとデータストアの組み合わせである人気のあるオープンソース技術です。運用が非常にシンプルで、直感的なプログラミングインターフェイスを持っています。一般的に、重要でないデータのキャッシュや、信頼性の高いメッセージングが必要でない非同期通信に使用されます。Azure Cache for Redisは、Redis APIを実装した完全管理型サービスであり、自己運用のRedisクラスターに直接置き換えることができます。

このラボでは、データキャッシュおよびメッセージキューとしてRedisを使用し、管理されたAzureサービスが提供する機能を見ていきます。

## 参考文献

- [Azure Redis 概要](https://learn.microsoft.com/ja-jp/azure/azure-cache-for-redis/cache-overview)

- [Redis 開発者ドキュメント](https://developer.redis.com)

- [Redis API コマンド](https://redis.io/commands/)

- [`az redis` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/redis?view=azure-cli-latest)


## Redis キャッシュを作成する

新しいリソースを作成し、_redis_ を検索します - たくさんの一致があります。

Redisはオープンソースプロジェクトであり、異なるベンダーがそれをAzure上で動作させるためにパッケージ化しています。_Azure Cache for Redis_ を選択してください（_Azure Cache for Redis Enterprise & Flash_ ではありません）。設定オプションを確認してください：

- あなたのRedisインスタンスは `redis.cache.windows.net` というサフィックスを使用する公開DNS名を持つでしょう
- _キャッシュタイプ_ は価格帯であり、容量と信頼性を定義します
- 高いティアではVirtual Networksが利用可能です
- セキュリティのない（非TLS）接続を選択し、Redisバージョンを選ぶことができます

CLIでRedisインスタンスを作成しましょう。リソースグループから始めます：



```
az group create -n labs-redis --tags courselabs=azure -l southeastasia
```


ヘルプテキストを確認してください：



```
az redis create --help
```


より高度な設定オプションにはJSONファイルを使用することができますが、それほど細かい制御は必要ありません。

📋 基本SKU v6 RedisインスタンスをC0サイズで、TLS 1.2を要求して作成します。

<details>
  <summary>わからない場合は？</summary>



```
az redis create --sku Basic --vm-size c0 --minimum-tls-version 1.2 --redis-version 6 -g labs-redis -n <redis-name> 
```


</details><br/>

> Redisインスタンスが完全にオンラインになるまでには時間がかかることがあります

ポータルでRedisインスタンスを開きます - まだ作成が完了していなくても、そこにあります。_アクセスキー_ が接続詳細を含んでいるのを見ることができ、このSKUでは利用できない多くの機能があります。それには、_Geo-replication_、_Cluster size_、_Data persistence_ などが含まれます。

これらの機能がなくても、基本的なRedisキャッシュはアプリにとって非常に強力な追加となる可能性があります。

## Piアプリを実行する

指定された小数点以下桁数までのPiを計算するシンプルなアプリケーションがあります：

- [pi/Program.cs](/src/pi/Program.cs) - アプリケーションのエントリーポイントで、キャッシュとイベントパブリッシングのためにRedisを使用する機能があります。

アプリを実行してみてください（[.NET 6 SDK](https://dotnet.microsoft.com/ja-jp/download)が必要です）、大きな桁数でPiを計算してみます：


```
# キャッシュなし - 数秒かかります：
dotnet run --project ./src/pi -dp 1000
```


CPUの速度にもよりますが、1、2秒かかるでしょう。これは計算集約型の操作ですが、計算される値は変わりません - 同じリクエストでPiの同じ結果が常に得られます。

> コマンドを繰り返して実行しても同じ結果が得られますが、最初から計算する必要があるため、やはり数秒かかります。

これはキャッシュにとって完璧なシナリオです - データがめったに（または決して）変わらない場合、ネットワークを介して再度計算するよりも、データを取得する方が速い場合です。

Redisをキャッシュとして使用します。Redisアクセスキーを取得します（ポータルでも確認できます）：



```
az redis list-keys -g labs-redis -n <redis-name>
```


そのキーはRedisクライアントのパスワードとして使用されます。キャッシュを有効にしてアプリを実行してください **自分のRedis DNS名とパスワードを設定する必要があります**：


```
dotnet run --project src/pi -dp 1000 -usecache -cs '<redis-name>.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False' 
```


応答を得るのに数秒かかりますが、それはキャッシュが空だったためです。計算した後、このアプリのインスタンスは結果をRedisに格納しました。ですので、将来のインスタンスが読み取るためにそこにあります。

> Redisは共有キャッシュです - 異なるインスタンスの異なるサービスが、データを共有したり、イベントを公開したりするために使用することができます

## Redisでデータを確認する

ポータルで、Redisブレードから_コンソール_を開きます。

これは[Redis CLI]()で、ポータル内に埋め込まれ、すでにあなたのRedisインスタンスに接続されています。

データを確認します - キャッシュキーは `pi-` に続いて小数点以下の桁数です：



```
GET pi-1000
```


.NETアプリを実行したときに見たのと同じ出力を見ることができるでしょう。

> アプリをもう一度実行してみてください。今回はRedisから値を読み取り、再計算する必要がないため、速くなるはずです。

これは非常に高速なマシンを持っている場合を除いて、より速くなるはずです。もし持っている場合は、もっと多くの小数点以下桁数（例えば `-dp 100000`）で再試行してください。それは私のマシンで10秒以上かかりますが、キャッシュされたら数秒で戻ってきます。

Redisのデータは、アクセス権を持つ任意のプロセスから読み書き可能です。コンソールに戻り、キャッシュされたデータを削除します：



```
DEL pi-1000
```


Redisコマンドは非常に簡潔です。値が削除された場合は `1` というレスポンスを、キーが見つからなかった場合は `0` というレスポンスを見るでしょう。アプリをもう一度実行すると、今回はキャッシュがないため、再計算する必要があります。

> キャッシュは重要ではありません - キャッシュなしでもアプリは正しく動作しますが、応答に時間がかかります

これはRedisのための完璧なユースケースです。基本的な階層では、Redisデータは複製されず、永続化されません。つまり、実質的には単一サーバー内のメモリ内にあります。再起動した場合、データは失われるため、取引データには適していませんが、キャッシュには問題ありません。

## イベントの購読

Redisは、データストレージと同じインスタンスでpub-subメッセージングもサポートしています。それは[Service Bus]()のような信頼性の高いメッセージングではありませんが、使用が速くて簡単です。

Piアプリケーションは、値を計算したときにイベントを公開するように設定することができます。キャッシュが機能しているかどうかを確認するのに便利な方法であり、他のプロセスが関心を持っているイベントかもしれません。

ポータルのRedisコンソールで、Piアプリが使用するチャンネルでメッセージを購読することができます：



```
SUBSCRIBE events.pi.computed
```


キューに購読されていることを確認するメッセージがいくつか印刷されます。これで、メッセージを待機しています。

自分の端末でいくつかのPi値を計算してみてください：

📋 `usecache` および `publishevents` フラグとRedis接続文字列を使用して、異なる小数点以下桁数のPiアプリをもう何度か実行してください。

<details>
  <summary>わからない場合は？</summary>

これらは新しい計算なので、それぞれを計算する必要があります：



```
dotnet run --project ./src/pi -dp 100 -usecache -publishevents -cs '<redis-name>.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False' 

dotnet run --project ./src/pi -dp 200 -usecache -publishevents -cs '<redis-name>.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False' 

dotnet run --project ./src/pi -dp 300 -usecache -publishevents -cs '<redis-name>.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False' 
```

</details><br/>

Azure Portalを確認すると、Redisコンソールでイベントが受信されているのを確認できます。各イベントは以下のような3行で印刷されます：


```
1) "message"
2) "events.pi.computed"
3) "Calculated Pi to: 200dp"
```


これはデモアプリですが、実際のアプリケーションでもこのパターンを見ることができます。集中的な操作はRedisに保存することでキャッシュされ、キャッシュにデータがあることを知らせるイベントが発行されます。そのデータを使用するコンシューマーはイベントを購読して、キャッシュからデータを再読込するタイミングを知ります。
## ラボ

Redisは、Service BusやEvent Hubsのような信頼性のあるメッセージキューではありません。複数のサブスクライバーがいてからPiを計算した場合、すべてのサブスクライバーがイベントを受け取りますか？Piアプリからイベントを発行しているときにサブスクライバーが実行されていない場合はどうなりますか？

Redisには無限のメモリがあるわけではなく、いっぱいになると古いキャッシュアイテムの削除を開始します。そのようなことが起きているかポータルで確認できますか？CLIもRedis APIの管理コマンドをサポートしています。キャッシュのすべてのエントリを削除してリセットできますか？

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボのリソースグループを削除します：



```
az group delete -y --no-wait -n labs-redis 
```

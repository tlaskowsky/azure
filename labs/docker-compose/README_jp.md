# Docker Compose

Docker Composeは、コンテナで実行される分散アプリケーションを記述するための仕様であり、それらの仕様を取り込んでDockerで実行するコマンドラインツールです。

これは_ARM_ や _Bicep_ と同じ考え方の _desired-state（望ましい状態）_ アプローチですが、YAMLでアプリをモデル化します。Composeコマンドは、モデル内の望ましい状態に到達するためのコンポーネントを作成または更新します。

## 参考文献

- [Docker Composeマニュアル](https://docs.docker.com/compose/)
- [Compose仕様 - GitHub](https://github.com/compose-spec/compose-spec/blob/master/spec.md)
- [Docker Compose v3構文](https://docs.docker.com/compose/compose-file/compose-file-v3/)


<details>
  <summary>CLI 概要</summary>

元のDocker Compose CLIは別のツールです:



```
docker-compose --help

docker-compose up --help
```


> 最新のDockerにはComposeコマンドが組み込まれています。コマンドは同じですが、ハイフンがないので`docker-compose`は`docker compose`になります。どちらを使用しても構いません。

</details><br/>


## マルチコンテナアプリ

Dockerはどんな種類のアプリも実行できます - コンテナイメージは軽量なマイクロサービスであっても、レガシーモノリスであっても構いません。それらはすべて同じ方法で動作しますが、コンテナは特に分散アプリケーションに適しており、各コンポーネントは別々のコンテナで実行されます。

サンプルアプリを実行してみましょう - これは[distributed App Service lab](/labs/appservice-api)で使用した乱数生成器のWebコンポーネントです:


```
docker run -d -p 8088:80 --name rng-web courselabs/rng-web:21.05
```


http://localhost:8088 にアクセスして乱数を試みてください。数秒後には失敗して`RNG service unavailable!`というエラーメッセージが表示されます。

📋 アプリケーションのログを確認して、何が起こっているかを見てみましょう。

<details>
  <summary>わからない場合は？</summary>



```
# コンテナはrng-webと呼ばれるはずです:
docker logs rng-web

# エラーが見られたら、コンテナIDを見つけてログコマンドを再実行してください:

docker ps

docker logs <id>
```


</details><br/>

> Webアプリはフロントエンドだけで、バックエンドREST APIサービスをhttp://numbers-api/rng で探していますが、そのようなサービスは実行されていません。

APIコンテナを`docker run`で開始できますが、イメージの名前、使用するポート、およびコンテナ同士が通信するためのネットワーク設定を知る必要があります。

代わりにDocker Composeを使用して、両方のコンテナをモデル化できます。

## Composeアプリ定義

Docker Composeは、コンテナで実行されるアプリのサービスと、コンテナを接続するネットワークを定義できます。

単純なアプリでもComposeを使用できます - これは単にNginxコンテナを定義しています:

- [docker-compose.yml](./nginx/docker-compose.yml)

> なぜこの内容をComposeファイルに入れるのか？イメージバージョンと使用するポートを指定することで、プロジェクトのドキュメントとして機能し、アプリの実行可能な仕様となります。

Docker Composeには独自のコマンドラインがあります - これで利用可能なコマンドを知ることができます:



```
docker-compose
```


📋 このアプリケーションを`docker-compose` CLIを使用して実行します。

<details>
  <summary>わからない場合は？</summary>



```
# アプリを開始するために'up'を実行し、Composeファイルを指します
docker-compose -f labs/docker-compose/nginx/docker-compose.yml up
```


</details><br/>

> Nginxコンテナはインタラクティブモードで起動し、すべてのログが表示されます。http://localhost:8082にアクセスして、正常に動作しているか確認できます。

Ctrl-C / Cmd-Cを使用して終了します - それによってコンテナが停止します。

## Composeでのマルチコンテナアプリ

Composeはより多くのコンポーネントがある場合により有用です:

- [rng/v1.yml](/labs/docker-compose/rng/v1.yml) は乱数アプリの2つの部分を定義しています。

この20行のYAMLにはかなり多くのことが行われています:

- 2つのサービスがあり、1つはAPI用、もう1つはWeb用です
- 各サービスは使用するイメージと公開するポートを定義しています
- Webサービスはロギングを設定するための環境変数を追加します
- 両方のサービスは同じコンテナネットワークに接続するように設定されています
- ネットワークは定義されていますが、特別なオプションは設定されていません。

📋 アプリを切り離されたコンテナで実行し、コンテナの状態とログをComposeで表示します。

<details>
  <summary>わからない場合は？</summary>



```
# アプリを実行します:
docker-compose -f ./labs/docker-compose/rng/v1.yml up -d

# このアプリのコンテナだけを表示するためにcomposeを使用します:
docker-compose -f ./labs/docker-compose/rng/v1.yml ps

# このアプリのログを表示します:
docker-compose -f ./labs/docker-compose/rng/v1.yml logs
```


</details><br/>

これらは標準のコンテナです - Compose CLIは、通常のDocker CLIが行うのと同じ方法でDockerエンジンにコマンドを送信します。

Composeで作成されたコンテナもDocker CLIを使用して管理できます:



```
docker ps
```


新しいWebアプリにアクセスしてhttp://localhost:8090で乱数を試してみてください。

> まだ動作していません！ Webアプリケーションのデバッグが必要のようです。

WebアプリはAPIから乱数を取得しています。それが失敗する理由は2つしかありません：

1. APIが動作していない
2. WebアプリがAPIに接続できない

APIはポートを公開しているので、独立して確認できます。

📋 APIの公開ポートを見つけて、`/rng`エンドポイントにアクセスしてみてください。

<details>
  <summary>わからない場合は？</summary>


```
# APIはポート8089でリスニングしています - ComposeファイルまたはCLIを使用してそれを確認できます:
docker-compose -f ./labs/docker-compose/rng/v1.yml port rng-api 80

curl localhost:8089/rng
```


</details><br/>

> ランダムな数が返され、APIが正しく動作していることがわかります。

WebアプリがAPIに接続していないようです。Webコンテナには、APIドメインがアクセス可能かどうかを確認するために使用できる`nslookup`ツールがパッケージされています。

📋 Webコンテナのログを確認して、使用しているAPIアドレスを確認し、そのドメインのIPアドレスをnslookupで取得してください。

<details>
  <summary>わからない場合は？</summary>


```
docker ps

docker logs rng-rng-web-1

# Webアプリは'domains-api'ドメインを使用しています

# コンテナ内でnslookupコマンドを実行します:
docker exec rng-rng-web-1 nslookup numbers-api
```


</details><br/>

> DNSエラーがあります - APIドメインにアクセスできません。

Composeファイルのサービス名は、そのサービスにアクセスするためにコンテナが使用できるDNS名になります。APIサービス名は`rng-api`であり、`numbers-api`ではありません。

ComposeファイルのAPIサービスを変更することも、WebアプリがAPI URLの設定値をサポートしているので、それを変更することもできます：

- [rng/v2.yml](/labs/docker-compose/rng/v2.yml) はその設定値を設定し、APIのログレベルも上げます。

ここで望ましい状態のアプローチを見ることができます。アプリケーションを変更する必要がある場合、YAMLを変更して再度`up`を実行します。Composeは実行中のものと要求されているものを見て、必要な変更を行います。

📋 `labs/docker-compose/rng/v2.yml`にある更新されたCompose仕様をデプロイし、Composeを使用してすべてのコンテナログをフォローします。

<details>
  <summary>わからない場合は？</summary>



```
docker-compose -f ./labs/docker-compose/rng/v2.yml up -d

docker-compose -f ./labs/docker-compose/rng/v2.yml logs -f
```

</details><br/>

> 仕様が変更されたため、Webコンテナが再作成されるのを見ることができます。今すぐアプリをhttp://localhost:8090で試してみてください。それは動作し、Composeからアプリのログを見ることができます。

## ラボ

Composeは、複数のコンテナにまたがるアプリを定義するために使用されますが、サービスはコンテナネットワークを通じてのみ関連しています。

RNGアプリ定義にNginxコンテナと別のネットワークを追加します。新しいネットワークと元のネットワークにNginxサービスを設定します。

更新された仕様をデプロイします。NginxコンテナにはどのIPアドレスがありますか？コンテナを接続できますか - RNG WebコンテナからNginxコンテナにアクセスできますか、それは後から作成されたにも関わらず？

> 行き詰まったら[hints](hints_jp.md)を試すか、[solution](solution_jp.md)を確認してください。

___
## クリーンアップ

すべてのコンテナを削除してクリーンアップします：



```
docker rm -f $(docker ps -aq)
```

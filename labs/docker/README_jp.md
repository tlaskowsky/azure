# Docker 101

Azure で .NET アプリをどのように実行しますか？ VM をプロビジョニングしてから接続し、.NET をインストールし、アプリのバイナリをダウンロードし、設定を行い、アプリを起動します。これらの手順を自動化するのは難しく、新しいインスタンスを起動するのに時間がかかり、複数のインスタンスを同期するのは困難です。また、App Service を使用することもできますが、設定することが多く、ローカルで持っているホスティング環境と異なるものになります。

Docker の登場です - アプリケーションのコンポーネントと依存関係を _イメージ_ と呼ばれるパッケージにまとめ、そのイメージを使用してアプリのインスタンスを _コンテナ_ として実行します。

## 参照

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) - ローカルマシンでコンテナを実行する最も簡単な方法
- [Docker の入門ガイド](https://docs.docker.com/get-started/) - Docker から
- [.NET コンテナイメージ](https://hub.docker.com/_/microsoft-dotnet) - .NET Core & 6 クロスプラットフォームイメージ
- [.NET Framework コンテナイメージ](https://hub.docker.com/_/microsoft-dotnet-framework) - .NET 3.5 & 4.8 Windows イメージ

## .NET Web コンテナの実行

まずはローカルでコンテナを実行し、後で Azure で実行するオプションを見ていきます。Docker Desktop が実行されていることを確認してください - タスクバーに Docker のクジラアイコンが表示されます（Windows を実行していて以前 Docker Desktop を使用したことがある場合は、Linux コンテナモードにしていることを確認してください）。

`docker` コマンドを使用してコンテナを実行および管理します。`az` コマンドのように、ヘルプテキストが組み込まれています：



```
docker --help
```


最初に最も使用するコマンドは `docker run` で、イメージから新しいコンテナを開始します。次のコマンドを実行して、コンテナでシンプルなWebサーバーを開始します：


```
docker run -d -p 8081:80 nginx:alpine 
```


たくさんの出力が表示され、最後には新しいコンテナの一意の ID が長いランダムな文字列として表示されます。このコマンドは何をしていますか？

- これは、`nginx:alpine` Docker イメージからコンテナを実行しています。これは公開されており、無料で使用できます。Alpine Linux OS ベースに Nginx Web サーバーがインストールされて構成されています。

- `-d` フラグはコンテナをバックグラウンドに置き、コマンドが返された後も実行を続けます。

- `-p` はコンテナのポートを公開し、Docker がコンテナへのネットワークトラフィックをルーティングできるようにします。このコンテナの場合、Docker はマシンのポート `8081` をリッスンし、コンテナのポート `80` へトラフィックを送信します。


> これで http://localhost:8081 にアクセスすると、新しい Web サーバーからの応答が表示されます。

他の `docker` コマンドを使用してコンテナアプリを管理します：



```
# 実行中のコンテナをリスト表示：
docker ps

# コンテナからログを取得：
docker logs <container-id>
```


📋 Microsoft の **ASP.NET サンプルアプリ** イメージから別のコンテナを実行します - それを [Docker Hub](https://hub.docker.com) で検索できます。ASP.NET コンテナをバックグラウンドで実行し、マシンのポート `8082` をコンテナのポート `80` に公開します。

<details>
  <summary>方法がわからない場合は？</summary>

Docker Hub で _.NET_ を検索すると、[ASP.NET](https://hub.docker.com/_/microsoft-dotnet-aspnet/) を含むすべてのイメージがリストされたページが表示されます。そこには `mcr.microsoft.com/dotnet/samples:aspnetapp` というイメージ名のサンプルアプリがあることがわかります。


```
docker run -d -p 8082:80 mcr.microsoft.com/dotnet/samples:aspnetapp
```


</details><br/>

> 新しいコンテナが実行されたら、http://localhost:8082 にアクセスします

Nginx アプリはまだ実行されていますか？コンテナにはどのバージョンの .NET が含まれていますか？ASP.NET サンプルアプリからログを出力できますか？

## ランタイム & SDK イメージ

Microsoft は .NET の Docker イメージを所有しており、異なるバリエーションを公開しています - Web アプリ用の ASP.NET を見てきましたが、コンソールアプリ用のランタイムイメージや、アプリケーションをビルドするために使用できる SDK イメージもあります。

コンテナを対話的に実行することができ、コンテナ内のシェルセッションに接続します。これは、クラウドに VM を作成して SSH で接続するのに似ています。

基本的な ASP.NET イメージから対話的なコンテナを実行し、環境を探索できます：



```
docker run -it --entrypoint sh mcr.microsoft.com/dotnet/aspnet:6.0

dotnet --list-runtimes

dotnet --list-sdks

exit
```


.NET および ASP.NET ランタイムがインストールされていることがわかりますが、SDK はありません。このイメージを使用してコンパイル済みのアプリを実行することはできますが、ソースコードからアプリをビルドすることはできません。

📋 .NET 6.0 **SDK** イメージから対話的なコンテナを実行します。これは Docker Hub で見つけることができます。それを使用して新しいコンソールアプリケーションを作成して実行します。

<details>
  <summary>方法がわからない場合は？</summary>

[.NET SDK](https://hub.docker.com/_/microsoft-dotnet-sdk/) には別のイメージがあります：



```
docker run -it --entrypoint sh mcr.microsoft.com/dotnet/sdk:6.0
```


これにより、.NET 6.0 ランタイムと SDK がインストールされたコンテナ内でシェルセッションが開始されます。

次にアプリを作成して実行します：



```
dotnet new console -o labs-docker

cd labs-docker

dotnet run
```


</details><br/>

新しいアプリを実行すると、標準的な _Hello, World!_ 出力が表示されます。これが Azure Cloud Shell の経験を思い出させる場合、それらのシェルセッションは実際には背後でコンテナで実行されています。

対話的なコンテナから離れてターミナルセッションに戻るには `exit` を実行します。

## コンテナ内で .NET アプリをビルド

コンテナ内でアプリをビルドすることは実験するための良い方法ですが、Docker の真の価値は自分自身の Docker イメージをパッケージングすることにあります：

- この [Dockerfile](/src/simple-web/Dockerfile) は、ASP.NET アプリを Docker にパッケージングするスクリプトです。SDK イメージを使用してアプリをビルドし、ASP.NET イメージを使用してアプリを実行します。

Dockerfile でできることはもっとたくさんありますが、これは良いスタートです。あなたのマシンに .NET 6.0 をインストールしなくても、.NET 6.0 アプリをビルドして実行できます。

以下を実行して、Dockerfile とソースコードから `simple-web` という名前のイメージをビルドします：



```
docker build -t simple-web src/simple-web
```


Docker が `dotnet` コマンドの出力を印刷し、アプリをビルドしてコンパイルするのを見ます。

📋 新しいイメージからバックグラウンドコンテナを実行し、マシンのポート `8083` をコンテナのポート `80` に公開します。

<details>
  <summary>方法がわからない場合は？</summary>

それは同じ `docker run` コマンドです。

イメージ名は Docker Hub または Microsoft のコンテナレジストリへの参照、またはローカルイメージにすることができます：



```
docker run -d -p 8083:80 simple-web 
```

</details><br/>

> http://localhost:8083 にアクセスしてアプリを確認してください。

アプリは非常にシンプルですが、改善することができます。`src/simple-web/src` フォルダ内のコードを編集し、変更をパッケージ化するためにビルドコマンドを再度実行してください。新しいコンテナを実行してテストしてください - ただし、同じ `docker run` コマンドを繰り返すことはできません - それはなぜでしょうか？

## ラボ

コンテナイメージは静的なパッケージです - 実際にはアプリケーションのバイナリや依存関係、ランタイム、オペレーティングシステムのツールをすべて含むZIPファイルに過ぎません。イメージ名には通常バージョン番号が含まれており、アプリの異なるバージョン用に異なるイメージを公開することができます。イメージからコンテナを実行する場所に関係なく、アプリは常に同じ方法で動作します。なぜなら、開始点は常に同じだからです。

通常、環境間で変化が生じるため、コンテナを実行する際にアプリに設定を注入する方法が必要です。最も簡単な方法は、_環境変数_ を使用することで、これをコンテナを実行する際に設定し、.NET の設定システムによって読み取られます。シンプルなウェブアプリは、ホームページ上で環境名を示すために設定を使用します - ポート `8084` でリスニングし、ホームページ上で環境名 `PROD` を示す新しいコンテナを実行してください。

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

多くのコンテナが実行されているでしょうが、コンテナは使い捨てのものとして意図されています。

すべてを削除するには次のコマンドを実行してください：



```
docker rm -f $(docker ps -aq)
```

# Web Apps のための App Service

IaaS オプションは、ホストマシンにアクセスしてアプリケーションを設定・デプロイする必要がある場合に適していますが、多くの管理オーバーヘッドを伴います。Platform-as-a-Service（PaaS）はそれを代わりに処理し、アプリケーションがPaaS環境でサポートされていれば、デプロイメントとアップデートを簡素化します。AzureにはいくつかのPaaSオプションがあり、App Serviceは非常に人気があります。

このラボでは、ローカルマシンからソースコードをプッシュすることでApp Serviceデプロイメントを作成します。Azureはアプリケーションをコンパイルし、設定してくれます。

## 参照

- [Azure App Service 概要](https://docs.microsoft.com/ja-jp/azure/app-service/overview)
- [App Service プラン概要](https://docs.microsoft.com/ja-jp/azure/app-service/overview-hosting-plans)
- [`az appservice` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/appservice?view=azure-cli-latest)
- [`az webapp` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/webapp?view=azure-cli-latest)

## App Service の探索

ポータルで新しいリソースを作成します - _Web app_ を検索します（App Service のタイプの1つ）：

- アプリにはリソースグループとApp Serviceプランが必要です
- ソースコード、Dockerコンテナ、静的Webコンテンツから公開するオプションがあります
- ソースコードオプションでは、ランタイムスタックとOS（例：Java on Linux or .NET on Windows）を選択できます

通常、ポータルからではなくCLIから作成します。

## App Service プランの作成

ラボ用のリソースグループを作成します：



```
az group create -n labs-appservice  -l southeastasia --tags courselabs=azure
```


アプリを作成する前に、アプリケーションを実行するために必要なインフラストラクチャを抽象化したApp Serviceプランが必要です。

📋 基本的なB1 SKUを使用して、2インスタンスでApp Serviceプランを作成します。

<details>
  <summary>方法がわからない場合</summary>

これは比較的簡単です：



```
az appservice plan create -g labs-appservice -n app-service-01 --sku B1 --number-of-workers 2
```


</details><br/>

ポータルでRGを開いてください。唯一のリソースはApp Serviceプランです。それを開くと、アプリのリストが空であり、プランSKUによって制限されるスケールアップとスケールアウトのオプションが表示されます。

## Git デプロイメント用のアプリの作成

新しいApp Serviceプランを使用してWebアプリを作成できます。サポートされているプラットフォームを確認するために利用可能なランタイムをリストアップします：


```
az webapp list-runtimes
```


Windowsオプションの下にはASP.NET 4.8があります。これはほとんどの古い.NETアプリケーションに対応しており、ソースコードがあればIaaSで得られる制御を必要としない場合に、クラウドへのアプリの移行に適しています。

📋 ASP.NET 4.8ランタイムを使用して、ローカルのGitリポジトリからデプロイするために設定されたサービスプラン内のWebアプリを作成します。

<details>
  <summary>方法がわからない場合</summary>

新しいWebアプリのヘルプテキストを確認します：



```
az webapp create --help
```


ランタイム、デプロイメント方法、一意のDNS名を指定する必要があります：


```
az webapp create -g labs-appservice --plan app-service-01  --runtime 'ASPNET:V4.8' --deployment-local-git --name <dns-unique-app-name>
```


</details><br/>

CLIコマンドが完了したら、ポータルで再度RGを確認してください。

> 今度はWebアプリが別のリソースとしてリストされています - タイプは _App Service_ です - しかし、プランとアプリの間でナビゲーションが可能です

Webアプリを開くと、設定したアプリケーション名を使用するパブリックURLがあります。HTTPSはプラットフォームによって提供されます。

アプリURLをブラウズすると、「コンテンツを待っている実行中のWebアプリ」というランディングページが表示されます。

## Webアプリのデプロイ

ローカルのGitリポジトリからWebアプリにデプロイするのは、`git push`を実行するのと同じくらい簡単ですが、まずいくつかの設定を行う必要があります。ソースコードはこのリポジトリにありますが、Azureに使用するブランチとコードへのパスを伝える必要があります。

Webアプリには、アプリケーションが読み取ることができる設定を適用できます。これらはプラットフォームによって使用されることもあります。これらの設定は、適切なアプリケーションコードがデプロイされることを保証します：



```
# メインブランチを使用します：
az webapp config appsettings set --settings DEPLOYMENT_BRANCH='main' -g labs-appservice -n <dns-unique-app-name>

# コードはWebFormsフォルダにあります：
az webapp config appsettings set --settings PROJECT='src/WebForms/WebApp/WebApp.csproj' -g labs-appservice -n <dns-unique-app-name>
```


これの仕組みは、WebアプリがGitサーバーとして機能することです。Webアプリをリモートリポジトリとして設定し、コードをプッシュすることができます。コードがプッシュされるたびに、コンパイルされ、Webアプリがそれを実行するように設定されます。

📋 Webアプリデプロイメントの公開資格情報を印刷し、特に `scmUri`（使用する必要のあるリモートGitの場所）を確認します。

<details>
  <summary>方法がわからない場合</summary>

Webアプリのための多くのサブコマンドがあります。公開資格情報をリストアップすると、Git URLと資格情報が表示されます：



```
az webapp deployment list-publishing-credentials --query scmUri -g labs-appservice -o tsv -n <dns-unique-app-name> 
```

</details><br/>

> Gitの資格情報がURLに埋め込まれているのはセキュリティ上の悪夢です。[代替のオプション](https://docs.microsoft.com/ja-jp/azure/app-service/deploy-configure-credentials?tabs=cli)がありますが、このラボではこれで十分です。

出力された資格情報コマンドを使用して、WebアプリSCMをGitリポジトリのリモートとして追加できます：



```
# ユーザー名にドル記号が含まれているのでシングルクォートを使用：
git remote add webapp '<url-with-credentials>'

# リモートが正しく保存されていることを確認：
git remote -v
```


正しいブランチを使用して、ローカルリポジトリをwebappリモートにプッシュすることでアプリをデプロイできます：



```
git push webapp main
```


通常のGit出力（オブジェクトの圧縮と書き込みについて）が表示された後、さらに多くの出力が表示されます。リモートはデプロイメントスクリプトを生成し、MSBuildの出力が表示されます。これは.NETアプリケーションがコンパイルされていることを意味します。`git push`が完了すると、アプリはコンパイルされてデプロイされています。

## ビルドの確認

WebアプリのURLを更新すると、標準的なASP.NETのホームページが表示されます。それほど複雑なアプリケーションではありませんが、VMやビルドサーバーなしで、ソースコードから数分でデプロイしました。

問題を診断するために接続できるVMはありませんが、ポータルには多くのツールがあります。Webアプリのブレードを開き、_コンソール_ オプションに移動します。これにより、Webアプリホストのターミナルセッションに接続されます。

ファイルシステムを探索します。これらのフォルダーはデプロイメントスクリプトによって作成され、データが追加されました：



```
dir

dir bin
```


ホストの環境変数をリストアップし、App Service固有の設定がたくさんあることがわかります：



```
set
```


## ラボ

Webアプリは少し退屈です。ホームページのコンテンツを変更して再デプロイしてみてください。更新が反映されるまでどのくらい時間がかかりますか？

> 困った場合は[ヒント](hints_jp.md)を参照するか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

リソースグループを削除してクリーンアップします：



```
az group delete -y -n labs-appservice
```

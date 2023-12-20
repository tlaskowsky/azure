# 静的Webアプリ用のApp Service

App ServiceはWebアプリのコンパイル、パッケージング、デプロイに非常に優れていますが、よりシンプルなアプリには別のオプションもあります。静的HTMLサイトやバックエンド処理のないシングルページアプリケーション（SPA）を持っている場合、それを静的Webアプリとしてデプロイできます。静的Webアプリのコンテンツは外部Gitリポジトリから読み込まれるため、ローカルファイルシステムからのデプロイはできません。

このラボでは、GitHubリポジトリから静的Webアプリを作成する方法と、静的コンテンツを持つ標準的なWebアプリとの比較を行います。

## 参考資料

- [Azure Static Web Apps 概要](https://learn.microsoft.com/ja-jp/azure/static-web-apps/overview)

- [App Service プラン 概要](https://docs.microsoft.com/ja-jp/azure/app-service/overview-hosting-plans)

- [`az appservice` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/appservice?view=azure-cli-latest)

- [`az staticwebapp` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/staticwebapp?view=azure-cli-latest)

## 静的Webアプリの作成

この演習にはGitHubアカウントが必要です（無料です - [こちらからサインアップできます](https://github.com/signup)）。ログインして、コースリポジトリのフォークを作成します。

- https://github.com/azureauthority/azure/fork

_フォークの作成_をクリックして確認します。

> 完了すると、GitHubの自分のユーザー名の下にコースラボの自分のコピーができます。

GitHubページからフォークのURLをコピーして（デフォルトのURLは `https://github.com/<github-username>/azure` です）、それをリモートとして使用します。


```
git remote add fork <github-fork-url>
```


これが静的Webアプリをデプロイするためのリポジトリになります。

📋 リソースグループを作成し、`staticwebapp create` コマンドを使用してリポジトリから新しい静的Webアプリをデプロイします。`main` ブランチを使用し、アプリケーションコンテンツの場所は `/labs/appservice-static/html` です。

<details>
  <summary>わからない場合</summary>

リソースグループを自分の好きな場所で作成します。


```
az group create -n labs-appservice-static  -l southeastasia --tags courselabs=azure
```


静的Webアプリを作成するためのヘルプテキストを確認します。



```
az staticwebapp create --help
```


GitHubとのインタラクティブなログインオプションがあり、アクセストークンを作成する必要がありません。



```
az staticwebapp create  -g labs-appservice-static --branch main --app-location '/labs/appservice-static/html' --login-with-github -n labsappservicestatices --source <github-fork-url>
```


</details><br/>

インタラクティブなGitHub認証を使用すると、CLIはシークレットコードを表示し、Azureからのアクセスを確認するように求めるWebページが起動します。

完了したら、Portalを開いてRGのリソースを調べます。

- 静的Webアプリリソースのみで、App Serviceやプランはありません
- リソースを開いて公開URLを確認します
- URLにアクセスして、サイトが表示されることを確認します

## コンテンツ変更のプッシュ

静的Webアプリにはデプロイメントワークフローへのリンクもあります。それを開くと、GitHubにあるYAMLファイルを見ることができます。これはAzureが作成してリポジトリに追加したGitHubアクションです。フォークに変更をプッシュするたびに実行されます。

このファイルのHTMLコンテンツを編集します。

- [html/index.html](/labs/appservice-static/html/index.html)

📋 gitコマンドラインを使用して、ワークフローファイルを同期するためにフォークからプルし、変更を加えてコミットし、フォークにプッシュします。

<details>
  <summary>わからない場合</summary>



```
git pull fork main

git add labs/appservice-static/html/index.html

git commit -m 'Update static web app'

git push fork main
```


</details><br/>

GitHubでフォークを開きます。_Actions_ タブに移動すると、更新されたWebコンテンツをデプロイしている新しいワークフローが表示されます。ログをすべて確認できます。

デプロイメントが完了したら、Webアプリページを更新して変更を確認します。

## 静的コンテンツ用のWebアプリの使用

静的Webアプリは非常にシンプルで、スケールが良く、バックエンドAPIと連携したい場合に他のAzureサービスと統合できます。

また、より多くのデプロイメントや管理オプションを必要とする場合には、標準的なWebアプリを静的コンテンツにも使用できます。

ローカルマシンの静的コンテンツのパスに移動し、`webapp up` コマンドでWebアプリとして作成します。



```
cd labs/appservice-static/html 

az webapp up -g labs-appservice-static --html --sku F1 -n <unique-dns-name> 
```


> CLIはApp ServiceプランとWebアプリを作成し、現在のディレクトリのコンテンツのZIPファイルを生成してデプロイします。

出力のURLにアクセスします。これはあなたの更新が含まれた同じ静的アプリですが、GitHubからコミットやプッシュする必要はなく、ローカルフォルダーから来ています。

Portalで再度RGを開きます。新しいApp ServiceプランとApp Serviceが表示されます。Webアプリを開くと、このアプリにはランタイムがないにもかかわらず、多くの管理オプションが表示されます。サイトがホストされているOSとWebサーバーは何ですか？

curlを使用してコンテンツの配信方法を確認します。



```
curl.exe -IL <url>
```

> WebサーバーはIISなので、これはWindowsサーバーである必要があります。実際にはASP.NETを使用しており、これはApp Serviceを使用する静的Webサイトのデフォルトです。

## Node.jsを使った混合コンテンツの利用

静的コンテンツとバックエンド処理が必要なコンテンツを持っている場合、それらをStatic Web Appといくつかのサーバーレス関数で提供することができます。既存のアプリケーションをそのアーキテクチャに対応させるためには変更が必要ですが、App Serviceアプリとしてデプロイすることもできます。

このラボには、2つのコンテンツを公開するNode.jsアプリがあります：

- 静的HTMLページ [index.html](/labs/appservice-static/node/public/index.html)
- ユーザーの認証情報を表示する/userエンドポイント [app.js](/labs/appservice-static/node/app.js)

📋 `node`フォルダからアプリをデプロイし、既存のApp Serviceプランを使用し、Node 16をランタイムとして指定してください。

<details>
  <summary>方法が不明な場合</summary>

Node.jsのランタイムを探すには：



```
az webapp list-runtimes --os Windows
```


プラン名を探します：



```
az appservice plan list -g labs-appservice-static -o table
```

Navigate back to the node folder:

```
cd ../node 
```


新しいWebアプリを作成します - App ServiceプランがWindowsなので、Windows Nodeランタイムを使用する必要があります：



```
az webapp up -g labs-appservice-static --runtime NODE:16LTS --os-type Windows --plan <app-service-plan> -n <unique-dns-name> 
```


</details><br/>

`up`コマンドを正しく入力することが重要です。既存のアプリプランを使用するために、設定を明示的に指定しないとエラーが発生します。

> デプロイが完了したら、Portalで新しいApp Serviceを確認できますが、同じApp Serviceプランを使用しています。

Portalで各App Serviceを探索します。アプリをホストしているマシン名を見つけることができますか？両方で同じマシンですか？

## ラボ

新しいNode.jsアプリのURLにアクセスすると、静的HTMLが表示されるはずです。/userエンドポイントを試すと、認証情報が`undefined`と表示されます。

アプリには認証プロバイダーの設定が必要です。コードはAzureアカウントを想定しています。Portalでアプリを設定し、アイデンティティプロバイダーを追加し、再度アプリにアクセスするとログインが必要になることを確認してください。

> 詰まった場合は、[ヒント](hints_jp.md)を参照するか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

RGを削除して整理整頓します：



```
az group delete -y -n labs-appservice-static --no-wait
```

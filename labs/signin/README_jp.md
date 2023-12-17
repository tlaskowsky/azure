# Azureサインイン

Azureは、Microsoftアカウントを使用して認証および認可を行います。企業環境ではアカウントが管理されますが、個人アカウントを作成し、自分自身の使用のためにAzureサブスクリプションを作成することができます。1つのアカウントで複数のサブスクリプションに対する権限を持つことができます。

## 参考資料

- [サブスクリプション](https://docs.microsoft.com/en-gb/learn/modules/configure-subscriptions/3-implement-azure-subscriptions)

- [Azureドキュメントホーム](https://docs.microsoft.com/en-gb/azure/?product=popular)

- [azコマンドラインインターフェース](https://docs.microsoft.com/en-us/cli/azure/reference-index?view=azure-cli-latest)


## Azureポータルの探索

https://portal.azure.com にアクセスして、Microsoftアカウントでサインインします。

[すべてのサービス](https://portal.azure.com/#allservices)ビューを開きます。ここでAzureの全てを見ることができます。たくさんありますね...

- _サブスクリプション_ サービスを探し、開きます。これで、アクセスできるすべてのサブスクリプションを確認できます。

- _仮想マシン_ ビューを開きます。新しいWindows VMを作成する方法と、指定する必要がある設定は何ですか？

- Azureホームに戻り、_クイックスタートセンター_ を探します。Azure Web Appsのリファレンスアーキテクチャでは何が見えますか？

> ポータルはAzureサービスを閲覧し、リソースを探索する素晴らしい方法です。しかし、繰り返し可能で自動化された体験は提供しません。


## Azure CLIの使用

[Az](https://docs.microsoft.com/en-us/cli/azure/) コマンドラインは、Azureと対話する別のオプションです。これを推奨する理由は以下の通りです：

- 新機能やサービスが最初に更新される
- 統合されたヘルプがある
- リソースの作成や管理に使用できる
- クロスプラットフォームでCI/CDパイプラインで簡単にスクリプト化できる

統合ヘルプの動作を確認するために、`az`コマンドを実行してみてください。

最新バージョンを持っていることを確認するためにアップグレードしてください：

```
az upgrade
```

その後、Azureにログインします：

```
az login
```

📋 コマンドラインを使用して、アクセスできるすべてのアカウントをリストします。最もユーザーフレンドリーな出力を見つけるために異なる出力を試してみてください。

<details>
  <summary>わからない場合は？</summary>

これでアカウントとサブスクリプションが表示されます：

```
az account list
```

そして `-o` または `--output` フラグを使用して、JSON、YAML、テーブル形式の間で切り替えます：

```
az account list -o table
```

</details><br/>

> 各サブスクリプションには一意のIDがあります。`CloudName`フィールドは何を表していると

思いますか？

## Azureシェル

CLIをインストールできない場合にAzureシェルを使用したいことがあります。その場合、Azure Cloud Shellを使用できます。これは、Azureツールがインストールされ設定されたターミナルセッションを提供するWebインターフェースです。

https://shell.azure.com にアクセスします

- PowerShellまたはBashをターミナル用に選択できます
- シェルセッションはAzureストレージを使用してファイルとコマンド履歴を永続化します
- `az`コマンドラインはすでにインストールされています

以下を実行してみてください：

```
az account list -o tsv
```

> タブ区切り変数形式でサブスクリプションが一覧表示されます。ポータルで認証したので、すでにサインインしています。

コマンドラインでCloud Shellを探索するのは難しいですが、検索すると事前にインストールされた多くのツールがあることがわかります。

## ラボ

これは、Cloud Shellで何ができるかを示す簡単なラボです。このフォルダにはC#のソースファイルがあります：

- [labs/signin/src/Program.cs](./src/Program.cs)

Cloud Shellで`dotnet new`コマンドを使用して、新しいコンソールプロジェクトを作成します。C#ファイルをプロジェクトディレクトリにアップロードして実行してみてください。

> つまずいたら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

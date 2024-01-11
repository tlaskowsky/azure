# Azure Functions

Azure Functionsは、_サーバーレス_ コンピュート プラットフォームです - コードを提供し、インフラストラクチャの準備やスケール管理は必要ありません。イベントに応答してファンクションが_トリガー_され、それはBlobストレージにファイルがアップロードされることや、HTTPリクエストが入ってくることなどが含まれます。

Azure FunctionsはFunctions App内でホストされ、これはApp Serviceの一種（Web Appsのような）です。Function AppsはApp Service Planの一部ですが、_消費_ モデルを使用でき、これによりファンクションが実行されている間のみインフラストラクチャが準備され、支払われます。アクティビティがないときのコストはありません。

このラボでは、HTTPコールからトリガーされるシンプルなファンクションを使って始めて、ローカルとAzureでの実行方法を見ていきます。

## 参照

- [Azure Functions 概要](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-overview)
- [Functions 開発者ガイド](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-reference?source=recommendations&tabs=blob)
- [`az functionapp` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/functionapp?view=azure-cli-latest)
- [`func` CLI コマンド](https://learn.microsoft.com/ja-jp/azure/azure-functions/functions-run-local?tabs=v4%2Cmacos%2Ccsharp%2Cportal%2Cbash#create-a-local-functions-project)

## Azure Functions 探索

Portalを開き、新しいリソースを作成します。'function'を検索し、_Function app_ を選択して_作成_します。いくつかの興味深いオプションがあります：

- function app名はDNSプレフィックスです
- デプロイメントの選択肢はコードまたはDockerコンテナです
- コードを選択する場合、ランタイムとOSを選ぶ必要があります
- 消費プランはサーバーレスですが、プレミアムプランで常時稼働するプランも使用できます
- ストレージアカウントが必要です。これはFunction Appがログや他の詳細を保存する場所です

> 新しいFunction Appを作成してください。新しいRGとストレージアカウントも作成します。

PortalでFunction Appを作成すると、ブラウザー内で直接コードを編集できます。左メニューから_Functions_を開き、_作成_をクリックします：

- ドロップダウンから_Portalで開発_ を選択
- リストから_HTTP トリガー_ テンプレートを選択
- 新しいファンクションに_hello_という名前を付けます
- _認証レベル_ を_匿名_に設定

ファンクションが作成されたら、_コード + テスト_ メニューに切り替えます。ここからファンクションを実行できますか？ファンクションが送り返すレスポンスを編集して保存します。URLをブラウズして、ブラウザーからファンクションを呼び出せますか？

Portalのファンクションに関するRGを探索し終えたら、削除しても構いません。

## Functions コマンドライン

Portalは実験には素晴らしいですが、遊び場以外の何かには、ソースコントロールにファンクションコードを置きたくなるでしょう。Azure Functionsには、プロジェクトを作成し、ローカルマシンでファンクションを実行し、AzureにデプロイするためのカスタムCLIがあります。

最初に、Azure Functions Core Toolsをインストールする必要があります（Macでは[Homebrew](https://brew.sh)、Windowsでは[Chocolatey](https://chocolatey.org/install#install-step2)を使用）：


```
# Windows:
choco install azure-functions-core-tools

# macOS:
brew tap azure/functions
brew install azure-functions-core-tools@4
```


> [他のインストーラーも利用可能](https://github.com/Azure/azure-functions-core-tools)ですが、OSに合わせてv4オプションをインストールしてください。

インストールを確認してください：


```
func --version
```


出力は、バージョン4（私のものは4.0.4829ですが、あなたのものは新しいかもしれません）であることを示しているはずです。

## ローカルでのFunctions実行

`calendar`フォルダー内には、HTTPトリガーを持つ.NET関数ライブラリがあります：

- [calendar/HttpTime.cs](/labs/functions/http/calendar/HttpTime.cs) - ログエントリを書き込み、現在の時刻でレスポンスを送信するだけです

`func` CLIを使用して、ローカルでファンクションを実行できます：



```
cd labs/functions/http/calendar

func start
```


> .NETビルドからの出力が表示され、その後、ファンクションがlocalhostでリスニングしているという行が表示されます

試してみてください：



```
curl http://localhost:7071/api/HttpTime
```


これは短いフィードバックループに最適です。Azureへのデプロイを待たずに機能をテストし、ライブに行く前に変更を加えて`func start`を再実行してテストできます。

ローカルでファンクションを実行するために必要なのは、その一つのコマンドだけです :) 次に、Azureへのデプロイに進みます。

## Azure Functionsへのデプロイ

Azureでは、事前に必要なリソースを作成する必要があります - リソースグループとストレージアカウント：



```
# サーバーレスファンクションをサポートする近くの場所を探します：
az functionapp list-consumption-locations -o table

# RGを作成します：
az group create -n labs-functions-http --tags courselabs=azure -l eastus

# そしてストレージアカウント - ファンクションのために使いたいリージョンを使用します：
az storage account create -g labs-functions-http --sku Standard_LRS -l eastus -n <sa-name>
```


📋 `az functionapp create` コマンドを使用して、ファンクションをホストするためのFunction Appを作成します。ランタイムとして.NETを選択し、Functionsバージョン4を選択します。消費プランのリージョンを選択し、作成したばかりのストレージアカウントにリンクします。

<details>
  <summary>方法がわからない場合は？</summary>

ヘルプテキストを確認してください：

```
az functionapp create --help
```

リージョンのフラグは通常とは異なります。これは、消費プランと場所を一つに指定するためです：



```
az functionapp create -g labs-functions-http  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name> 
```


</details><br/>

ポータルで確認してください - スケールアップやスケールアウトのオプションがないアプリサービスプランがあります。これはサーバーレス（消費）プランです。Functionアプリが以下の特徴を持っていることがわかります：

- パブリックURLを持つ
- Windowsを使用している（デフォルトのOS）
- Web Appsと同様のUXを持つ（すべてApp Serviceの機能だから）

これで、関数CLIから直接Azureにデプロイできます：



```
func azure functionapp publish <function-name>
```


> アプリがローカルでコンパイルされてアップロードされるのを見るでしょう

出力には関数のパブリックURLが含まれます。この関数は匿名認証を使用しているので、次のようにテストできます：



```
curl <public-url>/api/HttpTime
```


ポータルでストレージアカウントを確認します。Blobコンテナはロックされていますが、ファイルストレージは閲覧可能です。これはアプリのルートファイルシステムです。アプリケーションのログを見つけることができますか？

## 関数の追加

一つのFunctionアプリで複数の関数をホストすることができます - 通常、プロジェクトのすべての関数を一つのFunctionアプリにまとめます。

`update`フォルダに追加できるいくつかの関数があります：

- [update/HttpDate.cs](/labs/functions/http/update/HttpDate.cs) - 現在の日付に応答するHTTPトリガー

- [update/HttpDay.cs](/labs/functions/http/update/HttpDay.cs) - 週の現在の曜日に応答するHTTPトリガー



```
# calendarフォルダにまだいることを確認してください：
pwd

# このフォルダに新しい関数をコピーします：
cp ../update/*.cs .

# 関数エミュレータを起動します：
func start
```


> 新しい関数がプロジェクトフォルダにあるので、3つのHTTP関数がリストされているのを見るでしょう

コード内の関数の名前は、HTTPトリガーの呼び出し用URLパスになります。

新しい関数をテストしてください：



```
curl http://localhost:7071/api/HttpDate

curl http://localhost:7071/api/HttpDay
```


📋 Azureへの更新された関数をデプロイします。

<details>
  <summary>方法がわからない場合は？</summary>

新しい関数はローカルプロジェクトにあるので、同じ公開コマンドです：



```
func azure functionapp publish <function-name>
```


</details><br/>

デプロイされた関数をいくつか試してみてください：


```
curl <public-url>/api/HttpTime

curl <public-url>/api/HttpDate
```


ポータルで確認すると、プランにはまだ一つのFunctionアプリがあります。それを開いて、個々の_関数_を確認できます。一つを開いて、メニューオプションを確認してください - _インテグレーション_ ではトリガー、入力、出力を表示し、_モニター_ では最近の実行と結果を表示します。一つをクリックしてログを確認してください。

他の新しい関数を試してみてください：



```
curl -i <public-url>/api/HttpDay
```


> 認証が必要なので、「許可されていない」というエラーが出ます

📋 ポータルで関数のオプションを閲覧し、curlで`HttpDay`関数を呼び出すための認証キーを見つけます。

<details>
  <summary>方法がわからない場合は？</summary>

ポータルに戻り、関数を開いて_関数URLの取得_をクリックします - ここにはキーが含まれており、_関数キー_ でも表示されます。ポータルは関数が認証が必要であるためにキーを追加します。

トークンを含むURLは次のようになります：

```
curl https://courselabsazes.azurewebsites.net/api/HttpDay?code=UUGtEqCqMqF-whsqFzfaafajkURTOgXqVS1lZ9eepgHIXvObgAzFuJ2bGVA==
```


</details><br/>

関数の構造は他のAppサービスと同じです：

- Appサービスプラン -> Appサービス -> アプリ
- Appサービスプラン -> Functionアプリ -> 関数

関数の大きな違いは、Appサービスプランが消費モデルになり、サーバーを実行する必要がないことです。

## ラボ

これを試してみてください。新しいディレクトリを作成し、そこから`func new`で新しい関数を作成します。ランタイムとしてPowershellを選択し、HTTPトリガーテンプレートを選びます。関数をローカルで実行します - コード変更なしで動作しますか？関数をAzureに公開します。既存のFunctionアプリを使用して新しい関数をホストできますか？

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除します：


```
az group delete -y --no-wait -n labs-functions-http
```

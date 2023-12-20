# IaaS - アプリケーションのデプロイ

インフラストラクチャー・アズ・ア・サービス(IaaS)は、クラウドデプロイメントやクラウド移行を始めるのに簡単な方法です。VMに必要なものを構成し、任意のタイプのアプリケーションをデプロイできます。アプリをプラットフォーム・アズ・ア・サービス(PaaS)に移行するロードマップを持っているかもしれませんが、IaaSは良い出発点になることがあります。

このラボでは、インフラストラクチャを作成し、Windows上の古い.NETアプリケーション（SQL Serverデータベースを使用）をデプロイします。


## リソースグループの作成

いくつかのリソースを作成するので、再利用できる変数をいくつか保存しておきます：



```
# PowerShellで：
$location='southeastasia'
$rg='labs-iaas-apps'
$server='<unique-dns-name>'
$database='signup'

# またはBashで：
location='southeastasia'
rg='labs-iaas-apps'
server='<unique-dns-name>'
database='signup'

# RGを作成：
az group create -n $rg  -l $location --tags courselabs=azure
```


## SQLデータベースの作成

デプロイするアプリケーションはデータベーススキーマを作成するので、接続可能な空のデータベースから始めるだけで良いです。

📋 リソースグループにSQLサーバーとSQLデータベースを作成します（[SQLラボ](/labs/sql/README_jp.md)で説明されています）。

<details>
  <summary>わからない場合</summary>

すでに設定した変数を使用し、多くの設定項目のデフォルト値を受け入れるシンプルなコマンドを使用できます：



```
az sql server create -g $rg -l $location -n $server -u sqladmin -p '<admin-password>'

az sql db create -g $rg -n $database -s $server --no-wait
```


</details><br/>

SQLデータベースが準備できるのを待つ必要はありません。次のステップに進みましょう。

## Windows Server VMの作成

アプリケーションは.NET Frameworkを実行する必要があります。これは古いWindows専用プラットフォームですが、最新のWindows Serverリリースでまだサポートされています。

Windows Server VMイメージは、`MicrosoftWindowsServer`のパブリッシャーと`WindowsServer`のオファーの下にリストされています。

📋 Windows Server 2022の最新リリースを使用し、Datacenter Core 2nd-generation SKUでVMを作成します（[Windows VMラボ](/labs/vm-win/README.md)でSKUの検索方法を見ました）。

<details>
  <summary>わからない場合</summary>



```
# SKUをリストアウト：
az vm image list-skus -l westus -p MicrosoftWindowsServer -f WindowsServer -o table
```


出力の中に`2022-datacenter-core-g2`というSKUが表示されるはずです。


```
# 最新バージョンのイメージを使用してVMを作成：
az vm create -l $location -g $rg -n app01 --image MicrosoftWindowsServer:WindowsServer:2022-datacenter-core-g2:latest --size Standard_D2s_v5 --admin-username labs --public-ip-address-dns-name <your-unique-dns-name> --admin-password <your-strong-password>
```


</details><br/>

PIPのDNS名を追加する必要はありませんが、追加するとVMに接続するのが簡単になります。

## アプリのデプロイ

VMが起動して実行されたら、アプリを接続しデプロイできます。このラボでは手動で行いますが、後で自動化オプションを見ていきます。

パブリックIPアドレスまたはDNS名、および作成時に設定した資格情報を使用して、リモートデスクトップクライアントでVMに接続します。

> これはWindows Server Core VMなので、慣れ親しんだWindows GUIはありません。ターミナルセッションに入ります。

これで、一般的なパターンに従ってアプリのデプロイ手順に進むことができます：

- アプリの実行に必要な依存関係をインストール
- アプリケーションをインストール
- アプリケーション設定を構成

### .NET Framework 4.8

VMにはすでに.NETがインストールされています。最初にバージョンを確認します：



```
# インストールされている.NET Frameworkのバージョンを表示します：
Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP' -recurse |
Get-ItemProperty -name Version,Release -EA 0 |
Where { $_.PSChildName -match '^(?!S)\p{L}'} |
Select PSChildName, Version, Release
```


フルフレームワークが_4.8_である必要があります（そうでない場合はインストールする必要があります）。

### Webサーバー

アプリケーションにはWebサーバーが必要です。Windows ServerはInternet Information Services（IIS）を実行しますが、デフォルトではインストールされていません。Windows機能を追加することでインストールします：



```
# IISコンポーネントがインストールされていないことを確認：
Get-WindowsFeature

# 必要な機能をインストール：
Install-WindowsFeature Web-Server,NET-Framework-45-ASPNET,Web-Asp-Net45
```


### アプリケーションのインストール

アプリケーションはWindows Installer MSIとしてパッケージされ、GitHubに公開されています。

MSIをダウンロードして実行し、アプリをインストールします：



```
# パッケージをダウンロード：
curl -o signup.msi https://github.com/azureauthority/azure/releases/download/labs-iaas-apps-1.0/SignUp-1.0.msi

# デプロイします：
Start-Process msiexec.exe -ArgumentList '/i', 'signup.msi', '/quiet', '/norestart' -NoNewWindow -Wait
```


プロセスが完了したら、アプリケーションがデプロイされたことを確認します：



```
# パッケージによって作成されたファイルをリストアップ：
ls /docker4.net/SignUp.Web

# アプリケーションがWebサーバーに登録されていることを確認：
Get-WebApplication
```


VMでlocalhostにHTTPリクエストを行い、アプリをテストできます。ここでは応答を得られますが、エラーログがいっぱいになります。デプロイはまだ完了していません：



```
curl.exe -L http://localhost/signup
```

> これは応答するのに時間がかかり、エラーが表示されます - _サーバーが見つからないかアクセスできませんでした。_

ウェブサイトはデフォルトの設定ファイルを使用しています。これをSQL Azureデータベースを使用するように編集する必要があります。

### アプリケーションの設定

デフォルトの設定ファイルを更新して、正しいデータベース接続文字列を使用する必要があります：



```
# デフォルトのデータベース接続詳細を表示します：
cat C:\docker4.net\SignUp.Web\connectionStrings.config
```


接続文字列の値 `"Server=SIGNUP-DB-DEV01;Database=SignUp;User Id=sa;Password=DockerCon!!!;Connect Timeout=10;"` を正しいサーバー名と認証情報に置き換える必要があります。

データベースの接続文字列を見つけて（ポータルが便利です）、設定ファイルを更新します：



```
# Windows Server Coreには完全なGUIはありませんが、ノートパッドはあります :)
notepad C:\docker4.net\SignUp.Web\connectionStrings.config
```


VMでcurlを使ってローカルでアプリを再度試してみてください - 新しいエラーが出ます :)



```
curl.exe -L http://localhost/signup
```


VMからアクセスできるようにSQLデータベースを設定する必要があります。

### データベースの設定

ポータルでSQLサーバー（データベースではなくサーバー）を開き、_ネットワーキング_ タブを選択します。

- VMが接続されている仮想ネットワークからアクセスを許可する仮想ネットワークルールを追加します
- UIからのメッセージに注意してください...

VMでcurlを使ってアプリを再度テストします - エラーがないHTMLレスポンスが表示されるはずです：



```
curl.exe -L http://localhost/signup
```


## ラボ

アプリがローカルで動作しているので、インターネットからアクセスできるように公開する必要があります。以下のように変更して、ローカルマシンからDNS名でアプリケーションにアクセスしてみてください：

- http://[vm-fqdn]/signup

![サインアップアプリ](/img/signup-homepage.png)

_サインアップ_ ボタンをクリックして詳細を追加してください。SQLデータベースでいくつかのクエリを実行して、データが保存されていることを確認してください。

> 詰まった場合は、[ヒント](hints_jp.md)を参照するか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

リソースグループを削除します：


```
az group delete -y -n $rg
```

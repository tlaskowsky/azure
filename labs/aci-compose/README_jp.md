# Azure Container Instances 上の分散アプリ

ACIはAzure上で最もシンプルなコンテナプラットフォームです。単一のコンテナを実行することも、分散アプリケーションをホストするために複数のコンテナをグループで実行することもできます。アプリケーションをモデリングするにはいくつかのオプションがあります - AzureのYAML仕様をAzure CLIで使用することも、Docker Compose仕様をDocker CLIで使用することもできます。

このラボでは両方のオプションを使用し、ACIが他のAzureサービスとどのように統合されているかを確認します。

## 参考資料

- [ACI コンテナ グループの概要](https://learn.microsoft.com/ja-jp/azure/container-instances/container-instances-container-groups)

- [ACI YAML 仕様](https://learn.microsoft.com/ja-jp/azure/container-instances/container-instances-reference-yaml)

- [Docker ACI 統合](https://docs.docker.com/cloud/aci-integration/)

- [ACI Compose 機能](https://docs.docker.com/cloud/aci-compose-features/)

- [`az container` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/container?view=azure-cli-latest)

## ACI YAML で分散アプリをデプロイする

まず、ラボ用のリソース グループを作成します:



```
az group create -n labs-aci-compose --tags courselabs=azure -l eastus
```


ACIのYAMLモデルはBicepやComposeに少し似ていますが、それらとは異なるカスタムフォーマットです:

- [rng-aci-v1.yaml](/labs/aci-compose/rng-aci-v1.yaml) - 以前使用したランダム番号APIとウェブサイトイメージのための2つのコンテナを含む _コンテナ グループ_ を定義します

このモデルにはいくつかの特定の詳細が含まれています:

- コンテナのサイズ（CPU & メモリ）は必須です。そのためACIは計算リソースをプロビジョニングできます。
- コンテナグループは同じネットワーク空間を共有するので、環境変数は`localhost`を介して通信を設定します。
- どの公開サービスも、IPアドレスレベルとコンテナレベルでポートを指定する必要があります。

Azure CLIを使用してACIリソースを作成するときに、このモデルを渡すと、モデル内のすべてのコンテナが実行されます。

📋 `labs/aci-compose/rng-aci-v1.yaml`のファイルと`container create`コマンドを使ってアプリをデプロイします。

<details>
  <summary>わからない場合は？</summary>

ヘルプテキストを確認してください:



```
az container create --help
```


`file`パラメータと名前を指定することができます:



```
az container create -g labs-aci-compose -n rng-app --file labs/aci-compose/rng-aci-v1.yaml
```


</details><br/>

ポータルでACIリソースを開くと、APIとウェブコンテナの両方が実行されているのが見えます。各コンテナのプロパティとログを確認でき、問題がある場合はコンテナ内にシェルセッションに接続することもできます。

YAML仕様にはDNS名が含まれていませんでしたが、アプリを試すために使用できる公開IPアドレスがあります。

> `http://<aci-ip-address>`にアクセスしてボタンをクリックすると、ランダムな番号が表示されるはずです。

APIコンテナからのログはあまり詳細を提供してくれませんが、モデルを変更してロギングレベルを上げることができます:

- [rng-aci-v2.yaml](/labs/aci-compose/rng-aci-v2.yaml) - より多くのログを確認できるよう、各コンテナに新しい環境変数を追加します

📋 `labs/aci-compose/rng-aci-v2.yaml`にある更新された仕様をデプロイします。これは単なる設定変更ですが、ACIは実際にはどのように更新を実装しますか？

<details>
  <summary>わからない場合は？</summary>

同じコンテナ作成コマンドをインスタンス名と更新された仕様で使用してください:


```
az container create -g labs-aci-compose -n rng-app --file labs/aci-compose/rng-aci-v2.yaml
```


しばらくすると`Running...`という出力が表示されます。コマンドはコンテナを再作成し、新しいコンテナがオンラインになるのを待ちます。

</details><br/>

ポータルでコンテナの_イベント_テーブルを確認すると、コンテナが_開始_されたエントリーと古いコンテナを_削除_するエントリーが複数見られます。

実行中のコンテナの計算環境のプロパティを変更することはできません。環境変数、リソース要求、またはポートを更新する必要がある場合は、古いコンテナを削除して代替品を作成するしかありません。

> これはDocker、ACI、Kubernetesを含むすべてのコンテナランタイムに当てはまります

ACIには独自のYAML仕様があるため、すべての機能にアクセスできます。ACI固有の設定が不要な場合は、アプリを標準のDocker Composeファイルでモデル化し、Docker CLIを使用してACIにデプロイすることもできます。

## ACIへのComposeアプリのデプロイ

アプリのComposeモデルはずっとシンプルです:

- [rng-compose-v1.yml](/labs/aci-compose/rng-compose-v1.yml) - 依然として同じコンテナイメージを使用していますが、Compose統合はACIの違いのいくつかを処理します

ACIへのデプロイは`docker compose`コマンドを使用しますが、まずローカルCLIがAzureと通信するようにDocker Contextを設定する必要があります(これは[ACIラボ](/labs/aci)でカバーしました):


```
docker login azure

docker context create aci labs-aci-compose --resource-group labs-aci-compose

docker context use labs-aci-compose
```


これで`docker`および`compose`コマンドを実行すると、ラボリソースグループのACIコンテキストで作業していることになります:


```
# これはazコマンドでデプロイしたコンテナを表示します:
docker ps
```


📋 `labs/aci-compose/rng-compose-v1.yml`ファイルからアプリケーションを起動するために`docker compose`コマンドを使用します。

<details>
  <summary>わからない場合は？</summary>

通常の`up`コマンドです - プロジェクト名を指定することができ、それがACI名になります:



```
docker compose -f labs/aci-compose/rng-compose-v1.yml --project-name rng-app-2 up -d 
```


</details><br/>

> グループの作成に関する出力が表示され、個々のコンテナが並行して作成されます。

ポータルで新しいコンテナを確認することができますし、Dockerコマンドを使用して詳細を印刷することもできます:



```
# ACIコンテナの出力にはIPアドレスが含まれます:
docker ps
```


新しいデプロイメントにアクセスして、アプリが動作していることを確認してください。ポータルでコンテナリストを開くと、モデルで定義されている2つのコンテナにもかかわらず3つがあるのを見ることができます。追加のコンテナが何をしているのか考えてみてください。

## ACIコンテナとストレージアカウント

ストレージアカウントを作成します:



```
az storage account create --sku Standard_ZRS -g labs-aci-compose  -l southeastasia -n <sa-name>
```


接続文字列を取得します:



```
az storage account show-connection-string -g labs-aci-compose --query connectionString -o tsv -n <sa-name>
```


Blob Storageをデータベースとして使用してローカルでコンテナを実行します:



```
# ローカルのDockerエンジンに切り替えます:

docker context use default

# 'quotes'に注意してください - キーで始まり値で終わります:
docker run --name local -d -p 8013:80 -e 'ConnectionStrings__AssetsDb=<connection-string>' courselabs/asset-manager:22.11
```


http://localhost:8013 にアクセスすると、画面にいくつかのデータが表示されます。ポータルでBlob Storageコンテナを開くと、ローカルコンテナからアップロードされた生データがあります。

コンテナはローカルストレージにもファイルを書き込みます - これは実際には何もしませんが、ファイル名としてコンテナ名を使用します:


```
# コンテナ内のフォルダの内容をリストします:
docker exec local ls /app/lockfiles
```


ACIコンテナは同じコードを使用してBlob Storageにアクセスできますし、Azure Files共有もACIにマウントすることができます。共有はコンテナファイルシステムの一部として表示されますが、アプリがそこにデータを書き込むと実際には共有に保存されます。

📋 Azureストレージアカウントに`assetmanager`という名前のファイル共有を作成し、ストレージアカウントキーを表示します。

<details>
  <summary>わからない場合は？</summary>

これは[Azure Filesラボ](/labs/storage-files/README_jp.md)で行いました:



```
# 共有を作成します:
az storage share create -n assetmanager --account-name <sa-name>

# キーを表示します:
az storage account keys list -g labs-aci-compose --query "[0].value" -o tsv --account-name <sa-name>
```

</details><br/>

**ファイルを編集** [assetmanager-aci.yaml](/labs/aci-compose/assetmanager-aci.yaml)：

- `<sa-name>` と `<sa-key>` を自分のストレージアカウント名とキーに置き換えてください
- `<connection-string>` を自分のストレージアカウントの接続文字列に置き換えてください

その後、Blobストレージとファイルを使用してAsset Managerコンテナを実行するために新しいACIグループをデプロイします：



```
az container create -g labs-aci-compose --file labs/aci-compose/assetmanager-aci.yaml
```


アプリが実行されているときにブラウズします。ストレージアカウントでロックファイルを見つけることができますか？

## ラボ

ACIは、クラウドでコンテナを簡単かつ迅速に実行するためのソリューションです。コンテナが失敗した場合には再起動されるため信頼性がありますが、水平方向にスケールする機能はありません。ACIでAsset Managerアプリの別のコピーを実行してください。それらは同じファイル共有に書き込みますか？どのようにしてそれらの間でトラフィックを負荷分散しますか？

> 行き詰まったら[ヒント](hints_jp.md)を試してみるか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このラボのリソースグループ(RG)を削除して、Docker CLIで作成したコンテ

```
az group delete -y --no-wait -n labs-aci-compose
```


次に、ローカルのDocker DesktopにDockerコンテキストを戻し、ラボコンテキストを削除します：


```
docker context use default

docker context rm labs-aci-compose
```

# Azure ストレージ アカウント

ストレージ アカウントは、データの管理されたストレージ サービスであり、公開することも、ユーザーや他のAzureサービスに制限することもできます。データは高可用性のために複数の場所に複製され、パフォーマンスの異なるレベルを選択できます。

このラボでは、ストレージ アカウントの基本を探り、小さなファイルと大きなファイルをアップロードします。

## 参照

- [ストレージ アカウント概要](https://docs.microsoft.com/ja-jp/azure/storage/common/storage-account-overview)

- [Azure でのデータ冗長性](https://docs.microsoft.com/ja-jp/azure/storage/common/storage-redundancy?toc=%2Fazure%2Fstorage%2Fblobs%2Ftoc.json)

- [`az storage` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/storage?view=azure-cli-latest)

- [`az storage account` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/storage/account?view=azure-cli-latest)

## ストレージ アカウント オプションを探る

ポータルで新しいリソースを作成し、ストレージ アカウントを検索します。以下のオプションがあります：

- 名前は全世界で一意である必要があり、[命名規則](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/management/resource-name-rules#microsoftstorage)は厳格です
- リージョンは重要で、データが物理的に保存される場所です
- パフォーマンスと冗長性のレベルを選択します

冗長性レベルはデータの複製方法を設定します：

- _ローカル冗長ストレージ (LRS)_ はデータが単一のデータセンター内で複製されることを意味します
- _ゾーン冗長ストレージ (ZRS)_ は単一リージョン内のデータセンター間で複製します
- _地理冗長ストレージ (GRS)_ はリージョン間で複製します

データはより広範囲に複製されるほど安全になりますが、それはより高いコストを意味します。

## ストレージ アカウントの作成

CLIを使用して新しいストレージ アカウントを作成します。リソース グループを開始してから、新しいアカウントのヘルプテキストを確認してください：



```
az group create -n labs-storage  -l southeastasia --tags courselabs=azure

az storage account create --help
```


📋 標準パフォーマンスでゾーン冗長ストレージ アカウントを作成します。

<details>
  <summary>わからない場合は？</summary>

SKUパラメータにはパフォーマンスと冗長性の設定が含まれています。例えば:

- `Premium_LRS` はプレミアムパフォーマンス（SSDバックアップストレージ）でローカル冗長性があります

- `Standard_GRS` は標準パフォーマンス（HDD）で地理冗長性があります



```
az storage account create -g labs-storage  -l southeastasia --sku Standard_ZRS -n <sa-name>
```


</details><br/>

ポータルで新しいリソースを開きます。一つのストレージアカウントは、複数の種類のストレージをサポートできます。Blobストレージ（バイナリラージオブジェクト）はシンプルなファイルストレージオプションで、_コンテナ_ にファイルを保存できます。これらはフォルダのようなものです。

📋 このフォルダのファイル [document.txt](/labs/storage/document.txt) を _drops_ というコンテナにblobとしてアップロードしてください。

<details>
  <summary>わからない場合は？</summary>

ストレージ アカウントブレードにはメインメニューで _Upload_ オプションがあります。それを選択するとローカルファイルを参照してアップロードできます。

そのメニューから新しいコンテナを作成し、コンテナ名を指定できます。

</details><br/>

> Blobストレージは階層的ではありません - 他のコンテナ内にコンテナを持つことはできませんが、blob名にはスラッシュ（例：`my/blob/file.txt`）を含めることができ、これによりネストされたストレージを近似することができます

## Blobのアップロードとダウンロード

ポータル内からナイスなUIでストレージを管理できます。左ナビから _ストレージ ブラウザ_ をクリックし、_Blob コンテナ_ を開きます。

`drops`を開くと`document.txt`が表示されます。クリックすると、URLを含む概要が表示されます。ファイルのURLは何ですか？公開アクセス可能ですか？

curlを使用してダウンロードしてください：



```
# ここではエラーは発生しません：
curl -o download2.txt https://<sa-name>.blob.core.windows.net/drops/document.txt
```


ファイルがダウンロードされたように見えます。しかし、内容を確認してください：



```
cat download2.txt
```


> XMLエラーメッセージです...新しいblobコンテナはデフォルトでプライベートアクセスになっています。

📋 コンテナのアクセスレベルを変更して、blobをダウンロードできるようにしてください。

<details>
  <summary>わからない場合は？</summary>

ポータルで _drops_ コンテナに移動し、_アクセスレベルを変更_ を選択します：

- blobアクセスはURLを知っている人なら誰でもファイルをダウンロードできます
- containerアクセスは誰でもコンテナの内容を一覧表示し、すべてのblobをダウンロードできます

</details><br/>

公開アクセスレベルを設定したら、ファイルをダウンロードできます：



```
curl -o download3.txt https://<sa-name>.blob.core.windows.net/drops/document.txt

cat download3.txt
```


今度は正しい内容があります。

## VMディスク用のストレージ

BlobストレージはVMディスク用にも使用でき、他のデータと一緒にディスクを管理したい場合に便利です。

ただし、Blobストレージはデフォルトでは使用されません。[管理ディスク](https://docs.microsoft.com/ja-jp/azure/virtual-machines/managed-disks-overview)はストレージアカウントに含まれません。

ストレージを制御したい場合は、VMを作成するときに非管理ディスクを指定できます。

📋 プレミアムストレージアカウントを作成し、そのアカウントにOSディスクを保存するVMを作成してください - [VMの種類](https://docs.microsoft.com/ja-jp/azure/virtual-machines/sizes-general)を確認して、プレミアムストレージをサポートするものを選んでください。

<details>
  <summary>わからない場合は？</summary>

ストレージ アカウントは異なるSKUで同じコマンドです：



```
az storage account create -g labs-storage  -l southeastasia --sku Premium_LRS -n <disk-sa-name>
```


ディスクをblobとして格納するためのコンテナも必要です：


```
az storage container create -n vm-disks --account-name <disk-sa-name>
```


VM作成コマンドでは、SAとコンテナを指定します：


```
az vm create -l southeastasia -g labs-storage -n vm04 --image UbuntuLTS --size Standard_D2as_v5  --use-unmanaged-disk --storage-container-name vm-disks --storage-account <disk-sa-name>
```


</details><br/>

新しいストレージアカウントにアクセスしてみてください - ディスクはどのように保存されていますか？

> VHD blobとしてストレージコンテナにあります。このようなディスクは、管理されたディスクのようにポータルで別のリソースとして表示されません。

## ラボ

ストレージアカウントには、Azure内のSQL Serverと同様のファイアウォールオプションがあります。それを使用して、元のSAが自分のIPアドレスからのみアクセスできるようにセキュリティを強化します。document.txtファイルをダウンロードできることを確認し、その後VMにログインしてファイルがダウンロードできないことを確認してください。

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除します：



```
az group delete -y -n labs-storage --no-wait
```

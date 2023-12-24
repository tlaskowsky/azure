# Blobストレージ

Blobストレージを自分専用のDropboxの代替として使用することも、アプリケーションの強力なストレージバックエンドとして使用することもできます。ユーザーがファイルをアップロードできるシナリオがある場合は、リレーショナルデータベースよりもBlobストレージに保存する方が良いでしょう。アプリ内の参照データを管理する安価な方法として、JSONファイルをBlobに保存することもできます。

このラボでは、アクセストークンやストレージ階層など、Blobのより高度な機能について説明します。

## 参照

- [SASトークン](https://learn.microsoft.com/ja-jp/azure/cognitive-services/translator/document-translation/create-sas-tokens?tabs=Containers)

- [Blobストレージ アクセス階層](https://docs.microsoft.com/ja-jp/azure/storage/blobs/access-tiers-overview)

- [`az storage container` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/storage/container?view=azure-cli-latest)

- [`az storage blob` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/storage/blob?view=azure-cli-latest)

## CLIを使ったBlobストレージの管理

[ストレージアカウントラボ](/labs/storage/README.md)でポータルを使用してBlobストレージを探索しました。今回はBlobに対するCLIツールを見ていきます。

📋 `labs-storage-blob`という名前のリソースグループと、標準的なローカル冗長ストレージを持つストレージアカウントを作成します。

<details>
  <summary>わからない場合は？</summary>

SA名は小文字と数字のみ使用できることを覚えておいてください：



```
az group create -n labs-storage-blob --tags courselabs=azure -l southeastasia 

az storage account create -g labs-storage-blob  -l southeastasia --sku Standard_LRS -n <sa-name>
```


</details><br/>

いくつかのファイルをアップロードできるBlobコンテナを作成します：



```
az storage container create -n labs  -g labs-storage-blob --account-name <sa-name>
```

The CLI has the `upload-batch` command which you can use to upload files to blob storage in bulk. It has a useful `dryrun` flag which tells you what it would do without actually doing it:

```
# labsフォルダ全体をアップロードした場合の出力を確認します：
az storage blob upload-batch -d labs -s ./labs --dryrun -o table --account-name <sa-name>
```


> 出力は、アップロードされる各BlobのターゲットURLとファイルタイプを教えてくれます。

バッチアップロードはファイルパスを保持しますか？

📋 同じコマンドを追加パラメータと共に使用して、マークダウンファイル（`*.md`）のみをアップロードしてください。最初にドライランを使用してから、実際のアップロードを行ってください。

<details>
  <summary>わからない場合は？</summary>

`pattern`パラメータを使ってアップロードするファイルをフィルタリングします：



```
az storage blob upload-batch -d labs -s ./labs --dryrun -o table --pattern '*.md' --account-name <sa-name>
```


そして`dry-run`フラグなしでアップロード：


```
az storage blob upload-batch -d labs -s ./labs --pattern '*.md' --account-name <sa-name>
```


</details><br/>

> 出力には各BlobのURL、生成されたeTag、最終更新日が表示されます - これらはHTTPキャッシングに使用されます

ディレクトリリストを確認し、`labs`コンテナ内の`storage-blob`ディレクトリにあるファイルを出力します：



```
az storage blob directory list -c labs -d 'storage-blob' -o table --account-name <sa-name>
```


> これは非推奨のコマンドです - CLIは進化しているので、新しいバージョンにアップグレードするときに削除されたコマンドがないか確認する必要があります

📋 `storage blob show`コマンドを使用して、`storage-labs`フォルダ内のreadmeファイルに関する情報を出力します。

<details>
  <summary>わからない場合は？</summary>

Blobファイル名は大文字と小文字が区別されます：



```
az storage blob show --container-name labs --name 'storage-blob/README.md' -o table --account-name <sa-name>
```


`storage-blob/readme.md`を試した場合、`ErrorCode:BlobNotFound`というレスポンスが返されます。

</details><br/>

出力にはファイルのメタデータのみが表示され、内容は表示されません。

## 共有アクセストークンとポリシー

すべてのBlobには公開URLがあり、内容をダウンロードしたい場合はそれを使用できます。URLは標準的なパターンです：`https://<sa-name>.blob.core.windows.net/<container>/<blob-name>`。

READMEドキュメントをダウンロードしてみてください：



```
curl -o download.md https://<sa-name>.blob.core.windows.net/labs/storage-blob/README.md

cat download.md
```


> 出力はXMLエラー文字列です - コンテナは公開Blob用に有効になっていません

公開せずに誰かにBlobへのアクセスを許可するには、_Shared Access Signature_ (SASトークン)を作成します。これはBlobへの読み取りアクセスを承認します。

ポータルで`storage-blob/README.md`Blobを開き、省略記号をクリックして_Generate SAS_を選択します。Blobへの読み取り専用アクセスを許可するSASキーを作成します。これは1時間有効です。

ポータルで表示されるBlob SAS URLをコピーします。次のようになります：

_https://labsstorageblobes.blob.core.windows.net/labs/storage-blob/README.md?sp=r&st=2022-10-26T20:17:20Z&se=2022-10-26T21:17:20Z&spr=https&sv=2021-06-08&sr=b&sig=3b1TVwRMsgNHC%2BKE0tkR1VcqD0897%2BfbBJKfppfJ3B8%3D_

SAS URLを使用してファイルをダウンロードするためにcurlを使用します：



```
curl -o download2.md '<blob-url-with-sas-token>'

cat download2.md
```


今度は内容が表示されます。

そのSASトークンを安心して共有できます - 有効期限が過ぎれば役に立たなくなり、Blobにはアクセスできなくなります。しかし、シンプルなSASトークンは取り消すことができず、有効期限まで使用できます。

Blobの共有をよりよく管理するためには、[格納アクセスポリシー](https://learn.microsoft.com/ja-jp/rest/api/storageservices/define-stored-access-policy)でSASトークンを管理できます：



```
# 読み取り専用ポリシーを作成します：

az storage container policy create -n labs-reader --container-name labs --permissions r --account-name <sa-name>
```


アクセスポリシーに基づいてBlob用のSASトークンを作成します。有効期限の形式は_YYYY-MM-DDTHH:MMZ_です。例：_2022-10-30T01:00Z_



```
az storage blob generate-sas --help

# 日付の形式が無効な場合はエラーが発生しますが、過去の日付の場合はエラーが発生しません：

az storage blob generate-sas -n 'storage-blob/README.md' --container-name labs --policy-name labs-reader --full-uri --expiry '2022-10-30T01:00Z' --account-name <sa-name> 
```


新しいSASトークンを使用してファイルをダウンロードできることを確認します：


```
curl -o download3.md "<blob-uri-with-sas-token>"

cat download3.md
```


正しい内容が表示されます - トークンは有効期限内であり、ポリシーは読み取りアクセスを許可します。

ポリシーを削除します：



```
az storage container policy delete -n labs-reader --container-name labs --account-name <sa-name>
```


**以前と同じURIとトークンを使用して** 再度ダウンロードを試みます：



```
curl -o download4.md "<blob-uri-with-sas-token>"

cat download4.md
```


> 今度はXML認証失敗メッセージが表示されます - ポリシーがなければSASトークンは無効です

格納アクセスポリシーを使用するとアクセスを取り消すことができるため、SASトークンが知られていても使用できません。

## ラボ

Blobストレージは通常、高性能を必要としないため、Azureにはパフォーマンスとストレージコストの最適なミックスを得るための_アクセス階層_があります。_Hot_アクセスは高速ですが高価です。_Cool_は安価ですが遅く、_Archive_は最も安価です。

このラボのreadmeをアーカイブ階層に変更し、ダウンロードを試みてください。アーカイブBlobにアクセスするためには何が必要ですか？

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除します：


```
az group delete -y -n labs-storage-blob --no-wait
```

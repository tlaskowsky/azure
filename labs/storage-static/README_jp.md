# Azure Storage Blobを使用した静的ウェブサイト

Azure Storageの別の用途は静的ウェブコンテンツのためです。HTMLファイルや静的アセットをBlobとしてアップロードし、Blobコンテナを公開ウェブアクセス用に設定できます。ウェブサーバーを管理することなく、高速でスケーラブルなウェブサイトを得られます。

このラボでは、Blobストレージ上にウェブサイトをホストし、MicrosoftのContent Delivery Network (CDN)でスケールする方法を見ていきます。

## 参照

- [ストレージアカウント内の静的ウェブサイト](https://learn.microsoft.com/ja-jp/azure/storage/blobs/storage-blob-static-website)

- [CDNとは？](https://learn.microsoft.com/ja-jp/azure/cdn/cdn-overview)

- [Microsoft CDN ポイント・オブ・プレゼンスの場所](https://learn.microsoft.com/ja-jp/azure/cdn/cdn-pop-locations?toc=%2Fazure%2Ffrontdoor%2FTOC.json)

- [Microsoft Learn: Babylon.jsプロジェクトを公開ウェブにデプロイする](https://docs.microsoft.com/ja-jp/learn/modules/create-voice-activated-webxr-app-with-babylonjs/9-exercise-deploy-babylonjs-project-to-public-web?pivots=vr) - より大きなモジュールの一部として、この演習は静的ウェブホスティングをカバーしています

## 静的ウェブサイトのデプロイ

リソースグループと通常のストレージアカウントを作成して始めます：



```
az group create -n labs-storage-static --tags courselabs=azure -l southeastasia

az storage account create -g labs-storage-static --sku Standard_LRS -n <sa-name>
```


📋 `blob service-properties update` コマンドを使用して、ストレージアカウントを静的ウェブサイト用に設定します。

<details>
  <summary>わからない場合は？</summary>

メインページ（インデックス）と404ページのファイル名を指定し、`static-website`フラグを使用する必要があります：



```
az storage blob service-properties update  --static-website --404-document 404.html --index-document index.html --account-name <sa-name>
```


</details><br/>

> 静的ウェブサイト設定を有効にする前にコンテンツをアップロードする必要はありません

ポータルでストレージアカウントを開き、_静的ウェブサイト_ ブレードを参照します。以下を確認できます：

- ベースURLは標準のblob URLと異なります
- `$web`という新しいコンテナがあり、ウェブコンテンツをアップロードする必要があります

URLをブラウズして、何が表示されるかを確認します。

> サイトは応答しますが、_Requested content does not exist_ という標準の404エラーが表示されます

📋 `labs/storage-static/web` ディレクトリのコンテンツをコンテナにアップロードして、もう一度ブラウズします。

<details>
  <summary>わからない場合は？</summary>

ポータルで複数のファイルをアップロードすることも、CLIでバッチアップロードを使用することもできます：


```
az storage blob upload-batch -d '$web' -s labs/storage-static/web --account-name <sa-name>
```


</details><br/>

ブラウザを更新すると、ホームページが表示されます。他のパス（例：/missing）に移動すると、カスタマイズされた404が表示されます。

blob自体は公開されていません - ポータルでindex.htmlファイルのblob URLを見つけ、それをダウンロードしてみてください：



```
curl -o download.html 'https://<sa-name>.blob.core.windows.net/$web/index.html'

cat download.html
```


> XMLエラーファイルが得られます。ページは静的ウェブサイトドメインを介してアクセスする必要があります

代わりに公開URLで試してみてください：



```
curl -o download2.html https://<static-web-domain>/index.html

cat download2.html
```


## 二番目のリージョンへのレプリケーション

静的ウェブサイトは、データを安全に保ち、サイトが常に利用可能であることを確認するために、より高いレベルの冗長性を使用する良いシナリオです。

ストレージアカウントを_read-only globally redundant storage_（RA-GRS）に変更します。これは、コンテンツが二番目のリージョンに複製され、読み取りに使用できることを意味します：



```
az storage account update -g labs-storage-static --sku Standard_RAGRS -n <sa-name>
```


出力で、セカンダリロケーションとセカンダリエンドポイントのリストを確認できます。これには静的ウェブサイトのものも含まれます。セカンダリからもサイトにアクセスできますが、RAのストレージコストは完全なGRSより低くなります：


```
curl -v https://<secondary-web-endpoint>/
```


> データがセカンダリリージョンに同期されるまで時間がかかるため、エラーが発生する可能性があります

ポータルでストレージアカウントを開き、_冗長性_ タブを確認すると、レプリケーションの状態が確認できます。

地理レプリケーションの同期には時間がかかる場合があります。最終同期時間をクエリして状態を確認できます：



```
az storage account show -g labs-storage-static --expand geoReplicationStats --query geoReplicationStats.lastSyncTime -o tsv -n <sa-name>
```


> アカウントがまだ同期中の場合、_Last sync time is unavailable_ というメッセージが表示されます

DNSプロバイダで両方のエンドポイントを設定することで、一方が利用不可になった場合にウェブサイトが他方のリージョンから提供されるようにすることができます。別の選択肢としてCDNを使用することができます。

## CDNによるグローバルレプリケーション

Azure CDNは、静的ウェブサイトのフロントエンドとして使用できる別のサービスです。それはグローバルネットワークであり、コンテンツが複数のエッジロケーションにコピーされるため、ユーザーがブラウズするときにローカルエリアからレスポンスを得られます。

ストレージアカウントの_Azure CDN_をポータルで開きます - ここから新しいCDNエンドポイントを作成するか、CLIを使用して作成できます：



```
az cdn profile create --sku Standard_Microsoft -g labs-storage-static -n labs-storage-static

az cdn endpoint create  -g labs-storage-static --profile-name labs-storage-static --origin <static-website-domain> --origin-host-header <static-website-domain> -n <cdn-domain>
```


ポータルでステータスを確認します。`https://<cdn-domain>.azureedge.net`にアクセスします。CDNが準備されるまでに時間がかかるため、エラーページが表示される可能性があります。この時点でデータはネットワーク全体に複製されています。

リフレッシュを続けます。サイトが表示されたら、CDNが充実しており、データがあなたに近い場所から提供されていることを意味します。

📋 `labs/storage-static/web2` ディレクトリのコンテンツを静的ウェブサイトコンテナにアップロードして、サイトの内容を変更します

<details>
  <summary>わからない場合は？</summary>



```
az storage blob upload-batch -d '$web' -s labs/storage-static/web2 --overwrite  --account-name <sa-name>
```


</details><br/>

さまざまなURLを確認してください：

- 元の静的ウェブサイトURLではすぐに内容が表示されるはずです
- セカンダリエンドポイントもほぼ同じ速さで更新される可能性があります - **元の同期が完了している場合**
- CDNの更新には最も時間がかかるため、数分間は古い内容を表示する可能性があります

## ラボ

CDNの目的は、頻繁に読み取られるが、それほど頻繁に更新されないコンテンツをキャッシュすることです。キャッシュはかなり積極的であり、時にはCDN内のページを強制的にリフレッシュする必要があります。`index.html` ページについて、どのようにしてそれを行いますか？

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボRGを削除します：



```
az group delete -y -n labs-storage-static --no-wait
```

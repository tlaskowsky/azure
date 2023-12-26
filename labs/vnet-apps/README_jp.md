# キーボールトと仮想ネットワークでアプリを保護する

Azureで理想的なアプリケーションは、すべての認証に管理されたアイデンティティを使用し、すべての通信に制限された仮想ネットワークを使用します。そうすることで、管理や保存が必要な資格情報は存在せず、アクセス可能であるべき場所を超えてサービスが公開されることもありません。しかし、AzureのすべてのサービスがVNet接続をサポートしているわけではなく、アプリのすべてのコンポーネントが統合認証を持っているわけでもありません。

このラボでは、Blob Storageを使用し、接続の詳細をVNetに制限されたKeyVaultに格納するアプリをデプロイします。

## 参照

- [ストレージ アカウント ファイアウォールと仮想ネットワーク](https://learn.microsoft.com/ja-jp/azure/storage/common/storage-network-security?tabs=azure-portal)

- [アプリ サービス アプリのVNet統合](https://learn.microsoft.com/ja-jp/azure/app-service/overview-vnet-integration)

## RG、VNet、サブネットの作成

まずはコアリソース - RGとVNetから始めましょう:



```
az group create -n labs-vnet-apps --tags courselabs=azure 

az network vnet create -g labs-vnet-apps -n vnet1 --address-prefix "10.30.0.0/16"

az network vnet subnet create -g labs-vnet-apps --vnet-name vnet1 -n subnet1 --address-prefix "10.30.1.0/24"
```


ここで新しいことは何もありません。私たちは実際にVNetに何かをデプロイするわけではありません - サービス間の安全な通信を確保するための橋渡しとしてそれを使用します。

## ストレージアカウントとKeyVaultの作成

アプリはBlob Storageを使用するので、アカウントを作成し、接続文字列を取得する必要があります。このアプリケーションはblobコンテナを作成するコードを持っているので、事前にそれを行う必要はありません。


```
# アカウントの作成:
az storage account create -g labs-vnet-apps --sku Standard_ZRS -n <sa-name>

# 接続文字列の出力:
az storage account show-connection-string -o tsv -g labs-vnet-apps --name <sa-name> 
```


このキーはストレージアカウント内のすべてに完全アクセス権を与えるので、それを安全に保管する必要があります。KeyVaultを作成し、接続文字列をシークレットとして保存します：


```
# ボールトの作成:
az keyvault create -g labs-vnet-apps -n <kv-name> 

# シークレットの保存:
az keyvault secret set --name 'ConnectionStrings--AssetsDb'  --vault-name <kv-name> --value "<connection-string>"
```


📋 あなたのマシンからシークレットを読むことができるか確認してください。

<details>
  <summary>どうすればいいかわからない場合は？</summary>



```
az keyvault secret show --name 'ConnectionStrings--AssetsDb'  --vault-name <kv-name>
```


</details>

このシークレットはAzureの外部でアクセス可能である必要はないので、それを制限すべきです。

## アクセス制限

KeyVaultとStorageへの通信にサブネットを使用するので、そのためにサービスエンドポイントを設定する必要があります：



```
az network vnet subnet update -g labs-vnet-apps --vnet-name vnet1 --name subnet1 --service-endpoints Microsoft.KeyVault Microsoft.Storage
```


次に、KeyVaultがvnetからのみアクセス可能になるように制限します：



```
az keyvault network-rule add --vnet-name vnet1 --subnet subnet1 -g labs-vnet-apps --name <kv-name>

az keyvault update --default-action 'Deny' -g labs-vnet-apps -n <kv-name>

az keyvault network-rule list -g labs-vnet-apps --name <kv-name>
```


CLIまたはポータルで再度シークレットを読むことができるか確認してください。ルールが有効になるまで数分かかるかもしれませんが、それ以降はVNetの外部からシークレットがブロックされるはずです。

## VNet、KeyVault、Blob Storageを使用したWebアプリの作成

私たちのアプリは.NET 6のウェブサイトなので、PaaSに適しています。_App ServicesはVNets内で動作しません_ - それらは公開を意図しています。それでも、それらを安全に保つためにはいくつかの追加設定が必要です。

アプリをWebアプリとしてデプロイして始めましょう：



```
cd src/asset-manager

az webapp up -g labs-vnet-apps --plan app-plan-02 --os-type Linux --runtime dotnetcore:6.0 --sku B1 -n <app-name>
```


次に、いくつかのアプリ設定を行います。これらはアプリにBlob Storageをデータ用に使用し、接続文字列をKey Vaultから取得するように指示します：


```
az webapp config appsettings set -g labs-vnet-apps --settings Database__Api=BlobStorage KeyVault__Enabled=true KeyVault__Name=<kv-name> -n <app-name>
```


アプリにアクセスしてみると、エラーページが表示されます。

📋 アプリのログを開き、エラーを見つけてみてください。

<details>
  <summary>どうすればいいかわからない場合は？</summary>

ポータルでウェブアプリの_Advanced tools_を開き、Kuduセッションを起動します。_Log stream_リンクを開いて、しばらくお待ちください...

アプリは失敗するたびに再起動されます。最終的には役立つエラーログのようなものが表示されます：

_{"error":{"code":"Forbidden","message":"The user, group or application 'appid=19ee0b80-40d0-4a42-b4ca-b8697c84c6a8;oid=4a09a335-0716-406d-a12f-9cafadae0325;iss=https://sts.windows.net/68c58dc9-c7db-440f-8c32-ac672250d642/' does not have secrets list permission on key vault 'labsvnetappses;location=southeastasia'. For help resolving this issue, please see https://go.microsoft.com/fwlink/?linkid=2125287","innererror":{"code":"AccessDenied"}}}_

</details>

問題はKeyVaultへの接続エラーです - アプリはKeyVaultが信頼するアイデンティティを使用していません。

App Serviceはマネージドアイデンティティを使用できるので、接続文字列や他の資格情報なしでKeyVaultで認証できます。ウェブアプリにマネージドアイデンティティを設定し、そのアイデンティティにKeyVaultへのアクセス権を付与します：


```
# アイデンティティの割り当て - 出力にはアイデンティティのIDが含まれます:
az webapp identity assign -g labs-vnet-apps  -n <app-name>

# アイデンティティにシークレットを読む権限を付与:
az keyvault set-policy --secret-permissions get list --object-id "<principalId>" -n <kv-name>
```


アプリを再度試してみてください... それでも失敗します。

📋 アプリのログを開き、新しいエラーを見つけてみてください。

<details>
  <summary>どうすればいいかわからない場合は？</summary>

同じプロセスで、同じ長い待ち時間ですが、新しいエラーが表示されます：

_{"error":{"code":"Forbidden","message":"Client address is not authorized and caller is not a trusted service.\r\nClient address: 20.126.176.160\r\nCaller: appid=19ee0b80-40d0-4a42-b4ca-b8697c84c6a8;oid=4a09a335-0716-406d-a12f-9cafadae0325;iss=https://sts.windows.net/68c58dc9-c7db-440f-8c32-ac672250d642/;xmsmirid=/subscriptions/161aa8d6-1b59-4fff-946c-e1172b68d76c/resourcegroups/labs-vnet-apps/providers/Microsoft.Web/sites/app-name;xmsazrid=/subscriptions/161aa8d6-1b59-4fff-946c-e1172b68d76c/resourcegroups/labs-vnet-apps/providers/Microsoft.Web/sites/app-name\r\nVault: labsvnetappses;location=southeastasia","innererror":{"code":"ForbiddenByFirewall"}}}_

</details>

今ではApp Serviceが承認されたアイデンティティを使用していますが、呼び出しが信頼されていない場所から来ています。なぜなら、KeyVaultはサブネットに制限されているからです。

ここでの一つの選択肢は、WebアプリのアウトバウンドIPアドレスを取得し、それらをKeyVaultファイアウォールに追加することです。しかし、IPアドレスは変わることがあるので、WebアプリにVNet統合を追加する方が良いでしょう。そうすることで、App ServiceがAzure内で内部呼び出しをするとき、それはKey Vaultアクセスを持つサブネット経由になります：


```
az webapp vnet-integration add --vnet vnet1 --subnet subnet1 -g labs-vnet-apps  -n <app-name>

# アプリの確認:
az webapp show -g labs-vnet-apps -n <app-name> 
```


変更が反映されたら、アプリはKey Vaultに接続でき、そこからStorage Accountの接続文字列を読み取り、Blobコンテナからデータをダウンロードできます。

## ラボ

しかし、ストレージアカウントはまだインターネットに公開されています。ストレージアカウントはVNet内にデプロイされません（Webアプリと同様に、公開接続を意図しています）が、制限は可能です。サブネットを使用するサービスのみがアクセスできるようにストレージアカウントを修正してください。

> 詰まった？[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このコマンドでRGを削除し、すべてのリソースを


```
az group delete -y -n labs-vnet-apps
```

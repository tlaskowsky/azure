# AKS アプリの Key Vault および Virtual Networks を使用したセキュリティ対策

AKS クラスターは、Azure ネットワークプロバイダーを使用して VNets 内で実行できます。クラスター内のすべての Pod はサブネット範囲から IP アドレスを取得します。ファイアウォールルールをサポートする任意の Azure リソースは、サブネットからのトラフィックのみを許可するように設定できるため、AKS の Pod のみがそのサービスを使用できます。

このラボでは、AKS にアプリをデプロイし、Blob Storage を使用し、接続の詳細を KeyVault に保存し、クラスターのサブネットへのアクセスを制限します。

## 参照

- [AKS での管理された ID](https://learn.microsoft.com/ja-jp/azure/aks/use-managed-identity)

- [ストレージアカウントのファイアウォールと Virtual Networks](https://learn.microsoft.com/ja-jp/azure/storage/common/storage-network-security?tabs=azure-portal)

- [KeyVault のファイアウォールと Virtual Networks](https://learn.microsoft.com/ja-jp/azure/key-vault/general/network-security)

## RG、VNet および Subnet の作成

RG および VNet の基本リソースから始めましょう：



```
az group create -n labs-aks-apps --tags courselabs=azure 

az network vnet create -g labs-aks-apps -n appnet --address-prefix "10.30.0.0/16" -l eastus

az network vnet subnet create -g labs-aks-apps --vnet-name appnet -n aks --address-prefix "10.30.1.0/24"
```


ここでは新しいことはありません。AKS は管理されたサービスですが、VNet にデプロイできるため、Pod はサブネットの IP アドレスを使用します。つまり、Pod からのアクセスのみを許可して、他のすべてのサービスをセキュリティで保護することができます。

## AKS クラスターの作成

AKS 用のサブネット ID を取得する必要があります：



```
az network vnet subnet show -g labs-aks-apps --vnet-name appnet -n aks --query id -o tsv
```


次に、Azure ネットワーキングを使用して、KeyVault アドオンが有効なクラスターを作成します（[AKS KeyVault ラボ](/labs/aks-keyvault/README.md)で説明しました）：


```
az aks create -g labs-aks-apps -n aks06 --node-count 2 --enable-addons azure-keyvault-secrets-provider --enable-managed-identity --network-plugin azure --vnet-subnet-id '<subnet-id>' -l eastus
```


> これにより、VNet に対する AD ロールの伝播が行われ、しばらく時間がかかります。

新しいターミナルを開いて、残りのインフラストラクチャの作成を続けます。

## ストレージアカウントと KeyVault の作成

アプリは Blob Storage を使用するため、アカウントを作成し、接続文字列を取得する必要があります。このアプリケーションは起動時に blob コンテナを作成できますが、事前にそれを行うことが良い習慣です。


```
# ストレージアカウントの作成：
az storage account create -g labs-aks-apps --sku Standard_ZRS -l eastus -n <sa-name>

# そしてコンテナ：
az storage container create -n assetsdb -g labs-aks-apps --account-name <sa-name>

# 接続文字列の表示：
az storage account show-connection-string -o tsv -g labs-aks-apps --name <sa-name> 
```


**ファイル [asset-manager-connectionstrings.json](/labs/aks-apps/secrets/asset-manager-connectionstrings.json) を編集し**、`<sa-connection-string>` を自分の接続文字列に置き換えてください。

そのキーはストレージアカウント内のすべてへの完全なアクセスを提供するため、安全に保管する必要があります。KeyVault を作成し、接続文字列ファイルをシークレットとしてアップロードしましょう：


```
# ボールトの作成：
az keyvault create -g labs-aks-apps -l eastus -n <kv-name> 

# シークレットの保存：
az keyvault secret set --name asset-manager-connectionstrings  --file labs/aks-apps/secrets/asset-manager-connectionstrings.json --vault-name <kv-name>
```


📋 自分のマシンからシークレットを読むことができるか確認してください。

<details>
  <summary>方法がわからない場合は？</summary>



```
az keyvault secret show --name asset-manager-connectionstrings  --vault-name <kv-name>
```


</details>

このシークレットは AKS Pod で実行されているアプリによって読み取られますが、Azure の外部でアクセス可能である必要はありませんので、ロックダウンするべきです。

## KeyVault アクセスの制限

KeyVault およびストレージへの通信に AKS サブネットを使用するため、そのためのサービスエンドポイントを設定する必要があります：


```
az network vnet subnet update -g labs-aks-apps --vnet-name appnet --name aks --service-endpoints Microsoft.KeyVault Microsoft.Storage
```


次に、KeyVault を AKS サブネットからのみアクセス可能に制限します：


```
az keyvault network-rule add --vnet-name appnet --subnet aks -g labs-aks-apps --name <kv-name>

az keyvault update --default-action 'Deny' -g labs-aks-apps -n <kv-name>

az keyvault network-rule list -g labs-aks-apps --name <kv-name>
```


そして、AKS 管理 ID にシークレットの読み取り権限を付与します：



```
# ID を表示：
az aks show -g labs-aks-apps -n aks06 --query addonProfiles.azureKeyvaultSecretsProvider.identity.clientId -o tsv

# アクセスを許可するポリシーの追加：
az keyvault set-policy --secret-permissions get --spn '<identity-id>' -n <kv-name>
```


CLI またはポータルで再びシークレットを読むことができるか確認してください。ルールが有効になるまで数分かかる場合がありますが、今後は AKS 管理 ID で認証されたリクエストのみが許可され、AKS サブネットからのリクエストのみがブロックされるはずです。

## AKS へのアプリのデプロイ

AKS クラスターとその他のインフラストラクチャを作成し、接続したので、アプリをデプロイすることができます。

Kubernetes のモデルは比較的単純です：

- [service.yaml](/labs/aks-apps/specs/asset-manager/service.yaml) - アプリにパブリック IP アドレスでアクセスするための LoadBalancer サービスを定義します

- [deployment.yaml](/labs/aks-apps/specs/asset-manager/deployment.yaml) - KeyVault シークレットをボリュームマウントにロードする Pod スペックを持つデプロイメント

- [secretProviderClass.yaml](/labs/aks-apps/specs/asset-manager/secretProviderClass.yaml) - KeyVault シークレットをマウント可能にする SecretProviderClass

すべての詳細は正しいですが、シークレットプロバイダーのプレースホルダーを除きます。

**ファイル [secretProviderClass.yaml](/labs/aks-apps/specs/asset-manager/secretProviderClass.yaml) を編集し**、KeyVault 名、AKS ID、およびテナントの詳細を自分のものに入力してください。

次に、AKS に接続してアプリをデプロイできます：



```
az aks get-credentials -g labs-aks-apps -n aks06 --overwrite-existing

kubectl apply -f labs/aks-apps/specs/asset-manager
```


Pod が実行中になるまで待ちます：



```
kubectl get po --watch
```


アプリの外部 IP アドレスを取得します：



```
kubectl get svc asset-manager-lb
```


アプリをブラウズします - それは KeyVault から接続文字列をロードし、Blob Storage に接続し、データを挿入し、そのページに表示されるはずです。

## ラボ

しかし、ストレージアカウントはまだインターネットに公開されています。ストレージアカウントは VNet 内にデプロイすることはできませんが、制限をかけることは可能です。AKS で実行されている Pod のみがアクセスできるようにストレージアカウントを修正してください。

> 困ったときは [ヒント](hints.md) を試すか、[解決策](solution.md) を確認してください。

___

## クリーンアップ

このコマンドで RG を削除し、すべてのリソースを削除できます：


```
az group delete -y --no-wait -n labs-aks-apps
```

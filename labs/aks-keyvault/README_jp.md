# AKS と KeyVault シークレットストレージ

Kubernetes にはプラグイン可能なストレージアーキテクチャ、コンテナストレージインターフェイス（CSI）があります。異なるタイプのストレージをKubernetesクラスターに接続し、Podコンテナ内のボリュームとして提供することができます。

AKSのアドオンを使用すると、KeyVaultをストレージプロバイダーとして有効にすることができます。KeyVaultに機密設定ファイルを保存し、Kubernetesの他の場所では利用できないコンテナフォルダにマウントすることができます。

## 参考

- [シークレットストアCSIドライバー](https://secrets-store-csi-driver.sigs.k8s.io)

- [AKSクラスターでAzure Key VaultプロバイダーをシークレットストアCSIドライバーで使用する](https://docs.microsoft.com/ja-jp/azure/aks/csi-secrets-store-driver)

## AKSクラスターの作成

ラボ用の新しいリソースグループから始めます：



```
az group create -n labs-aks-keyvault --tags courselabs=azure -l eastus
```


KeyVaultのアクセスは制限されているため、AKSのノードが他のAzureサービスに接続する際に使用するセキュリティプリンシパルが必要です。

📋 マネージドIDを使用し、KeyVaultアドオンを有効にして、2つのノードで新しいAKSクラスターを作成します。

<details>
  <summary>方法がわからない場合</summary>



```
az aks create -g labs-aks-keyvault -n aks05 --node-count 2 --enable-addons azure-keyvault-secrets-provider --enable-managed-identity -l eastus
```


</details><br/>

クラスターが作成されたら、ポータルで開きます。KeyVault統合がUIに表示されているか確認しますか？

CSIドライバーなどの追加クラスターコンポーネントは通常、Kubernetes内のPodとして実行されますが、Kubernetesの_namespace_内で隔離されます。これは、Azureのリソースグループに似たワークロードの分離方法です。

新しいクラスターに接続し、Kubernetesシステムの名前空間内のPodを表示します：



```
az aks get-credentials -g labs-aks-keyvault -n aks05 --overwrite-existing

kubectl get pods --namespace kube-system -l app=secrets-store-csi-driver
```


`aks-secrets-store-csi-driver-`で始まる2つのPodが表示されるはずです。AKSとKeyVaultの接続に問題がある場合は、これらのPodのログを確認できます。

## KeyVaultを作成し、AKSに権限を付与

KeyVaultでは特別なことは必要ありません。デフォルトのオプションで問題ありません：



```
az keyvault create -g labs-aks-keyvault -n <kv-name>
```


次に、AKSが使用しているマネージドIDのIDを取得し、そのIDがKeyVaultを使用できるようにする必要があります（[KeyVaultアクセスラボ](/labs/keyvault-access/README.md)で説明しました）：


```
# IDを表示：
az aks show -g labs-aks-keyvault -n aks05 --query addonProfiles.azureKeyvaultSecretsProvider.identity.clientId -o tsv

# アクセス許可を付与するポリシーを追加：
az keyvault set-policy --secret-permissions get --spn '<identity-id>' -n <kv-name>
```


> 特定のAKSクラスターを1つのKeyVaultに直接リンクするわけではありません。

Kubernetesモデルでは、マウントするKeyVaultシークレットの詳細を指定します。アプリをデプロイすると、AKSはシークレットにアクセスしようとし、IDがKeyVaultにアクセスできる限り、シークレットは読み込まれてコンテナファイルシステムに注入されます。

## KeyVaultシークレットの作成とモデル化

KeyVaultの詳細は、特別なタイプのリソースである_SecretProviderClass_でモデル化されます。これはKubernetesのコアリソースではなく、KeyVaultアドオンをインストールするとクラスターに追加されます：

- [secretProviderClasses/keyVault.yaml](/labs/aks-keyvault/specs/secretProviderClasses/keyVault.yaml) - KeyVault名とAzure [テナントID](https://learn.microsoft.com/ja-jp/azure/active-directory/fundamentals/active-directory-how-to-find-tenant)を含む

これはシークレットに関して細かいアプローチが必要です。ボリュームマウントで利用可能にするKeyVaultオブジェクトを明示的に設定する必要があります：

- `objectName` はKeyVault内のシークレット（または証明書）の名前です。
- `objectType` はKeyVaultアイテムのタイプです。
- `objectAlias` はオブジェクトがボリュームマウントで表面化する際に使用するファイル名です。

> この例では、`configurable-secrets`という名前のKeyVaultシークレットが想定されており、そのシークレットの内容が`secret.json`というファイルで利用可能になります。

ローカルJSONファイル[configurable-secret.json](/labs/aks-keyvault/secrets/configurable-secret.json)をアップロードして、そのシークレットを作成しましょう：


```
az keyvault secret set --name configurable-secrets --file labs/aks-keyvault/secrets/configurable-secret.json --vault-name <kv-name>
```


ポータルでKeyVaultを開き、シークレットがそこにあるか確認します。シークレットは単一の値である必要はなく、最大25Kbのマルチライン文字列にすることができます。

📋 **[keyVault.yaml](/labs/aks-keyvault/specs/secretProviderClasses/keyVault.yaml)ファイルをあなたの詳細情報で更新**し、AKSクラスターにシークレットプロバイダークラスをデプロイします。

<details>
  <summary>方法がわからない場合</summary>

AzureテナントIDを取得：



```
az account list -o table
```


そしてAKSアイデンティティID：



```
az aks show -g labs-aks-keyvault -n aks05 --query addonProfiles.azureKeyvaultSecretsProvider.identity.clientId -o tsv
```


YAMLファイルの`<tenant-id>`、`<identity-id>`、`<kv-name>`の値を置き換え、デプロイします：


```
kubectl apply -f labs/aks-keyvault/specs/secretProviderClasses/keyVault.yaml
```


</details><br />

SCPがクラスター内に存在することを確認できますが、詳細からは多くのことはわかりません。そこにあり、KeyVaultシークレットからボリュームをマウントする準備ができているとき。

## KeyVaultボリュームを使用してアプリをデプロイ

これは良い古い設定可能なアプリに戻ります。この仕様を使用して：

- [configurable/deployment.yaml](/labs/aks-keyvault/specs/configurable/deployment.yaml) - シークレットストアドライバーを持つCSIボリュームを使用し、KeyVault SCPを指定して、`/app/secrets`にコンテナにマウントします。

アプリを実行します。これにより、デプロイメントとロードバランサーサービスが作成されます：



```
kubectl apply -f labs/aks-keyvault/specs/configurable
```


Podが正しく開始され、実行状態になることを確認します：


```
kubectl get po -l app=configurable  --watch
```


次に、KeyVaultシークレットのJSONが予想通りの場所にコンテナファイルシステムにロードされていることを確認します：


```
kubectl exec deploy/configurable -- cat /app/secrets/secret.json
```


ロードバランサーIPでアプリにアクセスします。ページにシークレットの値が表示されるはずです。

## ラボ

KubernetesのConfigMapsとSecretsは更新でき、それらをマウントするPodはボリュームの内容が更新されます。データはキャッシュされているため数分かかることがあり、変更された設定ファイルがアプリケーションによって取り込まれる保証はありません（一部のアプリは起動時に一度だけ設定ファイルを読み込みます）。

CSIシークレットストアについてはどうでしょうか？KeyVaultシークレットの内容を更新すると、設定可能なアプリに反映されますか？

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このラボのRGを削除すると、ストレージを含むすべてのリソースが削除されます。



```
az group delete -y --no-wait -n labs-aks-keyvault
```


次に、KubernetesのコンテキストをローカルのDocker Desktopに戻します：


```
kubectl config use-context docker-desktop
```

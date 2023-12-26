# キーボールトアクセスのセキュリティ保護

キーボールトには機密データが満載なので、それらへのアクセスをセキュリティ保護する必要があります。アクセスを制限するためにAzure ADを使用でき、これによりユーザーがポータルや `az` コマンドで行える操作を制限できます。また、必要なコンポーネントのみがデータを読むことができるように、キーボールトを内部的にもセキュリティ保護する必要があります。

このラボでは、仮想ネットワークとAzureのアイデンティティにキーボールトアクセスを制限する方法を見ていきます。

## 参考資料

- [キーボールトのベストプラクティス](https://learn.microsoft.com/ja-jp/azure/key-vault/general/best-practices)

- [管理されたアイデンティティ](https://docs.microsoft.com/ja-jp/azure/active-directory/managed-identities-azure-resources/overview)

- [`az vm identity assign` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/vm/identity?view=azure-cli-latest#az-vm-identity-assign)

- [`az keyvault network-rule` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/keyvault/network-rule?view=azure-cli-latest)

## RG、KeyVault、Secretの作成

新しいリソースグループでKeyVaultを作成して始めます：



```
az group create -n labs-keyvault-access --tags courselabs=azure -l eastus

az keyvault create -l eastus -g labs-keyvault-access -n <kv-name>
```


キーボールトにシークレットを作成し、自分のマシンから再び読み取れることを確認します：



```
az keyvault secret set --name secret01 --value azure-labs --vault-name <kv-name>

az keyvault secret show --name secret01 -o tsv --query "value" --vault-name <kv-name>
```


アカウントを作成したので、すべての権限があります。ポータルでキーボールトを開くと、_アクセスポリシー_ タブでどのように権限が定義されているかを確認できます。

📋 私のアカウント `siddheshpg@azureauthority.in` を使用して、キーボールトのシークレットをリストして読むためのアクセスを私に与えてもらえますか？

<details>
  <summary>方法がわからない場合</summary>

新しいアクセスポリシーを追加し、必要な権限を選択します。次に、権限を付与するための _プリンシパル_ を選択する必要があります。

私のメールアドレスを入力すると、結果が見つかりません。使用できるプリンシパルのリストは、ご自身のAzure Active Directory (AD) アカウントに限られており、私のアイデンティティは異なるADアカウントにあります。

私にアクセスを与える場合は、Azure Active Directoryに私を外部IDとして追加する必要があります。

ですが、本当にアクセスは必要ありません :)

</details><br/>

Azureはセキュリティを適用する際に _プリンシパル_ について話します。これは一般的な用語で、以下を指すことがあります：

- Microsoftアカウントを持つユーザー
- ユーザーグループ
- Azureリソースによって使用されるシステムアイデンティティ
- Azureによって管理されるリソースの管理アイデンティティ

これらすべてを考慮する必要があります。なぜなら、シークレットへの不正なアクセスは望ましくないからです。

プリンシパルが認証を行う前に、キーボールトへのネットワークアクセスも制限することができます。

## VNetへのアクセス制限

仮想ネットワークを作成し、そのネットワーク内でVMを実行します。キーボールトはVMからのみ使用できるように設定します。

VNetとサブネットから始めます：



```
az network vnet create -g labs-keyvault-access -n vnet1 --address-prefix "10.10.0.0/16"

az network vnet subnet create -g labs-keyvault-access --vnet-name vnet1 -n subnet1 --address-prefix "10.10.1.0/24"
```


📋 サブネット内で実行されている任意のサービスからKeyVaultへのアクセスを許可するために `keyvault network-rule add` コマンドを使用してください。

<details>
  <summary>方法がわからない場合</summary>

ヘルプを確認します：



```
az keyvault network-rule add --help
```


サブネットを追加してみます：



```
# これはエラーを表示します：
az keyvault network-rule add -g labs-keyvault-access --vnet-name vnet1 --subnet subnet1 --name <kv-name>
```


他のサービスがサブネットへのトラフィックをルーティングすることを許可されていない限り、_サービスエンドポイント_ で明示的に許可する必要があります。これにより、キーボールトリソースがサブネットに入ることが許可されます：


```
az network vnet subnet update -g labs-keyvault-access --vnet-name vnet1 -n subnet1 --service-endpoints 'Microsoft.KeyVault'
```


これでネットワークルールを追加できます：


```
az keyvault network-rule add -g labs-keyvault-access --vnet-name vnet1 --subnet subnet1 -n <kv-name>
```


</details><br/>

サブネットへのアクセスが必要なAzureリソースは、[サービスエンドポイント](https://learn.microsoft.com/ja-jp/azure/virtual-network/virtual-network-service-endpoints-overview)の設定が必要ですが、これはサービスタイプごとに一度だけ行う必要があります。

> ポータルでVNetを開きます。_サブネット_ タブでサブネットを選択すると、_サービスエンドポイント_ にキーボールトがリストされているのがわかります。

ローカルマシンからもう一度シークレットの値を印刷しようとします。あなたのマシンはVNet内にない - アクセスエラーが発生しますか？


```
# まだシークレットを印刷できます：
az keyvault secret show --name secret01 -o tsv --query "value" --vault-name <kv-name>
```


> ポータルでキーボールトを開き、_ネットワーキング_ に移動します。_すべてのネットワークからの公開アクセスを許可する_ が選択されているのがわかります - ネットワークルールを追加してもこれは変わりません。

アクセスを拒否するためのネットワークルールがない場合は、アクセスが拒否されるようにキーボールトを更新します：



```
az keyvault update --default-action Deny -g labs-keyvault-access -n <kv-name>
```


今、シークレットを印刷しようとします：



```
# これは失敗します：
az keyvault secret show --name secret01 -o tsv --query "value" --vault-name <kv-name>
```


キーボールトは今ロックダウンされており、サブネット内のリソースのみがそれを使用できます。

## キーボールトにアクセスできるVMの作成

シークレットにまだアクセスできることを証明するために、サブネット内にVMを作成します。これは、キーボールトを使用するために必要なPythonとライブラリをインストールする [scripts/setup.sh](/labs/keyvault-access/scripts/setup.sh) と、マシン上で実行するテストアクセスキーボールトのPythonスクリプト [scripts/read-secret.py](/labs/keyvault-access/scripts/read-secret.py) を使用するシンプルなUbuntu Server VMです。

セットアップスクリプトを使ってVMを作成します：



```
az vm create -g labs-keyvault-access -n vm01 --image UbuntuLTS --vnet-name vnet1 --subnet subnet1 --custom-data @labs/keyvault-access/scripts/setup.sh
```


VMが準備できたら、接続してPythonスクリプトを試してください：



```
ssh <vm-public-ip>

# Pythonスクリプトをダウンロードします：
curl -o read-secret.py https://raw.githubusercontent.com/azureauthority/azure/main/labs/keyvault-access/scripts/read-secret.py

# これは失敗します：
python3 read-secret.py
```


認証エラーが表示されます。VMはキーボールトにアクセスできるサブネット内にありますが、**しかし**シークレットを利用するためには認証済みのAzureプリンシパルを使用する必要があります。そのために、管理されたアイデンティティを使用します。

新しいターミナルで、VMにシステム生成の管理アイデンティティを追加します：



```
# このコマンドの出力には管理アイデンティティIDが含まれています：
az vm identity assign -n vm01 -g labs-keyvault-access

# アイデンティティにシークレットを読む権限を与えます：
az keyvault set-policy --secret-permissions get --object-id <systemAssignedIdentity>  -n <kv-name>
```


VMのシェルセッションでPythonスクリプトを再度実行します：



```
python3 read-secret.py
```

> 秘密の値が表示されます。VMはマネージドアイデンティティで認証されるため、KeyVaultへのアクセスに資格情報を供給する必要はありません。

マネージドアイデンティティは、Azureサービス内の他のAzureサービスに対する認証にのみ使用されます。ポータルでKeyVaultのアクセスポリシーを確認すると、その設定がどのようになっているかがわかります。

## ラボ

Key Vaultにはデフォルトでソフトデリートポリシーがあります - 誤ってシークレットを削除した場合でも、それを復元することができます。`secret01` シークレットでそれを試してみてください。削除してから再作成し、VMのPythonスクリプトが新しいシークレットの値を印刷するようにするには何をする必要がありますか？

> 詰まったら [ヒント](hints_jp.md) を試すか、[解決策](solution_jp.md) を確認してください。

___

## クリーンアップ

このラボのリソースグループを削除して、すべてのリソースを削除します：



```
az group delete -y --no-wait -n labs-keyvault-access
```

# Azure Resource Manager JSONテンプレート

Azure CLIは、デプロイを探索し自動化するための素晴らしいツールですが、それは命令的なアプローチです。スクリプトで使用する場合は、既に存在するリソースを作成しようとしていないことを確認するために多くのチェックを追加する必要があります。代替手段は宣言的アプローチです - 望む最終結果を記述し、ツールがリソースを作成または更新する必要があるかどうかを判断させます。

このラボでは、デプロイメントにAzure Resource Manager（ARM）テンプレートを使用します。これらは、リソースの望ましい状態のJSONモデルで、ソースコントロールに存在することができます。

## 参照

- [Azure Resource Manager 概要](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/management/overview)

- [ARM クイックスタートテンプレートギャラリー](https://azure.microsoft.com/ja-jp/resources/templates/) および [GitHub リポジトリ](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts)

- [テンプレートスキーマ参照](https://docs.microsoft.com/ja-jp/azure/templates/)

- [ARM テンプレートの構造と構文](https://learn.microsoft.com/ja-jp/azure/azure-resource-manager/templates/syntax)

## ARM テンプレート JSON

ARMテンプレートは強力ですが、扱いにくいものです。ここにシンプルなテンプレートがあります：

- [storage-account/azuredeploy.json](./storage-account/azuredeploy.json)

このテンプレートはストレージアカウントを作成します。JSONには複数のブロックがあります：

- パラメータ、各デプロイメントで変更できる値
- 変数、テンプレートの残りの部分で使用する値を設定
- リソース、変数とパラメータを使用して作成する実際のリソースを宣言

このスニペットは作成するストレージアカウントを定義しています：



```
    "resources": [
      {
        "type": "Microsoft.Storage/storageAccounts",
        "apiVersion": "2019-06-01",
        "name": "[parameters('storageAccountName')]",
        "location": "[parameters('location')]",
        "sku": {
          "name": "[variables('storageSku')]"
        },
        "kind": "StorageV2",
        "properties": {
          "supportsHttpsTrafficOnly": true
        }
      }
    ]
```


- `type` と `apiVersion` は定義しているリソースを表します ([ARM内のストレージアカウント](https://docs.microsoft.com/ja-jp/azure/templates/microsoft.storage/storageaccounts?tabs=json)を参照)
- ストレージアカウントの名前と場所はパラメータから読み取られます
- SKUは変数から読み取られます
- 他のプロパティはリソースブロックで設定されます

このテンプレートを共有すると、デプロイされるたびにHTTPSアクセスのみを設定したStandard LRS SKUを使用するv2ストレージアカウントが作成されることが保証されます。

## 望ましい状態のデプロイメント

ARMテンプレートはCLIを使用してデプロイできます。テンプレートは常に既存のリソースグループにデプロイされます。

まず、リソースグループを作成します：


```
az group create -n labs-arm --tags courselabs=azure --location southeastasia
```

📋 `labs/arm/storage-account/azuredeploy.json`のARMテンプレートをデプロイするために`deployment group create`コマンドを使用します。

<details>
  <summary>わからない場合</summary>



```
# ヘルプテキストを表示：
az deployment group create --help
```


追加の設定なしでテンプレートをデプロイすることができ、CLIはパラメータ値を入力するように促します：


```
az deployment group create --name storage-account -g labs-arm  --template-file labs/arm/storage-account/azuredeploy.json
```


または、デプロイメントコマンドにパラメータ値を指定することもできます：



```
az deployment group create --name storage-account -g labs-arm  --template-file labs/arm/storage-account/azuredeploy.json  --parameters storageAccountName=<unique-name>
```


</details><br/>

すべてのパラメータに値が必要ですが、ロケーションパラメータにはテンプレートにデフォルトが設定されているため、ストレージアカウント名のみを設定する必要があります。

> デプロイが実行されている間、ポータルでリソースグループを確認してください。

CLIでもデプロイメントを確認できます。これは基本的な詳細を表示します：



```
az deployment group list -g labs-arm -o table
```


ARMデプロイメントは繰り返し可能です - 何度実行しても、現在の状態にかかわらず、同じ結果が得られるはずです。

`what-if`フラグを使用すると、実際に変更を加えることなく、デプロイメントの結果が何になるかを教えてくれます - 同じパラメータ値を使用していることを確認してください


```
az deployment group create --name storage-account -g labs-arm  --template-file labs/arm/storage-account/azuredeploy.json --what-if --parameters storageAccountName=<storage-account-name>
```


> ストレージアカウントが「変更なし」としてリストされます

ARMデプロイメントは、デプロイメントが手動で変更され、テンプレートにその更新が反映されない場合に_drift_（ズレ）を識別して修正するのに便利です。

📋 `storage account upate`を使用してストレージアカウントを変更し、SKUを`Standard_GRS`に設定します。その後、もう一度what-ifデプロイメントを実行します。

<details>
  <summary>わからない場合</summary>

ヘルプテキストを表示：



```
az storage account update --help
```


SKUを変更：



```
az storage account update -g labs-arm --sku Standard_GRS -n <storage-account-name>
```


同じパラメータ値でwhat-ifコマンドを実行：


```
az deployment group create --name storage-account -g labs-arm  --template-file labs/arm/storage-account/azuredeploy.json --what-if --parameters storageAccountName=<storage-account-name>
```

</details><br/>

> これで出力は、デプロイメントがSKUを`Standard_LRS`に戻すと表示されます。

これは実際に稼働しているデプロイメントを監査し、手動で変更されていないか確認するのに非常に役立ちます。

## ARMテンプレートの動的な値

ARMテンプレートは繰り返し可能であることを意図していますが、常にそうとは限りません。いくつかのAzure設定は動的であり、各デプロイメントで変更される可能性があります。ARMテンプレートにそのような設定が含まれている場合、それは冪等ではありません。

GitHubのAzureクイックスタートリポジトリからこのテンプレートを確認してください：

- [vm-simple-linux/azuredeploy.json](/labs/arm/vm-simple-linux/azuredeploy.json) - VMと関連するすべてのリソース（VNet、サブネット、PIP、NSG、NIC）を定義します。

📋 このテンプレートが繰り返し可能でない設定を見つけることができますか？

<details>
  <summary>わからない場合</summary>

NICリソース内でIP構成設定を見ると：



```
"properties": {
        "ipConfigurations": [
          {
            "name": "ipconfig1",
            "properties": {
              "subnet": {
                "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', parameters('virtualNetworkName'), parameters('subnetName'))]"
              },
              "privateIPAllocationMethod": "Dynamic"
```


サブネット内のプライベートIPアドレスの割り当て方法が_Dynamic_に設定されています。つまり、毎回異なるアドレスが使用される可能性があります。

</details><br/>

動的な値はテンプレートをアップグレードに使うのが難しくします。

📋 リソースグループにVMテンプレートをデプロイしてください - フォルダ内のパラメータファイルを使用してLinuxユーザー名を提供しますが、パスワードとDNS名はデプロイメントコマンドで設定します：



```
az deployment group create --name vm-simple-linux -g labs-arm  --template-file labs/arm/vm-simple-linux/azuredeploy.json  --parameters @labs/arm/vm-simple-linux/azuredeploy.parameters.json adminPasswordOrKey='<strong-password>' dnsLabelPrefix=<unique-dns-name>
```


デプロイメントが完了したら、`--what-if`フラグで再度コマンドを実行します - 変更はありませんか？

> デプロイメントはIPアドレスを変更したいと表示されます。なぜなら、リソースには実際のIPアドレスがあるため、テンプレート仕様と一致しません。


## ラボ

JSONテンプレートでVMの仕様を変更し、静的IPアドレス `10.1.0.102` を使用するようにします。新しいリソースグループを作成し、新しいテンプレートをデプロイして、デプロイメントが繰り返し可能であることを確認してください。

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

CLIでARMデプロイメントを削除できます：



```
az deployment group delete -g labs-arm -n storage-account
```


** ただし、これはデプロイメントのメタデータのみを削除し、実際のリソースは削除しません **

実際にクリーンアップするには、リソースグループを削除する必要があります：



```
az group delete -n labs-arm 

az group delete -n labs-arm-lab
```

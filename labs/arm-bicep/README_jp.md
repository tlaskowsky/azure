# ARMテンプレートとBicep

ARMテンプレートの背後にある概念は重要です - インフラストラクチャをコードとして、パラメータ化されたデプロイメント、希望の状態へのデリバリー。しかし、特に複数のリソースを含む大規模なアプリケーションでは、JSON形式での作業は難しいです。

ARMの進化として、Bicepという新しいツールがあり、これはリソースをよりシンプルで管理しやすいテンプレートで定義するためのカスタム言語を使用します。

## 参照資料

- [Bicep概要](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/bicep/overview?tabs=bicep)
- [ARMクイックスタートテンプレートギャラリー](https://azure.microsoft.com/ja-jp/resources/templates/) および [GitHubリポジトリ](https://github.com/Azure/azure-quickstart-templates/tree/master/quickstarts)
- [テンプレートスキーマ参照](https://docs.microsoft.com/ja-jp/azure/templates/)
- [Bicep Playground](https://aka.ms/bicepdemo)

## Bicepの構文とデプロイメント

[Storage Account](https://docs.microsoft.com/ja-jp/azure/templates/microsoft.storage/storageaccounts?tabs=bicep)を定義するためのBicepのスニペットは以下の通りです：



```
resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageSku
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: httpsOnly
  }
}
```


リソースタイプは使用しているスキーマのバージョンを指定しますが、残りのテンプレート [storage-account/main.bicep](/labs/arm-bicep/storage-account/main.bicep) はJSONの代わりにはるかに読みやすいです。

📋 リソースグループ `labs-arm-bicep` を作成し、`labs/arm-bicep/storage-account/main.bicep` のBicepファイルを `deployment group create` コマンドでデプロイしてください。

<details>
  <summary>わからない場合</summary>



```
az group create -n labs-arm-bicep  --tags courselabs=azure --location southeastasia
```


このコマンドはCLIからパラメータ値を要求します：



```
az deployment group create -g labs-arm-bicep --template-file labs/arm-bicep/storage-account/main.bicep
```


またはコマンドで値を提供します：



```
az deployment group create -g labs-arm-bicep --template-file labs/arm-bicep/storage-account/main.bicep --parameters storageAccountName=<unique-name>
```


</details><br/>

> デプロイメントコマンドの出力は、JSONとBicepの仕様について同じです。

## Bicepツールの使用

BicepはARMを使用する推奨の方法ですが、JSONテンプレートよりも新しいため、多くのプロジェクトではまだJSONが使用されています。Bicepツールを使用すると、JSON仕様からBicepファイルを生成し、その逆もできます。

CLIから直接Bicepツールをインストールできます：



```
az bicep install

# bicepコマンドには独自のヘルプテキストがあります：
az bicep --help
```


> それが機能しない場合は、[Bicepの別のインストールオプション](https://docs.microsoft.com/ja-jp/azure/azure-resource-manager/bicep/install#deployment-environment)を試してください。

このARM仕様は、[ARMラボ](/labs/arm/README.md)で使用された同じLinux VM用ですが、すべてのリソースがJSON形式で定義されています：

- [vm-simple-linux/azuredeploy.json](/labs/arm-bicep/vm-simple-linux/azuredeploy.json)

📋 Bicep CLIを使用して、JSONからBicepファイルを生成してください。

<details>
  <summary>わからない場合</summary>

`decompile` コマンドはARMからBicepを生成します：



```
az bicep decompile --help 

az bicep decompile -f labs/arm-bicep/vm-simple-linux/azuredeploy.json
```

</details><br/>

> すべてのリソースの作成に関する出力が表示され、生成されたテンプレートについての警告があるかもしれません。

📋 Bicepファイル内の警告を修正し、NICを静的IP `10.1.0.103` に更新してから、テンプレートをデプロイしてください。

<details>
  <summary>わからない場合</summary>

更新されたファイルの例：

- [vm-simple-linux/azuredeploy-updated.bicep](/labs/arm-bicep/vm-simple-linux/azuredeploy-updated.bicep)

これによりARMラボで使用された同じ_privateIP_の値が設定されます。



```
az deployment group create -g labs-arm-bicep --template-file labs/arm-bicep/vm-simple-linux/azuredeploy-updated.bicep --parameters adminUsername=linuxuser adminPasswordOrKey=<strong-password>
```


</details><br/>

Bicepファイルはナビゲートや編集がはるかに簡単であることが分かるでしょう。

## インフラストラクチャ仕様の進化

Bicepテンプレートは通常、リソースグループ内のすべてのリソースを記述することを意図しています。

ARMのデフォルトのデプロイメントモードは_incremental_で、テンプレート内の新しいリソースは追加され、一致するものはそのまま残され、リソースグループ内のテンプレートに記述されていない余分なものもそのままにされます。

- [vm-and-sql-db/main.bicep](/labs/arm-bicep/vm-and-sql-db/main.bicep) - 既存のLinux VMテンプレートにSQLサーバーとデータベースの仕様を追加します

生成されたBicepからリソース識別子が整理されていますが、仕様は同じです。

what-ifデプロイメントを実行すると、3つの新しいリソースが追加されることと、既存のリソースに変更がないことが確認できます：



```
az deployment group create -g labs-arm-bicep --template-file labs/arm-bicep/vm-and-sql-db/main.bicep  --parameters adminUsername=linuxuser adminPasswordOrKey=<strong-password> sqlAdminPassword=<strong-password> --what-if
```


## ラボ

Bicepテンプレートには非常に明確な入力と出力があります。一般的なメンテナンスタスクは、テンプレート内の固定設定をパラメータに移動し、デプロイメントをより柔軟にすることです。

`vm-and-sql-db/main.bicep` のBicepテンプレートを更新して、2つの設定を構成可能にします：

- SQLデータベースSKUは`Basic`または`Standard`のいずれかでなければなりません
- デプロイするサンプルデータベーススキーマの名前は、`AdventureWorksLT`または`WideWorldImportersStd`のいずれかでなければなりません

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

デプロイメントではなく、リソースグループを削除する必要があります：



```
az group delete -y --no-wait -n labs-arm-bicep

az group delete -y --no-wait -n labs-arm-bicep-lab
```

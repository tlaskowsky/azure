# ラボ解決策

サンプルのBicepファイルはこちらです：

- [lab/main.bicep](/labs/arm-bicep/lab/main.bicep)

それは2つの新しいパラメータを追加し、それぞれに許可される値のリストを持っています：


```
@allowed([
  'Basic'
  'Standard'
])
param databaseSku string

@allowed([
  'AdventureWorksLT'
  'WideWorldImportersStd'
])
param databaseSampleSchema string
```


そして、SQLデータベースリソースを更新して、パラメータからSkuを読み込み、サンプルスキーマ名を持つ新しいpropertiesブロックを追加します：



```
  sku: {
    name: databaseSku
    tier: databaseSku
  }
  properties: {
    sampleName: databaseSampleSchema
  }
```


新しいリソースグループにテンプレートをデプロイできます：



```
az group create -n labs-arm-bicep-lab  --tags courselabs=azure --location southeastasia

# これにより、SKUとサンプルスキーマを選択するように求められます：
az deployment group create -g labs-arm-bicep-lab --template-file labs/arm-bicep/lab/main.bicep  --parameters adminUsername=linuxuser adminPasswordOrKey=<strong-password> sqlAdminPassword=<strong-password> 
```


ポータルでSQLデータベースにアクセスし、クエリエディタを開くと、選択したサンプルデータベースのスキーマとデータがデプロイされているのが確認できます。

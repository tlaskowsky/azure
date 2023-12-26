# Azure Key Vault（Azure キー ボールト）

Key Vaultは特化したストレージサービスで、少量の機密データを保存するために使います。ユーザーの資格情報、APIキー、証明書、その他のアプリケーション構成（プレーンテキストで表示されるべきでないもの）に使用します。

Key Vaultのデータはレスト時に暗号化され、誰が値を読むことができるかの権限を設定でき、Key Vault全体へのアクセスをブロックして、データを読む必要がある時だけ利用可能にすることができます。

## 参照

- [Key Vault ドキュメント](https://docs.microsoft.com/ja-jp/azure/key-vault/)

- [`az keyvault` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/keyvault?view=azure-cli-latest)

- [`az keyvault secret` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/keyvault/secret?view=azure-cli-latest)

## ポータルでKey Vaultを探索する

ポータルを開き、新しいKey Vaultリソースを作成するために検索します。主なオプションを見てみましょう:

- プレミアム価格帯はハードウェア暗号化を提供します
- リカバリーオプションは削除されたデータを自動的に保持するセーフガードです
- アクセスポリシーでは、低レベルの読み取りと書き込みの権限を設定できます

実際に新しいKey Vaultを作成するためにCLIを使用します。

## CLIでKey Vaultを作成する

新しいリソースグループを使用し、好みのリージョンで始めてください:



```
az group create -n labs-keyvault --tags courselabs=azure -l eastus
```


📋 `keyvault create` コマンドで新しいKey Vaultを作成します。

<details>
  <summary>方法がわからない場合</summary>

ヘルプから始めてください:


```
az keyvault create --help
```


リソースグループ、リージョン、グローバルに一意の名前を指定する必要があります:


```
az keyvault create -l eastus -g labs-keyvault -n <kv-name>
```


</details><br/>

> Key Vaultの作成には1分か2分かかります。実行中に、ドキュメントを確認してください:

- Key Vaultにどのタイプのデータを保存できますか？

## ポータルでシークレットを管理する

ポータルで新しいKey Vaultに移動します。

`sql-password`というキーでシークレットを作成して、資格情報を保存することができます:

- ワークフローは理解できますか？
- シークレットを作成したとき、どのようにしてそれを再度表示しますか？
- シークレットを更新する必要がある場合はどうしますか？

> シークレットはバージョン管理されています。現在のバージョンを表示できますし、値を更新すると新しいバージョンが作成されて現在のバージョンになります。古いバージョンは依然として利用可能です。

## CLIでシークレットを管理する

シークレットには、Key Vaultの名前、シークレットの名前、バージョンが含まれる一意の識別子があります。ポータルで表示されます - 最新バージョンのシークレットの識別子をクリップボードにコピーしてください（このように見えます `https://sc-kv01-2003.vault.azure.net/secrets/sql-password/9989912ad43d4588971d9db2184990a6`）。

IDだけを使ってシークレットデータを表示できます:



```
az keyvault secret show --id <secret-id>
```


レスポンスにはすべてのシークレットフィールドが含まれます。自動化のためにシークレット値だけを取得したい場合があります。

📋 `secret show` コマンドに追加して、プレーンテキストで値だけを表示します。

<details>
  <summary>方法がわからない場合</summary>

他の`az`コマンドと同様に、出力とクエリパラメータを追加できます:



```
az keyvault secret show -o tsv --query "value" --id <secret-id>
```


</details><br/>

IDがわからない場合、シークレット名を使用して最新バージョンを取得できます:



```
az keyvault secret show --name sql-password  --vault-name <kv-name>
```


📋 他の`secret`コマンドを使用して値を更新し、すべてのバージョンを出力します。

<details>
  <summary>方法がわからない場合</summary>

使用可能なコマンドを確認してください:



```
az keyvault secret --help
```


`secret set` を使用してシークレットを作成または更新します:



```
az keyvault secret set --name sql-password --value pw124123v4 --vault-name <kv-name>
```


すべてのバージョンをリストアップできます:



```
az keyvault secret list-versions --name sql-password --vault-name <kv-name>
```
</details><br/>

> シークレットバージョンのリストには値が表示されず、どれが現在のバージョンかは表示されません。

## ラボ

シークレットはKeyVaultで保存できるデータの一種です。暗号化キーとTLS証明書も生成して保存できます。CLIを使用して、主体共通名（CN）が`azure.azureauthority.in`で、有効期間が6ヶ月の自己署名証明書を作成してください。新しい証明書の公開キーと秘密キーをダウンロードします。

> 詰まった場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このラボのリソースグループを削除して、すべてのリソースを削除します:


```
az group delete -y --no-wait -n labs-keyvault
```

# ラボの解決策

サンプル解決策では、スクリプトURIのパスを変更し、出力行を追加しています：

- [lab/vm.bicep](/labs/iaas-bicep/lab/vm.bicep)

これは、ドメイン名をハードコーディングしないために[environment](https://learn.microsoft.com/en-gb/azure/azure-resource-manager/bicep/bicep-functions-deployment#environment)関数を使用し、文字列補間を使って完全なURIを構築しています：



```
'https://courselabspublic.blob.${environment().suffixes.storage}/iaasbicep/vm-setup.ps1'
```


また、PIPからFQDNを使用し、完全なURLを構築するために文字列補間を使用する[output](https://learn.microsoft.com/en-gb/azure/azure-resource-manager/bicep/outputs?tabs=azure-powershell#define-output-values)を追加しています：



```
output url string = 'http://${publicIPAddress.properties.dnsSettings.fqdn}/signup'
```


もう一度デプロイメントコマンドを実行すると、アプリケーションを確認するためにブラウズできる新しいURLを含む出力が表示されます：


```
az deployment group create -g labs-iaas-bicep --name vm2 --template-file labs/iaas-bicep/lab/vm.bicep --mode incremental --parameters adminPassword=<vm-password> sqlPassword=<sql-password>
```


run-commandを含むVMのデプロイメントを繰り返しても、コマンドは再実行されません。ログファイルを印刷すると、最初に実行したときのタイムスタンプが同じであることが確認できます。

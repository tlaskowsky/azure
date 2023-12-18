# ラボ解決策

こちらが私のサンプル解決策です：
- [lab/azuredeploy.json](/labs/arm/lab/azuredeploy.json)

これはIPアドレスの割り当て方法を静的に変更し、特定のIPアドレスとIPアドレスバージョンを設定します。

デプロイメント用の新しいRGを作成します：



```
az group create -n labs-arm-lab --tags courselabs=azure  --location southeastasia
```


テンプレートをデプロイします：



```
az deployment group create --name lab -g labs-arm-lab  --template-file labs/arm/lab/azuredeploy.json  --parameters @labs/arm/vm-simple-linux/azuredeploy.parameters.json adminPasswordOrKey='<strong-password>' dnsLabelPrefix=<unique-dns-name>
```


デプロイメントが完了したら、what-ifで確認します：



```
az deployment group create --name vm-simple-linux -g labs-arm-lab  --template-file labs/arm/lab/azuredeploy.json  --parameters @labs/arm/vm-simple-linux/azuredeploy.parameters.json adminPasswordOrKey='<strong-password>' dnsLabelPrefix=<unique-dns-name> --what-if
```

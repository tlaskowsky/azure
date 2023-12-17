# ラボ解決策

VM の詳細を表示します：



```
az vm show -g labs-vm -n vm01
```


そこでは `osDisk` のセクションがあり、ID と名前が含まれています。これは `storageProfile` オブジェクト内にネストされています。出力をフィルタリングして、ディスク名を変数に格納できます：



```
# PowerShell:
$diskName=$(az vm show -g labs-vm -n vm01 --query "storageProfile.osDisk.name" -o tsv)

# sh:
diskName=$(az vm show -g labs-vm -n vm01 --query "storageProfile.osDisk.name" -o tsv)
```


これで [az disk]() コマンドを使用してディスクのすべての詳細を表示できます：



```
az disk show --help

az disk show -g labs-vm -n $diskName
```


`diskIopsReadWrite` フィールドに IOPS が表示されます - 異なるディスクタイプには異なるパフォーマンスレベルがあります。

次に VM を削除します：



```
az vm delete -g labs-vm -n vm01 --yes
```


ポータルのリソースグループをチェックします - VM 削除後もディスクとすべてのネットワークリソースが保持されていることがわかります。

> [演習](README_jp.md)に戻ります。
